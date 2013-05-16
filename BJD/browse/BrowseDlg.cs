using System;
using System.Linq;
using System.Windows.Forms;
using System.Threading;
using Bjd.ctrl;
using Bjd.remote;
using Bjd.sock;

namespace Bjd.browse
{
    public partial class BrowseDlg:Form {
        readonly Kernel _kernel;

        readonly CtrlType _ctrlType;
        readonly SockTcp _sockTcp;
        readonly BrowseData _browseData;

        public BrowseDlg(Kernel kernel,SockTcp sockTcp,CtrlType ctrlType) {
            InitializeComponent();

            _kernel = kernel;
            _sockTcp = sockTcp;
            _ctrlType = ctrlType;

            _browseData = new BrowseData(textBox,buttonOk,_ctrlType);

            listView.Columns.Add(kernel.IsJp() ? "名前" : "name");
            listView.Columns.Add(kernel.IsJp() ? "サイズ" : "size");
            listView.Columns[1].TextAlign = HorizontalAlignment.Right;
            listView.Columns.Add(kernel.IsJp() ? "種類" : "type");
            listView.Columns.Add(kernel.IsJp() ? "更新日" : "date");

            kernel.WindowSize.Read(this);//ウインドサイズの復元
            kernel.WindowSize.Read(listView);//カラム幅の復元
        }
        public void Init() {
            Wait(true);
            //一覧情報の取得
            var info = GetBrowseInfo("");//ドライブ情報
            foreach(var str in info.Split('\t')) {
                var p = new OneBrowse(str);//オブジェクトを初期化
                var browseImage = BrowseImage.DriveFixed;
                string name;
                switch(p.BrowseKind) {
                    case BrowseKind.DriveFixed:
                        name = string.Format(_kernel.IsJp() ? "ローカルディスク ({0})" : "LocalDisk ({0})", p.Name);
                        break;
                    case BrowseKind.DriveCdrom:
                        name = string.Format(_kernel.IsJp() ? "DVD/DD RWドライブ ({0})" : "DVD/DD RW Drive ({0})", p.Name);
                        break;
                    case BrowseKind.DriveRemovable:
                        name = string.Format(_kernel.IsJp() ? "リムーバブル ディスク ({0})" : "Removable Disk ({0})", p.Name);
                        browseImage = BrowseImage.DriveCdrom;
                        break;
                    default:
                        continue;
                }
                var node = treeView.Nodes.Add(name);
                node.ImageIndex = (int)browseImage;
                node.SelectedImageIndex = (int)browseImage;
                node.Tag = p.Name;
            }
            Wait(false);
        }


        //ノードの検索
        TreeNode SearchNode(TreeNode parent,string name) {
            if(parent == null) {
                foreach(TreeNode node in treeView.Nodes) {
                    if(((string)node.Tag).ToLower() == name.ToLower()) {
                        return node;
                    }
                }
            } else {
                foreach(TreeNode node in parent.Nodes){
                    if (((string) node.Tag).ToLower() != name.ToLower()) continue;
                    return node;
                }
            }
            return null;
        }
        //リストビュー更新
        void RefreshList() {
            listView.Items.Clear();//リストビューの初期化

            //List<One_Browse> ar = new List<One_Browse>();
            var info = GetBrowseInfo(_browseData.Dir);
            var ar = info.Split('\t').Select(str => new OneBrowse(str)).ToList();

            //リストビューへの追加
            foreach(OneBrowse p in ar) {
                if(p.BrowseKind == BrowseKind.Dir) {
                    ListViewItem item = listView.Items.Add(p.Name);
                    item.SubItems.Add("");
                    item.SubItems.Add("ファイルフォルダ");
                    item.SubItems.Add(p.Dt.ToString());
                    item.ImageIndex = (int)BrowseImage.FolderClose;
                } else if(p.BrowseKind == BrowseKind.File) {
                    var item = listView.Items.Add(p.Name);
                    item.ImageIndex = (int)BrowseImage.File;
                    var size = p.Size / 1024;
                    if(p.Size != 0)
                        size += 1;
                    item.SubItems.Add(string.Format("{0}KB",size));//サイズ
                    item.SubItems.Add("");//種類
                    item.SubItems.Add(p.Dt.ToString());//日付
                }
            }
        }

