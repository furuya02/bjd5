using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bjd;
using Bjd.mail;

namespace Pop3Server {
    class MessageList {
        readonly List<OneMessage> _ar = new List<OneMessage>();

        private MessageList() {
            //使用禁止
        }

        public MessageList(string folder) {
            string[] files = Directory.GetFiles(folder, "DF_*");
            Array.Sort(files);//ファイル名をソート（DF_名は作成日付なので、結果的に日付順となる）FAT32対応
            foreach (string fileName in files) {
                var mailInfo = new MailInfo(fileName);
                string fname = Path.GetFileName(fileName);
                Add(new OneMessage(folder, fname.Substring(3), mailInfo.Uid, mailInfo.Size));
            }
        }

        public void Add(OneMessage oneMessage) {
            _ar.Add(oneMessage);
        }

        //インデクサ
        public OneMessage this[int n] {
            get {
                return _ar[n];
            }
        }

        //総数（削除マーク付きはカウントしない）
        public int Count {
            get{
                return _ar.Count(t => !t.Del);
            }
        }
        public int Max {
            get {
                return _ar.Count;
            }
        }
        //総サイズ（削除マーク付きはカウントしない）
        public long Size {
            get{
                return _ar.Where(t => !t.Del).Sum(t => t.Size);
            }
        }
        //削除マークの全クリア
        public void Rset() {
            foreach (OneMessage oneMessage in _ar) {
                oneMessage.Del = false;
            }
        }

        public void Update() {
            foreach (OneMessage oneMessage in _ar) {
                if (oneMessage.Del) {
                    oneMessage.DeleteFile();
                }
            }
        }
    }
}