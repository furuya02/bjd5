using System.Collections.Generic;
using System.Linq;
using Bjd.option;
using Bjd.util;

namespace FtpServer{


    public class ListMount : ListBase<OneMount>{

        public ListMount(IEnumerable<OneDat> dat){
            if (dat != null){
                //有効なデータだけを対象にする
                foreach (var o in dat.Where(o => o.Enable)){
                    Add(o.StrList[0], o.StrList[1]);
                }
            }
        }

        public void Add(string fromFolder, string toFolder){
            Ar.Add(new OneMount(fromFolder, toFolder));
        }
    }
}