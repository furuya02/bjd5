using System;
using System.IO;
using System.Text;

namespace WebServer {
    class Cgi {
        public bool Exec(Target target, string param, Env env, WebStream inputStream, out WebStream outputStream, int cgiTimeout) {
            var cmd = target.CgiCmd;
            if(cmd==null){
                outputStream = new WebStream(-1);
                outputStream.Add(Encoding.ASCII.GetBytes("cmd==null"));
                return false;
            }
            if (cmd.ToUpper().IndexOf("COMSPEC") == 0) {
                cmd = Environment.GetEnvironmentVariable("ComSpec");
                // /cがウインドウクローズのために必要
                param = "/c " + param;
            } else if (cmd.ToUpper().IndexOf("CMD.EXE") != -1) {
                cmd = target.FullPath;
            } else {
                param = string.Format("{0} {1}", Path.GetFileName(target.FullPath), param);
            }

            var execProcess = new ExecProcess(cmd, param, Path.GetDirectoryName(target.FullPath),env);
            return execProcess.Start(inputStream,out outputStream);
        }
    }
}
