using System;

namespace Bjd.util{
//    public class Timeout{
//        private DateTime _endTime;
//
//        public Timeout(int msec){
//            _endTime = DateTime.Now;
//            _endTime = _endTime.AddMilliseconds(msec);
//        }
//
//        public bool IsFinish(){
//            if (_endTime.Ticks < DateTime.Now.Ticks){
//                return true;
//            }
//            return false;
//        }
//    }

    //Java fix UpDateを追加

    public class Timeout {
        private DateTime _endTime;
        private readonly int _msec;

        public Timeout(int msec) {
            _msec = msec;
            Update();
        }


        //Java fix
        //Ver5.8.6
        public void Update() {
            _endTime = DateTime.Now.AddMilliseconds(_msec);
        }

        public bool IsFinish() {
            if (_endTime.Ticks < DateTime.Now.Ticks) {
                return true;
            }
            return false;
        }
    }
}