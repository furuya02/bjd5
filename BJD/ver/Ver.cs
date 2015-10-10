using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.IO;
using System.Windows.Forms;
using Bjd.util;

namespace Bjd {
    //バージョン管理クラス
    public class Ver {
        //Kernel kernel;
        readonly List<string> _ar = new List<string>();
        public Ver() {
            //this.kernel = kernel;

            string[] files = Directory.GetFiles(Path.GetDirectoryName(Application.ExecutablePath), "*.dll");
            foreach (string file in files) {
                _ar.Add(Path.GetFileNameWithoutExtension(file));
            }
            _ar.Sort();

        }
        public string Version() {
            return Application.ProductVersion;
        }

        string FullPath(string name) {
            return string.Format("{0}\\{1}.dll", Path.GetDirectoryName(Application.ExecutablePath), name);
        }
        

        //ファイルの最終更新日時を文字列で取得する
        string FileDate(string fileName) {
            var  info = new FileInfo(fileName);
            return info.LastWriteTime.Ticks.ToString();
        }
        //ファイル日付の検証
        bool CheckDate(string ticks, string fileName) {
            var dt = new DateTime(Convert.ToInt64(ticks));
            var info = new FileInfo(fileName);
            if (info.LastWriteTime == dt)
                return true;
            return false;
        }

        //【バージョン情報】（リモートサーバがクライアントに送信する）
        public string VerData() {
            var sb = new StringBuilder();

            sb.Append(Version() + "\t");//バージョン文字列
            sb.Append(FileDate(Application.ExecutablePath) + "\t");//BJD.EXEのファイル日付
            foreach (var name in _ar) {
                sb.Append(name+"\t");//DLL名
                sb.Append(FileDate(FullPath(name)) + "\t");//DLLのファイル日付
            }
            return sb.ToString();
        }
        
        //【バージョン情報の確認】（受け取ったバージョン情報を検証する）
        public bool VerData(string verDataStr) {
            var match = true;
            var sb = new StringBuilder();
            var tmp = verDataStr.Split(new[]{'\t'},StringSplitOptions.RemoveEmptyEntries);
            var c = 0;
            
            //バージョン文字列
            var verStr = tmp[c++];
            if (verStr != Version()) {
                sb.Append(string.Format("\r\nA version does not agree. (Server:{0} Client:{1})", verStr,Version()));
                match = false;
            }

            //BJD.EXEのファイル日付
            var ticks = tmp[c++];
            //Ver5.7.0 解凍する際のアーカイバによってファイル日付が変化する可能性があるため、この確認は実施しない
            //if (!CheckDate(ticks, Application.ExecutablePath)) {
            //    sb.Append(string.Format("\r\n[BJD.EXE]  Timestamp is different"));
            //    match = false;
            //}

            for (; c < tmp.Length;c+=2) {
                var name = tmp[c];
                ticks = tmp[c+1];
                if (_ar.IndexOf(name) == -1) {//DLL名（存在確認）
                    sb.Append(string.Format("\r\n[{0}.dll] not found", name));
                    match = false;
                } else {//DLLのファイル日付確認
                    //Ver5.7.0 解凍する際のアーカイバによってファイル日付が変化する可能性があるため、この確認は実施しない
                    //if (!CheckDate(ticks, FullPath(name))) {
                    //    sb.Append(string.Format("\r\n[{0}.dll] Timestamp is different", name));
                    //    match = false;
                    //}
                }
            }

            if (!match) {
                Msg.Show(MsgKind.Error,"リモートクライアントを使用することはできません。\r\n" + sb);
            }
            return match;

        }
    }
}
