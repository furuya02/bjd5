using Bjd;

namespace FtpServer {
    public class MountList : ListBase<OneMount> {
        public void Add(string fromFolder, string toFolder) {
            Ar.Add(new OneMount(fromFolder, toFolder));
        }
    }
}