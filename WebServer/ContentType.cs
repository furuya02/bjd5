using System.IO;
using System.Linq;
using Bjd;
using Bjd.option;

namespace WebServer {
    public class ContentType {
        //readonly OneOption _oneOption;
        readonly Conf _conf;
        public ContentType(Conf conf) {
            //_oneOption = oneOption;
            _conf = conf;
        }
        // 拡張子から、Mimeタイプを取得する sendPath()で使用される
        public string Get(string fileName) {
            var ext = Path.GetExtension(fileName);

            //パラメータにドットから始まる拡張子が送られた場合、内部でドット無しに修正する
            if(ext!=null){
                if (ext.Length > 0 && ext[0] == '.')
                    ext = ext.Substring(1);
            }

            var mimeList = (Dat)_conf.Get("mime");
            //mimeListからextの情報を検索する
            string mimeType = null;
            if(ext!=null){
                foreach (var o in mimeList) {
                    if (o.StrList[0].ToUpper() != ext.ToUpper())
                        continue;
                    mimeType = o.StrList[1];
                    break;
                }
            }
            
            
            if(mimeType == null){
                //拡張子でヒットしない場合は、「.」での設定を検索する
                //DOTO  Dat2.Valの実装をやめたのので動作確認が必要
                //mimeType = (string)mimeList.Val(0,".",1);
                //mimeType = null;
                foreach (var o in mimeList.Where(o => o.StrList[0] == ".")){
                    mimeType = o.StrList[1];
                    break;
                }
                if(mimeType == null)
                    mimeType = "application/octet-stream";//なにもヒットしなかった場合
            }
            return mimeType;
        }
    }

}
