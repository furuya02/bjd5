using System;

namespace Bjd.browse {
    internal class OneBrowse {
        public string Name { get; private set; }
        public long Size { get; private set; }
        public BrowseKind BrowseKind { get; private set; }
        public DateTime Dt { get; private set; }

        //RemoteServerでのデータ生成
        public OneBrowse(BrowseKind browseKind, string name, long size, DateTime dt) {
            BrowseKind = browseKind;
            Name = name;
            Size = size;
            Dt = dt;
        }

        //RemoteClientでの復元
        public OneBrowse(string str) {
            Name = "";
            if (str == null)
                return;
            var tmp = str.Split('\b');
            if (tmp.Length != 4)
                return;
            BrowseKind = (BrowseKind)Enum.Parse(typeof(BrowseKind), tmp[0]);
            Name = tmp[1];
            Size = long.Parse(tmp[2]);
            Dt = DateTime.Parse(tmp[3]);
        }

        //RemoteServerでの送信データ作成
        public override string ToString() {
            return string.Format("{0}\b{1}\b{2}\b{3}", BrowseKind, Name, Size, Dt);
        }
    }
}
