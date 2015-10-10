using System;
using System.Windows.Forms;

namespace Bjd.menu {
    public class OneMenu : IDisposable{
        public string JpTitle { get; private set; }
        public string EnTitle { get; private set; }
        public char Mnemonic { get; private set; }
        public string Name { get; private set; }
        public ListMenu SubMenu { get; set; }
        public Keys Accelerator { get; private set; }


        public string Title(bool isJp){
            var title = EnTitle;
            if (isJp){
                title = Mnemonic == ' ' ? string.Format("{0}", JpTitle) : string.Format("{0}({1})", JpTitle, Mnemonic);
            }
            return title;
        }

        //�Z�p���[�^�p
        public OneMenu(){
            Name = "-";
            JpTitle = "";
            EnTitle = "";
            Mnemonic = 'Z';
            SubMenu = null;
            Accelerator = Keys.None;

        }

        public OneMenu(String name, string jpTitle, string enTitle, char mnemonic, Keys accelerator){
            Name = name;
            JpTitle = jpTitle;
            EnTitle = enTitle;
            Mnemonic = mnemonic;
            SubMenu = new ListMenu();
            Accelerator = accelerator;

        }

        public void Dispose(){
        }
    }

}

    //OneMenu �P�̃��j���[��\������N���X
    /*public class OneMenu : IDisposable {
        public string JpTitle { get; private set; }
        public string EnTitle { get; private set; }
        public string Name { get; private set; }
        public ListMenu SubMenu { get; set; }
        public Keys Keys { get; private set; }
        public OneMenu(string name, string jpTitle, string enTitle, Keys keys = Keys.None) {
            Name = name;
            JpTitle = jpTitle;
            EnTitle = enTitle;
            SubMenu = new ListMenu();
            Keys = keys;
        }

        public void Dispose() {
        }
    }
}

    */