using System;
using System.Windows.Forms;
using Bjd.option;
using Bjd.util;

namespace Bjd{
    //ウインドウサイズ及びGridViewのカラム幅を記憶するクラス
    //存在しないファイルを指定した場合は新規作成される
    //保存ファイルのIOエラーが発生した場合は、例外（Exception）が発生する
    //例外発生以後は、このオブジェクトを使用しても、何も処理されない
    public class WindowSize : IDisposable{

        //Windowの外観を保存・復元する
        private readonly Conf _conf;
        private readonly Reg _reg; //記録する仮想レジストリ

        public WindowSize(Conf conf, String path){
            _conf = conf;
            //ウインドサイズ等を記録する仮想レジストリ
            try{
                _reg = new Reg(path);
            }
            catch (Exception e){
                _reg = null; // reg=nullとし、事後、アクセス不能とする
                throw e;
            }
        }

        //終了処理
        //Regが保存される
        public void Dispose(){
            if (_reg == null){
                //初期化に失敗している
                return;
            }
            //明示的に呼ばないと、保存されない
            _reg.Dispose(); //Regの保存
        }

        //ウインドウサイズの復元
        public void Read(Form form){
            if (_reg == null){
                //初期化に失敗している
                return;
            }
            if (form == null){
                return;
            }
            if (_conf == null){
                return; //リモート操作の時、ここでオプション取得に失敗する
            }

            var useLastSize = (bool)_conf.Get("useLastSize");
            if (!useLastSize) {
                return;
            }
            int w = 0;
            int h = 0;
            try{
                w = _reg.GetInt(string.Format("{0}_width", form.Text));
                h = _reg.GetInt(string.Format("{0}_hight", form.Text));
            } catch (Exception) {
                w = -1;
                h = -1;
            }
            if (h <= 0) {
                h = 400;
            }
            if (w <= 0) {
                w = 800;
            }
            form.Height = h;
            form.Width = w;

            try {
                int y = _reg.GetInt(string.Format("{0}_top", form.Text));
                int x = _reg.GetInt(string.Format("{0}_left", form.Text));
                if (y <= 0) {
                    y = 0;
                }
                if (x <= 0) {
                    x = 0;
                }
                form.Top = y;
                form.Left = x;
            } catch (Exception) {
                // 読み込めない場合は、何も処理しない

            }

        }

        //カラム幅の復元
        public void Read(ListView listView){
            if (_reg == null){
                //初期化に失敗している
                return;
            }
            if (listView == null){
                return;
            }
            if (_conf == null){
                return; //リモート操作の時、ここでオプション取得に失敗する
            }

            var useLastSize = (bool) _conf.Get("useLastSize");
            if (!useLastSize){
                return;
            }

            for (int i = 0; i < listView.Columns.Count; i++){
                var key = string.Format("{0}_col-{1}", listView.Name, i);
                try{
                    int width = _reg.GetInt(key);
                    if (width <= 0){
                        width = 100; //最低100を確保する
                    }
                    listView.Columns[i].Width = width;
                }catch (Exception){
                    listView.Columns[i].Width = 100; //デフォルト値
                }
            }
        }


        //ウインドウサイズの保存
        public void Save(Form form){
            if (_reg == null){
                //初期化に失敗している
                return;
            }
            if (form == null){
                return;
            }

            if (form.WindowState == FormWindowState.Normal){
                _reg.SetInt(string.Format("{0}_width", form.Text), form.Width);
                _reg.SetInt(string.Format("{0}_hight", form.Text), form.Height);

                //Ver5.5.3 終了位置の保存
                _reg.SetInt(string.Format("{0}_top", form.Text), form.Top);
                _reg.SetInt(string.Format("{0}_left", form.Text), form.Left);
            }

        }


        //カラム幅の保存
        public void Save(ListView listView){
            if (_reg == null){
                //初期化に失敗している
                return;
            }
            if (listView == null){
                return;
            }


            for (int i = 0; i < listView.Columns.Count; i++){
                var key = string.Format("{0}_col-{1}", listView.Name, i);
                try{
                    _reg.SetInt(key, listView.Columns[i].Width);
                }
                catch (Exception){
                    Util.RuntimeException("WindowSaze.save()");
                }
            }
        }
    }
}

/*
    //Windowの外観を保存・復元する
    public class WindowSize:IDisposable {
        readonly Kernel _kernel;
        readonly Reg _reg;//記録する仮想レジストリ
        public WindowSize(Kernel kernel) {
            _kernel = kernel;
            _reg = new Reg(kernel.ProgDir(), "BJD");//ウインドサイズ等を記録する仮想レジストリ
        }
        //Disposeを明示的に呼ばないと、保存されない
        public void Dispose() {
            _reg.Dispose();//Regの保存
        }
        //ウインドウサイズの復元
        public void Read(Form form) {
            if (form == null)
                return;
            var op = _kernel.ListOption.Get("Basic");
            if (op == null)
                return;//リモート操作の時、ここでオプション取得に失敗する
            
            var useLastSize = (bool)op.GetValue("useLastSize");
            if (!useLastSize)
                return;
            var w = _reg.GetInt(string.Format("{0}_width", form.Text));
            if (w <= 0)
                w = 800;
            form.Width = w;
            var h = _reg.GetInt(string.Format("{0}_hight", form.Text));
            if (h <= 0)
                h = 400;
            form.Height = h;


            //Ver5.5.3 終了位置の復元
            var t = _reg.GetInt(string.Format("{0}_top", form.Text));
            if (t != -1) {
                if (t <= 0)
                    t = 0;
                form.Top = t;
            }

            var l = _reg.GetInt(string.Format("{0}_left", form.Text));
            if (l == -1)
                return;
            if (l <= 0)
                l = 0;
            form.Left = l;
        }

        //カラム幅の復元
        public void Read(ListView listView) {
            if (listView == null)
                return;
            var op = _kernel.ListOption.Get("Basic");
            if (op == null)
                return;//リモート操作の時、ここでオプション取得に失敗する

            var useLastSize = (bool)op.GetValue("useLastSize");
            if (!useLastSize)
                return;
            //string n = listView.Name;
            for (var i = 0; i < listView.Columns.Count; i++) {
                var key = string.Format("{0}_col-{1}", listView.Name, i);
                var width = _reg.GetInt(key);
                if (width < 0)
                    width = 100;//最低100を確保する
                listView.Columns[i].Width = width;
            }
        }
        //ウインドウサイズの保存
        public void Save(Form form) {
            if (form == null)
                return;
            if (form.WindowState == FormWindowState.Normal) {
                _reg.SetInt(string.Format("{0}_width", form.Text), form.Width);
                _reg.SetInt(string.Format("{0}_hight", form.Text), form.Height);

                //Ver5.5.3 終了位置の保存
                _reg.SetInt(string.Format("{0}_top", form.Text), form.Top);
                _reg.SetInt(string.Format("{0}_left", form.Text), form.Left);
            }

        }
        //カラム幅の保存
        public void Save(ListView listView) {
            if (listView == null)
                return;
            //string n = listView.Name;
            for (int i = 0; i < listView.Columns.Count; i++){
                var key = string.Format("{0}_col-{1}", listView.Name, i);
                _reg.SetInt(key, listView.Columns[i].Width);
            }
        }
    }
}
    */