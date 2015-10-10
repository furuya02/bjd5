using Bjd.net;

namespace Bjd.trace {
    class OneTrace {
        public TraceKind TraceKind { get; private set; }
        public string Str { get; private set; }
        public int ThreadId { get; private set; }
        public Ip Ip { get; private set; }

        public OneTrace(TraceKind traceKind, string str, int threadId, Ip ip) {
            TraceKind = traceKind;
            Str = str;
            ThreadId = threadId;
            Ip = ip;
        }
    }
}