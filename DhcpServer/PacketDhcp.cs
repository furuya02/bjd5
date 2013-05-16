using System;
using System.Text;
using System.Runtime.InteropServices;
using Bjd;
using Bjd.net;
using Bjd.util;

namespace DhcpServer
{
    //パケットを処理するクラス
    internal class PacketDhcp {

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct HeaderDhcp {
            public byte Opcode;
            public byte HwType;
            public byte HwAddrLen;
            public byte HopCount;
            public uint TransactionId;
            public short NumberOfSecounds;
            public short Flags;
            public uint ClientIp;
            public uint YourIp;
            public uint ServerIp;
            public uint GatewayIp;
            
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte [] ClientHwAddr;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string ServerHostName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string BootFile;
            public uint MagicCookie;
        }
        // 内部バッファ
        HeaderDhcp _headerDhcp;
        int _optionLen;
        byte[] _option;

        //受信パケット用のコンストラクタ
        public PacketDhcp() {

        }

        //送信パケット用のコンストラクタ
        public PacketDhcp(uint id,Ip requestIp,Ip serverIp,Mac mac,DhcpType dhcpType,int leaseTime,Ip maskIp,Ip gwIp,Ip dnsIp0,Ip dnsIp1,string wpadUrl) {
        //public PacketDhcp(uint id,Ip requestIp,Ip serverIp,Mac mac,DHCP_TYPE dhcpType) {


            //Dmy init
            _headerDhcp.HopCount = 0;
            _headerDhcp.NumberOfSecounds = 0;
            _headerDhcp.Flags = 0;
            _headerDhcp.ClientIp = 0;
            _headerDhcp.GatewayIp = 0;

            
            _headerDhcp.ClientHwAddr = new byte[16];
            _headerDhcp.ServerHostName = new String((char)0,64);
            _headerDhcp.BootFile = new String((char)0, 128);

            _headerDhcp.Opcode = 0x02;// 応答パケット
            _headerDhcp.HwType = 0x01;
            _headerDhcp.HwAddrLen = 0x06;
            _headerDhcp.TransactionId = id;
            _headerDhcp.MagicCookie = 0x63538263;
            if(requestIp!=null)
                _headerDhcp.YourIp = Util.htonl(requestIp.AddrV4);
            _headerDhcp.ServerIp = Util.htonl(serverIp.AddrV4);
            Buffer.BlockCopy(mac.GetBytes(),0,_headerDhcp.ClientHwAddr,0,6);


            //オプション
            _option = new byte[1];
            _option[0] = 0xFF;//終端ポインタをセット


            byte[] buf;//オプション追加時のテンポラリ
            //if (dhcpType != DHCP_TYPE.INFRM) {
            if (dhcpType == DhcpType.Ack || dhcpType == DhcpType.Offer) {
            // ERR if (dhcpType == DHCP_TYPE.OFFER) {
                //leaseTime
                double d = leaseTime/0x2;
                uint i = (uint)d;
                buf = BitConverter.GetBytes(Util.htonl(i));
                SetOptions(0x3a,buf);//Renewal Time

                d = leaseTime*0.85;
                i = (uint)d;
                buf = BitConverter.GetBytes(Util.htonl(i));
                SetOptions(0x3b,buf);//Rebinding Time

                i = (uint)leaseTime;
                buf = BitConverter.GetBytes(Util.htonl(i));
                SetOptions(0x33,buf);//Lease Time

                //serverIp
                byte [] dat = BitConverter.GetBytes(Util.htonl(serverIp.AddrV4));
                SetOptions(0x36,dat);
            
                //maskIp
                buf = BitConverter.GetBytes(Util.htonl(maskIp.AddrV4));
                SetOptions(0x01,buf);

                //gwIp
                buf = BitConverter.GetBytes(Util.htonl(gwIp.AddrV4));
                SetOptions(0x03, buf);


                //dnsIp0
                buf = new byte[0];
                if(dnsIp0!=null){
                    buf = BitConverter.GetBytes(Util.htonl(dnsIp0.AddrV4));
                }
                if(dnsIp1!=null){
                    byte [] tmp = BitConverter.GetBytes(Util.htonl(dnsIp1.AddrV4));
                    buf = Bytes.Create(buf,tmp);
                }
                if(buf.Length!=0){
                    SetOptions(0x06,buf);
                }
                //wpad
                if(wpadUrl != null) {
                    byte[] tmp = Encoding.ASCII.GetBytes(wpadUrl);
                    SetOptions(252,tmp);
                }

            }
            
            buf = new byte[] { 0 };
            switch (dhcpType) {
                case DhcpType.Discover:
                    buf[0] = 0x01;
                    break;
                case DhcpType.Offer:
                    buf[0] = 0x02;
                    break;
                case DhcpType.Request:
                    buf[0] = 0x03;
                    break;
                case DhcpType.Decline:
                    buf[0] = 0x04;
                    break;
                case DhcpType.Ack:
                    buf[0] = 0x05;
                    break;
                case DhcpType.Nak:
                    buf[0] = 0x06;
                    break;
                case DhcpType.Release:
                    buf[0] = 0x07;
                    break;
                case DhcpType.Infrm:
                    buf[0] = 0x08;
                    break;

            }
            SetOptions(0x35, buf);
        }
        public byte[] GetBuffer() {

            return Bytes.Create(
                _headerDhcp.Opcode,
                _headerDhcp.HwType,
                _headerDhcp.HwAddrLen,
                _headerDhcp.HopCount,
                _headerDhcp.TransactionId,
                _headerDhcp.NumberOfSecounds,
                _headerDhcp.Flags,
                _headerDhcp.ClientIp,
                _headerDhcp.YourIp,
                _headerDhcp.ServerIp,
                _headerDhcp.GatewayIp,
                _headerDhcp.ClientHwAddr,
                _headerDhcp.ServerHostName,
                _headerDhcp.BootFile,
                _headerDhcp.MagicCookie,
                _option);
        }

