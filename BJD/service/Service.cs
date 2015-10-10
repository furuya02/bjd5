
namespace Bjd.service {
    class Service : System.ServiceProcess.ServiceBase{

        Kernel _kernel;

        public Service() {
            ServiceName = "BlackJumboDog";
            CanStop = true;
            CanPauseAndContinue = true;
            AutoLog = true;
        }
        public static void ServiceMain() {
            Run(new Service());
        }
        protected override void OnStart(string[] args) {
            _kernel = new Kernel(null, null, null,null);
            _kernel.Menu.EnqueueMenu("StartStop_Start", true/*synchro*/);
        }
        protected override void OnPause() {
            _kernel.Menu.EnqueueMenu("StartStop_Stop", true/*synchro*/);
        }
        protected override void OnContinue() {
            _kernel.Menu.EnqueueMenu("StartStop_Start", true/*synchro*/);
        }

        protected override void OnStop() {
            _kernel.Menu.EnqueueMenu("StartStop_Stop", true/*synchro*/);

            _kernel.Dispose();
            _kernel = null;
        }

    }

}
