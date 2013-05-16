using System.Threading;

namespace Bjd.wait {
    public class Wait {

        readonly WaitDlg _dlg;
        int _max;//プログレスバーの最大値
        int _val;//プログレスバーの値
        string _msg;//ダイアログで表示するメッセージ

        bool _busy;

        public Wait() {
            _dlg = new WaitDlg(this);
        }

        public bool Life { get; set; }//WailMsgDlgからセットされる（クローズ、若しくは「キャンセル」）
        public string Msg {
            get{ return _msg;}
            set {
                _msg = value;
                _dlg.Renew();
            }
        }
        public int Max {
            get{ return _max;}
            set {
                _val = 0;
                _max = value;
                _dlg.Renew();
            }
        }
        public int Val {
            get{ return _val;}
            set {
                _val = value;
                _dlg.Renew();
            }
        }

        
        //別スレッドで、ダイログを表示する
        void ShowDlg() {
            _busy = true;
            _dlg.Open();
            _busy = false;
        }
        public void Start(string msg) {
            while (_busy) {
                Thread.Sleep(100);
            }
            _msg = msg;
            _max = 0;
            _val = 0;
            Life = true;

            //別スレッドで、ダイログを表示する
            var t = new Thread(ShowDlg){IsBackground = true};
            t.Start();
            Thread.Sleep(10);
        }
        public void Stop() {
            while(!_busy) {
                Thread.Sleep(100);
            }
            _dlg.Close();
        }
    }

    class WaitImpl : Wait{}
}
