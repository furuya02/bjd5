using System;
using System.Linq;
using Bjd.net;
using Bjd.option;
using Bjd.plugin;
using Bjd.util;

namespace Bjd.server{
    public class ListServer : ListBase<OneServer>, IDisposable{

        private Kernel kernel;

        public ListServer(Kernel kernel, ListPlugin listPlugin){
            this.kernel = kernel;

            Initialize(listPlugin);
        }

        //名前によるサーバオブジェクト(OneServer)の検索
        //一覧に存在しない名前で検索を行った場合、設計上の問題として処理される
        public OneServer Get(String nameTag){
            foreach (OneServer oneServer in Ar){
                if (oneServer.NameTag == nameTag){
                    return oneServer;
                }
            }
            Util.RuntimeException(string.Format("nameTag={0}", nameTag));
            return null;
        }

        // 初期化
        private void Initialize(ListPlugin listPlugin){
            Ar.Clear();

            //Java fix
            if (kernel.RunMode == RunMode.Remote){
                return;
            }

            foreach (OneOption op in kernel.ListOption){

                if (!op.UseServer){
                    //サーバオプション以外は対象外にする
                    continue;
                }

                //プラグイン情報の検索
                OnePlugin onePlugin = listPlugin.Get(op.NameTag);
                //			if (onePlugin == null) {
                //				//設計上の問題
                //				Util.RuntimeException(string.Format("ListServer.initialize() listPlugin.get(%s)==null", op.getNameTag()));
                //			}

                if (op.NameTag.IndexOf("Web-") == 0){

                    //既に同一ポートで仮想サーバがリストされている場合はサーバの生成は行わない
                    bool find = false;
                    int port = (int) op.GetValue("port");
                    BindAddr bindAddr = (BindAddr) op.GetValue("bindAddress2");
                    foreach (OneServer sv in Ar){
                        if (sv.NameTag.IndexOf("Web-") == 0){
                            OneOption o = kernel.ListOption.Get(sv.NameTag);
                            if (o != null){
                                //同一ポートの設定が既にリストされているかどうか
                                if (port == (int) o.GetValue("port")){
                                    // バインドアドレスが競合しているかどうか
                                    if (bindAddr.CheckCompetition((BindAddr) o.GetValue("bindAddress2"))){
                                        find = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    if (!find){
                        AddServer(new Conf(op), onePlugin); //サーバ（OneServer）生成
                    }
                }
                else{
                    AddServer(new Conf(op), onePlugin); //サーバ（OneServer）生成
                }
            }
        }

        //サーバ（OneServer）の生成
        private void AddServer(Conf conf, OnePlugin onePlugin){

            var protocol = (ProtocolKind)conf.Get("protocolKind");
            //ProtocolKind protocol = ProtocolKind.ValueOf((int) conf.Get("protocolKind"));

            BindAddr bindAddr = (BindAddr) conf.Get("bindAddress2");

            if (bindAddr.BindStyle != BindStyle.V4Only){
                var oneBind = new OneBind(bindAddr.IpV6, protocol);
                var o = onePlugin.CreateServer(kernel, conf, oneBind);
                if (o != null){
                    Ar.Add((OneServer) o);
                }
            }
            if (bindAddr.BindStyle != BindStyle.V6Only){
                var oneBind = new OneBind(bindAddr.IpV4, protocol);
                var o = onePlugin.CreateServer(kernel, conf, oneBind);
                if (o != null){
                    Ar.Add((OneServer) o);
                }
            }
        }

        //１つでも起動中かどうか
        public bool IsRunnig(){
            //全スレッドの状態確認
            foreach (var sv in Ar){
                if (sv.ThreadBaseKind==ThreadBaseKind.Running){
                    return true;
                }
            }
            return false;
        }

        //サーバ停止処理
        public void Stop(){
            //全スレッド停止 
            foreach (OneServer sv in Ar) {
                sv.Stop();
            }
            //Java fix Ver5.9.0
            GC.Collect();
        }


        //サーバ開始処理
        public void Start(){
            if (IsRunnig()){
                return;
            }
            //全スレッドスタート
            foreach (var sv in Ar){
                sv.Start();
            }
            //Java fix Ver5.9.0
            GC.Collect();
        }
    }
}
