namespace Bjd.ctrl {
    public class CtrlFolder : CtrlBrowse{
        public CtrlFolder(string help, int digits, Kernel kernel) 
            : base(help, digits, kernel){
            
        }

        public override CtrlType GetCtrlType(){
            return CtrlType.Folder; 
        }
    }
}
