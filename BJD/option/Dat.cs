using System;
using System.Text;
using Bjd.ctrl;
using Bjd.util;

namespace Bjd.option{
    public class Dat : ListBase<OneDat>{

        private readonly bool[] _isSecretList;
        private readonly int _colMax;

        public Dat(CtrlType[] ctrlTypeList){
            //カラム数の初期化
            _colMax = ctrlTypeList.Length;
            //isSecretListの生成
            _isSecretList = new bool[_colMax];
            for (int i = 0; i < _colMax; i++){
                _isSecretList[i] = false;
                if (ctrlTypeList[i] == CtrlType.Hidden){
                    _isSecretList[i] = true;
                }
            }
        }

	    //文字列によるOneDatの追加
	    //内部で、OneDatの型がチェックされる
        public bool Add(bool enable, string str){
            if (str == null){
                return false; //引数にnullが渡されました
            }
            var list = str.Split('\t');
            if (list.Length != _colMax){
                return false; //カラム数が一致しません
            }
            OneDat oneDat;
            try{
                oneDat = new OneDat(enable, list, _isSecretList);
            }
            catch (ValidObjException){
                return false; // 初期化文字列が不正
            }
            Ar.Add(oneDat);
            return true;
        }

        //文字列化
        //isSecret 秘匿が必要なカラムを***に変換して出力する
        public String ToReg(bool isSecret){
            var sb = new StringBuilder();
            foreach (var o in Ar){
                if (sb.Length != 0){
                    sb.Append("\b");
                }
                sb.Append(o.ToReg(isSecret));
            }
            return sb.ToString();
        }

        //文字列による初期化
        public bool FromReg(String str){
            Ar.Clear();
            if (string.IsNullOrEmpty(str)){
                return false;
            }

            //Ver5.7.x以前のiniファイルをVer5.8用に修正する
            var tmp = Util.ConvValStr(str);
            str = tmp;

            // 各行処理
            String[] lines = str.Split('\b');
            if (lines.Length <= 0){
                return false; //"lines.length <= 0"
            }

            foreach (var l in lines){
                var s = l;
                //OneDatの生成
                OneDat oneDat;
                try{
                    oneDat = new OneDat(true, new String[_colMax], _isSecretList);
                }
                catch (ValidObjException){
                    return false;
                }

                if (s.Split('\t').Length != _isSecretList.Length + 1){
                    // +1はenableカラムの分
                    //カラム数の不一致
                    return false;
                }

                //fromRegによる初期化
                if (oneDat.FromReg(s)){
                    Ar.Add(oneDat);
                    continue; // 処理成功
                }
                //処理失敗
                Ar.Clear();
                return false;
            }
            return true;
        }
    }
}
