using System;
using System.Collections.Generic;
using System.Drawing;
using Bjd;
using Bjd.net;
using Bjd.option;
using Bjd.ctrl;
using Bjd.util;
using NUnit.Framework;


namespace BjdTest.option{

    [TestFixture]
    internal class OneValTest{

        [TestCase(CtrlType.CheckBox, true, "true")]
        [TestCase(CtrlType.CheckBox, false, "false")]
        [TestCase(CtrlType.Int, 100, "100")]
        [TestCase(CtrlType.Int, 0, "0")]
        [TestCase(CtrlType.Int, -100, "-100")]
        [TestCase(CtrlType.File, "c:\\test.txt", "c:\\test.txt")]
        [TestCase(CtrlType.Folder, "c:\\test", "c:\\test")]
        [TestCase(CtrlType.TextBox, "abcdefg１２３", "abcdefg１２３")]
        [TestCase(CtrlType.Radio, 1, "1")]
        [TestCase(CtrlType.Radio, 5, "5")]
        [TestCase(CtrlType.Font, null, "Microsoft Sans Serif,10,Regular")]
        [TestCase(CtrlType.Memo, "1\r\n2\r\n3\r\n", "1\t2\t3\t")]
        [TestCase(CtrlType.Memo, "123", "123")]
        [TestCase(CtrlType.Hidden, null, "0t9GC1bkpWNzg1uea3drbQ==")] //その他はA004でテストする
        [TestCase(CtrlType.AddressV4, "192.168.0.1", "192.168.0.1")]
        //[TestCase(CtrlType.Dat, new Dat(new CtrlType[] { CtrlType.TextBox, CtrlType.TextBox }), "")] // CtrlDatはTESTBOX×2で初期化されている
        [TestCase(CtrlType.Dat, null, "")] // CtrlDatはTESTBOX×2で初期化されている
        //[TestCase(CtrlType.BindAddr, new BindAddr(), "V4ONLY,INADDR_ANY,IN6ADDR_ANY_INIT")]
        [TestCase(CtrlType.BindAddr, null, "V4ONLY,INADDR_ANY,IN6ADDR_ANY_INIT")]
        //[TestCase(CtrlType.BindAddr, new BindAddr(BindStyle.V4ONLY, new Ip(InetKind.V4), new Ip(InetKind.V6)), "V4ONLY,0.0.0.0,::0")]
        [TestCase(CtrlType.ComboBox, 0, "0")] 
        [TestCase(CtrlType.ComboBox, 1, "1")]
        public void デフォルト値をtoRegで取り出す(CtrlType ctrlType, Object val, String expected) {
            //setUp
            const bool isSecret = false;
            var sut = Assistance.CreateOneVal(ctrlType, val);
            //exercise
            var actual = sut.ToReg(isSecret);
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase(CtrlType.CheckBox, "true")]
        [TestCase(CtrlType.CheckBox, "false")]
        [TestCase(CtrlType.Int, "100")]
        [TestCase(CtrlType.Int, "0")]
        [TestCase(CtrlType.File, "c:\\test.txt")]
        [TestCase(CtrlType.Folder, "c:\\test")]
        [TestCase(CtrlType.TextBox, "abcdefg１２３")]
        [TestCase(CtrlType.Radio, "1")]
        [TestCase(CtrlType.Radio, "0")]
        [TestCase(CtrlType.Font, "Times New Roman,2,Bold")]
        [TestCase(CtrlType.Font, "ＭＳ ゴシック,1,Strikeout")]
        [TestCase(CtrlType.Font, "Arial,1,Bold")]
        [TestCase(CtrlType.Font, "Arial,1,Italic")]
        [TestCase(CtrlType.Font, "Arial,1,Underline")]
        [TestCase(CtrlType.Memo, "1\t2\t3\t")]
        [TestCase(CtrlType.Hidden, "qmw+Wuj6Y3f3WlWdncmLEQ==")]
        [TestCase(CtrlType.Hidden, "Htt+6zREaQU3sc7UrnAWHQ==")]
        [TestCase(CtrlType.AddressV4, "192.168.0.1")]
        [TestCase(CtrlType.Dat, "\tn1\tn2")]
        [TestCase(CtrlType.Dat, "\tn1\tn2\b\tn1#\tn2")]
        [TestCase(CtrlType.BindAddr, "V4Only,INADDR_ANY,IN6ADDR_ANY_INIT")]
        [TestCase(CtrlType.BindAddr, "V6Only,198.168.0.1,ffe0::1")]
        [TestCase(CtrlType.ComboBox, "1")]
        public void FromRegで設定した値をtoRegで取り出す(CtrlType ctrlType, String str){
            //setUp
            const bool isSecret = false;
            OneVal sut = Assistance.CreateOneVal(ctrlType, null);
            sut.FromReg(str);
            var expected = str;
            //exercise
            String actual = sut.ToReg(isSecret);
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase(CtrlType.CheckBox, "true", true)]
        [TestCase(CtrlType.CheckBox, "TRUE", true)]
        [TestCase(CtrlType.CheckBox, "false", true)]
        [TestCase(CtrlType.CheckBox, "FALSE", true)]
        [TestCase(CtrlType.CheckBox, "t", false)] // 不正入力
        [TestCase(CtrlType.CheckBox, "", false)] // 不正入力
        [TestCase(CtrlType.Int, "-100", true)]
        [TestCase(CtrlType.Int, "0", true)]
        [TestCase(CtrlType.Int, "aaa", false)] // 不正入力
        [TestCase(CtrlType.File, "c:\\test.txt", true)]
        [TestCase(CtrlType.Folder, "c:\\test", true)]
        [TestCase(CtrlType.TextBox, "abcdefg１２３", true)]
        [TestCase(CtrlType.Radio, "0", true)]
        [TestCase(CtrlType.Radio, "5", true)]
        [TestCase(CtrlType.Radio, "-1", false)] //不正入力 Radioは0以上
        [TestCase(CtrlType.Font, "Default,-1,1", false)] //不正入力(styleが無効値)
        [TestCase(CtrlType.Font, "Default,2,-1", false)] //不正入力(sizeが0以下)
        [TestCase(CtrlType.Font, "XXX,1,8", false)] //　C#:エラー Java:(Font名ではエラーが発生しない)
        [TestCase(CtrlType.Font, "Serif,1,-1", false)] //不正入力
        [TestCase(CtrlType.Memo, null, false)] //不正入力
        [TestCase(CtrlType.Hidden, null, false)] //不正入力
        [TestCase(CtrlType.AddressV4, null, false)] //不正入力
        [TestCase(CtrlType.AddressV4, "xxx", false)] //不正入力
        [TestCase(CtrlType.AddressV4, "1", false)] //不正入力
        [TestCase(CtrlType.Dat, "", false)] //不正入力
        [TestCase(CtrlType.Dat, null, false)] //不正入力
        [TestCase(CtrlType.Dat, "\tn1", false)] //不正入力(カラム不一致)
        [TestCase(CtrlType.BindAddr, null, false)] //不正入力
        [TestCase(CtrlType.BindAddr, "XXX", false)] //不正入力
        [TestCase(CtrlType.ComboBox, "XXX", false)] //不正入力
        [TestCase(CtrlType.ComboBox, null, false)] //不正入力
        [TestCase(CtrlType.ComboBox, "2", false)] //不正入力 list.size()オーバー
        public void FromRegの不正パラメータ判定(CtrlType ctrlType, String str, bool expected){
            //setUp
            var sut = Assistance.CreateOneVal(ctrlType, null);
            //exercise
            var actual = sut.FromReg(str);
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase(CtrlType.Hidden, true, "123", "***")]
        [TestCase(CtrlType.Hidden, false, "123", "qmw+Wuj6Y3f3WlWdncmLEQ==")]
        [TestCase(CtrlType.Hidden, false, "", "0t9GC1bkpWNzg1uea3drbQ==")]
        [TestCase(CtrlType.Hidden, false, null, "0t9GC1bkpWNzg1uea3drbQ==")]
        [TestCase(CtrlType.Hidden, false, "本日は晴天なり", "Htt+6zREaQU3sc7UrnAWHQ==")] 
        public void IsDebugTrueの時のToReg出力(CtrlType ctrlType, bool isDebug, String str, String expected){
            //setUp
            OneVal sut = Assistance.CreateOneVal(ctrlType, str);
            //exercise
            String actual = sut.ToReg(isDebug);
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase(CtrlType.CheckBox, true)]
        [TestCase(CtrlType.Hidden, "123")]
        [TestCase(CtrlType.TextBox, "123")]
        [TestCase(CtrlType.Memo, "123\n123")]
        [TestCase(CtrlType.CheckBox, true)]
        [TestCase(CtrlType.Int, 0)]
        [TestCase(CtrlType.Folder, "c:\\test")]
        [TestCase(CtrlType.TextBox, "abcdefg１２３")]
        [TestCase(CtrlType.Radio, 1)]
        //[TestCase(CtrlType.Font, new Font("Times New Roman", Font.ITALIC, 15))]
        [TestCase(CtrlType.Memo, "1\r\n2\r\n3\r\n")]
        //[TestCase(CtrlType.AddressV4, new Ip(IpKind.V4Localhost))]
        //[TestCase(CtrlType.AddressV4, new Ip(IpKind.V6Localhost))] //追加
        //×[TestCase(CtrlType.Dat, new Dat(new CtrlType[] { CtrlType.TextBox, CtrlType.TextBox }))]
        //[TestCase(CtrlType.BindAddr, new BindAddr())]
        [TestCase(CtrlType.ComboBox, 0)]
        public void ReadCtrlFalseでデフォルトの値に戻るかどうかのテスト(CtrlType ctrlType, Object value){
            //setUp
            var sut = Assistance.CreateOneVal(ctrlType, value);
            var tabindex = 0;
            sut.CreateCtrl(null, 0, 0, ref tabindex);
            var b = sut.ReadCtrl(false); //isConfirm = false; 確認のみではなく、実際に読み込む
            Assert.IsTrue(b); // readCtrl()の戻り値がfalseの場合、読み込みに失敗している
            var expected = value;
            //exercise
            var actual = sut.Value;
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }
    }


