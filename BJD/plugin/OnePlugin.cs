using System;
using System.IO;
using Bjd.net;
using Bjd.option;
using Bjd.server;
using Bjd.util;

namespace Bjd.plugin{
    public class OnePlugin : IDisposable{
        readonly string _path;

        public OnePlugin(string path){
            _path = path;
        }


        public string Name{
            get{
                var str = Path.GetFileNameWithoutExtension(_path);
                var index = str.IndexOf("Server");
                if (index != 0 && (str.Length - index)==6) {
                    return str.Substring(0, index);
                }
                return str;
            }
        }

        //	 //classオブジェクトの作成
        //	private Constructor createConstructor(File file, String className, Class[] args) throws Exception {
        //		try {
        //			URL url = file.getCanonicalFile().toURI().toURL();
        //			URLClassLoader loader = new URLClassLoader(new URL[] { url });
        //			Class cobj = loader.loadClass(className);
        //			//loader.close(); //これを実行すると例外が発生する
        //			return cobj.getConstructor(args);
        //		} catch (IOException e) {
        //			throw new Exception("IOException");
        //		} catch (ClassNotFoundException e) {
        //			throw new Exception("ClassNotFoundException");
        //		} catch (NoSuchMethodException e) {
        //			throw new Exception("NoSuchMethodException");
        //		} catch (SecurityException e) {
        //			throw new Exception("SecurityException");
        //		}
        //	}
        //
        //    //Optionインスタンスの生成
        //	public OneOption createOption(Kernel kernel) {
        //		try {
        //			Constructor constructor = createConstructor(file, classNameOption,
        //					new Class[] { Kernel.class, String.class });
        //			return (OneOption) constructor.newInstance(new Object[] { kernel, file.getPath() });
        //		} catch (Exception e) {
        //			//何の例外が発生しても、プラグインとしては受け付けない
        //			Util.runtimeException(e.getMessage()/*e.getClass().getName()*/);
        //			return null;
        //		}
        //	}
        //
            //プラグイン固有のOptionインスタンスの生成
        	public OneOption CreateOption(Kernel kernel, String className, string nameTag) {
		        return (OneOption)Util.CreateInstance(kernel,_path,className, new object[] { kernel, _path ,nameTag});
        	}

            public OneServer CreateServer(Kernel kernel, Conf conf, OneBind oneBind) {
                return (OneServer)Util.CreateInstance(kernel, _path, "Server", new Object[] { kernel, conf, oneBind });
            }

        //
        //    //Serverインスタンスの生成
        //	public OneServer createServer(Kernel kernel, Conf conf, OneBind oneBind) {
        //		try {
        //			Constructor constructor = createConstructor(file, classNameServer, new Class[] { Kernel.class, Conf.class,
        //					OneBind.class });
        //			return (OneServer) constructor.newInstance(new Object[] { kernel, conf, oneBind });
        //		} catch (Exception e) {
        //			//何の例外が発生しても、プラグインとしては受け付けない
        //			Util.runtimeException(this, e);
        //			return null;
        //		}
        //}
        //
        public void Dispose(){}

    }
}