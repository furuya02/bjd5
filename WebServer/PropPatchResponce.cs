using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace WebServer {
    //PROPPATCHにおいて処理した結果を保存してレスポンスを生成するクラス
    class PropPatchResponce {
        readonly List<OneProp> _ar = new List<OneProp>();
        readonly List<string> _nameSpaceList = new List<string>();
        readonly string _href;
        public PropPatchResponce(string href) {
            _href = href;
        }

        public void Add(string nameSpace, string name, int respoceCode) {
            if (nameSpace == "")
                return;

            var index = _nameSpaceList.IndexOf(nameSpace);
            if (index == -1) {
                _nameSpaceList.Add(nameSpace);
                index = _nameSpaceList.Count - 1;
            }
            var tag = string.Format("ns{0}", index);

            var statusStr = "HTTP/1.1 200 OK";
            string responsedescriptionStr = null;
            if (respoceCode != 200) {
                statusStr = "HTTP/1.1 409 (status)";
                responsedescriptionStr = "Property is read-onry.";
            }

            _ar.Add(new OneProp(nameSpace, tag, name, statusStr, responsedescriptionStr));
        }

        override public string ToString() {
            string doc;

            using (var sw = new StringWriter()) {
                using (var writer = new XmlTextWriter(sw)) {
                    writer.Formatting = Formatting.Indented;

                    writer.WriteProcessingInstruction("xml", "version=\"1.0\" encoding=\"utf-8\"");
                    writer.WriteStartElement("D", "multistatus", "DAV:");
                    for (var i = 0; i < _nameSpaceList.Count; i++) {
                        var tag = string.Format("ns{0}", i);
                        writer.WriteAttributeString("xmlns", tag, null, _nameSpaceList[i]);
                    }

                    writer.WriteStartElement("D", "response", "DAV:");
                    writer.WriteElementString("D", "href", "DAV:", _href);
                    foreach (OneProp oneProp in _ar) {
                        //１つの設定値を１つのpropstatエレメントに格納する
                        writer.WriteStartElement("D", "propstat", "DAV:");

                        writer.WriteStartElement("D", "prop", "DAV:");
                        writer.WriteElementString(oneProp.Tag, oneProp.Name, oneProp.NameSpace, "");
                        writer.WriteEndElement();//prop

                        writer.WriteElementString("D", "status", "DAV:", oneProp.StatusStr);
                        if (oneProp.ResponsedescriptionStr != null) {
                            writer.WriteElementString("D", "responsedescription", "DAV:", oneProp.ResponsedescriptionStr);
                        }
                        writer.WriteEndElement();//propstat
                    }
                    writer.WriteEndElement();//response
                    writer.WriteEndElement();//multistatus
                    writer.Flush();
                    writer.Close();
                    doc = sw.ToString();
                }
                sw.Flush();
                sw.Close();
            }
            return doc;
        }

        class OneProp {
            public string NameSpace { get; private set; }
            public string Tag { get; private set; }
            public string Name { get; private set; }
            public string StatusStr { get; private set; }
            public string ResponsedescriptionStr { get; private set; }
            public OneProp(string nameSpace, string tag, string name, string statusStr, string responsedescriptionStr) {
                NameSpace = nameSpace;
                Tag = tag;
                Name = name;
                StatusStr = statusStr;
                ResponsedescriptionStr = responsedescriptionStr;
            }
        }
    }
}
