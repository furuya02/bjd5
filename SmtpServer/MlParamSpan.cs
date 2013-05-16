using System;

namespace SmtpServer {
    //Startが-1のとき、データは無効となる
    class MlParamSpan {
        public int Start { get; private set; }
        public int End { get; private set; }
        public MlParamSpan(string paramStr, int current) {
            Start = -1;
            End = -1;
            if (paramStr == "") { //全部
                Start = 1;
                End = current;
                return;
            }

            string[] tmp = paramStr.Split(':');
            if (tmp.Length == 2) {
                if (tmp[0].ToUpper() == "LAST") {
                    try {
                        int no = Convert.ToInt32(tmp[1]);
                        Start = current - no + 1;
                        End = current;
                    } catch {
                        Start = -1;
                    }
                } else if (tmp[0].ToUpper() == "FIRST") {
                    try {
                        int no = Convert.ToInt32(tmp[1]);
                        if (no > current)
                            no = current;
                        Start = 1;
                        End = no;
                    } catch {
                        Start = -1;
                    }
                } else {
                    Start = -1;
                }
                //ver5.6.4
                if(Start<0 || End==-1 || current<End){
                    //無効
                    Start = -1;
                    End = -1;
                }
                return;
            }

            tmp = paramStr.Split('-');
            if (tmp.Length == 2) {
                try {
                    Start = Convert.ToInt32(tmp[0]);
                    End = Convert.ToInt32(tmp[1]);
                    
                    if(Start>current){
                        Start = -1;
                        End = -1;
                    }else{
                        if (Start < 1)
                            Start = 1;
                        if (End > current)
                            End = current;
                    }

                } catch {
                    Start = -1;
                }
            } else {
                try {
                    Start = Convert.ToInt32(paramStr);
                    if (Start < 1)
                        Start = 1;
                    End = Start;
                } catch {
                    Start = -1;
                }
            }
            //ver5.6.4
            if (Start <= 0 || End <= 0 || current < End) {
                //無効
                Start = -1;
                End = -1;
            }else{
                //有効
                //if(Start!=0 && End!=0 && Start>End){//逆転
                if(Start>End){//逆転
                    var n = End;
                    End = Start;
                    Start = n;
                }
            }

        }
    }
}