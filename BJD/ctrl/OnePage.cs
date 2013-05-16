using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bjd.option;

namespace Bjd.ctrl {
    // オプションダイアログに表示するタブの１ページを表現するクラス
    public class OnePage {

        public ListVal ListVal { get; private set; }
        public string Title { get; private set; }
        public string Name { get; private set; }


        public OnePage(string name, string title){

            Name = name;
		    Title = title;
            ListVal =new ListVal();

        }

        public void Add(OneVal oneVal){
            ListVal.Add(oneVal);
        }
    }
}
