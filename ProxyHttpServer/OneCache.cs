using System;
using System.IO;
using System.Text;
using Bjd;
using Bjd.util;

namespace ProxyHttpServer {
    public class OneCache {
        public OneCache(string hostName, int port, string uri) {
            HostName = hostName;
            Port = port;
            Uri = uri;
            Header = new Header();
            Body = new byte[0];
            LastModified = new DateTime(0);//ドキュメントの最終更新日時（ヘッダに指定されていない場合は0となる）
            Expires = new DateTime(0);//有効期限（ヘッダに指定されていない場合は0となる）
            CreateDt = DateTime.Now;//このOneCacheが作成された日時（expire保存期間に影響する）
            LastAccess = new DateTime(0);//このOneCahceを最後に使用した日時（キャッシュＤＢに留まるかどうかの判断に使用される）
        }

        //****************************************************************
        //プロパティ
        //****************************************************************
        public string HostName { get; private set; }
        public int Port { get; private set; }
        public string Uri { get; private set; }
        public Header Header { get; private set; }
        public byte[] Body { get; private set; }
        public DateTime LastModified { get; private set; }
        public DateTime Expires { get; private set; }
        public DateTime CreateDt { get; private set; }
        public DateTime LastAccess { get; set; }

        public long Length {
            get {
                return Header.GetBytes().Length + Body.Length;
            }
        }

        public void Add(Header header, byte[] body) {
            //ドキュメントのLast-Modifiedを記録する
            string str = header.GetVal("Last-Modified");
            if (str != null) {
                LastModified = Util.Str2Time(str);
            }
            //ドキュメントのExpiresを記録する
            str = header.GetVal("Expires");
            if (str != null) {
                Expires = Util.Str2Time(str);
            }

            //Headerへのコピー
            Header = new Header(header);

            //Bodyへのコピー
            Body = new byte[body.Length];
            Buffer.BlockCopy(body, 0, Body, 0, body.Length);
        }

        public bool Save(string fileName) {
            if (Body.Length == 0)
                return false;
            try {
                //ディレクトリが存在しない場合は、作成する
                string directory = Path.GetDirectoryName(fileName);
                if (directory != null && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write);

                //バージョン記録
                WriteLine(fs, "V100");

                WriteLine(fs, Uri);
                WriteLine(fs, HostName);
                WriteLong(fs, Port);
                WriteLong(fs, LastModified.Ticks);
                WriteLong(fs, CreateDt.Ticks);
                WriteLong(fs, LastAccess.Ticks);
                WriteLong(fs, Expires.Ticks);
                //if(Body.Length == 0) {
                //    Msg.Show(MSG_KIND.ERROR,"設計エラー OneCache.Save()");
                //}

                //Header保存
                byte[] header = Header.GetBytes();
                WriteLong(fs, header.Length);
                fs.Write(header, 0, header.Length);

                //Body保存
                WriteLong(fs, Body.Length);
                fs.Write(Body, 0, Body.Length);

                fs.Close();
                return true;
            } catch {
                return false;
            }
        }

        public bool Read(string fileName) {
            if (File.Exists(fileName)) {
                try {
                    var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);

                    //バージョン復元
                    string verStr = ReadLine(fs);
                    if (verStr != "V100")
                        return false;//バージョンミスマッチ

                    Uri = ReadLine(fs);
                    HostName = ReadLine(fs);
                    Port = (int)ReadLong(fs);
                    LastModified = new DateTime(ReadLong(fs));
                    CreateDt = new DateTime(ReadLong(fs));
                    LastAccess = new DateTime(ReadLong(fs));
                    Expires = new DateTime(ReadLong(fs));

                    //ヘッダ復元
                    long len = ReadLong(fs);//データサイズ
                    var buf = new byte[len];
                    fs.Read(buf, 0, (int)len);//データ本体
                    Header = new Header(buf);

                    //Body復元
                    len = ReadLong(fs);//データサイズ
                    Body = new byte[len];
                    fs.Read(Body, 0, (int)len);//データ本体

                    fs.Close();

                    return true;
                } catch (Exception){
                }
            }
            return false;
        }

        string ReadLine(FileStream fs) {
            var sb = new StringBuilder();
            while (true) {
                var c = (char)fs.ReadByte();
                if (c == '\n')
                    break;
                sb.Append(c);
            }
            return sb.ToString();
        }

        long ReadLong(FileStream fs) {
            return Convert.ToInt64(ReadLine(fs));
        }

        bool WriteLine(FileStream fs, string str) {
            try {
                foreach (char c in str)
                    fs.WriteByte((byte)c);
                fs.WriteByte((byte)'\n');
                return true;
            } catch {
                return false;
            }
        }

        bool WriteLong(FileStream fs, long n) {
            return WriteLine(fs, n.ToString());
        }
    }
}
