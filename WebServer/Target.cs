using System;
using System.Collections.Generic;
using System.IO;
using Bjd;
using Bjd.log;
using Bjd.option;
using Bjd.util;

namespace WebServer {
    /*******************************************************/
    //�Ώہi�t�@�C���j�Ɋւ���e��̏���܂Ƃ߂Ĉ����N���X
    /*******************************************************/
    class Target {
        //readonly OneOption _oneOption;
        readonly Conf _conf;
        readonly Logger _logger;
        public Target(Conf conf, Logger logger) {
            //_oneOption = oneOption;
            _conf = conf;
            _logger = logger;

            DocumentRoot = (string)_conf.Get("documentRoot");
            if (!Directory.Exists(DocumentRoot)) {
                DocumentRoot = null;//�h�L�������g���[�g����
            }
            FullPath = "";
            TargetKind = TargetKind.Non;
            Attr = new FileAttributes();
            FileInfo = null;
            CgiCmd = "";
            Uri = null;
        }
        public string DocumentRoot { get; private set; }//�h�L�������g���[�g
        public string FullPath { get; private set; }
        public TargetKind TargetKind { get; private set; }
        public WebDavKind WebDavKind { get; private set; }//Ver5.1.x
        public FileAttributes Attr { get; private set; }//�t�@�C���̃A�g���r���[�g
        public FileInfo FileInfo { get; private set; }//�t�@�C���C���t�H���[�V����
        public string CgiCmd { get; private set; }//CGI���s�v���O����
        public string Uri { get; private set; }
        /*************************************************/
        // ������
        /*************************************************/
        //uri�ɂ�鏉����
        public void InitFromUri(string uri) {
            Init(uri);
        }
        //filename�ɂ�鏉����
        public void InitFromFile(string file) {

            var tmp = file.ToLower();// fullPath����uri�𐶐�����
            var root = DocumentRoot.ToLower();
            if (tmp.IndexOf(root) != 0)
                return;
            var uri = file.Substring(root.Length);
            uri = Util.SwapChar('\\', '/', uri);

            Init(uri);
        }
        //�R�}���h�ɂ�鏉����
        public void InitFromCmd(string fullPath) {
            TargetKind = TargetKind.Cgi;
            CgiCmd = "COMSPEC";
            FullPath = fullPath;
        }
        void Init(string uri) {

            Uri = uri;

            TargetKind = TargetKind.File;//�ʏ�t�@�C���ł���Ɖ��u������
            var enableCgiPath = false;//�t�H���_��CGI���s�\���ǂ���
            WebDavKind = WebDavKind.Non;//Ver5.1.x WebDAV�ΏۊO�ł��邱�Ƃ���u������

            //****************************************************************
            //WebDav�p�X�Ƀq�b�g�����ꍇ�Auri�y�уh�L�������g���[�g��C������
            //****************************************************************
            if ((bool)_conf.Get("useWebDav")) {
                var db = (Dat)_conf.Get("webDavPath");
                foreach (var o in db) {
                    if (o.Enable) {
                        var name = o.StrList[0];
                        var write = Convert.ToBoolean(o.StrList[1]);//�������݋���
                        var dir = o.StrList[2];
                        if (uri.ToUpper().IndexOf(name.ToUpper()) == 0) {
                            if (name.Length >= 1) {
                                uri = uri.Substring(name.Length - 1);
                            } else {
                                uri = "/";
                            }
                            DocumentRoot = dir;
                            //WevDav�p�X��`�Ƀq�b�g�����ꍇ
                            WebDavKind = (write) ? WebDavKind.Write : WebDavKind.Read;
                            break;
                        }
                    }
                }

                // �Ōオ/�Ŗ����ꍇ�́A�ۊǂ��ăq�b�g���邩�ǂ�����m�F����
                if (uri[uri.Length - 1] != '/') {
                    var exUri = uri + "/";
                    foreach (var o in db) {
                        if (o.Enable) {
                            var name = o.StrList[0];
                            var write = Convert.ToBoolean(o.StrList[1]);//�������݋���
                            var dir = o.StrList[2];
                            if (exUri.ToUpper().IndexOf(name.ToUpper()) == 0) {
                                if (name.Length >= 1) {
                                    uri = exUri.Substring(name.Length - 1);
                                } else {
                                    uri = "/";
                                }
                                Uri = exUri;//���N�G�X�g�Ɋ���/���t���Ă����悤�ɓ��삳����
                                DocumentRoot = dir;
                                //WevDav�p�X��`�Ƀq�b�g�����ꍇ
                                WebDavKind = (write) ? WebDavKind.Write : WebDavKind.Read;
                                break;
                            }
                        }
                    }
                }


            }

            //****************************************************************
            //CGI�p�X�Ƀq�b�g�����ꍇ�Auri�y�уh�L�������g���[�g��C������
            //****************************************************************
            bool useCgiPath = false;//CGI�p�X��`�����݂��邩�ǂ����̃t���O
            if (WebDavKind == WebDavKind.Non) {

                if ((bool)_conf.Get("useCgi")) {
                    foreach (var o in (Dat)_conf.Get("cgiPath")) {
                        if (o.Enable) {
                            useCgiPath = true;//�L����CGI�p�X�̒�`�����݂���
                            var name = o.StrList[0];
                            var dir = o.StrList[1];
                            if (uri.ToUpper().IndexOf(name.ToUpper()) == 0) {
                                if (name.Length >= 1) {
                                    uri = uri.Substring(name.Length - 1);
                                } else {
                                    uri = "/";
                                }
                                DocumentRoot = dir;
                                //CGI�p�X��`�Ƀq�b�g�����ꍇ
                                enableCgiPath = true;//CGI���s���\�ȃt�H���_�ł���
                                break;
                            }
                        }
                    }
                    if (!useCgiPath) {//�L����CGI�p�X��`�������ꍇ�́A
                        enableCgiPath = true;//CGI���s���\�ȃt�H���_�ł���
                    }
                }
            }


            //****************************************************************
            //�ʖ��Ƀq�b�g�����ꍇ�Auri�y�уh�L�������g���[�g��C������
            //****************************************************************
            if (WebDavKind == WebDavKind.Non && !useCgiPath) {
                foreach (var o in (Dat)_conf.Get("aliaseList")) {
                    if (o.Enable) {
                        var name = o.StrList[0];
                        var dir = o.StrList[1];

                        if (uri.Length >= 1) {
                            if (uri.ToUpper() + "/" == name.ToUpper()) {
                                //�t�@�C���w�肳�ꂽ�^�[�Q�b�g���t�@�C���ł͂Ȃ��f�B���N�g���̏ꍇ
                                TargetKind = TargetKind.Move;
                                return;
                            }
                            if (uri.ToUpper().IndexOf(name.ToUpper()) == 0) {
                                if (name.Length >= 1) {
                                    uri = uri.Substring(name.Length - 1);
                                } else {
                                    uri = "/";
                                }
                                DocumentRoot = dir;
                                break;
                            }
                        }
                    }
                }
            }

            /*************************************************/
            // uri���畨���I�ȃp�X���𐶐�����
            /*************************************************/
            FullPath = Util.SwapChar('/', '\\', DocumentRoot + uri);

            /*************************************************/
            //�t�@�C���w�肳�ꂽ�^�[�Q�b�g���t�@�C���ł͂Ȃ��f�B���N�g���̏ꍇ
            /*************************************************/
            if (WebDavKind == WebDavKind.Non) {
                if (FullPath[FullPath.Length - 1] != '\\') {
                    if (Directory.Exists(FullPath)) {
                        TargetKind = TargetKind.Move;
                        return;
                    }
                }
            } else {
                if (TargetKind == TargetKind.File) {
                    if (Directory.Exists(FullPath)) {
                        TargetKind = TargetKind.Dir;
                        return;
                    }

                }
            }

            /*************************************************/
            // welcome�t�@�C���̃Z�b�g
            /*************************************************/
            //Uri�Ńt�@�C�������w�肳��Ă��Ȃ��ꍇ�ŁA���Y�f�B���N�g����welcomeFileName�����݂���ꍇ
            //�t�@�C�����Ƃ��Ďg�p����
            if (WebDavKind == WebDavKind.Non) {
                //Ver5.1.3
                try {
                    if (Path.GetFileName(FullPath) == "") {
                        var tmp = ((string)_conf.Get("welcomeFileName")).Split(',');
                        foreach (string welcomeFileName in tmp) {
                            var newPath = Path.GetDirectoryName(FullPath) + "\\" + welcomeFileName;
                            if (File.Exists(newPath)) {
                                FullPath = newPath;
                                break;
                            }
                        }
                    }
                } catch (Exception ex) {//Ver5.1.3
                    _logger.Set(LogKind.Error, null, 37, string.Format("uri={0} FullPath={1} {2}", uri, FullPath, ex.Message));
                    TargetKind = TargetKind.Non;
                    return;
                }

            }
            /*************************************************/
            //�^�[�Q�b�g�̓t�@�C���Ƃ��đ��݂��邩
            /*************************************************/
            if (!File.Exists(FullPath)) {
                //�f�B���N�g��t�Ƃ��đ��݂���̂�
                if (Directory.Exists(FullPath)) {
                    if ((bool)_conf.Get("useDirectoryEnum")) {
                        if (WebDavKind == WebDavKind.Non) {
                            TargetKind = TargetKind.Dir;
                            return;
                        }
                    }
                }
                TargetKind = TargetKind.Non;//���݂��Ȃ�
                return;
            }

            /*************************************************/
            // �g���q���f
            /*************************************************/
            // �uCGI���s���\�ȃt�H���_�̏ꍇ�@�g���q���q�b�g����΃^�[�Q�b�g��CGI�ł���
            if (WebDavKind == WebDavKind.Non) {
                if (enableCgiPath) {
                    var ext = Path.GetExtension(FullPath);
                    if (ext!=null && ext.Length > 1) {
                        ext = ext.Substring(1);
                        foreach (var o in (Dat)_conf.Get("cgiCmd")) {
                            if (o.Enable) {
                                var cgiExt = o.StrList[0];
                                var cgiCmd = o.StrList[1];
                                if (cgiExt.ToUpper() == ext.ToUpper()) {
                                    TargetKind = TargetKind.Cgi;//CGI�ł���
                                    CgiCmd = cgiCmd;
                                }
                            }
                        }
                    }
                }
            }

            /*************************************************/
            // �^�[�Q�b�g��SSI���ǂ����̔��f
            /*************************************************/
            if (WebDavKind == WebDavKind.Non) {
                if (TargetKind == TargetKind.File) {
                    //�uSSI��g�p����v�ꍇ
                    if ((bool)_conf.Get("useSsi")) {
                        // SSI�w��g���q���ǂ����̔��f
                        var cgiExtList = new List<string>(((string)_conf.Get("ssiExt")).Split(','));
                        var ext = Path.GetExtension(FullPath);
                        if (ext!=null && 1 <= ext.Length) {
                            if (0 <= cgiExtList.IndexOf(ext.Substring(1))) {
                                //�^�[�Q�b�g�t�@�C���ɃL�[���[�h���܂܂�Ă��邩�ǂ����̊m�F
                                if (0 <= Util.IndexOf(FullPath, "<!--#")) {
                                    TargetKind = TargetKind.Ssi;
                                }
                            }
                        }
                    }
                }
            }
            /*************************************************/
            // �A�g���r���[�g�y�уC���t�H���[�V�����̎擾
            /*************************************************/
            if (TargetKind == TargetKind.File || TargetKind == TargetKind.Ssi) {
                //�t�@�C���A�g���r���[�g�̎擾
                Attr = File.GetAttributes(FullPath);
                //�t�@�C���C���t�H���[�V�����̎擾
                FileInfo = new FileInfo(FullPath);
            }

        }

        //���X�g�Ƀq�b�g�����ꍇ�Auri�y�уh�L�������g���[�g�����������
        //Ver5.0.0-a13�C��
        /*
         * bool Aliase(Dat2 db) {
            int index = uri.Substring(1).IndexOf('/');//�擪��'/'�ȍ~�ōŏ��Ɍ����'/'���������
            if (0 < index) {
                string topDir = uri.Substring(1, index);
                foreach (OneLine oneLine in db.Lines) {
                    if (oneLine.Enabled) {
                        string name = (string)oneLine.ValList[0].Obj;
                        string dir = (string)oneLine.ValList[1].Obj;
                        if (name.ToLower() == topDir.ToLower()) {
                            DocumentRoot = dir;
                            uri = uri.Substring(index);
                            return true;//�ϊ��i�q�b�g�j����
                        }
                    }
                }
            }
            return false;
        }
         * */

    }
}

