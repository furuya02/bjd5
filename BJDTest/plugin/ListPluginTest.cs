using Bjd;
using Bjd.net;
using Bjd.option;
using Bjd.plugin;
using NUnit.Framework;

namespace BjdTest.plugin {

    public class ListPluginTest {
	
	[Test]
	public void Pluginsフォルダの中のdllファイルを列挙() {
		//setUp
		const string currentDir = @"C:\tmp2\bjd5\BJD\out";

        var sut = new ListPlugin(currentDir);
		const int expected = 17; 
		//exercise
		var actual = sut.Count; 
		//verify
		Assert.That(actual, Is.EqualTo(expected));
	}
	
	[Test]
	public void Option及びServerインスタンスの生成() {

		var kernel = new Kernel();
        const string currentDir = @"C:\tmp2\bjd5\BJD\out";


		var sut = new ListPlugin(string.Format("{0}\\bin\\plugins", currentDir));
        foreach (var onePlugin in sut) {
			//Optionインスタンス生成
			var oneOption = onePlugin.CreateOption(kernel,"Option","nameTag");
			Assert.NotNull(oneOption);
			
			//Serverインスタンス生成
			var conf = new Conf(oneOption);
			var oneBind = new OneBind(new Ip(IpKind.V4Localhost), ProtocolKind.Tcp);
			var oneServer = onePlugin.CreateServer(kernel, conf, oneBind);
			Assert.NotNull(oneServer);
		}
	}

}

}
