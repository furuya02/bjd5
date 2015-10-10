using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Bjd;
using Bjd.log;
using Bjd.option;

namespace ProxyHttpServer {
    //*****************************************************************
    //���ۃL���b�V���N���X
    //�������y�уf�B�X�N�L���b�V����B�����āA�P�̃L���b�V���Ƃ��ĕ\������
    //*****************************************************************
    public class Cache : ThreadBase {
        readonly Logger logger;
        //readonly OneOption _oneOption;
        readonly Conf _conf;

        readonly MemoryCache _memoryCache;//�������L���b�V��
        readonly DiskCache _diskCache;//�f�B�X�N�L���b�V��

        readonly int _memorySize;//�������L���b�V���̃T�C�Y
        readonly int _diskSize;//�f�B�X�N�L���b�V���̃T�C�Y

        readonly CacheTarget _cacheTargetHost;//�Ώۃz�X�g
        readonly CacheTarget _cacheTargetExt;//�Ώۊg���q

        readonly bool _useCache;//�I�v�V�����u�L���b�V����g�p����v
        readonly int _expires;//�f�t�H���g�̗L������(h)
        readonly int _maxSize;//�L���b�V���ɕۑ�����ő�t�@�C���T�C�Y

        System.Threading.Timer _timer;
        bool _cacheRefresh;//�L���b�V�����|

        public Cache(Kernel kernel, Logger logger, Conf conf)
            : base(logger) {
            this.logger = logger;
            //_oneOption = oneOption;
            _conf = conf;
            _useCache = (bool)conf.Get("useCache");

            if (!_useCache)
                return;

            _expires = (int)conf.Get("expires");
            _maxSize = (int)conf.Get("maxSize");
            _diskSize = (int)conf.Get("diskSize");
            _memorySize = (int)conf.Get("memorySize");


            //�L���b�V���Ώۃ��X�g
            _cacheTargetHost = new CacheTarget((Dat)conf.Get("cacheHost"), (int)conf.Get("enableHost"));
            _cacheTargetExt = new CacheTarget((Dat)conf.Get("cacheExt"), (int)conf.Get("enableExt"));

            //�f�B�X�N�L���b�V��
            var cacheDir = (string)conf.Get("cacheDir");//�L���b�V����ۑ�����f�B���N�g��
            if (cacheDir == "" || !Directory.Exists(cacheDir)) {
                logger.Set(LogKind.Error, null, 15, string.Format("dir = {0}", cacheDir));
                _diskSize = 0;
            }
            if (_diskSize != 0) {
                _diskCache = new DiskCache(cacheDir, logger);
            }

            if (_memorySize != 0)//�������L���b�V��
                _memoryCache = new MemoryCache(logger);

        }

        void SetTimer(long hour) {
            long msec = hour * 1000 * 60 * 60;
            _timer = new System.Threading.Timer(TimerTick, null, msec, 1000);
        }

        new public void Dispose() {
            if (_timer != null)
                _timer.Dispose();
            Stop();

            // �������L���b�V���̓f�B�X�N�ɑޔ����
            if (_memoryCache != null && _diskCache != null) {
                while (true) {
                    var oneCache = _memoryCache.Old();
                    if (oneCache == null)
                        break;
                    _diskCache.Add(oneCache);
                    _memoryCache.Remove(oneCache.HostName, oneCache.Port, oneCache.Uri);
                }
            }

            base.Dispose();
        }
        override protected bool OnStartThread() {
            if (!_useCache)
                return false;//�L���b�V����g�p���Ȃ�
            //�f�B�X�N�L���b�V���̒���I����
            //Ver5.8.4
            //if (_diskSize == 0)
            //    return false;//�f�B�X�N�L���b�V���Ȃ�
            return true;
        }
        override protected void OnStopThread() { }
        override protected void OnRunThread() {

            //[C#]
            ThreadBaseKind = ThreadBaseKind.Running;


            var hour = (int)_conf.Get("testTime");
            SetTimer(hour);//�^�C�}�[�ݒ�

            long lastSize = 0;
            while (IsLife()) {
                if (_cacheRefresh) {
                    logger.Set(LogKind.Normal, null, 23, string.Format("Interval={0}h", hour));
                    _cacheRefresh = false;
                    var infoList = new List<CacheInfo>();

                    try {
                        long size = _diskCache.GetInfo(ref infoList, 1, this);
                        if (size != lastSize) {
                            infoList.Sort((x, y) => x != null ? x.LastAccess.CompareTo(y.LastAccess) : 0);
                            for (int i = 0; IsLife() && _diskSize * 1024 < size; i++) {
                                size -= infoList[i].Size;
                                if (!Remove(CacheKind.Disk, infoList[i].HostName, infoList[i].Port, infoList[i].Uri))
                                    break;
                            }
                            lastSize = size;
                        }
                    } catch (Exception ex) {
                        logger.Set(LogKind.Error, null, 27, ex.Message);
                    }
                    SetTimer(hour);//�^�C�}�[�ݒ�
                    logger.Set(LogKind.Normal, null, 24, string.Format("Interval={0}h", hour));
                } else {
                    Thread.Sleep(300);
                }
            }
            _timer.Dispose();
        }

