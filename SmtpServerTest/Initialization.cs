using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Bjd;
using SmtpServer;
using BjdTest;

namespace SmtpServerTest {
    class Initialization2:IDisposable {
        //public string TmpDir2 { get; private set; }
        public List<string> Docs { get; private set; }
        public MailSave MailSave { get; private set; }
        public Kernel Kernel { get; private set; }
        public Logger Logger { get; private set; }
        public MlAddr MlAddr { get; private set; }
        public Dat MemberList { get; private set; }
        public MlOption MlOption { get; private set; }
        public MlMailDb MlMailDb { get; private set; }
        //public MlUserList MlUserList { get; private set; }
        //public MlSubject MlSubject { get; private set; }
        public MlSender MlSender { get; private set; }
        string mlName = "1ban";
        //string manageDir = "";
        List<string> domainList;
        public string MlName { get { return mlName; } }

        
        public Ml Ml { get; private set; }
        
        public Initialization2() {

            var tsDir = new TsDir();
            //var tsOption = new TsOption(tsDir);
            //var manageDir = tsDir.Src + "\\TestDir";
            
            //TmpDir2 = string.Format("{0}/../../TestDir", Directory.GetCurrentDirectory());
            var optionDef = tsDir.Src + "\\Option.def";


            //Docs
            Docs = new List<string>();
            var lines = File.ReadAllLines(optionDef, Encoding.GetEncoding(932));
            foreach (MlDocKind docKind in Enum.GetValues(typeof(MlDocKind))) {
                var tag = string.Format("MEMO=Ml\b{0}Document=", docKind.ToString().ToLower());
                bool hit = false;
                foreach (var l in lines) {
                    if (l.IndexOf(tag) == 0) {
                        Docs.Add(l.Substring(tag.Length));
                        hit = true;
                        break;
                    }
                }
                if (!hit) {
                    Docs.Add("");
                }
            }

            Kernel = new Kernel(null, null, null, null);
            Logger = Kernel.CreateLogger("LOG", true, null);
            domainList = new List<string>() { "example.com" };
            MlAddr = new MlAddr(mlName, domainList);
            var mailQueue = new MailQueue(tsDir.Src + "TestDir");
            var oneOption = new Option(Kernel,"","");
            var mailBox = new MailBox(Kernel, oneOption);
            MailSave = new MailSave(Kernel,mailBox,Logger,mailQueue,"",domainList);
            MlOption = CreateMlOption();
            //MlUserList = CreateMlUsers();

            Ml = new Ml(Kernel, Logger, MailSave, MlOption, mlName, domainList);
            //３０件のメールを保存
            for (int i = 0; i < 30; i++) {
                var mail = new Mail(null);
                mail.Init(Encoding.ASCII.GetBytes("\r\n"));//区切り行(ヘッダ終了)
                mail.AddHeader("subject", string.Format("[{0}:{1:D5}]TITLE", mlName, i + 1));
                mail.Init(Encoding.ASCII.GetBytes("1\r\n"));//本文
                mail.Init(Encoding.ASCII.GetBytes("2\r\n"));//本文
                mail.Init(Encoding.ASCII.GetBytes("3\r\n"));//本文

                Ml.Save(mail);
            }
            
        }
        public void Dispose(){
            Ml.Dispose();

            Ml.Remove();//テストのため作成したメールはここですべて削除する
        }

        public Ml CreateMl() {
            return new Ml(Kernel, Logger, MailSave,MlOption,mlName,domainList);
        }

        MlUserList CreateMlUsers2() {
            var kernel = new Kernel(null, null, null, null);
            var logger = new Logger(kernel, "", false, null);

            //参加者
            MemberList = new Dat();
            bool manager = false;
            MemberList.Add(true, string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", "USER1", "user1@example.com", manager, true, true, "")); //読者・投稿
            MemberList.Add(true, string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", "USER2", "user2@example.com", manager, true, false, ""));//読者 　×
            MemberList.Add(false, string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", "USER6", "user6@example.com", manager, false, true, ""));//×　　投稿 (Disable)
            MemberList.Add(true, string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", "USER3", "user3@example.com", manager, false, true, ""));//×　　投稿
            manager = true;//管理者
            MemberList.Add(true, string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", "ADMIN", "admin@example.com", manager, false, true, "123"));//×　　投稿
            MemberList.Add(true, string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", "ADMIN2", "admin2@example.com", manager, true, true, "456"));//読者　　投稿
            MemberList.Add(false, string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", "ADMIN3", "admin3@example.com", manager, true, true, "789"));//読者　　投稿 (Disable)

            var mlUserList = new MlUserList(MemberList);

            return mlUserList;
        }

        MlOption CreateMlOption() {
            const int maxSummary = 20;
            const int maxGet = 20;
            const bool autoRegistration = true; //自動登録
            var tsDir = new TsDir();
            const int titleKind = 1;

            var memberList = new Dat();
            memberList.Add(true, string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", "USER1", "user1@example.com", false, true, true, "")); //読者・投稿
            memberList.Add(true, string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", "USER2", "user2@example.com", false, true, false, ""));//読者 　×
            memberList.Add(false, string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", "USER6", "user6@example.com", false, false, true, ""));//×　　投稿 (Disable)
            memberList.Add(true, string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", "USER3", "user3@example.com", false, false, true, ""));//×　　投稿
            memberList.Add(true, string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", "ADMIN", "admin@example.com", true, false, true, "123"));//×　　投稿
            memberList.Add(true, string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", "ADMIN2", "admin2@example.com", true, true, true, "456"));//読者　　投稿
            memberList.Add(false, string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", "ADMIN3", "admin3@example.com", true, true, true, "789"));//読者　　投稿 (Disable)

            return new MlOption(null,maxSummary, maxGet, autoRegistration,titleKind,Docs,tsDir.Src + "\\TestDir",memberList);
        }
    }
}

