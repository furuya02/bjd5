
namespace Bjd.net {
    public class OneBind {
        public Ip Addr { get; private set; }
        public ProtocolKind Protocol { get; private set; }
        public OneBind(Ip addr,ProtocolKind protocol){
            Addr = addr;
            Protocol = protocol;
        }
    	public override string ToString() {
		    return string.Format("{0}-{1}", Addr.ToString(), Protocol.ToString());
	    }
    }
}