        public override string GetMsg(int no){
            throw new NotImplementedException();
        }


        void TimerTick(object state) {
            _cacheRefresh = true;
            _timer.Dispose();
        }


        //���N�G�X�g���L���b�V���̃^�[�Q�b�g���ǂ����𔻒f����
        public bool IsTarget(string hostName, string uri, string ext) {
            //�I�v�V�����u�L���b�V����g�p����v
            if (!_useCache)
                return false;

            // �ΏہE�ΏۊO�̃z�X�g���������
            if (!_cacheTargetHost.IsHit(hostName)) {
                logger.Set(LogKind.Detail, null, 12, uri);
                return false;
            }
            // �ΏہE�ΏۊO�̊g���q���������
            if (!_cacheTargetExt.IsMatch(ext)) {
                logger.Set(LogKind.Detail, null, 13, uri);
                return false;
            }
            return true;
        }

        // �L���b�V���ǉ�
        public bool Add(OneCache oneCache) {
            if (!_useCache)
                return false;

            if (oneCache == null)
                return false;

            //�T�C�Y��0�̂�̂́A�L���b�V���ΏۊO�Ƃ���
            if (oneCache.Length <= 0) {
                return false;
            }

            if (oneCache.Length > _maxSize * 1000) {//�ő�T�C�Y�𒴂����f�[�^�̓L���b�V���̑ΏۊO�ƂȂ�
                logger.Set(LogKind.Detail, null, 20, string.Format("{0}:{1}{2}", oneCache.HostName, oneCache.Port, oneCache.Uri));
                return false;
            }
            lock (this) { // �r������
                //�������L���b�V���ւ̕ۑ�
                if (_memoryCache != null) {
                    //�������L���b�V���Ɏ��܂邩�ǂ����̔��f
                    while (_memoryCache.Length + oneCache.Length > _memorySize * 1024) {
                        OneCache old = _memoryCache.Old();//��ԌÂ���̂�擾����
                        if (old == null)
                            return false;
                        //��ԌÂ���̂�������L���b�V������폜����
                        _memoryCache.Remove(old.HostName, old.Port, old.Uri);
                        //��ԌÂ���̂�f�B�X�N�L���b�V���ɕۑ�����
                        if (_diskCache != null)
                            _diskCache.Add(old);
                    }
                    if (_memoryCache.Add(oneCache)) {
                        logger.Set(LogKind.Detail, null, 4, string.Format("{0}:{1}{2}", oneCache.HostName, oneCache.Port, oneCache.Uri));
                        return true;
                    }
                }
                //�f�B�X�N�L���b�V���ւ̕ۑ�
                if (_diskCache != null) {
                    if (_diskCache.Add(oneCache)) {
                        logger.Set(LogKind.Detail, null, 5, string.Format("{0}:{1}{2}", oneCache.HostName, oneCache.Port, oneCache.Uri));
                        return true;
                    }
                }
            } // �r������
            logger.Set(LogKind.Detail, null, 18, string.Format("{0}:{1}{2}", oneCache.HostName, oneCache.Port, oneCache.Uri));
            return false;
        }

        public bool Remove(string hostName, int port, string uri) {
            bool action = false;//�폜�������ǂ���
            // �r������
            lock (this) {
                if (_memoryCache != null) {
                    if (_memoryCache.Remove(hostName, port, uri))
                        action = true;
                }
                if (_diskCache != null) {
                    if (_diskCache.Remove(hostName, port, uri)) {
                        action = true;
                    }
                }
            }
            return action;
        }


