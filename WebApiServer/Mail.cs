using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WebApiServer {
    class Mail {
        public string Exec(Method method,string cmd, Dictionary<string, string> param){
            if (cmd == "message"){
                return Message();
            }

            return JsonConvert.SerializeObject(new Error("Not Implemented", "unknown", 404)); 
        }

        public string Message(){
            return "TEST";
        }
    }


}
