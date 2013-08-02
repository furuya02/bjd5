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

2013.06.28 Ver5.9.2
(1) オプションの読み込み(プロキシSMTPの拡張設定)に失敗するバグを修正
(2) HTTPSサーバの動作不良を修正

2013.08.03 Ver5.9.3
(1)SMTPサーバにおいてヘッダ変換時に改行が削除されてしまうバグを修正
(2)SMTPサーバにおいてAUTHコマンドのパラメータが小文字に対応できていないバグを修正
(3)SMTPさーばにおいてメールボックスへの格納時のログを修正

2013.08.xx Ver5.9.4
(1)



[C# next]

SaveMailの見直し

Fetchのリファクタリング(一つ前の作業)
OK=>OneFetchJob.Jobで、RETRの後のMAIL保存が完成したら、Job2と置き換える（Job2は破棄）

Agentのリファクタリング（現在の作業）

※クラスSmtpClient作成中
SmtpClientTest_PopBeforeSmtp


PopClientもAPOPに対応させる


自動受信
OK=>サーバに残すー0日　でサーバから削除されてしまう
OK=>保存したメールのFromがおかしい


SockObj Kernelパラメータは、トレースのみに使用されている
トレースを扱うオブジェクトを作成して、Kernelと置き換え、トレースを使用しない時は、ダミーnew TraceObj() でも動作するようにする


DHCPでWINS情報
HTTP/0.9

[Java next]
VerDlg
WindowsSize
RemoteServer Client


