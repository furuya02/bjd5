using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using Bjd;
using Bjd.net;
using Bjd.option;

namespace DhcpServer {

    public class Lease : IDisposable {
        //readonly Logger logger;
        readonly int leaseTime;
        readonly string fileName;

        readonly List<OneLease> ar = new List<OneLease>();

        //public Lease(Logger logger, string fileName, Ip startIp, Ip endIp, int leaseTime, Dat2 macAcl) {
        public Lease(string fileName, Ip startIp, Ip endIp, int leaseTime, Dat macAcl) {
            //this.logger = logger;
            this.fileName = fileName;
            this.leaseTime = leaseTime;
            uint start = startIp.AddrV4;
            uint end = endIp.AddrV4;
            int count = 2048;//�ő�ێ���

            for (uint i = start; i <= end && count > 0; i++) {
                Ip ip = new Ip(i);
                ar.Add(new OneLease(ip));//MAC�w��Ȃ�
                count--;
            }

            foreach (var o in macAcl) {
                if (o.Enable) {//�L���ȃf�[�^������Ώۂɂ���
                    string macStr = o.StrList[0];//MAC�A�h���X(99-99-99-99-99-99)
                    Mac mac = new Mac(macStr);
                    Ip ip = new Ip(o.StrList[1]);//IP�A�h���X
                    if (ip.ToString() == "255.255.255.255") {
                        ar.Add(new OneLease(ip, mac));//MAC�w�肠��őS���ǉ�
                    } else {

                        // ��{�ݒ�͈̔͂̃e�[�u�������
                        bool find = false;
                        for (int i = 0; i < ar.Count; i++) {
                            if (ar[i].Ip == ip) {
                                ar[i] = new OneLease(ip, mac);//MAC�w�肠��ɕύX
                                find = true;
                                break;
                            }
                        }
                        if (!find) { // ��{�ݒ�͈̔͊O�̏ꍇ
                            ar.Add(new OneLease(ip, mac));//MAC�w�肠��Ƃ��Ēǉ�
                        }
                    }
                }
            }
            // ���[�X���f�[�^�̓ǂݍ���
            Read();
        }

        public void Dispose() {
            for (int i = 0; i < ar.Count; i++) {
                ar[i].Refresh();
            }
            Save();// ���[�X���̃f�[�^��ۑ�
        }
        //MAC�w��݂̂̏ꍇ�A�f�[�^�x�[�X�ɑ��݂��邩�ǂ�����m�F����
        public bool SearchMac(Mac mac) {
            for (int i = 0; i < ar.Count; i++) {
                if (ar[i].MacAppointment && ar[i].Mac == mac)
                    return true;
            }
            return false;
        }

        //RELEASE����
        public Ip Release(Mac mac) {
            // ���Y�f�[�^�x�[�X�̌���
            for (int i = 0; i < ar.Count; i++) {
                if (ar[i].Mac == mac) {
                    ar[i].SetUnuse();
                    Save();// ���[�X���̃f�[�^��ۑ�
                    return ar[i].Ip;
                }
            }
            return null;
        }

        //DISCOVER����
        public Ip Discover(Ip requestIp, uint id, Mac mac) {
            int i = SearchDiscover(requestIp, id, mac);
            if (i != -1) {
                ar[i].SetReserve(id, mac);
                // ���N�G�X�g���ꂽIP�ȊO���������ꂽ�ꍇ�����
                return ar[i].Ip;
            }
            return null;
        }

        //REQUEST����
        public Ip Request(Ip requestIp, uint id, Mac mac) {

            int i = SearchRequest(requestIp, id);

            if (i != -1) {

                //����MAC�ł��łɎg�p���̂�̂�����Δj������
                for (int n = 0; n < ar.Count; n++) {
                    if (n == i)
                        continue;
                    if (ar[n].Mac == mac)
                        ar[n].SetUnuse();
                }


                ar[i].SetUsed(id, mac, DateTime.Now.AddSeconds(leaseTime));
                Save();// ���[�X���̃f�[�^��ۑ�

                // ���N�G�X�g���ꂽIP�ȊO���������ꂽ�ꍇ�����
                return ar[i].Ip;
            }
            return null;

        }

