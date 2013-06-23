using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Bjd.ctrl;
using Bjd.option;

namespace Bjd.util{
    //ファイルを使用した設定情報の保存<br>
    //1つのデフォルト値ファイルを使用して2つのファイルを出力する<br>
    public class IniDb{
        private readonly String _fileIni;
        private readonly String _fileDef;
        private readonly String _fileTxt;

        public IniDb(String progDir, String fileName){
            _fileIni = progDir + "\\" + fileName + ".ini";
            _fileDef = progDir + "\\" + fileName + ".def";
            _fileTxt = progDir + "\\" + fileName + ".txt";
        }

        public string Path{
            get{
                return _fileIni;
            }
        }

        private string CtrlType2Str(CtrlType ctrlType){
            switch (ctrlType){
                case CtrlType.CheckBox:
                    return "BOOL";
                case CtrlType.TextBox:
                    return "STRING";
                case CtrlType.Hidden:
                    return "HIDE_STRING";
                case CtrlType.ComboBox:
                    return "LIST";
                case CtrlType.Folder:
                    return "FOLDER";
                case CtrlType.File:
                    return "FILE";
                case CtrlType.Dat:
                    return "DAT";
                case CtrlType.Int:
                    return "INT";
                case CtrlType.AddressV4:
                    return "ADDRESS_V4";
                case CtrlType.BindAddr:
                    return "BINDADDR";
                case CtrlType.Font:
                    return "FONT";
                case CtrlType.Group:
                    return "GROUP";
                case CtrlType.Label:
                    return "LABEL";
                case CtrlType.Memo:
                    return "MEMO";
                case CtrlType.Radio:
                    return "RADIO";
                case CtrlType.TabPage:
                    return "TAB_PAGE";
            }
            throw new Exception("コントロールの型名が実装されていません OneVal::TypeStr()　" + ctrlType);
        }


        //１行を読み込むためのオブジェクト
        private class LineObject{
            public string NameTag { get; private set; }
            public string Name { get; private set; }
            public string ValStr { get; private set; }
            // public LineObject(CtrlType ctrlType, String nameTag, String name,String valStr) {
            public LineObject(String nameTag, String name, String valStr){
                // this.ctrlType = ctrlType;
                NameTag = nameTag;
                Name = name;
                ValStr = valStr;
            }
        }

        //解釈に失敗した場合はnullを返す
        private static LineObject ReadLine(String str){
            var index = str.IndexOf('=');
            if (index == -1){
                return null;
            }
            //		CtrlType ctrlType = str2CtrlType(str.substring(0, index));
            str = str.Substring(index + 1);
            index = str.IndexOf('=');
            if (index == -1){
                return null;
            }
            var buf = str.Substring(0, index);
            var tmp = buf.Split('\b');
            if (tmp.Length != 2){
                return null;
            }
            var nameTag = tmp[0];
            var name = tmp[1];

            var valStr = str.Substring(index + 1);
            return new LineObject(nameTag, name, valStr);
        }

        private bool Read(String fileName, String nameTag, ListVal listVal){
            var isRead = false;
            if (File.Exists(fileName)){
                var lines = File.ReadAllLines(fileName, Encoding.GetEncoding(932));
              
                foreach (var s in lines){
                    var o = ReadLine(s);
                    if (o != null){
                        if (o.NameTag == nameTag || o.NameTag == nameTag+"Server"){
                            var oneVal = listVal.Search(o.Name);

                            //Ver5.9.2 過去バージョンのOption.ini読み込みへの対応
                            //ProxyPop3 拡張設定
                            if (o.NameTag == "ProxyPop3Server" && o.Name == "specialUser") {
                                oneVal = listVal.Search("specialUserList");
                            }

                            //Ver5.8.8 過去バージョンのOption.ini読み込みへの対応
                            if (oneVal == null){
                                if (o.Name == "nomalFileName"){
                                    oneVal = listVal.Search("normalLogKind");
                                } else if (o.Name == "secureFileName"){
                                    oneVal = listVal.Search("secureLogKind");
                                    //Ver5.9.2
                                } else if (o.Name == "LimitString"){
                                    oneVal = listVal.Search("limitString");
                                } else if (o.Name == "UseLimitString"){
                                    oneVal = listVal.Search("useLimitString");
                                } else if (o.Name == "EnableLimitString"){
                                    oneVal = listVal.Search("isDisplay");
                                } else if (o.Name == "useLog"){
                                    oneVal = listVal.Search("useLogFile");
                                }
                            }
                            
                            
                            if (oneVal != null){
                                if (!oneVal.FromReg(o.ValStr)){
                                    if (o.ValStr != ""){
                                        //Ver5.8.4コンバートしてみる
                                        if (oneVal.FromRegConv(o.ValStr)) {

                                        }
                                    }
                                }
                                isRead = true; // 1件でもデータを読み込んだ場合にtrue
                            }
                        }
                    }
                }
            }

            return isRead;
        }

