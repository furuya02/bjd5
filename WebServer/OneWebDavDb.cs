namespace WebServer {
    class OneWebDavDb {
        public string Uri { get; private set; }
        public string NameSpace { get; private set; }
        public string Name { get; private set; }
        public string Value { get; private set; }
        public OneWebDavDb(string uri, string nameSpace, string name, string value) {
            Uri = uri;
            NameSpace = nameSpace;
            Name = name;
            Value = value;
        }

        public OneWebDavDb(string str) {
            var tmp = str.Split('\t');
            if (tmp.Length == 4) {
                Uri = tmp[0];
                NameSpace = tmp[1];
                Name = tmp[2];
                Value = tmp[3];
            } else {
                Uri = "";
                NameSpace = "";
                Name = "";
                Value = "";
            }
        }

        public override string ToString() {
            return string.Format("{0}\t{1}\t{2}\t{3}", Uri, NameSpace, Name, Value);
        }
    }
}
