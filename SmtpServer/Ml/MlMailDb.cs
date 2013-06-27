using System;
using System.IO;
using Bjd.log;
using Bjd.mail;

namespace SmtpServer {
    //**************************************************************
    // １ML分の蓄積メール及びカウンタを処理するクラス
    // managerDirの下にmlNameでフォルダを作成して作業領域とする
    //**************************************************************
    class MlMailDb : IDisposable {
        public bool Status { get; private set; }//初期化の成否

        readonly string _dir;
        readonly Logger _logger;
        public MlMailDb(Logger logger, string manageDir, string mlName) {
            
            Status = false;
            _logger = logger;
           
            _dir = string.Format("{0}\\{1}", manageDir, mlName);
            if (!Directory.Exists(_dir)) {
                try {
                    Directory.CreateDirectory(_dir);
                } catch {
                    if(logger!=null)
                        logger.Set(LogKind.Error, null,31, _dir);
                    return;
                }
            }
            Status = true;
        }
        public void Dispose() {
            //中身が空だったら、作業フォルダ自身も削除する
            if (Count() == 0) {
                if(Directory.Exists(_dir)){
                    Directory.Delete(_dir,true);
                }
            }
        }

        public bool Save(Mail mail) {

            //ディレクトリが存在しない場合は作成する
            if (!Directory.Exists(_dir)) {
                Directory.CreateDirectory(_dir);
            }

            var fileName = MailFile(Count(true));//インクリメントした連番を取得する
            if (!mail.Save(fileName)) {
                _logger.Set(LogKind.Error, null, 9000059, mail.GetLastError());
                _logger.Set(LogKind.Error, null, 33, fileName);
                return false;
            }
            return true;
        }
        public Mail Read(int no) {
            //ディレクトリが存在しない場合は失敗となる
            if (!Directory.Exists(_dir)) {
                return null;
            }
                
            var fileName = MailFile(no);
            if (File.Exists(fileName)) {
                var mail = new Mail();
                if (mail.Read(fileName)) {
                    return mail;
                }
            }
            _logger.Set(LogKind.Error, null, 44, fileName);
            return null;
        }
        public bool Remove() {
            if (Directory.Exists(_dir)) {
                var dirInfo = new DirectoryInfo(_dir);
                var files = dirInfo.GetFiles("*.eml");
                foreach (var f in files) {
                    try {
                        File.Delete(f.FullName);
                    } catch {
                        return false;
                    }
                }
                File.Delete(NoFile());//連番ファイルを削除する
            }
            return true;
        }
        //連番ファイル
        private string NoFile() {
            return string.Format("{0}\\number", _dir);
        }
        //メールファイル
        private string MailFile(int no) {
            return string.Format("{0}\\{1:D5}.eml", _dir,no);
        }
        //外部から連番を尋ねられた場合は、現在の連番だけを返す
        public int Count() {
            return Count(false);//インクリメントしない
        }
        //連番の取得(inc=trueの場合、連番をインクリメントして保存する)
        private int Count(bool inc) {
            //ディレクトリが存在しない場合は0となる
            if (!Directory.Exists(_dir)) {
                return 0;
            }
            lock (this) {
                //連番の記憶ファイル
                var fileName = NoFile();
                var n = 0;
                if (File.Exists(fileName)) {
                    try {
                        using (var sr = new StreamReader(fileName)) {
                            string str = sr.ReadToEnd();
                            sr.Close();
                            n = Convert.ToInt32(str);
                        }
                    } catch {
                        n = 0;
                    }
                }
                if (inc || n == 0) {
                    if(inc)
                        n++;
                    using (var sw = File.CreateText(fileName)) {
                        sw.WriteLine(n.ToString());
                        sw.Close();
                    }
                }
                return n;
            }
        }
    }

}
