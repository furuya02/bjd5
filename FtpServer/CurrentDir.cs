using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bjd.util;

namespace FtpServer{

    //カレントディレクトリ
    //プログラム内部で保持している現在のディレクトリ（\\表記）
    //最後は必ず\\になるように管理されている
    public class CurrentDir{
        string _current = "";

        //ユーザのホームディレクトリ
        readonly string _homeDir;

        //仮想フォルダ関連
        readonly ListMount _listMount; //設定一覧
        OneMount _oneMount; //仮想フォルダ外にいる場合 null

        //ホームディレクトリで初期化される
        public CurrentDir(string homeDir, ListMount listMount){
            _listMount = listMount;
            if (homeDir[homeDir.Length - 1] != '\\'){
                _homeDir = homeDir + "\\";
            } else{
                _homeDir = homeDir;
            }
            _current = _homeDir;
        }

        //ディレクトリ変更(変更の階層は１つのみ)
        private bool Cwd1(string name){
            const bool isDir = true;
            if (_oneMount == null){
                var path = CreatePath(_current, name, isDir);
                //矛盾が発生した場合は、nullとなる
                if (path != null){
                    //ホームディレクトリ階層下のディレクトリへの移動のみ許可
                    if (path.IndexOf(_homeDir) == 0){
                        //ホームディレクトリより上位のディレクトリへの移動は許可しない
                        if (_homeDir.Length <= path.Length){
                            //ディレクトリの存在確認          
                            if (Directory.Exists(path)){
                                //if (Directory.Exists(path)) {
                                _current = path;
                                return true;
                            }
                        }
                    }
                }
                //仮想フォルダへの移動を確認する
                foreach (var a in _listMount){
                    if (a.IsToFolder(_current)){
                        if (string.Format("{0}{1}\\", _current, a.Name) == path){
                            _current = a.FromFolder + "\\";
                            _oneMount = a;
                            return true;
                        }
                    }
                }
            } else{

                //パラメータから新しいディレクトリ名を生成する
                var path = CreatePath(_current, name, isDir);
                //矛盾が発生した場合は、nullとなる
                if (path != null){
                    //仮想フォルダのマウント先の階層下のディレクトリへの移動のみ許可
                    if (path.IndexOf(_oneMount.FromFolder) == 0){
                        //仮想フォルダのマウント先より上位のディレクトリへの移動は許可しない
                        if (_oneMount.FromFolder.Length <= path.Length){
                            //ディレクトリの存在確認
                            if (Directory.Exists(path)){
                                _current = path;
                                return true;
                            }
                        }
                    }
                }
                //仮想フォルダ外への移動の場合

                //マウント位置を追加して、仮想pathを生成する
                var s = _current.Substring(_oneMount.FromFolder.Length);
                path = string.Format("{0}\\{1}{2}", _oneMount.ToFolder, _oneMount.Name, s);

                path = CreatePath(path, name, isDir);
                //矛盾が発生した場合は、nullとなる
                if (path != null){
                    //ホームディレクトリ階層下のディレクトリへの移動のみ許可
                    if (path.IndexOf(_homeDir) == 0){
                        //ホームディレクトリより上位のディレクトリへの移動は許可しない
                        if (_homeDir.Length <= path.Length){
                            //ディレクトリの存在確認                  
                            if (Directory.Exists(path)){
                                _oneMount = null;
                                _current = path;
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        //ディレクトリ変更
        public bool Cwd(string paramStr){
            //失敗した場合、移動しない
            var keepCurrent = _current;
            var keepOneMount = _oneMount;

            //絶対パス指定の場合、いったんルートまで戻る
            if (paramStr[0] == '/'){
                if (!Cwd1("/")){
                    _current = keepCurrent;
                    _oneMount = keepOneMount;
                    return false;
                }
                paramStr = paramStr.Substring(1);
            }
            if (paramStr != ""){
                //１階層づつ処理する
                var tmp = paramStr.Split(new[]{'\\', '/'},StringSplitOptions.RemoveEmptyEntries);
                if (tmp.Any(name => !Cwd1(name))){
                    _current = keepCurrent;
                    _oneMount = keepOneMount;
                    return false;
                }
            }
            return true;
        }

        //カレントディレクトリ（表示テキスト表現）
        public string GetPwd(){
            string path = _current;
            //仮想フォルダ内の場合
            if (_oneMount != null){
                //マウント位置を追加して、仮想pathを生成する
                string s = _current.Substring(_oneMount.FromFolder.Length);
                path = string.Format("{0}\\{1}{2}", _oneMount.ToFolder, _oneMount.Name, s);

            }
            //パスのうちホームディレレクトリ部分以降が表示用のカレントディレクトリとなる
            string tmpStr = path.Substring(_homeDir.Length - 1);
            //表示用に\\を/に置き換える
            //tmpStr = Util.SwapChar('\\', '/', tmpStr);
            tmpStr = tmpStr.Replace('\\', '/');
            //ルートディレクト以外は、最後の/を出力しない
            if (tmpStr != "/"){
                tmpStr = tmpStr.Substring(0, tmpStr.Length - 1);
            }
            return tmpStr;
        }

        //ファイル一覧取得
        public List<string> List(string mask, bool wideMode){
            var ar = new List<string>();

            //ディレクトリ一覧取得 　*.* の場合は、*を使用する
            //???		string dirMask = (mask.equals("*.*")) ? "*" : mask;
            var di = new DirectoryInfo(_current);
            try{
                var dirs = di.GetDirectories(mask);
                foreach (var info in dirs) {
                    ar.Add(wideMode ? string.Format("drwxrwxrwx 1 nobody nogroup 0 {0} {1}", Util.DateStr(info.LastWriteTime), info.Name) : info.Name);
                }
            } catch (Exception){
                //Ver5.9.1 例外を処理するのみ
                return ar;

            }

            //仮想フォルダ外の場合、仮想フォルダがヒットした時、一覧に追加する
            if (_oneMount == null){
                foreach (var a in _listMount){
                    if (a.IsToFolder(_current)){
                        ar.Add(wideMode ? string.Format("drwxrwxrwx 1 nobody nogroup 0 {0} {1}", Util.DateStr(a.Info.LastWriteTime), a.Name) : a.Name);
                    }
                }
            }
            try{
                var files = di.GetFiles(mask);
                foreach (var info in files) {
                    ar.Add(wideMode ? string.Format("-rwxrwxrwx 1 nobody nogroup {0} {1} {2}", info.Length, Util.DateStr(info.LastWriteTime), info.Name) : info.Name);
                }
            } catch (Exception){
                //Ver5.9.1 例外を処理するのみ
                return ar;
            }
            return ar;

        }

        //string strの中の文字 c文字が連続している場合1つにする 
        public string MargeChar(char c, string str){
            var buf = new[]{c, c};
            var tmpStr = new string(buf);

            while (true){
                var index = str.IndexOf(tmpStr);
                if (index < 0){
                    break;
                }
                str = str.Substring(0, index) + str.Substring(index + 1);
            }
            return str;
        }

	    //パスの生成
	    //失敗した時nullが返される
	    //path 元のパス
    	//param 追加するパス
        //isDir ディレクトリかどうか
        public string CreatePath(string path, string param, bool isDir){
            //特別処理（後程リファクタリングの対象とする）
            if (path == null){
                path = _current;
            }

            // 「homeDir」「CurrentDir」及び「newDir」は '\\' 区切り 「param」は、'/'区切りで取り扱われる

            //paramの'/'を'\\'に変換する
            //param = Util.SwapChar('/', '\\', param);
            param = param.Replace('/', '\\');

            //paramの'\\'が連続している個所を１つにまとめる
            param = MargeChar('\\', param);

            if (isDir){
                //パラメータの最後が\\でない場合は付加する
                if (param[param.Length - 1] != '\\'){
                    param = param + "\\";
                }
            }

            //相対パスで指定されている場合
            var tmpPath = path + param;
            // フルパスで指定されている場合
            if (param[0] == '\\'){
                tmpPath = _homeDir + param;
            }

            //isDir==falseの時、ディレクトリ + ファイル名として処理する(FileName には..の処理をしない)
            var dir = tmpPath;
            var fileName = "";
            if (!isDir){
                var index = dir.LastIndexOf('\\');
                if (index < 0){
                    return null;
                }
                fileName = dir.Substring(index + 1);
                dir = dir.Substring(0, index + 1);
            }

            //Ver6.0.4
            dir = Path.GetDirectoryName(dir) + "\\";

            // .. を処理する
            while (true){
                int p1 = dir.IndexOf("..");
                if (p1 < 0){
                    break;
                }
                //..の前の最後の\\を消した文字列を作業対象にする
                var tmpStr = dir.Substring(0, p1 - 1);
                var p2 = tmpStr.LastIndexOf('\\');
                if (p2 < 0){
                    return null;
                }
                //最後の\\の前までを残す
                tmpStr = tmpStr.Substring(0, p2);
                if (dir.Length > p1 + 2){
                    tmpStr = tmpStr + dir.Substring(p1 + 2); //..の2文字を消して、..以降の文字列を戻す
                }
                dir = tmpStr;
            }
            var newPath = dir + fileName;

            //newDirの'\\'が連続している個所を１つにまとめる
            newPath = MargeChar('\\', newPath);
            //\\.\\は\\にまとめる
            //newPath = Util.SwapStr("\\.\\", "\\", newPath);
            newPath = newPath.Replace("\\.\\", "\\");
            //先頭の"."は削除する
            //if (newDir.Length >= 2 && newDir[0] == '.' && newDir[1] != '.') {
            //    newDir = newDir.Substring(1);
            //}
            
            //Ver6.0.3 ホームより上への移動は、無効とする
            if (_oneMount == null){
                if (newPath.IndexOf(_homeDir) == -1){
                    return null;
                }
            } else{
                var tmp = newPath.Replace(_oneMount.FromFolder, _oneMount.ToFolder);
                if (tmp.IndexOf(_homeDir) == -1) {
                    return null;
                }
            }
            return newPath;
        }
    }
}
