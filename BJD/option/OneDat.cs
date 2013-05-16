using System;
using System.Collections.Generic;
using System.Text;

namespace Bjd.option{
    public class OneDat : ValidObj, IDisposable{
        public bool Enable { get; private set; }
        public List<string> StrList { get; private set; }
	    private readonly bool[] _isSecretList;

        private OneDat() {
		    // デフォルトコンストラクタの隠蔽
        }

        public OneDat(bool enable, string[] list, bool[] isSecretList){
            if (list == null) {
                throw new ValidObjException("引数に矛盾があります  list=null");
            }
            if (isSecretList == null) {
                throw new ValidObjException("引数に矛盾があります  isSecretList == null");
            }
            if (list.Length != isSecretList.Length) {
                throw new ValidObjException("引数に矛盾があります  list.length != isSecretList.length");
            }

            Enable = enable;
            _isSecretList = new bool[list.Length];
            StrList = new List<string>();
            for (int i = 0; i < list.Length; i++) {
                StrList.Add(list[i]);
                _isSecretList[i] = isSecretList[i];
            }
        }

        public string ToReg(bool isSecret){
            var sb = new StringBuilder();
            if (!Enable) {
                sb.Append("#");
            }
            for (int i = 0; i < StrList.Count; i++) {
                sb.Append('\t');
                if (isSecret && _isSecretList[i]) { // シークレットカラム
                    sb.Append("***");
                } else {
                    sb.Append(StrList[i]);
                }
            }
            return sb.ToString();
        }

        public bool FromReg(string str){
            if (str == null) {
                return false;
            }
            string[] tmp = str.Split('\t');

            //カラム数確認
            if (tmp.Length != StrList.Count + 1) {
                return false;
            }

            //enableカラム
            switch (tmp[0]) {
                case "":
                    Enable = true;
                    break;
                case "#":
                    Enable = false;
                    break;
                default:
                    return false;
            }
            //以降の文字列カラム
            StrList = new List<String>();
            for (var i = 1; i < tmp.Length; i++) {
                StrList.Add(tmp[i]);
            }
            return true; 
        }

        protected override void Init() {
            StrList.Clear();
        }
        
        // toRegと誤って使用しないように注意
        public override string ToString(){
            return "ERROR";
        }

        public void Dispose(){
        }
    }
}
