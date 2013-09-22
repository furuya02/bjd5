using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using Bjd.log;

//using System.Collections;

namespace Bjd.util{



    public static class Util{

        //private Util(){}//デフォルトコンストラクタの隠蔽

        //***********************************************
        // htons()
        //***********************************************
        public static ushort htons(ushort i){
            return (ushort) ((i << 8) + (i >> 8));
        }

        public static uint htonl(uint i){
            return
                (uint) ((i & 0xff000000) >> 24 | (i & 0x00ff0000) >> 8 | (i & 0x0000ff00) << 8 | (i & 0x000000ff) << 24);
        }

        public static UInt64 htonl(UInt64 i){
            return
                (UInt64)
                ((i & 0xff00000000000000) >> 56 | (i & 0x00ff000000000000) >> 40 | (i & 0x0000ff0000000000) >> 24 |
                 (i & 0x000000ff00000000) >> 8
                 | (i & 0x00000000ff000000) << 8 | (i & 0x0000000000ff0000) << 24 | (i & 0x000000000000ff00) << 40 |
                 (i & 0x00000000000000ff) << 56);
        }

        //string strの中の文字 before を文字 afterに置き換える
        public static string SwapChar(char before, char after, string str){
            while (true){
                int index = str.IndexOf(before);
                if (index < 0)
                    break;
                //\bが指定された場合、ブランクにする
                if (after == '\b'){
                    str = str.Substring(0, index) + str.Substring(index + 1);
                }
                else{
                    str = str.Substring(0, index) + after + str.Substring(index + 1);
                }
            }
            return str;
        }

        //string strの中の文字 beforeStr を afterStrに置き換える
        public static string SwapStr(string beforeStr, string afterStr, string str){
            var offset = 0; //処理済み文字位置
            while (true){
                var index = str.Substring(offset).IndexOf(beforeStr);
                if (index < 0)
                    break;
                index += offset;
                str = str.Substring(0, index) + afterStr + str.Substring(index + beforeStr.Length);
                offset = index + afterStr.Length;
            }
            return str;
        }

        //string strの中の文字 c文字が連続している場合1つにする 
        public static string MargeChar(char c, string str){
            var buf = new char[]{c, c};
            var tmpStr = new string(buf);

            while (true){
                var index = str.IndexOf(tmpStr);
                if (index < 0)
                    break;
                str = str.Substring(0, index) + str.Substring(index + 1);
            }
            return str;
        }

        public static string DateStr(DateTime dt){
            string[] monthName = {
                                     "", "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"
                                 };

            if (DateTime.Now.Year == dt.Year){
                return string.Format("{0} {1:D2} {2:D2}:{3:D2}",
                                     monthName[dt.Month],
                                     dt.Day,
                                     dt.Hour,
                                     dt.Minute);
            }
            return string.Format("{0} {1:D2} {2:D4}",
                                 monthName[dt.Month],
                                 dt.Day,
                                 dt.Year);
        }

        //指定ファイルの中で最初にtag文字列が出現する位置を返す
        public static int IndexOf(string fileName, string tag){
            int len = 0; //カウンタ
            if (File.Exists(fileName)){
                using (var sr = new StreamReader(fileName, Encoding.GetEncoding("Shift_JIS"))){
                    while (true){
                        var str = sr.ReadLine();
                        if (str == null)
                            break;
                        var index = str.IndexOf(tag);
                        if (0 <= index){
                            sr.Close();
                            return len + index;
                        }
                        len += str.Length + 2; //１行分の文字数でカウンタをアップする
                    }
                    sr.Close();
                }
            }
            return -1;
        }


        //Thu, 27 Nov 2008 20:45:50 GMT
        public static string UtcTime2Str(DateTime dt){
            string[] monthList = {"Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"};
            string[] weekList = {"Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"};

            var str = string.Format("{0}, {1:D2} {2} {3:D4} {4:D2}:{5:D2}:{6:D2} GMT"
                                    , weekList[(int) dt.DayOfWeek]
                                    , dt.Day
                                    , monthList[dt.Month - 1]
                                    , dt.Year
                                    , dt.Hour
                                    , dt.Minute
                                    , dt.Second);
            return str;

        }

