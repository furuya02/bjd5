using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Bjd.ctrl;
using Bjd.util;

namespace Bjd.option {
    //OneValのリストを表現するクラス<br>
    //OneValと共に再帰処理が可能になっている<br>
    public class ListVal : ListBase<OneVal>{

        //[C#]コントロール変化時のイベント
        public delegate void OnChangeHandler();//デリゲート
        public event OnChangeHandler OnChange;//イベント


        private Size _dimension;

        public void Add(OneVal oneVal){

            // 追加オブジェクトの一覧
            var list = oneVal.GetList(null);

            foreach (var o in list){
                if (null != Search(o.Name)){
                    Msg.Show(MsgKind.Error, string.Format("ListVal.add({0}) 名前が重複しているため追加できませんでした", o.Name));
                }
            }
            // 重複が無いので追加する
            Ar.Add(oneVal);

            oneVal.OnChange += oneVal_OnChange;
        }

        //[C#] コントロールの変化を伝達する
        void oneVal_OnChange() {
            if(OnChange!=null){
                OnChange();
            }
        }

        //階層下のOneValを一覧する(全部の値を列挙する)
        public List<OneVal> GetList(List<OneVal> list){
            if (list == null){
                list = new List<OneVal>();
            }
            foreach (var o in Ar){
                list = o.GetList(list);
            }
            return list;
        }

        //階層下のOneValを一覧する(DATの下は検索しない)
        public List<OneVal> GetSaveList(List<OneVal> list) {
            if (list == null) {
                list = new List<OneVal>();
            }
            foreach (var o in Ar) {
                list = o.GetSaveList(list);
            }
            return list;
        }

        // 階層下のOneValを検索する
        // 見つからないときnullが返る
        // この処理は多用されるため、スピードアップのため、例外を外してnullを返すようにした
        public OneVal Search(String name){
            foreach (var o in GetList(null)){
                if (o.Name == name){
                    return o;
                }
            }
            //例外では、処理が重いので、nullを返す
            return null;
            //throw new Exception();
        }

        // コントロール生成
        public void CreateCtrl(Control mainPanel, int baseX, int baseY,ref int tabIndex){

            // オフセット計算用
            int x = baseX;
            int y = baseY;
            int h = y; // １行の中で一番背の高いオブジェクトの高さを保持する・
            int w = x; // xオフセットの最大値を保持する
            foreach (var o in Ar){

                o.CreateCtrl(mainPanel, x, y,ref tabIndex);

                // すべてのコントロールを作成した総サイズを求める
                if (h < y + o.Size.Height) {
                    h = y + o.Size.Height;
                }
                x += o.Size.Width;
                if (w < x){
                    w = x;
                }

                if (o.Crlf == Crlf.Nextline){
                    y = h;
                    x = baseX;
                }
            }
            // 開始位置から移動したオフセットで、このListValオブジェクトのwidth,heightを算出する
            _dimension = new Size(w - baseX, h - baseY);
        }

        // コントロール破棄
        public void DeleteCtrl(){
            foreach (var o in Ar){
                o.DeleteCtrl();
            }
        }

        // コントロールからの値のコピー(isComfirm==true 確認のみ)
        public bool ReadCtrl(bool isComfirm){
            foreach (var o in Ar){
                if (!o.ReadCtrl(isComfirm)){
                    return false;
                }
            }
            return true;
        }

        public Size Size{
            get{
                return _dimension;
            }
        }

        public bool IsComplete(){
            foreach (OneVal o in Ar){
                if (!o.IsComplete()){
                    return false;
                }
            }
            return true;
        }

        //public void setListener(ICtrlEventListener listener){
        //    foreach (OneVal o in Ar){
        //        o.setListener(listener);
        //    }
        //}
    }
}
