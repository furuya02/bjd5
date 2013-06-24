using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SmtpServer{
    public enum SmtpClientStatus{
        Idle = 0, //（切断中）接続完了前まで
        Authorization = 1, //（認証）認証完了前まで
        Transaction = 2, //(転送)QUIT発行前まで
        Update = 3, //(更新)切断前まで
    }
}