        //Sun, 1 Feb 2009 09:28:20 +0900
        public static string LocalTime2Str(DateTime dt){
            string[] monthList = {"Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"};
            string[] weekList = {"Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"};

            var span = TimeZone.CurrentTimeZone.GetUtcOffset(dt);
            var hour = span.Hours;
            var zoneStr = string.Format("+{0:D2}00", hour);
            if (hour < 0){
                zoneStr = string.Format("-{0:D2}00", hour*(-1));
            }
            var str = string.Format("{0}, {1:D2} {2} {3:D4} {4:D2}:{5:D2}:{6:D2} {7}"
                                    , weekList[(int) dt.DayOfWeek]
                                    , dt.Day
                                    , monthList[dt.Month - 1]
                                    , dt.Year
                                    , dt.Hour
                                    , dt.Minute
                                    , dt.Second
                                    , zoneStr);
            return str;

        }

        public static DateTime Str2Time(string str){
            string[] monthList = {"Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"};

            try{
                var tmp = str.Split(' ');
                var time = tmp[4].Split(':');

                var year = Convert.ToInt32(tmp[3]);
                int month;
                for (var m = 0; m < monthList.Length; m++){
                    if (monthList[m] != tmp[2])
                        continue;
                    month = m + 1;
                    goto next;
                }
                return new DateTime(0);
                next:
                var day = Convert.ToInt32(tmp[1]);
                var hour = Convert.ToInt32(time[0]);
                var minute = Convert.ToInt32(time[1]);
                var second = Convert.ToInt32(time[2]);
                var dt = new DateTime(year, month, day, hour, minute, second);

                return dt;
            }
            catch{
                return new DateTime(0);
            }

        }

        public static Object CreateInstance(Kernel kernel, string path, string className, Object[] param){
            if (File.Exists(path)){
                var dllName = Path.GetFileNameWithoutExtension(path);
                try{
                    var asm = Assembly.LoadFile(path);
                    if (asm != null){
                        return asm.CreateInstance(dllName + "." + className, true, BindingFlags.Default, null, param,
                                                  null, null);
                    }
                }
                catch (Exception ex){

                    var logger = kernel.CreateLogger("CreateInstance", false, null);
                    logger.Set(LogKind.Error, null, 9000051,
                               string.Format("DLL={0} CLASS={1} {2}", dllName, className, ex.Message));

                    return null;
                }
            }
            return null;
        }

        //例外を発生させプログラムを停止する（設計上の問題）
        public static void RuntimeException(string msg){
            Msg.Show(MsgKind.Error, msg);
            throw new Exception("RuntimeException" + msg);
        }

        //テンポラリディレクトリの作成
        public static string CreateTempDirectory(){
            var path = Path.GetTempFileName();
            File.Delete(path);
            var info = Directory.CreateDirectory(path);
            return info.FullName;
        }


        //ファイル若しくはディレクトリが存在するかどうか
        //path==null の場合、ExistsKind.Noneとなる
        //path 検査対象のパス
        public static ExistsKind Exists(string path) {
            if (path != null){
                if (Directory.Exists(path)){
                    return ExistsKind.Dir;
                }
                if (File.Exists(path)){
                    return ExistsKind.File;
                }
            }
            return ExistsKind.None;
        }

        //Ver5.7.x以前のiniファイルをVer5.8用に修正する
        public static String ConvValStr(String src){

            String[] t = src.Split(new char[]{'\b'},StringSplitOptions.RemoveEmptyEntries);
            if (t.Length < 1){
                return src;
            }
            try{
                //旧ファイルかどうかの判断
                if (t[0][0] == '\t'){
                    return src; //新ファイル
                }
                if (t[0][0] == '#' && t[0][1] == '\t'){
                    return src; //新ファイル
                }
            }catch (Exception){
            }

            var ar = new List<String>();
            foreach (var l in t) {
                if (l[0] == '#'){
                    ar.Add("#\t" + l.Substring(1));
                }else{
                    ar.Add("\t" + l.Substring(0));
                }
            }
            var sb = new StringBuilder();
            foreach (var a in ar){
                if(sb.Length!=0){
                    sb.Append("\b");
                }
                sb.Append(a);
            }
            return sb.ToString();
        }

        //ディレクトリのコピー
        public static bool CopyDirectory(string srcPath, string dstPath) {
            Directory.CreateDirectory(dstPath);
            File.SetAttributes(dstPath, File.GetAttributes(srcPath));

            foreach (var dir in Directory.GetDirectories(srcPath)) {
                var name = dir.Substring(srcPath.Length);
                var nextSrcPath = srcPath + name + "\\";
                var nextDstPath = dstPath + name + "\\";
                if (!CopyDirectory(nextSrcPath, nextDstPath))
                    return false;
            }
            foreach (var file in Directory.GetFiles(srcPath)) {
                var name = file.Substring(srcPath.Length);
                var nextSrcPath = srcPath + name;
                var nextDstPath = dstPath + name;
                File.Copy(nextSrcPath, nextDstPath);
            }
            return true;
        }

    }
}
