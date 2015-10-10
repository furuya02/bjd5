using System;
using System.Text;
using Bjd;
using Bjd.log;
using Bjd.net;
using Bjd.sock;
using Bjd.util;

namespace ProxyHttpServer
{
    //********************************************************
    //���N�G�X�g/���X�|���X�����N���X
    //********************************************************
    public class Request {
        
        public Request() {
            HostName = "";
            Uri = "";
            Ext = "";
            Cgi = false;
            RequestStr = "";
            Port = 80;
            HttpMethod = HttpMethod.Unknown;
            Protocol = ProxyProtocol.Unknown;
            HttpVer = "";
        }
        

        //****************************************************************
        //�v���p�e�B
        //****************************************************************
        public string HostName { get; private set; }
        public string Uri { get; private set; }
        public string Ext { get; private set; }
        public bool Cgi { get; private set; }
        public string RequestStr { get; private set; }
        public int Port { get; private set; }
        public HttpMethod HttpMethod { get; private set; }
        public ProxyProtocol Protocol { get; private set; }
        public string HttpVer { get; private set; }
        public string User { get; private set; }
        public string Pass { get; private set; }
        private Encoding _urlEncoding = Encoding.ASCII;

        public byte [] SendLine(bool useUpperProxy) {
            var str = string.Format("{0} {1} {2}\r\n", HttpMethod.ToString().ToUpper(), Uri, HttpVer);
            if (useUpperProxy) {
                str = string.Format("{0}\r\n", RequestStr);
            }
            return _urlEncoding.GetBytes(str);//�����̃G���R�[�h�`���ɖ߂�
        }


