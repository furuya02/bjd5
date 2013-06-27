using System;

namespace Bjd {
    public abstract class LastError {
        String _str = "";
        protected void SetLastError(String str){
            _str = str;
        }
        public String GetLastError(){
            return _str;
        }
    }
}
