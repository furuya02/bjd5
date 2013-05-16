using System;
using System.Text;
using Bjd.util;
using NUnit.Framework;

namespace BjdTest.util {
   
/**
 * テストの性格上、リソース解放のdisposeは省略する
 * @author SIN
 *
 */
public class ListBaseTest {

	//テストのためにListBaseを継承するクラスを定義する
	class OneClass : IDisposable {
		private readonly string _s;

		public OneClass(string s) {
			_s = s;
		}

		public string GetS() {
			return _s;
		}

		
		public void Dispose() {
		}
	}

	class TestClass : ListBase<OneClass> {
		public void Add(OneClass o) {
			Ar.Add(o);
		}
	}

	[Test]
	public void 要素を３つ追加してsizeは3になる(){
		//setUp
		var sut = new TestClass();
		sut.Add(new OneClass("1"));
		sut.Add(new OneClass("2"));
		sut.Add(new OneClass("3"));

		const int expected = 3;

		//exercise
		var actual = sut.Count;

		//verify
		Assert.That(actual, Is.EqualTo(expected));
	}

	[Test]
	public void 要素を３つ追加してforループを回す(){
		//setUp
		var sut = new TestClass();
		sut.Add(new OneClass("1"));
		sut.Add(new OneClass("2"));
		sut.Add(new OneClass("3"));

		const int expected = 3;

		//exercise
		var actual = 0;
		foreach (var o in sut) {
			actual++;
		}

		//verify
		Assert.That(actual, Is.EqualTo(expected));
	}

//	[Test] これはC#では未実装
//	public void 要素を３つ追加してwhileで回す(){
//		//setUp
//		TestClass sut = new TestClass();
//		sut.Add(new OneClass("1"));
//		sut.Add(new OneClass("2"));
//		sut.Add(new OneClass("3"));
//
//		int expected = 3;
//
//		//exercise
//		int actual = 0;
//		while (sut.hasNext()) {
//			sut.next();
//			actual++;
//		}
//
//		//verify
//		Assert.That(actual, Is.EqualTo(expected));
//	}

	[Test]
	public void 要素を３つ追加してgetSで取得する(){
		//setUp
		var sut = new TestClass();
		sut.Add(new OneClass("1"));
		sut.Add(new OneClass("2"));
		sut.Add(new OneClass("3"));

		const string expected = "123";

		//exercise
		var sb = new StringBuilder();
		foreach (var o in sut) {
			sb.Append(o.GetS());
		}
		var actual = sb.ToString();

		//verify
		Assert.That(actual, Is.EqualTo(expected));
	}

	[Test]
	public void 要素を３つ追加してremobveで一部の要素を削除する(){
		//setUp
		var sut = new TestClass();
		sut.Add(new OneClass("1"));
		sut.Add(new OneClass("2"));
		sut.Add(new OneClass("3"));
		sut.Remove(0);

		const string expected = "23";

		//exercise
		var sb = new StringBuilder();
		foreach (var o in sut) {
			sb.Append(o.GetS());
		}
		var actual = sb.ToString();

		//verify
		Assert.That(actual, Is.EqualTo(expected));
	}
}
}
