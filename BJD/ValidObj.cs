using System;
using Bjd.util;

namespace Bjd {
    public abstract class ValidObj {
	    // 初期化に失敗するとtrueに設定される
	    //trueになっている、このオブジェクトを使用すると「実行時例外」が発生する
	    private bool _initialiseFailed = false; //初期化失敗

        protected abstract void Init();

        //コンストラクタで初期化に失敗した時に使用する呼び出す<br>
	    //内部変数が初期化され例外（IllegalArgumentException）がスローされる<br>
    	protected void ThrowException(String paramStr){
		    _initialiseFailed = true; //初期化失敗
		    Init(); // デフォルト値での初期化
		    throw new ValidObjException(String.Format("[ValidObj] 引数が不正です。 \"{0}\"", paramStr));
	    }

        //初期化が失敗している場合は、実行時例外が発生する<br>
	    //全ての公開メソッドの最初に挿入する<br>
    	protected void CheckInitialise() {
	    	if (_initialiseFailed) {
                Util.RuntimeException("[ValidObj] このオブジェクトは、初期化に失敗しているため使用できません");
		    }
	    }
    }
}
