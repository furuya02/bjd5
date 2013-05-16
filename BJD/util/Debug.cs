using System.Diagnostics;
using Bjd.log;

//StopWatch

namespace Bjd {
    public class Debug{

        readonly Stopwatch _stopWatch = new Stopwatch();
        readonly Logger _logger;
        readonly int _ident;
        public Debug(Logger logger,int ident) {
            _ident = ident;
            _logger = logger;
            _stopWatch.Start();
        }
        public void Check(int n) {
            _logger.Set(LogKind.Debug,null,9999,string.Format("[{0}-{1} {2}msec]",_ident,n,_stopWatch.ElapsedMilliseconds));
            _stopWatch.Reset();
            _stopWatch.Start();
        }
        public void Check(int n,string str) {
            _logger.Set(LogKind.Debug,null,9999,string.Format("[{0}-{1} {2}msec] {3}",_ident,n,_stopWatch.ElapsedMilliseconds,str));
            _stopWatch.Reset();
            _stopWatch.Start();
        }
    }

}
