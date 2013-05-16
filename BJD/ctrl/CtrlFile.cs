namespace Bjd.ctrl {
    public class CtrlFile : CtrlBrowse{
        public CtrlFile(string help, int digits, Kernel kernel)
            : base(help, digits, kernel){
            
        }

        public override CtrlType GetCtrlType(){
            return CtrlType.File;
        }
    }
}
