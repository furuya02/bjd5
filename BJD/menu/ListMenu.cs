using Bjd.util;

namespace Bjd.menu {
    public class ListMenu : ListBase<OneMenu>{

        public OneMenu Add(OneMenu o){
            Ar.Add(o);
            return o;
        }

        public OneMenu Insert(int index, OneMenu o){
            Ar.Insert(index, o);
            return o;
        }
    }
}
