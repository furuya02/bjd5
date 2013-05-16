using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Bjd;
using Bjd.util;

namespace WebServer {
    //PROPFINDにおいて検索した結果を保存してレスポンスを生成するクラス
    class PropFindResponce {
        readonly List<OneResponse> _ar = new List<OneResponse>();
        readonly List<string> _nameSpaceList = new List<string>();
        readonly WebDavDb _webDavDb;

        public PropFindResponce(WebDavDb webDavDb) {
            _webDavDb = webDavDb;
        }

        public void Add(bool isCollection,
            string name,
            string hrefHost,
            string hrefUri,
            string contentType,
            string etag,
            long length,
            DateTime creationdate,
            DateTime lastModified) {
            //OneResponse oneResponse = new OneResponse(isCollection, name, hrefHost, hrefUri, contentType, etag, length, creationdate, lastModified);
            var oneResponse = new OneResponse(isCollection, hrefHost, hrefUri, contentType, etag, length, creationdate, lastModified);
            var oneWebDavList = _webDavDb.Get(hrefUri);
            foreach (var o in oneWebDavList) {
                //string tag;
                var index = _nameSpaceList.IndexOf(o.NameSpace);
                if (index == -1) {
                    _nameSpaceList.Add(o.NameSpace);
                    index = _nameSpaceList.Count - 1;
                }
                var tag = string.Format("ns{0}", index);
                oneResponse.Add(o.NameSpace, tag, o.Name, o.Value);
            }
            _ar.Add(oneResponse);
        }

        public override string ToString() {
            string doc;

            using (var sw = new StringWriter()) {
                using (var writer = new XmlTextWriter(sw)) {
                    writer.Formatting = Formatting.Indented;
                    //writer.WriteStartDocument();
                    writer.WriteProcessingInstruction("xml", "version=\"1.0\" encoding=\"utf-8\"");
                    writer.WriteStartElement("D", "multistatus", "DAV:");
                    for (int i = 0; i < _nameSpaceList.Count; i++) {
                        string tag = string.Format("ns{0}", i);
                        writer.WriteAttributeString("xmlns", tag, null, _nameSpaceList[i]);
                    }

                    foreach (var oneResponse in _ar) {
                        oneResponse.Xml(writer);
                    }
                    writer.WriteEndElement();
                    //writer.WriteEndDocument();
                    writer.Flush();
                    writer.Close();
                    doc = sw.ToString();
                    writer.Flush();
                    writer.Close();
                }
                sw.Flush();
                sw.Close();
            }
            return doc;
        }

        class OneResponse {
            readonly List<OneProp> _ar = new List<OneProp>();
            readonly string _href;//URI
            readonly bool _isCollection;//コレクションかどうかのフラグ
            public OneResponse(bool isCollection,
                //string name,
                string hrefHost,
                string hrefUri,
                string contentType,
                string etag,
                long length,
                DateTime creationdate,
                DateTime lastModified) {
                _isCollection = isCollection;

                //hrefUriのデコード
                //hrefUri = Util.SwapStr("%2f","/",HttpUtility.UrlEncode(hrefUri));
                //hrefUri = HttpUtility.UrlPathEncode(hrefUri);

                string uri = Uri.EscapeDataString(hrefUri);
                //Ver5.4.6
                //hrefUri = Util.SwapStr("%2f","/",Util.SwapStr("+","%20",uri));
                hrefUri = Util.SwapStr("+", "%20", uri);
                hrefUri = Util.SwapStr("%2f", "/", hrefUri);
                hrefUri = Util.SwapStr("%2F", "/", hrefUri);

                if (_isCollection) {
                    if (hrefUri.Length > 1 && hrefUri[hrefUri.Length - 1] != '/')
                        hrefUri = hrefUri + "/";
                }
                _href = hrefHost + hrefUri;

                if (_isCollection) {
                    if (_href.Length > 1 && _href[_href.Length - 1] != '/')
                        _href = _href + "/";
                }

                var creationdate1 = creationdate.ToUniversalTime();
                var lastModified1 = lastModified.ToUniversalTime();

                _ar.Add(new OneProp("DAV:", "D", "creationdate", creationdate1.ToString("s") + "Z"));//WindwosXPでは、この書式でないとアクセスできない
                _ar.Add(new OneProp("DAV:", "D", "getlastmodified", lastModified1.ToString("R")));

                if (_isCollection) {
                    _ar.Add(new OneProp("DAV:", "D", "iscollection", "1"));
                } else {
                    _ar.Add(new OneProp("DAV:", "D", "iscollection", "0"));
                    _ar.Add(new OneProp("DAV:", "D", "getcontentlength", length.ToString()));
                    _ar.Add(new OneProp("DAV:", "D", "getcontenttype", contentType));
                    if (etag != "")
                        _ar.Add(new OneProp("DAV:", "D", "getetag", etag));
                }
            }

            public void Add(string nameSpace, string tag, string name, string value) {
                _ar.Add(new OneProp(nameSpace, tag, name, value));
            }

            public void Xml(XmlTextWriter writer) {
                writer.WriteStartElement("D", "response", "DAV:");
                writer.WriteElementString("D", "href", "DAV:", _href);
                writer.WriteStartElement("D", "propstat", "DAV:");
                writer.WriteElementString("D", "status", "DAV:", "HTTP/1.1 200 OK");
                writer.WriteStartElement("D", "prop", "DAV:");
                foreach (var oneProp in _ar) {
                    writer.WriteElementString(oneProp.Tag, oneProp.Name, oneProp.NameSpace, oneProp.Value);
                }
                if (_isCollection) {
                    writer.WriteStartElement("D", "resourcetype", "DAV:");
                    writer.WriteStartElement("D", "collection", "DAV:");
                    writer.WriteEndElement();//collection
                    writer.WriteEndElement();//resourcetype
                }

                //DEBUG
                //writer.WriteStartElement("D", "lockdiscovery", "DAV:");
                //writer.WriteStartElement("D", "activelock", "DAV:");

                //writer.WriteStartElement("D", "locktype", "DAV:");
                //writer.WriteElementString("D", "write", "DAV:", "");
                //writer.WriteEndElement();//locktype

                //writer.WriteStartElement("D", "lockscope", "DAV:");
                //writer.WriteElementString("D", "exclusive", "DAV:", "");
                //writer.WriteEndElement();//lockscope

                //writer.WriteElementString("D", "depth", "DAV:", "infinity");
                //writer.WriteElementString("D", "timeout", "DAV:", "Infinite");

                //writer.WriteStartElement("D", "locktoken", "DAV:");
                //writer.WriteElementString("D", "href", "DAV:", "opaquelocktoken:05924cbe-b6b6-0310-b47a-8a2f7e50823a");
                //writer.WriteEndElement();//locktoken

                //writer.WriteEndElement();//activelock
                //writer.WriteEndElement();//lockdiscovery

                writer.WriteEndElement();//prop
                writer.WriteEndElement();//propstat
                writer.WriteEndElement();
            }

            class OneProp {
                public string NameSpace { get; private set; }
                public string Tag { get; private set; }
                public string Name { get; private set; }
                public string Value { get; private set; }
                public OneProp(string nameSpace, string tag, string name, string value) {
                    NameSpace = nameSpace;
                    Tag = tag;
                    Name = name;
                    Value = value;
                }
            }
        }
    }
}
