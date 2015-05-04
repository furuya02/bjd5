using System.Linq;
using System.Windows.Forms;
using System.IO;
using Bjd.menu;
using Bjd.util;

namespace Bjd.tool {

    //****************************************************************
    // ツール管理クラス(Managerの中でのみ使用される)
    //****************************************************************
    
    public class ListTool : ListBase<OneTool> {
        public OneTool Get(string nameTag){
            return Ar.FirstOrDefault(o => o.NameTag == nameTag);
        }

        //null追加を回避するために、ar.Add()は、このファンクションを使用する
        bool Add(OneTool o) {
            if (o == null)
                return false;
            Ar.Add(o);
            return true;
        }
        //メニュー取得
        public ListMenu Menu() {

            var menu = new ListMenu();

            foreach (var a in Ar) {
                var nameTag = string.Format("Tool_{0}", a.NameTag);
                menu.Add(new OneMenu(nameTag, a.JpMenu, a.EnMenu,a.Mnemonic,Keys.None));

            }
            return menu;
        }


        //ツールリストの初期化
        public void Initialize(Kernel kernel) {
            Ar.Clear();

            //「ステータス表示」の追加
            var nameTag = Path.GetFileNameWithoutExtension(Application.ExecutablePath);
            //Add((OneTool)Util.CreateInstance(kernel,Application.ExecutablePath, "Tool", new object[] { kernel, nameTag }));
            Add(new Tool(kernel,nameTag));


            //OptionListを検索して初期化する
            foreach (var o in kernel.ListOption) {
                if (o.UseServer) {
                    var oneTool = (OneTool)Util.CreateInstance(kernel, o.Path, "Tool", new object[] { kernel, o.NameTag });
                    if (oneTool != null) {
                        Ar.Add(oneTool);
                    }
                }
            }
        }

        //メニュー取得
        public ListMenu GetListMenu(){

            var mainMenu = new ListMenu();

            foreach (var a in Ar) {
                var nameTag = string.Format("Tool_{0}", a.NameTag);
                mainMenu.Add(new OneMenu(nameTag, a.JpMenu, a.EnMenu, a.Mnemonic, Keys.None));

            }
            return mainMenu;
        }


    }
}
