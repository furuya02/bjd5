using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Bjd;
using Bjd.mail;
using Bjd.option;
using Newtonsoft.Json;

namespace WebApiServer {
    class Mail{
        private MailBox _mailBox;
        private string _mailQueue = "";

        public Mail(Kernel kernel){
            _mailBox = kernel.MailBox;
            _mailQueue = kernel.ProgDir() + "\\MailQueue";
        }
        
        public string Exec(Method method,string cmd, Dictionary<string, string> param){
            if (cmd == "message"){
                return Message();
            }

            return JsonConvert.SerializeObject(new Error("Not Implemented", "unknown", 404)); 
        }

        public string Message(){

            var ar = new List<OneMail>();

            //各ユーザのメール取得           
            foreach (var user in _mailBox.UserList){
                var folder = string.Format("{0}\\{1}", _mailBox.Dir, user);
                var files = Directory.GetFiles(folder, "DF_*");
                Array.Sort(files);//ファイル名をソート（DF_名は作成日付なので、結果的に日付順となる）FAT32対応
                foreach (var fileName in files){
                    var oneMail = new OneMail(user,fileName);
                    ar.Add(oneMail);
                }
            }
            //メールキューのメール取得           
            {
                var files = Directory.GetFiles(_mailQueue, "DF_*");
                Array.Sort(files);//ファイル名をソート（DF_名は作成日付なので、結果的に日付順となる）FAT32対応
                foreach (var fileName in files) {
                    var oneMail = new OneMail("mailQueue", fileName);
                    ar.Add(oneMail);
                }
            }
            return "TEST";
        }
    }


}
