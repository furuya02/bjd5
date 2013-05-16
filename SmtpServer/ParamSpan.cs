using System;

namespace SmtpServer {
    //Startが-1のとき、データは無効となる
    class ParamSpan {
        public int Start { get; private set; }
        public int End { get; private set; }
        public ParamSpan(string paramStr, int current) {
            this.Start = -1;
            this.End = -1;
            if (paramStr == "") { //全部
                this.Start = 1;
                this.End = current;
                return;
            }

            string[] tmp = paramStr.Split(':');
            if (tmp.Length == 2) {
                if (tmp[0].ToUpper() == "LAST") {
                    try {
                        int no = Convert.ToInt32(tmp[1]);
                        this.Start = current - no + 1;
                        this.End = current;
                    } catch {
                        this.Start = -1;
                    }
                } else if (tmp[0].ToUpper() == "FIRST") {
                    try {
                        int no = Convert.ToInt32(tmp[1]);
                        if (no > current)
                            no = current;
                        this.Start = 1;
                        this.End = no;
                    } catch {
                        this.Start = -1;
                    }
                } else {
                    this.Start = -1;
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
                    this.Start = Convert.ToInt32(tmp[0]);
                    this.End = Convert.ToInt32(tmp[1]);
                    if (Start < 1)
                        Start = 1;
                    if (End > current)
                        End = current;
                } catch {
                    this.Start = -1;
                }
            } else {
                try {
                    this.Start = Convert.ToInt32(paramStr);
                    if (Start < 1)
                        Start = 1;
                    this.End = Start;
                } catch {
                    this.Start = -1;
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