using System;
using System.Collections.Generic;

namespace Bjd.util {

    //****************************************************************
    // オリジナルのListクラスを生成する場合の基底クラス
    // Tの指定するクラスは IDisposableの制約がある
    //****************************************************************
    public class ListBase<T> : IEnumerable<T>, IDisposable where T : IDisposable {
        protected List<T> Ar = new List<T>();

        public virtual void Dispose() {
            foreach (var o in Ar) {
                o.Dispose();//終了処理
            }
            Ar.Clear();//SvBase 破棄
        }
        
        public void Remove(int index) {
            Ar.RemoveAt(index);
        }   


        //IEnumerable<T>の実装
        public IEnumerator<T> GetEnumerator(){
            return ((IEnumerable<T>) Ar).GetEnumerator();
        }

        //IEnumerable<T>の実装
        System.Collections.IEnumerator
            System.Collections.IEnumerable.GetEnumerator() {
            throw new NotImplementedException();
        }
        //IEnumerable<T>の実装(関連プロパティ)
        public int Count {
            get {
                return Ar.Count;
            }
        }
        public T this[int i] {
            get { return Ar[i]; }
        }
    }
}
