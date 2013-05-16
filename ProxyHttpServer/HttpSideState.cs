namespace ProxyHttpServer {
    enum HttpSideState {
        Non,

        ClientSideRecvRequest,//リクエスト受信完了 HTTP(CLIENT_SIDE)
        ClientSideSendHeader,//ヘッダ送信完了      HTTP(CLIENT_SIDE)
        ClientSideSendBody,//本体送信完了          HTTP(CLIENT_SIDE)

        ServerSideSendHeader,//リクエスト送信完了  HTTP(SERVER_SIDE)
        ServerSideSendBody,//本体送信完了  HTTP(SERVER_SIDE)
        ServerSideRecvHeader,//ヘッダ受信完了      HTTP(SERVER_SIDE) 
        ServerSideRecvBody//本体受信完了           HTTP(SERVER_SIDE)
    }
}