        public byte[] GetBytes(){
           return GetBuffer();
        }

        //******************************************************
        //パケットの解釈
        //******************************************************
        public bool Read(byte[] buffer) {
            unsafe {
                int offSet = 0;
                fixed (byte* p = buffer) {
                    //コピー先構造体のサイズ確認
                    int size = Marshal.SizeOf(typeof(HeaderDhcp));
                    //コピー先のサイズが必要分存在するかどうかの確認
                    if (offSet + size > buffer.Length) {
                        return false;// 受信バイト数超過
                    }
                    _headerDhcp = (HeaderDhcp)Marshal.PtrToStructure((IntPtr)(p + offSet), typeof(HeaderDhcp));
                    offSet += size;

                    if (_headerDhcp.MagicCookie == 0x63538263) {
                        _optionLen = buffer.Length - offSet;
                        if (_optionLen > 0) {
                            _option = new byte[_optionLen];
                            Marshal.Copy((IntPtr)(p + offSet), _option, 0, _optionLen);
                        }
                    }

                }
                return true;
            }
        }

        public Mac Mac{
            get {
                return new Mac(_headerDhcp.ClientHwAddr);
            }
        }
        public byte Opcode {
            get{
                return _headerDhcp.Opcode;
            }
        }
        public uint Id {
            get {
                return _headerDhcp.TransactionId;
            }
        }
        
        ////DHCPメッセージタイプの取得
        public DhcpType Type{
            get {
                byte[] buf;
                if (GetOptions(0x35, out buf)) {
                    switch (buf[0]) {
                        case 0x01:
                            return DhcpType.Discover;
                        case 0x02:
                            return DhcpType.Offer;
                        case 0x03:
                            return DhcpType.Request;
                        case 0x04:
                            return DhcpType.Decline;
                        case 0x05:
                            return DhcpType.Ack;
                        case 0x06:
                            return DhcpType.Nak;
                        case 0x07:
                            return DhcpType.Release;
                        case 0x08:
                            return DhcpType.Infrm;
                    }
                }
                return DhcpType.Unknown;
            }
        }

        public Ip RequestIp{
            get {
                byte[] buf;
                if (!GetOptions(0x32, out buf)) {
                    //0x32が無い場合もエラーではない
                    //Ver5.7.5 return new Ip("0,0,0,0");
                    return new Ip(IpKind.V4_0);
                }
                return new Ip(Util.htonl(BitConverter.ToUInt32(buf, 0)));
            }
        }
        public Ip ServerIp{
            get {
                byte[] buf;
                if (!GetOptions(0x36, out buf)) {
                    //0x36が無い場合もエラーではない
                    //Ver5.7.5 return new Ip("0,0,0,0");
                    return new Ip(IpKind.V4_0);
                }
                return new Ip(Util.htonl(BitConverter.ToUInt32(buf, 0)));
            }
        }


        //オプションからのデータ取得
        bool GetOptions(byte code, out byte[] buf) {
            int i=0;
            while(true){
                int c = _option[i++]; 
                if(c==0xff)
                    break;
                
                //overflow
                if(i>=_option.Length)
                    break;

                if(c==0)
                    continue;

                int size = _option[i++];

                //overflow
                if(i+size>=_option.Length)
                    break;
                
               if(c==code){
                   buf = new byte[size];
                   Buffer.BlockCopy(_option,i,buf,0,size);
                   return true;
                }
                i+=size;
            }
            buf = new byte[0];
            return false;
        }

        //オプションへの追加
        public void SetOptions(byte code, byte[] dat) {
            
            //追加分のフィールドサイズ
            int len = 1 + 1 + dat.Length;
            
            //現在の内容をバックアップする
            var tmp = new byte[_option.Length];
            Buffer.BlockCopy(_option,0,tmp,0,_option.Length);

            //新規の領域を確保する
            _option = new byte[tmp.Length+len];
            
            //新しいデータフィールドを追加する
            int offset = 0;
            _option[offset++] = code;
            _option[offset++] = (byte)dat.Length;
            Buffer.BlockCopy(dat,0,_option, offset,dat.Length);
            offset += dat.Length;
            //バックアップした内容を追加する
            Buffer.BlockCopy(tmp,0,_option,offset,tmp.Length);
        }
    }
}