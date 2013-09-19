using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Dynamic;
using Bjd;
using Bjd.mail;
using Bjd.option;
using Newtonsoft.Json;

namespace WebApiServer {
    class SvMail{
        private readonly MailBox _mailBox;
        private readonly string _mailQueue = "";

        public SvMail(Kernel kernel){
            _mailBox = kernel.MailBox;
            _mailQueue = kernel.ProgDir() + "\\MailQueue";
        }
        
        public string Exec(Method method,string cmd, Dictionary<string, string> param){
            if (cmd == "message"){
                return Message(method,param);
            }

            return JsonConvert.SerializeObject(new Error("Not Implemented Command", "unknown", 404)); 
        }

        public string Message(Method method, Dictionary<string, string> param) {

            if (method == Method.Get){


                string owner = "";
                if (param.ContainsKey("owner")){
                    owner = param["owner"];
                }
                var  fields = new List<string>();
                if (param.ContainsKey("fields")) {
                    var s =param["fields"];
                    fields = s.Split(',').ToList();
                }

                dynamic json = new ExpandoObject();
                var data = new List<object>();
                foreach (var o in GetMailList(owner)){
                    dynamic tmp = new ExpandoObject();
                    tmp = AddFields(o,fields, tmp);
                    data.Add(tmp);
                }
                json.data = data;
                return JsonConvert.SerializeObject(json);
            }




            return "TEST";
        }

        private dynamic AddFields(OneMail oneMail, List<String> fields, dynamic tmp){
            foreach (var field in fields){
                switch (field){
                    case "subject":
                        tmp.subject = oneMail.Subject;
                        break;
                    case "date":
                        tmp.date = oneMail.Date;
                        break;
                    case "size":
                        tmp.size = oneMail.Size;
                        break;
                    case "from":
                        tmp.from = oneMail.From;
                        break;
                    case "to":
                        tmp.to = oneMail.To;
                        break;
                }
            }
            return tmp;
        }

        //メールの取得
        List<OneMail> GetMailList(string owner){
            var ar = new List<OneMail>();
            //各ユーザのメール取得           
            foreach (var user in _mailBox.UserList) {
                if (owner != "" && owner != user){
                        continue;
                }
                var folder = string.Format("{0}\\{1}", _mailBox.Dir, user);
                var files = Directory.GetFiles(folder, "DF_*");
                Array.Sort(files);//ファイル名をソート（DF_名は作成日付なので、結果的に日付順となる）FAT32対応
                foreach (var fileName in files) {
                    var oneMail = new OneMail(user, fileName);
                    ar.Add(oneMail);
                }
            }
            //メールキューのメール取得           
            {
                if (owner == "" || owner == "mqueue"){
                    var files = Directory.GetFiles(_mailQueue, "DF_*");
                    Array.Sort(files); //ファイル名をソート（DF_名は作成日付なので、結果的に日付順となる）FAT32対応
                    foreach (var fileName in files){
                        var oneMail = new OneMail("mailQueue", fileName);
                        ar.Add(oneMail);
                    }
                }
            }
            return ar;
        } 
    }


}
