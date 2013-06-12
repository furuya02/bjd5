2013.06.04 Ver5.9.0
(1) 終了時にオプション設定のテンポラリファイルが残ってしまうバグを修正($Remote.ini Tmp.ini)
(2) ThreadBaseTest追加
(3) プロキシPOP3及びプロキシSMTPの多重ログインによる誤動作を修正
(4) Webサーバにおいて、不正なリクエストのURLエンコードで発生する例外に対処
(5) Ftpサーバにおいて、LISTコマンドで発生する例外に対処
(6) プロキシーサーバにおけるメモリリークを修正

2013.06.13 Ver5.9.1
(1) WebサーバにおいSSIの#include指定で、CGI以外の入力でヘッダ処理をしてしまうバグを修正
(2) 旧バージョンのオプションの読み込みに失敗するバグを修正

2013.06.xx Ver5.9.2


[C# next]
SmtpServerを依存関係の少ない小さなクラスに分割していく

Mail からLoggerを分離する

SmtpServer.Data.csの内部にMailプロパティを作成してSession.Mailと分離する
Data.Recvが成功してから、Session.Mailにコピーする

SmtpServer.Data.csのテスト作成

Server.RecvLineはData.Recvに乗せ換え
たたし、OneFetchでもServer.RecvLineが使用されているので、
後ほど、これもData.Recvに変えてからServer.RecvLineを破棄する


[Java next]
VerDlg
WindowsSize
RemoteServer Client


