using System;
using System.Collections.Generic;
using System.Reflection;
using Bjd;
using Bjd.ctrl;
using Bjd.option;
using Bjd.util;
using NUnit.Framework;

namespace BjdTest.ctrl{
    
    [TestFixture]
    internal class CtrlDatTest{
        [Test]
        public void PrivateメンバのimportDatとexportDatの整合性を確認する(){
            //const bool isJp = true;

            var list = new ListVal();
            list.Add(new OneVal("combo", 0, Crlf.Nextline,
                                new CtrlComboBox("コンボボックス", new[]{"DOWN", "PU", "FULL"}, 200)));
            list.Add(new OneVal("fileName2", "c:\\work", Crlf.Nextline,
                                new CtrlFolder("フォルダ", 30, new Kernel())));
            list.Add(new OneVal("text", "user1", Crlf.Nextline, new CtrlTextBox("テキスト入力", 30)));
            list.Add(new OneVal("hidden", "123", Crlf.Nextline, new CtrlHidden("パスワード", 30)));
            var sut = new CtrlDat("help", list, 100, LangKind.Jp);
            var tabindex = 0;
            sut.Create(null, 0, 0, null,ref tabindex);

            var input = new List<String>();
            input.Add("#	0	c:\\work	user1	c3a5e1369325e2ca");
            input.Add(" 	1	c:\\work	user2	b867684066caf9dc");
            input.Add(" 	2	c:\\work	user3	4911d0d49c8911ed");

            try{
                //リフレクションによるprivateメンバへのアクセス
                var cls = sut;
                var type = cls.GetType();
                var exportDat = type.GetMethod("ExportDat", BindingFlags.NonPublic | BindingFlags.Instance);
                var importDat = type.GetMethod("ImportDat", BindingFlags.NonPublic | BindingFlags.Instance);
                importDat.Invoke(cls, new object[]{input});
                var output = (List<String>) exportDat.Invoke(cls, new object[]{});
                for (var i = 0; i < input.Count; i++){
                    Assert.That(input[i], Is.EqualTo(output[i]));
                }
            }
            catch (Exception e){
                Assert.Fail(e.Message);
            }
        }

    }
}



