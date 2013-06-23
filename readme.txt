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
(1) オプションの読み込み(プロキシSMTPの拡張設定)に失敗するバグを修正
(2) HTTPSサーバの動作不良を修正



[C# next]

SaveMailの見直し
Fetchのリファクタリング(現在作業中)
Agentのリファクタリング(クラスSmtpClient作成)

DHCPでWINS情報
HTTP/0.9

[Java next]
VerDlg
WindowsSize
RemoteServer Client


