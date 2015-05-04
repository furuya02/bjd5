using System;
using System.Collections.Generic;
using System.Text;
using Bjd.ctrl;
using Bjd.option;
using Bjd.util;
using NUnit.Framework;

namespace BjdTest.option{
    //テストでは、リソースの開放（dispose）を省略する
    internal class ListValTest{

        //テスト用のListVal作成(パターン１)
        private ListVal CreateListVal1(){

            var listVal = new ListVal();
            listVal.Add(new OneVal("n1", 1, Crlf.Nextline, new CtrlInt("help", 10)));
            listVal.Add(new OneVal("n2", 1, Crlf.Nextline, new CtrlInt("help", 10)));

            var datList = new ListVal();
            datList.Add(new OneVal("n3", 1, Crlf.Nextline, new CtrlInt("help", 10)));
            datList.Add(new OneVal("n4", 1, Crlf.Nextline, new CtrlInt("help", 10)));
            listVal.Add(new OneVal("n5", 1, Crlf.Nextline, new CtrlDat("help", datList, 10, LangKind.Jp)));

            datList = new ListVal();
            datList.Add(new OneVal("n6", 1, Crlf.Nextline, new CtrlInt("help", 10)));
            datList.Add(new OneVal("n7", 1, Crlf.Nextline, new CtrlInt("help", 10)));
            listVal.Add(new OneVal("n8", 1, Crlf.Nextline, new CtrlDat("help", datList, 10, LangKind.Jp)));

            return listVal;
        }

        //テスト用のListVal作成(パターン２)
        private ListVal CreateListVal2(){

            var listVal = new ListVal();

            var pageList = new List<OnePage>();

            var onePage = new OnePage("page1", "ページ１");
            onePage.Add(new OneVal("n0", 1, Crlf.Nextline, new CtrlInt("help", 10)));
            pageList.Add(onePage);

            onePage = new OnePage("page2", "ページ２");
            onePage.Add(new OneVal("n1", 1, Crlf.Nextline, new CtrlInt("help", 10)));
            pageList.Add(onePage);

            listVal.Add(new OneVal("n2", null, Crlf.Nextline, new CtrlTabPage("help", pageList)));
            return listVal;
        }

        //listValを名前一覧（文字列）に変換する
        private String ArrayToString(IEnumerable<OneVal> list){
            var sb = new StringBuilder();
            foreach (var o in list){
                sb.Append(o.Name);
                sb.Append(",");
            }
            return sb.ToString();
        }

        [Test]
        public void パターン１で作成したListValをgetListで取得する(){
            //setUp
            var sut = CreateListVal1();
            const string expected = "n1,n2,n3,n4,n5,n6,n7,n8,";

            //exercise
            var actual = ArrayToString(sut.GetList(null));

            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void パターン２で作成したListValをgetListで取得する(){
            //setUp
            var sut = CreateListVal2();
            const string expected = "n0,n1,n2,";

            //exercise
            var actual = ArrayToString(sut.GetList(null));

            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void 存在するデータを検査するとnull以外が返る(){
            //setUp
            var sut = CreateListVal1();

            //exercise
            var actual = sut.Search("n1");

            //verify
            Assert.IsNotNull(actual);
        }

        [Test]
        public void 存在しないデータを検査するとnullが返る() {
            //setUp
            var sut = CreateListVal1();

            //exercise
            var actual = sut.Search("xxx");

            //verify
            Assert.IsNull(actual);
        }

    }
}
