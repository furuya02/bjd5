using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        private Config _config;

        public SvMail(Kernel kernel){
            var op = (Option) kernel.ListOption.Get("WebApi");
            if (op != null){
                _config = op.Config;
            }
            _mailBox = kernel.MailBox;
            _mailQueue = kernel.ProgDir() + "\\MailQueue";
        }
        
        public string Exec(Method method,string cmd, Dictionary<string, string> param){
            if (cmd == "message"){
                return Message(method, param);
            } else if (cmd == "control"){
                return Control(method,param);

            }

            return JsonConvert.SerializeObject(new Error(501, "unknown command")); 
        }

        public string Control(Method method, Dictionary<string, string> param) {
            if (method == Method.Put){
                if (param.ContainsKey("service")){
                    var service = param["service"];
                    switch (service){
                        case "start":
                            _config.Service = true;
                            return JsonConvert.SerializeObject(new Error(200, "start service [control]"));
                        case "stop":
                            _config.Service = false;
                            return JsonConvert.SerializeObject(new Error(200, "stop service [control]"));
                        default:
                            return JsonConvert.SerializeObject(new Error(504, "service = [start,stop] [control]"));
                    }
                }
                return JsonConvert.SerializeObject(new Error(503, "unknown parameter[control]"));
            }
            return JsonConvert.SerializeObject(new Error(502, "unknown method [control]"));
        }

        public string Message(Method method, Dictionary<string, string> param) {
            var owner = new List<string>();
            if (param.ContainsKey("owner")) {
                var s = param["owner"];
                owner = s.Split(',').ToList();
            }
            var limit = 0;
            if (param.ContainsKey("limit")) {
                var s = param["limit"];
                if (!Int32.TryParse(s, out limit)) {
                    limit = 0;
                }
            }

            if (method == Method.Get){

                var fields = new List<string>();
                if (param.ContainsKey("fields")){
                    var s = param["fields"];
                    fields = s.Split(',').ToList();
                }

                dynamic json = new ExpandoObject();
                var data = new List<object>();
                foreach (var o in GetMailList(owner, limit)){
                    dynamic tmp = new ExpandoObject();
                    tmp = AddFields(o, fields, tmp);
                    data.Add(tmp);
                }
                json.data = data;
                return JsonConvert.SerializeObject(json);
            } else if (method == Method.Delete){
                int count = 0;
                foreach (var o in GetMailList(owner, limit)){
                    if (o.Owner == "mailQueue"){
                        DeleteFile(_mailQueue, (string) o.Get("filename"));
                        count++;
                    } else{
                        DeleteFile(_mailBox.Dir + "\\" + o.Owner, (string) o.Get("filename"));
                        count++;
                    }
                }
                return JsonConvert.SerializeObject(new Error(200, string.Format("{0} mails deleted",count)));

            }
            return JsonConvert.SerializeObject(new Error(502, "unknown method [message]"));
        }

        void DeleteFile(String dir,String filename){
            var path = string.Format("{0}\\MF_{1}", dir,filename);
            if (File.Exists(path)) {
                File.Delete(path);
            }
            path = string.Format("{0}\\DF_{1}", dir,filename);
            if (File.Exists(path)) {
                File.Delete(path);
            }
        }



        private dynamic AddFields(OneMail oneMail, List<String> fields, dynamic tmp){
            if (fields.Count == 0){
                fields.Add("subject");
            }

            var p = tmp as IDictionary<String, object>;
            foreach (var field in fields){
                p[field] = oneMail.Get(field);
            }
            return tmp;
        }

        //メールの取得
        List<OneMail> GetMailList(List<string> owner,int limit){
            var ar = new List<OneMail>();
            //各ユーザのメール取得           
            foreach (var user in _mailBox.UserList) {
                if (owner.Count == 0 || owner.IndexOf(user) != -1){
                    var folder = string.Format("{0}\\{1}", _mailBox.Dir, user);
                    var files = Directory.GetFiles(folder, "DF_*");
                    foreach (var fileName in files){
                        if (limit == 0 || ar.Count < limit){
                            var oneMail = new OneMail(user, fileName);
                            ar.Add(oneMail);
                        }
                    }
                }
            }
            //メールキューのメール取得           
            {
                if (owner.Count==0 || owner.IndexOf("mqueue")!=-1){
                    var files = Directory.GetFiles(_mailQueue, "DF_*");
                    foreach (var fileName in files){
                        if (limit == 0 || ar.Count < limit){
                            var oneMail = new OneMail("mailQueue", fileName);
                            ar.Add(oneMail);
                        }
                    }
                }
            }
            //時刻デーソート

            ar.Sort((a, b) => ((string)a.Get("date")).CompareTo(((string)b.Get("date"))));
            return ar;
        } 
    }


}
