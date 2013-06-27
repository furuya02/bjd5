using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SmtpServer{
    public enum SmtpClientStatus{
        Idle = 0, //（切断中）接続完了前まで
        Helo = 1, //（挨拶）HELO/EHLO前まで
        Transaction = 2, //(転送)QUIT発行前まで
    }
}