        public int SearchRequest(Ip requestIp, uint id) {


            for (int i = 0; i < ar.Count; i++) {

                if (ar[i].DbStatus == DhcpDbStatus.Reserve && ar[i].Id == id)
                    return i;
                if (ar[i].DbStatus == DhcpDbStatus.Used && ar[i].Ip == requestIp) {
                    return i;
                }
            }
            return -1;
        }

        int SearchDiscover(Ip ip, uint id, Mac mac) {

            //���ł�DISCOVER��󂯂ă��U�[�u��Ԃ̃f�[�^������ꍇ�́A����������Ԃ�
            for (int i = 0; i < ar.Count; i++) {
                if (ar[i].DbStatus == DhcpDbStatus.Reserve && ar[i].Id == id) {
                    return i;
                }
            }

            //MAC�w��̃f�[�^��D�悵�Č�������
            for (int i = 0; i < ar.Count; i++) {
                if (ar[i].MacAppointment && ar[i].Mac == mac) {
                    if (ar[i].Ip.ToString() == "255.255.255.255") {
                        goto next;//            
                    }
                    return i;
                }
            }

            // ����MAC�̃f�[�^������΁A�����̃f�[�^��j�����ă��[�X�ΏۂƂ���
            for (int i = 0; i < ar.Count; i++) {
                if (ar[i].Mac == mac) {
                    ar[i].SetUnuse();// �ˑ��f�[�^��N���A
                    return i;
                }
            }
            //�v���h�o�������Ă���ꍇ�́A���[�X�Ώۂɂ���
            for (int i = 0; i < ar.Count; i++) {
                if (!ar[i].MacAppointment && ar[i].DbStatus == DhcpDbStatus.Unused && ar[i].Ip == ip) {
                    return i;
                }
            }
        next:
            //IP�͂Ȃ�ł�����̂ŋ󂢂Ă����̂�Ώۂɂ���
            for (int i = 0; i < ar.Count; i++) {
                ar[i].Refresh();//���Ԓ��߂��Ă���f�[�^�͏���������
                if (!ar[i].MacAppointment && ar[i].DbStatus == DhcpDbStatus.Unused) {
                    return i;
                }
            }
            return -1;
        }
        // ���[�X���̃f�[�^�̕ۑ�
        void Save() {
            using (StreamWriter sw = new StreamWriter(fileName, false, Encoding.ASCII)) {
                for (int i = 0; i < ar.Count; i++) {
                    if (ar[i].DbStatus == DhcpDbStatus.Used) {
                        string str = string.Format("{0}\t{1}\t{2}\t{3}",
                            ar[i].Ip.ToString(),
                            ar[i].Dt.Ticks,
                            ar[i].Id,
                            ar[i].Mac.ToString());
                        sw.WriteLine(str);
                    }
                }
                sw.Flush();
                sw.Close();
            }
        }
        // ���[�X���̃f�[�^�̓ǂݍ���
        void Read() {
            if (!File.Exists(fileName))
                return;
            using (StreamReader sr = new StreamReader(fileName, Encoding.ASCII)) {
                while (true) {
                    string str = sr.ReadLine();
                    if (str == null)
                        break;
                    string[] tmp = str.Split('\t');
                    if (tmp.Length == 4) {
                        try {
                            Ip ip = new Ip(tmp[0]);
                            DateTime dt = new DateTime(Convert.ToInt64(tmp[1]));
                            uint id = Convert.ToUInt32(tmp[2]);
                            Mac mac = new Mac(tmp[3]);
                            for (int i = 0; i < ar.Count; i++) {
                                if (ar[i].Ip == ip) {
                                    if (ar[i].MacAppointment && ar[i].Mac != mac) {
                                        break;
                                    }
                                    ar[i].SetUsed(id, mac, dt);
                                    break;
                                }
                            }
                        } catch {
                            
                        }

                    }
                }
                sr.Close();
            }

        }
        public string GetInfo() {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ar.Count; i++) {
                ar[i].Refresh();//���Ԓ��߂��Ă���f�[�^�͏���������
                sb.Append(ar[i].ToString() + "\b");
            }
            return sb.ToString();
        }
    }
}