    internal class Assistance{
        //OneValの生成
		//デフォルト値(nullを設定した場合、適切な値を自動でセットする)
		public static OneVal CreateOneVal(CtrlType ctrlType, Object val) {
			//Kernel kernel = new Kernel();
			const string help = "help";
			OneCtrl oneCtrl;
		    switch (ctrlType) {
				case CtrlType.CheckBox:
					if (val == null) {
						val = true;
					}
					oneCtrl = new CtrlCheckBox(help);
					break;
				case CtrlType.Int:
					if (val == null) {
						val = 1;
					}
					oneCtrl = new CtrlInt(help, 3); // ３桁で決め打ち
					break;
				case CtrlType.File:
					if (val == null) {
						val = "1.txt";
					}
					oneCtrl = new CtrlFile(help, 200, new Kernel());
					break;
				case CtrlType.Folder:
					if (val == null) {
						val = "c:\temp";
					}
					oneCtrl = new CtrlFolder(help, 200,  new Kernel());
					break;
				case CtrlType.TextBox:
					if (val == null) {
						val = "abc";
					}
					oneCtrl = new CtrlTextBox(help, 20);
					break;
				case CtrlType.Radio:
					if (val == null) {
						val = 0;
					}
					oneCtrl = new CtrlRadio(help, new[] { "1", "2", "3" }, 30, 3);
					break;
				case CtrlType.Font:
					if (val == null){
					    val = new Font("MS ゴシック", 10f);
					}
                    oneCtrl = new CtrlFont(help, LangKind.Jp);
					break;
				case CtrlType.Memo:
					if (val == null) {
						val = "1";
					}
					oneCtrl = new CtrlMemo(help, 10, 10);
					break;
				case CtrlType.Hidden:
					if (val == null) {
						val = "";
					}
					oneCtrl = new CtrlHidden(help, 30);
					break;
				case CtrlType.AddressV4:
					if (val == null) {
						val = "";
					}
					oneCtrl = new CtrlAddress(help);
					break;
				case CtrlType.BindAddr:
					if (val == null) {
						val = "V4ONLY,INADDR_ANY,IN6ADDR_ANY_INIT";
					}
					var list = new List<Ip>();
					try {
						list.Add(new Ip(IpKind.InAddrAny));
						list.Add(new Ip("192.168.0.1"));
					} catch (ValidObjException ex) {
						Assert.Fail(ex.Message);

					}
					oneCtrl = new CtrlBindAddr(help, list.ToArray(), list.ToArray());
					break;
				case CtrlType.ComboBox:
					//listを{"1","2"}で決め打ち

					if (val == null) {
						val = 0;
					}
					oneCtrl = new CtrlComboBox(help, new[] { "1", "2" }, 10);
					break;
				case CtrlType.Dat:
					//カラムはTEXTBOX×2で決め打ち
					var listVal = new ListVal{
					    new OneVal("name1", true, Crlf.Nextline, new CtrlCheckBox("help")),
					    new OneVal("name2", true, Crlf.Nextline, new CtrlCheckBox("help"))
					};

			        if (val == null) {
						val = new Dat(new[] { CtrlType.CheckBox, CtrlType.CheckBox });
					}

                    oneCtrl = new CtrlDat(help, listVal, 300, LangKind.Jp);
					break;
				default:
					throw new Exception(ctrlType.ToString());
			}
			return new OneVal("name", val, Crlf.Nextline, oneCtrl);
		}
    }
}
