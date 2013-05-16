using Bjd.net;

namespace Bjd.acl {
    public abstract class Acl : ValidObj {
        public bool Status { get; protected set; }
        public string Name { get; protected set; }
        public Ip Start { get; protected set; }
        public Ip End { get; protected set; }
        public abstract bool IsHit(Ip ip);

        protected Acl(string name) {
            Name = name;
            Status = false;
        }

        protected void Swap(){
            var ip = Start;
            Start = End;
            End = ip;
        }
    }
}
