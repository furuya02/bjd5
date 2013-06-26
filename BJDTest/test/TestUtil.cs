using System;
using System.IO;
using System.Text;
using Bjd;
using Bjd.net;
using Bjd.option;
using Bjd.util;
using NUnit.Framework;

namespace BjdTest.test {

    public class TestUtil{
        private TestUtil(){
            //デフォルトコンストラクタの隠蔽
        }

        public static String ProjectDirectory(){
            return "c:\\tmp2\\bjd5";
        }

        //テンポラリディレクトリの作成<br>
        //最初に呼ばれたとき、ディレクトリが存在しないので、新規に作成される
        public static String GetTmpDir(String tmpDir){
//            String currentDir = new File(".").getAbsoluteFile().getParent(); // カレントディレクトリ
//            String dir = string.Format("%s\\%s", currentDir, tmpDir);
//            File file = new File(dir);
//            if (!file.exists()){
//                file.mkdir();
//            }
            var dir = string.Format("{0}\\{1}", Directory.GetCurrentDirectory(), tmpDir);
            if (!Directory.Exists(dir)){
                Directory.CreateDirectory(dir);
            }

            return dir;
        }

        //指定したテンポラリディレクトリ(tmpDir)の中での作成可能なテンポラリファイル(もしくはディレクトリ)名を生成する
        //return テンポラリファイル（ディレクトリ）名(パス)
        public static String GetTmpPath(String tmpDir){
//            String prefix = "test";
//            String suffix = ".ts";
//            File file = File.CreateTempFile(prefix, suffix, new File(getTmpDir(tmpDir)));
//            if (file.exists()){
//                file.delete();
//            }

            var filename = string.Format("{0}\\{1}", GetTmpDir(tmpDir), Path.GetRandomFileName());
            if (File.Exists(filename)){
                File.Delete(filename);
            }
            return filename;
        }

        //テスト用のダミーのConf生成
        public static Conf CreateConf(String optionName){
            var kernel = new Kernel();
            if (optionName == "OptionSample"){
                return new Conf(new OptionSample(kernel, ""));
            } 
            if (optionName == "OptionLog"){
                return new Conf(new OptionLog(kernel, ""));
            }
            if (optionName == "OptionBasic"){
                return new Conf(new OptionBasic(kernel, ""));
            }
            Util.RuntimeException(string.Format("{0} not found", optionName));
            return null; //k実行時例外により、ここは実行されない
        }

        //private static String lastBanner = "";

        //テスト用のIpオブジェクトの生成<br>
        //パラメータ不良による制外発生をAssertで吸収
        public static Ip CreateIp(String ipStr){
            Ip ip = null;
            try{
                ip = new Ip(ipStr);
            } catch (ValidObjException e){
                Assert.Fail(e.Message);
            }
            return ip;
        }

        //パケットストリームの変換
        public static byte[] HexStream2Bytes(String str){
            var buf = new byte[str.Length/2];
            for (int i = 0; i < buf.Length; i++){
                buf[i] = (byte)Int32.Parse(str.Substring(i * 2, 2), System.Globalization.NumberStyles.HexNumber);
            }
            return buf;
        }

        //コンソール出力用
        public static String ToString(byte[] buf){
            var sb = new StringBuilder();
            if (buf == null){
                sb.Append("null");
            } else{
                foreach (byte b in buf){
                    sb.Append(string.Format("0x{0:X2} ", b & 0xFF));
                }
            }
            return sb.ToString();
        }

        public static String ToString(String str){
            str = str.Replace("\r", "/r");
            str = str.Replace("\n", "/n");
            return str;
        }

        //msg msg==nullの時、改行のみ表示（TearDown用）
        public static void WaitDisp(String msg){
            //		if (msg == null) {
            //			System.out.println("");
            //		} else {
            //			System.out.print("\n" + msg);
            //			Runnable r = new Runnable() {
            //				public void run() {
            //					while (true) {
            //						System.out.print("*");
            //						try {
            //							Thread.sleep(100);
            //						} catch (InterruptedException e) {
            //						}
            //					}
            //				}
            //			};
            //			Thread thr1 = new Thread(r);
            //			thr1.start();
            //		}
        }
    }
}
