using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using Pop3Server;

namespace Pop3ServerTest{
    internal class ApopTest{
        [TestCase("user1", "user1", true)]
        [TestCase("user1", "xxx", false)] //パスワードが間違えた場合失敗する
        [TestCase("user4", "", false)] //登録されていないユーザは失敗する
        [TestCase("user3", "", false)] //パスワードが無効のユーザは、失敗する
        public void APopAuthによる認証_チャレンジ文字列対応(string user, string pass, bool expected){
            //setUp
            const string challengeStr = "solt";
            byte[] data = Encoding.ASCII.GetBytes(challengeStr + pass);
            MD5 md5 = new MD5CryptoServiceProvider();


            byte[] result = md5.ComputeHash(data);
            var sb = new StringBuilder();
            for (int i = 0; i < 16; i++){
                sb.Append(string.Format("{0:x2}", result[i]));
            }

            //exercise
            //MailBoxの設定がuser=passだった場合のテスト
            //パラメータのpassはクライアントからの入力と仮定する
            var actual = APop.Auth(user, user,challengeStr, sb.ToString());
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}
