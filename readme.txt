2013.06.04 Ver5.9.0
(1) 終了時にオプション設定のテンポラリファイルが残ってしまうバグを修正($Remote.ini Tmp.ini)
(2) ThreadBaseTest追加
(3) プロキシPOP3及びプロキシSMTPの多重ログインによる誤動作を修正
(4) Webサーバにおいて、不正なリクエストのURLエンコードで発生する例外に対処
(5) Ftpサーバにおいて、LISTコマンドで発生する例外に対処
(6) プロキシーサーバにおけるメモリリークを修正

2013.06.0x Ver5.9.1
(1) 


[C# next]
SMTPServer test
ドメイン外への中継・中継拒否

SmtpServer
Server.csを最初から順番に読んで行って、最小単位が見つかったら順次テストを追加する
その過程で、簡単なリファクタリング（名前変更など）があったら、そのまま実施する

ぼちぼち・・・・SmtpServerを依存関係の少ない小さなクラスに分割していく

今、PopBeforeSmtp



[Java next]
VerDlg
WindowsSize
RemoteServer Client


