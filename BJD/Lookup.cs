using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//using System.Drawing;
//using System.Collections;
//using System.Windows.Forms;
//using System.Data;
//�ǉ��������O���
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Management;
using Bjd.util;


namespace Bjd {
    public class Lookup {

        private Lookup(){}//�f�t�H���g�R���X�g���N�^�̉B��
        
        // DNS�T�[�o�A�h���X��擾����(�ݒ�l�擾)
        static public List<string> DnsServer() {
            var list = new List<string>();    
            var mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
            var moc = mc.GetInstances();
            foreach (var mo in moc) {
                if ((bool)mo["IPEnabled"]){
                    var dnsSet = (string[])mo["DNSServerSearchOrder"];
                    if (dnsSet != null) {
                        list.AddRange(dnsSet);
                    }
                }
            }
            return list;
        }
        static public List<string> QueryA(string hostName){
            var addrList = new List<string>();
            try{
                var hostEntry = Dns.GetHostEntry(hostName);
                addrList.AddRange(hostEntry.AddressList.Select(ipAddress => ipAddress.ToString()));
            }
            catch {
            }
            return addrList;
        }
        
        static public List<string> QueryMx(string domainName,string dnsServer) {
            var hostList = new List<string>();
            var noList = new List<int>();

            var s = domainName.Split('.');

            //���M�o�b�t�@�̒���
            var len = 16;
            foreach (var ss in s) {
                len += ss.Length;
                len++;
            }
            len++;
            //���M�p�o�b�t�@��p�ӂ���
            var buffer = new byte[len];

    
            //���ʎq�̐���
            var id= new byte[2];
            var rnd = new RNGCryptoServiceProvider();
            rnd.GetNonZeroBytes(id);

            Array.Copy(id, buffer, 2);
            buffer[2] = 0x01;
            //buffer[3] = 0x00;
            //buffer[4] = 0x00;
            buffer[5] = 0x01;
            //buffer[6] = 0x00;
            //buffer[7] = 0x00;
            //buffer[8] = 0x00;
            //buffer[9] = 0x00;
            //buffer[10] = 0x00;
            //buffer[11] = 0x00;

            //����Z�N�V�����̏�����
            var p = 12;
            foreach (var tmp in s) {
                buffer[p++] = (byte)tmp.Length;
                Encoding.ASCII.GetBytes(tmp, 0, tmp.Length, buffer, p);
                p += tmp.Length;
            }
            buffer[p++] = 0x00;
            buffer[p++] = 0x00;
            buffer[p++] = 0x0F;
            buffer[p++] = 0x00;
            buffer[p++] = 0x01;

            //�N�G���[�̑��M
            
            
            //UdpClient udpClient = new UdpClient();
            //IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(dnsServer), 53);
            //udpClient.Connect(endPoint);//connect
            //udpClient.Send(buffer, p);//send
            //buffer = new byte[512];//�o�b�t�@���M�p�ɏ�����
            //buffer = udpClient.Receive(ref endPoint);//deceive
            //udpClient.Close();


            var client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            var endPoint = new IPEndPoint(IPAddress.Parse(dnsServer), 53);
            //3�b�Ń^�C���A�E�g
            client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 3000);

            //byte[] q = Encoding.ASCII.GetBytes(query);
            client.SendTo(buffer,p,SocketFlags.None, endPoint);//���M
            //IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
            
            var senderEP = (EndPoint)endPoint;
            try {
                var data = new byte[1024];
                var recv = client.ReceiveFrom(data, ref senderEP);//��M
                buffer = new byte[recv];
                Buffer.BlockCopy(data, 0, buffer, 0, recv);
                client.Close();
            } catch {//�^�C���A�E�g
                client.Close();
                return hostList;
            }


            //���ʎq�̊m�F
            if (buffer[0]!=id[0] || buffer[1]!=id[1])
                return hostList;

            //Qcount
            p = 4;
            var qcount = (short)Util.htons(BitConverter.ToUInt16(buffer,p));
            p += 2;
            if (qcount == 0)
                return hostList;
            
            //Acount
            var acount = (short)Util.htons(BitConverter.ToUInt16(buffer,p));
            if (acount == 0)
                return hostList;
            

            p = 12;
            //Question��W�����v����
            while (buffer[p] != 0x00) 
                p++;
            p += 5;

            for (int i = 0; i< acount; i++) {
                //NAME��X�L�b�v
                while (true) {
                    if (buffer[p] >= 0xC0) {
                        p += 2;
                        break;
                    }
                    if (buffer[p] == 0x00) {
                        p++;
                        break;
                    }
                    p++;
                }

                p += 8; //TYPE(2),CLASS(2),TTL(4) ���v8�o�C�g

                //���\�[�X�̒���
                var rlen = (short)Util.htons(BitConverter.ToUInt16(buffer,p));
                p += 2;
                int offset = p;//���\�[�X�̐擪�ʒu
                //���t�@�����X���擾
                var preference = (short)Util.htons(BitConverter.ToUInt16(buffer,offset));
                offset += 2;
                //�z�X�g���擾
                var host = "";
                while (true) {
                    if (buffer[offset] == 0x00)
                        break;
                    if (buffer[offset] >= 0xC0) {//���k�`��
                        //offset = (int)Util.htons(Bytes.ReadUInt16(buffer,offset));
                        offset = Util.htons(BitConverter.ToUInt16(buffer, offset));
                        offset = offset & 0x3FFF;
                    } else {
                        int nlen = buffer[offset++];
                        host += Encoding.ASCII.GetString(buffer, offset, nlen);
                        host += ".";
                        offset += nlen;
                    }
                }
                //���t�@�����X���̏�������̂���X�g�̍ŏ��ɓ����
                var set = false;
                for (int n = 0; n < noList.Count; n++) {
                    if (preference < noList[n]) {
                        noList.Insert(n, preference);
                        hostList.Insert(n, host);
                        set = true;
                        break;
                    }
                }
                if (!set) {
                    hostList.Add(host);
                    noList.Add(preference);
                }
                p += rlen; //���̃��R�[�h�ʒu�փW�����v
            }
            return hostList;
        }
    }
}
