using System;
using System.IO;
using Bjd.util;

namespace BjdTest.test{

    public class TmpOption : IDisposable{
        //private readonly Kernel _kernel= new Kernel();
        private readonly String _originName;
        private readonly String _backupName;
        private readonly String _targetName;
        private readonly string _testDataPath;


        public TmpOption(string subDir,string fileName){

            _testDataPath = Util.CreateTempDirectory();

            //オリジナルファイル
            //var dir = TestUtil.ProhjectDirectory() + "\\BJD\\out";
            _originName = string.Format("{0}\\Option.ini", TestUtil.ProjectDirectory() + "\\BJD\\out");
            //BACKUPファイル
            _backupName = string.Format("{0}\\Option.bak", _testDataPath);

            //上書きファイル
            _targetName = string.Format("{0}\\{1}\\{2}",TestUtil.ProjectDirectory(),subDir,fileName);

            if (!File.Exists(_targetName)){
                throw new Exception(string.Format("指定されたファイルが見つかりません。 {0}", _targetName));
            }

            //バックアップ作成
            if (File.Exists(_originName)){
                File.Copy(_originName, _backupName, true);
            }
            //上書き
            File.Copy(_targetName, _originName, true);
        }

        public void Dispose(){
            if (File.Exists(_backupName)){
                File.Copy(_backupName, _originName,true);
                File.Delete(_backupName);
            }
        }
    }
}
