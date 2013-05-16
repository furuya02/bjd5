namespace Bjd.net{
    public class OneDnsCache{
        public string Name { get; private set; }
        public Ip[] IpList { get; private set; }

        public OneDnsCache(string name, Ip[] ipList){
            IpList = ipList;
            Name = name;
        }
    }
}