        //�f�[�^�擾�i����f�[�^�́A�����������j
        public bool Recv(Logger logger, SockTcp tcpObj,int timeout,ILife iLife) {

            var buf= tcpObj.LineRecv(timeout,iLife);
            if (buf == null)
                return false;
            buf = Inet.TrimCrlf(buf);

            _urlEncoding = MLang.GetEncoding(buf);//URL�G���R�[�h�̌`����ۑ�����
            
            //Ver5.9.8
            if (_urlEncoding == null){
                var sb = new StringBuilder();
                for (int i = 0; i < buf.Length; i++) {
                    sb.Append(String.Format("0x{0:X},", buf[i]));
                }
                logger.Set(LogKind.Error, tcpObj, 9999, String.Format("_urlEncoding==null buf.Length={0} buf={1}", buf.Length,sb.ToString()));
                //���̂܂ܗ�O�֓˓�������
            }
            
            var str = _urlEncoding.GetString(buf);
          
            // ���\�b�h�EURI�E�o�[�W�����ɕ���
            //"GET http://hostname:port@user:pass/path/filename.ext?param HTTP/1.1"
            RequestStr = str;

            //(�󔒂ŕ�������)�@"GET <=> http://hostname:port@user:pass/path/filename.ext?param HTTP/1.1"
            var index = str.IndexOf(' ');
            if (index < 0) //Ver5.0.0-a8
                return false;

            //(�O��) "GET"
            var methodStr = str.Substring(0, index);
            foreach (HttpMethod m in Enum.GetValues(typeof(HttpMethod))) {
                if (methodStr.ToUpper() == m.ToString().ToUpper()) {
                    HttpMethod = m;
                    break;
                }
            }
            if (HttpMethod == HttpMethod.Unknown) {
                logger.Set(LogKind.Secure,tcpObj,1,string.Format("{0}",RequestStr));//�T�|�[�g�O�̃��\�b�h�ł��i������p���ł��܂���j
                return false;
            }
            if (HttpMethod == HttpMethod.Connect) {
                Protocol = ProxyProtocol.Ssl;
                Port = 443;//�f�t�H���g�̃|�[�g�ԍ���443�ɂȂ�
            }

            //(�㔼) "http://hostname:port@user:pass/path/filename.ext?param HTTP/1.1"
            str = str.Substring(index + 1);


            //(�󔒂ŕ�������)�@"http://hostname:port@user:pass/path/filename.ext?param <=> HTTP/1.1"
            index = str.IndexOf(' ');
            if (index < 0) //Ver5.0.0-a8
                return false;
            //(�㔼) "HTTP/1.1"
            HttpVer = str.Substring(index + 1);
            
            if(HttpVer != "HTTP/0.9" && HttpVer != "HTTP/1.0" && HttpVer != "HTTP/1.1") {
                logger.Set(LogKind.Secure,tcpObj,2,RequestStr);//�T�|�[�g�O�̃o�[�W�����ł��i������p���ł��܂���j
                return false;
            }

            //(�O��) "http://hostname:port@user:pass/path/filename.ext?param"
            str = str.Substring(0, index);

            if (Protocol == ProxyProtocol.Unknown) {//�v���g�R���擾
                //("://"�ŕ�������)�@"http <=> hostname:port@user:pass/path/filename.ext?param <=> HTTP/1.1"
                index = str.IndexOf("://");
                if (index < 0) //Ver5.0.0-a8
                    return false;
                //(�O��) "http"
                var protocolStr = str.Substring(0, index);

                if (protocolStr.ToLower() == "ftp") {
                    Protocol = ProxyProtocol.Ftp;//�v���g�R����FTP�ɏC��
                    Port = 21;//FTP�ڑ��̃f�t�H���g�̃|�[�g�ԍ���21�ɂȂ�
                } else if(protocolStr.ToLower() != "http") {
                    //Ver5.6.7
                    //Msg.Show(MsgKind.Error,"�݌v�G���[�@Request.Recv()");
                    //�G���[�\����|�b�v�A�b�v���烍�O�ɕύX
                    logger.Set(LogKind.Error, tcpObj, 29, string.Format("protocolStr={0}", protocolStr));
                    return false;
                } else {
                    Protocol = ProxyProtocol.Http;
                }
                //(�㔼) "hostname:port@user:pass/path/filename.ext?param"
                str = str.Substring(index + 3);
            }
            //(�ŏ���"/"�ŕ�������)�@"hostname:port@user:pass <=> /path/filename.ext?param"
            index = str.IndexOf('/');
            HostName = str;
            if (0 <= index) {
                //(�O��) ""hostname:port@user:pass"
                HostName = str.Substring(0, index);

                //(�㔼) "/path/filename.ext?param"
                str = str.Substring(index);
            } else {
                // GET http://hostname HTTP/1.0 �̂悤�ɁA���[�g�f�B���N�g�����w�肳��Ă��Ȃ��ꍇ�̑Ώ�
                str = "/";
            }

            //�z�X�g�������Ƀ��[�U���F�p�X���[�h�������Ă���ꍇ�̏���
            index = HostName.IndexOf("@");
            if (0 <= index) {
                var userpass = HostName.Substring(0,index);

                //���[�U���F�p�X���[�h��j������
                HostName = HostName.Substring(index + 1);

                var i = userpass.IndexOf(':');
                if(i == -1) {
                    User = userpass;
                } else {
                    User = userpass.Substring(0,i);
                    Pass = userpass.Substring(i + 1);
                }
            }
            //Ver5.1.2 IPv6�A�h���X�\�L�̃z�X�g���ɑΉ�
            var tmp = HostName.Split(new[] { '[',']' });
            if(tmp.Length == 3) {//IPv6�A�h���X�\�L�ł���Ɣ��f����
                HostName = string.Format("[{0}]",tmp[1]);
                index = tmp[2].IndexOf(":");
                if(0 <= index) {
                    var s = tmp[2].Substring(index + 1);
                    Port = Convert.ToInt32(s);
                }
            }else{

                //�z�X�g�������Ƀ|�[�g�ԍ��������Ă���ꍇ�̏���
                index = HostName.IndexOf(":");
                if (0 <= index) {
                    var s = HostName.Substring(index + 1);
                    Port = Convert.ToInt32(s);
                    HostName = HostName.Substring(0, index);
                }
            }
                
            Uri = str;
            
            //CGI����
            if(-1!=Uri.LastIndexOf('?'))
                Cgi=true;

            //�g���q�擾
            if (!Cgi) {
                index = Uri.LastIndexOf('/');
                if (index != -1)
                    str = Uri.Substring(index + 1);
                index = str.LastIndexOf('.');
                if (index != -1) {
                    Ext = str.Substring(index + 1);
                }
            }
            return true;
        }
    }
}