        //ファイルの削除
        public void Delete() {
            if (File.Exists(_fileTxt)) {
                File.Delete(_fileTxt);
            }
            if (File.Exists(_fileIni)){
                File.Delete(_fileIni);
            }
        }



        // 読込み
        public void Read(string nameTag, ListVal listVal){
            var isRead = Read(_fileIni, nameTag, listVal);
            if (!isRead){
                //１件も読み込まなかった場合
                //defファイルには、Web-local:80のうちのWeb (-の前の部分)がtagとなっている
                var n = nameTag.Split('-')[0];
                Read(_fileDef, n, listVal); //デフォルト設定値を読み込む
            }
        }


        // 保存
        public void Save(String nameTag, ListVal listVal){
            // Ver5.0.1 デバッグファイルに対象のValListを書き込む
            for (var i = 0; i < 2; i++){
                var target = (i == 0) ? _fileIni : _fileTxt;
                var isSecret = i != 0;

                // 対象外のネームスペース行を読み込む
                var lines = new List<string>();
                if (File.Exists(target)){
                    foreach (var s in File.ReadAllLines(target, Encoding.GetEncoding(932))){
                        LineObject o;
                        try{
                            o = ReadLine(s);
                            // nameTagが違う場合、listに追加
                            if (o.NameTag != nameTag) {
                                //Ver5.8.4 Ver5.7.xの設定を排除する
                                var index = o.NameTag.IndexOf("Server");
                                if (index != -1 && index == o.NameTag.Length - 6){
                                    // ～～Serverの設定を削除
                                } else{
                                    lines.Add(s);
                                }
                                
                            }
                        }catch{
                            //TODO エラー処理未処理
                        }
                    }
                }
                // 対象のValListを書き込む
                //foreach (var o in listVal.GetList(null)){
                foreach (var o in listVal.GetSaveList(null)){
                    // nullで初期化され、実行中に一度も設定されていない値は、保存の対象外となる
                    //if (o.Value == null){
                    //    continue;
                    //}

                    // データ保存の必要のない型は省略する（下位互換のため）
                    var ctrlType = o.OneCtrl.GetCtrlType();
                    if (ctrlType == CtrlType.TabPage || ctrlType == CtrlType.Group || ctrlType == CtrlType.Label){
                        continue;
                    }

                    var ctrlStr = CtrlType2Str(ctrlType);
                    lines.Add(string.Format("{0}={1}\b{2}={3}", ctrlStr, nameTag, o.Name, o.ToReg(isSecret)));
                }
                File.WriteAllLines(target, lines.ToArray(), Encoding.GetEncoding(932));
            }
        }

        // 設定ファイルから"lang"の値を読み出す
        public bool IsJp(){
            var listVal = new ListVal{
                new OneVal("lang", 0, Crlf.Nextline,
                           new CtrlComboBox("Language", new[]{"Japanese", "English"}, 80))
            };
            Read("Basic", listVal);
            var oneVal = listVal.Search("lang");
            return ((int) oneVal.Value == 0);

        }
    }
}

