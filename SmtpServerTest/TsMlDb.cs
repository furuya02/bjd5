using System.IO;
using Bjd;
using SmtpServer;

namespace SmtpServerTest {
    //MlDbのモックオブジェクト
    class TsMlDb : MlDb {
        int no = 0;
        readonly string ext = "testDb.eml";
        readonly string tmpDir;
        public TsMlDb(Kernel kernel, string tmpDir)
            : base(kernel, null, tmpDir) {
            this.tmpDir = tmpDir;
            for (int i = 0; i < 300; i++) {
                Clear(i);
            }
        }

        //モック専用
        public void Clear(int no) {
            var f = string.Format("{0}\\{1}.{2}", tmpDir, no, ext);
            if (File.Exists(f)) {
                File.Delete(f);
            }
        }

        //モック専用
        public void SetNo(int no) {
            this.no = no;
        }

        //モック専用
        public void SetMail(Mail mail, int no) {
            var f = string.Format("{0}\\{1}.{2}", tmpDir, no, ext);
            mail.Save(f);
        }

        override public int GetNo(string mlName) {
            return no;
        }

        override public int IncNo(string mlName) {
            no++;
            return no;
        }

        override public Mail Read(string name, int no) {
            var f = string.Format("{0}\\{1}.{2}", tmpDir, no, ext);
            if (File.Exists(f)) {
                var mail = new Mail(null);
                mail.Read(f);
                return mail;
            }
            return null;
        }

        override public bool Save(string name, int no, Mail mail) {
            return true;
        }
    }
}
