var fso = new ActiveXObject("Scripting.FileSystemObject");

//バージョン
var version = "6.0.1";
//ソースコードのフォルダ
//var srcDir = fso.GetFolder("C:\\tmp2\\bjd5");
var currentDir = WScript.CreateObject("WScript.Shell").CurrentDirectory;
var srcDir = fso.GetFolder(currentDir);


//var files = srcDir.Files;//階層下のファイルの一覧
var dirs = srcDir.SubFolders;//階層下のフォルダの一覧
var ar = new Enumerator(dirs);
for (; !ar.atEnd(); ar.moveNext()){
	var file =  ar.item()+"\\Properties\\AssemblyInfo.cs";
	if(fso.FileExists(file)==1){//存在確認
		EditFile(file);//編集
	}
}
WScript.Echo("バージョンを"+version+"に修正しました")

//ファイルをオープンして「」の行を編集する
function EditFile(path){

	var charset="utf-8";
	var str = adoLoadText(path,charset);

	var tmp = "";
	var ar = new Enumerator(str.split("\r\n"));//行単位で処理する
	for (; !ar.atEnd(); ar.moveNext()){
		//該当行は編集して１行保存
		if(ar.item().indexOf("[assembly: AssemblyVersion(")==0){
			tmp = tmp + "[assembly: AssemblyVersion(\"" + version + "\")]"+"\r\n"
		}else if(ar.item().indexOf("[assembly: AssemblyFileVersion(")==0){
			tmp = tmp + "[assembly: AssemblyFileVersion(\"" + version + "\")]"+"\r\n"
		}else{//その他はそのまま１行保存
			tmp = tmp + ar.item() +"\r\n";
		}
	}
	adoSaveText(path,tmp, charset);
}


function adoLoadText(filename, charset) {
	var stream, text;
	stream = new ActiveXObject("ADODB.Stream");
	stream.type = 2; //2:TypeText
	stream.charset = charset;
	stream.open();
	stream.loadFromFile(filename);
	text = stream.readText(-1);//-1:ReadAll
	stream.close();
	return text;
}

function adoLoadLinesOfText(filename, charset) {
  var stream;
  var lines = new Array();
  stream = new ActiveXObject("ADODB.Stream");
  stream.type = adTypeText;
  stream.charset = charset;
  stream.open();
  stream.loadFromFile(filename);
  while (!stream.EOS) {
    lines.push(stream.readText(adReadLine));
  }
  stream.close();
  return lines;
}

function adoSaveText(filename, text, charset) {
  var stream;
  stream = new ActiveXObject("ADODB.Stream");
  stream.type = 2;//2:TypeText;
  stream.charset = charset;
  stream.open();
  stream.writeText(text);
  stream.saveToFile(filename, 2);//2:adSaveCreateOverWrite
  stream.close();
}