        // �L���b�V���폜(�ꗗ�\������̂݌Ăяo�����)
        public bool Remove(CacheKind cacheKind, string hostName, int port, string uri) {
            // �r������
            lock (this) {
                if (cacheKind == CacheKind.Memory && _memoryCache != null)
                    return _memoryCache.Remove(hostName, port, uri);
                if (cacheKind == CacheKind.Disk && _diskCache != null)
                    return _diskCache.Remove(hostName, port, uri);
            }
            return false;
        }
        // �L���b�V���擾
        public OneCache Get(Request request, DateTime modified) {
            // �r������
            lock (this) {
                // �������L���b�V����ɑ��݂��邩�ǂ����H
                if (_memoryCache != null) {
                    OneCache oneCache = _memoryCache.Get(request.HostName, request.Port, request.Uri);
                    if (oneCache != null) {
                        //�������L���b�V���Ńq�b�g���� 
                        if (modified.Ticks == 0 || oneCache.LastModified.Ticks == 0 || modified == oneCache.LastModified) {
                            //�L������
                            long d = oneCache.Expires.Ticks;//�w�b�_�Ŏ����ꂽ�ꍇ
                            if (d == 0) {//�w�b�_�Ŏ�����Ă��Ȃ��ꍇ�́A�{�T�[�o�̃f�t�H���g�l���g�p�����
                                d = oneCache.CreateDt.AddHours(_expires).Ticks;
                            }
                            if (d > DateTime.Now.Ticks) {//�L���������؂�Ă��Ȃ����ǂ���
                                return oneCache;//�L���L���b�V��
                            }
                        }
                        // �������L���b�V���Ƀf�[�^�����݂��邪�u�L���������o�߂��Ă���v������́A�uModified����v���Ȃ��v�̂ō폜����
                        _memoryCache.Remove(request.HostName, request.Port, request.Uri);
                        if (_diskCache != null)
                            _diskCache.Remove(request.HostName, request.Port, request.Uri);
                        return null;
                    }
                }
                // �f�B�X�N�L���b�V����ɑ��݂��邩�ǂ����H
                if (_diskCache != null) {
                    OneCache oneCache = _diskCache.Get(request.HostName, request.Port, request.Uri);
                    if (oneCache != null) {
                        //�f�B�X�N�L���b�V���Ńq�b�g���� 
                        if (modified.Ticks == 0 || oneCache.LastModified.Ticks == 0 || modified == oneCache.LastModified) {
                            //�L������
                            long d = oneCache.Expires.Ticks;//�w�b�_�Ŏ����ꂽ�ꍇ
                            if (d == 0) {//�w�b�_�Ŏ�����Ă��Ȃ��ꍇ�́A�{�T�[�o�̃f�t�H���g�l���g�p�����
                                d = oneCache.CreateDt.AddHours(_expires).Ticks;
                            }
                            if (d > DateTime.Now.Ticks) {//�L���������؂�Ă��Ȃ����ǂ���
                                //�������L���b�V���ւ̈ړ�
                                if (_memoryCache != null && _memoryCache.Add(oneCache)) {
                                    logger.Set(LogKind.Detail, null, 19, string.Format("{0}:{1}{2}", oneCache.HostName, oneCache.Port, oneCache.Uri));
                                }
                                return oneCache;//�L���L���b�V��
                            }
                        }
                        //�f�B�X�N�L���b�V���Ƀf�[�^�����݂��邪�u�L���������o�߂��Ă���v������́A�uModified����v���Ȃ��v�̂ō폜����
                        _diskCache.Remove(request.HostName, request.Port, request.Uri);
                        return null;
                    }
                }
            }// �r������
            return null;
        }
        // �L���b�V����Ԏ擾
        public long GetInfo(CacheKind cacheKind, ref List<CacheInfo> infoList) {
            if (cacheKind == CacheKind.Memory) {//������
                if (_memoryCache != null)
                    return _memoryCache.GetInfo(ref infoList, 0, this);
            } else {//�f�B�X�N
                if (_diskCache != null)
                    return _diskCache.GetInfo(ref infoList, 0, this);
            }
            return 0;

        }
    }
}
