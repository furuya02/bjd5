using System;
using System.Threading;
using Bjd.log;

namespace Bjd{

    //スレッドの起動停止機能を持った基本クラス
    public abstract class ThreadBase : IDisposable, ILogger, ILife{

        Thread _t;
        //Ver5.8.6 Java fix
        //private bool _isRunning;
        private KindThreadBase _kindThreadBase;

        public KindThreadBase KindThreadBase {
            get { return _kindThreadBase; }
            protected set { _kindThreadBase = value; }
        }
        private bool _life; //スレッドを停止するためのスイッチ
        readonly Logger _logger;

        //logger　スレッド実行中に例外がスローされたとき表示するためのLogger(nullを設定可能)
        protected ThreadBase(Logger logger){
            _logger = logger;
        }

        //時間を要するループがある場合、ループ条件で値がtrueであることを確認する<br>
        // falseになったら直ちにループを中断する
        public bool IsLife(){
            return _life;
        }
        
        //暫定処置
        protected void StopLife(){
            _life = false;

        }

        //終了処理
        //Override可能
        public void Dispose(){
            Stop();
        }

        //【スレッド開始前処理】
        //falseでスレッド起動をやめる
        protected abstract bool OnStartThread();

        //開始処理
        //Override可能
        public void Start(){
            //if (_isRunning){
            if (_kindThreadBase == KindThreadBase.Running){
                return;
            }

            if (!OnStartThread()){
                return;
            }
            
            try {
                _life = true;
                _t = new Thread(Loop) { IsBackground = true };
                _t.Start();

                //スレッドが起動してステータスがRUNになるまで待機する
                Thread.Sleep(1);
                //while (!_isRunning) {
                while (_kindThreadBase==KindThreadBase.Before) {
                    Thread.Sleep(10);
                }
            } catch{
            }
        }

        //【スレッド終了処理】
        protected abstract void OnStopThread();

        //停止処理
        //Override可能
        public void Stop(){

            //if (_t != null && _isRunning) {//起動されている場合
            if (_t != null && _kindThreadBase == KindThreadBase.Running) {//起動されている場合
                _life = false;//スイッチを切るとLoop内の無限ループからbreakする
                //while (_isRunning) {
                while (_kindThreadBase!=KindThreadBase.After) {
                    Thread.Sleep(100);//breakした時点でIsRunがfalseになるので、ループを抜けるまでここで待つ
                }
            }
            _t = null;
            OnStopThread();
        }

        protected abstract void OnRunThread();
        void Loop() {
            //[Java] 現在、Javaでは、ここでIsRunningをtrueにしている
            //IsRunnig = true;
            try {
                
                //[C#] C#の場合は、Start()が終了してしまうのを避けるため、OnRunThreadの中で、準備が完了してから
                //IsRunningをtrueにする
                OnRunThread();
            } catch (Exception ex) {
                if (_logger != null){
                    _logger.Set(LogKind.Error, null, 1, ex.Message);
                    _logger.Exception(ex, null, 2);
                }
            }

            //life = true;//Stop()でスレッドを停止する時、life=falseでループから離脱させ、このlife=trueで処理終了を認知する
            //_isRunning = false;
            _kindThreadBase = KindThreadBase.After;
        }

        public abstract string GetMsg(int no);
    }
}
/*
    //スレッドの起動停止機能を持った基本クラス
    public abstract class ThreadBase : IDisposable,ILogger,ILife {
        Thread _t;
        protected bool Life;//スレッドを停止するためのスイッチ

        readonly Logger _logger;
        protected Kernel Kernel;
        public string NameTag { get; private set; }

        //【スレッドが走っているかどうか】
        public bool IsRunnig { get; private set; }

        //コンストラクタ
        protected ThreadBase(Kernel kernel, string nameTag) {
            Kernel = kernel;
            NameTag = nameTag;
            _logger = kernel.CreateLogger(nameTag, true, this);
            IsRunnig = false;
        }
        public void Dispose() {
        
        }
        public bool IsLife() {
            return Life;
        }

        public string GetMsg(int messageNo) {
            switch (messageNo) {
                case 1: return (Kernel.IsJp()) ? "ThreadBase::loop()で例外が発生しました" : "An exception occurred in ThreadBase::Loop()";
                case 2: return (Kernel.IsJp()) ? "【例外】" : "[Exception]";
            }
            return "unknown";
        }
        //【スレッド開始前処理】//return falseでスレッド起動をやめる
        abstract protected bool OnStartThread();
        public void Start() {
            if (IsRunnig)
                return;

            if (!OnStartThread())
                return;

            try {


                Life = true;
                _t = new Thread(Loop){IsBackground = true};
                _t.Start();

                //スレッドが起動してステータスがRUNになるまで待機する
                while (!IsRunnig) {
                    Thread.Sleep(10);
                }
            } catch (Exception){
            }

        }
        //【スレッドループ】
        abstract protected void OnLoopThread();
        void Loop() {
            IsRunnig = true;
            try {
                OnLoopThread();
            } catch (Exception ex) {
                _logger.Set(LogKind.Error, null, 1, ex.Message);
                _logger.Exception(ex, null, 2);
            }

            //life = true;//Stop()でスレッドを停止する時、life=falseでループから離脱させ、このlife=trueで処理終了を認知する
            IsRunnig = false;
            Kernel.View.SetColor();
        }
        //【スレッド終了処理】
        abstract protected void OnStopThread();
        public void Stop() {
            if (_t != null && IsRunnig) {//起動されている場合
                Life = false;//スイッチを切るとLoop内の無限ループからbreakする
                while (IsRunnig) {
                    Thread.Sleep(100);//breakした時点でIsRunがfalseになるので、ループを抜けるまでここで待つ
                }
            }
            _t = null;
            OnStopThread();
        }

    }
}
    */