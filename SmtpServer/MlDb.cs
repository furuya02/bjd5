using System;
using System.IO;
using Bjd;

namespace SmtpServer {
    class MlDb {
        public string Dir { get; private set; }
        public bool Status { get; private set; }//初期化の成否
        readonly Logger logger;
        public MlDb(Kernel kernel, Logger logger, string dir) {
            this.logger = logger;
            this.Dir = "";
            Status = false;
            if (dir != null && dir != "") {
                dir = kernel.Env(dir);//環境変数の展開
                //ディレクトリが存在しない場合は作成する
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                if (Directory.Exists(dir))
                    Status = true;//ステータス（初期化完了）
                this.Dir = dir;
            }
        }
        virtual public Mail Read(string name, int no) {
            var fileName = string.Format("{0}\\{1}.{2:D5}.eml", Dir, name, no);
            if (File.Exists(fileName)) {
                var mail = new Mail(logger);
                if (mail.Read(fileName)) {
                    return mail;
                }
            }
            logger.Set(LogKind.Error, null, 44, fileName);
            return null;
        }
        virtual public bool Save(string name, int no, Mail mail) {
            var fileName = string.Format("{0}\\{1}.{2:D5}.eml", Dir, name, no);
            if (!mail.Save(fileName)) {
                logger.Set(LogKind.Error, null, 33, fileName);
                return false;
            }
            return true;
        }
        //連番の取得
        virtual public int GetNo(string mlName) {
            lock (this) {
                //連番の記憶ファイル
                string fileName = string.Format("{0}\\{1}.no", Dir, mlName);
                int no = 0;
                if (File.Exists(fileName)) {
                    try {
                        using (StreamReader sr = new StreamReader(fileName)) {
                            string str = sr.ReadToEnd();
                            sr.Close();
                            no = Convert.ToInt32(str);
                        }
                    } catch {
                        no = 0;
                    }
                }
                return no;
            }
        }
        virtual public int IncNo(string mlName) {
            var no = GetNo(mlName);
            no++;
            lock (this) {
                //連番の記憶ファイル
                string fileName = string.Format("{0}\\{1}.no", Dir, mlName);
                using (StreamWriter sw = new StreamWriter(fileName)) {
                    sw.Write(no);
                    sw.Close();
                }
            }
            return no;
        }
    }
}