        //ツリー更新
        void RefreshTree(TreeNode node,string path) {
            var info = GetBrowseInfo(path);
            var ar = info.Split('\t').Select(str => new OneBrowse(str)).ToList();
            foreach(var p in ar) {
                if (p.BrowseKind != BrowseKind.Dir) continue;
                //ツリービューへの追加
                var treeNode = node.Nodes.Add(p.Name);
                treeNode.Tag = p.Name;
                treeNode.ImageIndex = (int)BrowseImage.FolderClose;
                treeNode.SelectedImageIndex = (int)BrowseImage.FolderOpen;
            }
        }
        //ビューの更新
        TreeNode RefreshView() {
            TreeNode node = null;
            TreeNode result = null;
            Wait(true);
            var names = _browseData.Dir.Split('\\');
            var path = "";
            foreach(var name in names) {
                if(path == "")
                    path = name;
                else
                    path = path + "\\" + name;

                if(node != null && node.Nodes.Count == 0) {
                    RefreshTree(node,path);//ツリーの構築
                }
                if(name == "") {
                    _browseData.Set(path);//新しいパスの決定
                    RefreshList();//リストビュの更新

                    treeView.SelectedNode = node;//ツリービューの選択
                    result = treeView.SelectedNode;
                    goto end;
                }
                node = SearchNode(node,name);
                if(node == null)
                    break;
            }
        end:
            Wait(false);
            return result;
        }

        //ツリービュー（ディレクトリ一覧）の選択
        private void TreeViewAfterSelect(object sender,TreeViewEventArgs e) {
            var currentNode = treeView.SelectedNode;
            if(currentNode == null)
                return;

            //tツリーノードから現在パスの生成
            var node = currentNode;
            var path = "";//初期化
            while(true) {
                if(path == "")
                    path = (string)node.Tag;
                else
                    path = (string)node.Tag + "\\" + path;

                if(node.Parent == null)
                    break;
                node = node.Parent;
            }
            _browseData.Set(path + "\\");//パスの決定

            RefreshView();//ビューの更新
        }
        //リストビュー（ファイル一覧）のダブルクリック
        private void ListViewDoubleClick(object sender,EventArgs e){
            const bool doubleClick = true;
            SelectListView(doubleClick);
        }

        //リストビュー（ファイル一覧）の選択
        private void ListViewSelectedIndexChanged(object sender,EventArgs e){
            const bool doubleClick = false;
            SelectListView(doubleClick);
        }

        void SelectListView(bool doubleClick) {
            if(listView.SelectedItems.Count <= 0)
                return;
            var item = listView.SelectedItems[0];

            //クリックされたのはフォルダかどうかの判断
            if(item.ImageIndex == (int)BrowseImage.FolderClose) {//フォルダ
                if(!doubleClick)//ダブルクリックで無い場合は処置なし
                    return;
                _browseData.Set(_browseData.Dir + item.Text + "\\");//新しいパスの決定

                TreeNode node = RefreshView();//ビューの更新
                //リストビューで選択された場合は、選択したツリーを展開状態にする
                node.Expand();

            } else {//ファイル
                _browseData.Set(_browseData.Dir + item.Text);//新しいパスの決定
            }

        }

        //ウインドウのクローズ
        private void BrowseDlgFormClosed(object sender,FormClosedEventArgs e) {
            _kernel.WindowSize.Save(this);//ウインドサイズの保存
            _kernel.WindowSize.Save(listView);//カラム幅の保存
        }

        public string Result {
            get{
                return _ctrlType == CtrlType.Folder ? _browseData.Dir : _browseData.File;
            }
        }

        void Wait(bool sw) {
            if(sw) {
                Cursor.Current = Cursors.WaitCursor;//待機カーソル
                Enabled = false;
            } else {
                Cursor.Current = Cursors.Default;// カーソルを元に戻す
                Enabled = true;
            }
        }

        string _lines;
        string GetBrowseInfo(string path) {
            _lines = null;
            
            //(BrowseDlg用）データ要求(C->S)
            RemoteData.Send(_sockTcp,RemoteDataKind.CmdBrowse, path);

            while(_lines == null) {
                Thread.Sleep(100);
            }
            return _lines;
        }
        public void CmdRecv(string buffer) {
            _lines = buffer;
        }


    }
}
