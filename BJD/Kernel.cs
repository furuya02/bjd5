using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using Bjd.browse;
using Bjd.log;
using Bjd.mail;
using Bjd.net;
using Bjd.option;
using Bjd.plugin;
using Bjd.remote;
using Bjd.server;
using Bjd.service;
using Bjd.sock;
using Bjd.tool;
using Bjd.trace;
using Bjd.util;
using Bjd.wait;
using Menu = Bjd.menu.Menu;

namespace Bjd{
    public class Kernel : IDisposable{

        //�v���Z�X�N�����ɏ����������ϐ�
        public RunMode RunMode { get; set; } //�ʏ�N��;
        public bool EditBrowse { get; private set; } //�u�Q�Ɓv�̃e�L�X�g�{�b�N�X�̕ҏW
        public Wait Wait { get; private set; }
        public RemoteConnect RemoteConnect { get; set; } //�����[�g����Őڑ�����Ă��鎞���������������
        public RemoteClient RemoteClient { get; private set; }
        public TraceDlg TraceDlg { get; private set; } //�g���[�X�\��
        public DnsCache DnsCache { get; private set; }
        public Ver Ver { get; private set; }
        public View View { get; private set; }
        public LogView LogView { get; private set; }
        public WindowSize WindowSize { get; private set; }
        public Menu Menu { get; private set; }
        private readonly bool _isTest; //TEST�p��Kernel�𐶐�����ꍇ�Atrue�ɐݒ肳���
        public MailBox MailBox { get; private set; }

    

        //�T�[�o�N�����ɍŏ����������ϐ�
        public ListOption ListOption { get; private set; }
        public ListServer ListServer { get; private set; }
        public ListTool ListTool { get; private set; } //�c�[���Ǘ�
        public LogFile LogFile { get; private set; }
        private bool _isJp = true;
        private Logger _logger;

        //Ver5.9.6
        public WebApi WebApi { get; private set; }
        
        //Ver5.8.6
        public IniDb IniDb { get; private set; }

    
        //private MailBox mailBox = null; //���ۂɕK�v�ɂȂ������ɐ��������(SMTP�T�[�o�Ⴕ����POP3�T�[�o�̋N����)


        public bool IsJp(){
            return _isJp;
            //return (lang == Lang.JP) ? true : false;
        }

        public string ServerName{
            get{
                var oneOption = ListOption.Get("Basic");
                if (oneOption != null){
                    return (String) oneOption.GetValue("serverName");
                }
                return "";
            }
        }

        //�e�X�g�p�R���X�g���N�^
        public Kernel(){
            _isTest = true;
            DefaultInitialize(null, null, null, null);
        }

        //�e�X�g�p�R���X�g���N�^(MailBox�̂ݏ�����)
        public Kernel(String option){
            _isTest = true;
            DefaultInitialize(null, null, null, null);

            if (option.IndexOf("MailBox") != -1){
                var op = ListOption.Get("MailBox");
                var conf = new Conf(op);
                var dir = ReplaceOptionEnv((String)conf.Get("dir"));
                var datUser = (Dat)conf.Get("user");
                MailBox = new MailBox(null, datUser, dir);
            }
        }


        //* �ʏ�g�p�����R���X�g���N�^
        public Kernel(MainForm mainForm, ListView listViewLog, MenuStrip menuStrip, NotifyIcon notifyIcon){
            DefaultInitialize(mainForm, listViewLog, menuStrip, notifyIcon);
        }

        //�N�����ɁA�R���X�g���N�^����Ăяo����鏉����
        private void DefaultInitialize(MainForm mainForm, ListView listViewLog, MenuStrip menuStrip, NotifyIcon notifyIcon){

            RunMode = RunMode.Normal;
            RemoteConnect = null;//�����[�g����Őڑ�����Ă��鎞���������������

            //logger�����������܂ł̃��O��ꎞ�I�ɕۊǂ���
            //ArrayList<LogTemporary> tmpLogger = new ArrayList<>();

            //�v���Z�X�N�����ɏ����������
            View = new View(this, mainForm, listViewLog, notifyIcon);
            //logView = new LogView(listViewLog);
            LogView = new LogView(this,listViewLog);
            Menu = new Menu(this, menuStrip); //�����ł́A�I�u�W�F�N�g�̐����̂݁Amenu.Initialize()�́AlistInitialize()�̒��ŌĂяo�����
            DnsCache = new DnsCache();
            Wait = new Wait();

            Ver = new Ver(); //�o�[�W�����Ǘ�

            //Java fix
            //RunMode�̏�����
            if (mainForm == null){
                RunMode = RunMode.Service; //�T�[�r�X�N��
            } else{
                if (Environment.GetCommandLineArgs().Length > 1){
                    RunMode = RunMode.Remote; //�����[�g�N���C�A���g
                } else{
                    //�T�[�r�X�o�^�̏�Ԃ�擾����
                    var setupService = new SetupService(this);
                    if (setupService.IsRegist)
                        RunMode = RunMode.NormalRegist; //�T�[�r�X�o�^�������
                }
            }

            //Ver5.8.6 Java fix
            //OptionIni.Create(this); //�C���X�^���X�̏�����
            
            IniDb = new IniDb(ProgDir(), (RunMode == RunMode.Remote) ? "$remote" : "Option");
            
            MailBox = null;

            ListInitialize(); //�T�[�o�ċN���ŁA�ēx���s����鏉���� 


            if (_isTest){
                return;
            }

            //�E�C���h�T�C�Y�̕���
            var path = string.Format("{0}\\BJD.ini", ProgDir());
            try{
                //�E�C���h�E�̊O�ς�ۑ��E����(View���O�ɏ���������)
                WindowSize = new WindowSize(new Conf(ListOption.Get("Basic")), path);
                View.Read(WindowSize);
            } catch (IOException){
                WindowSize = null;
                // �w�肳�ꂽWindow���ۑ��t�@�C��(BJD.ini)��IO�G���[���������Ă���
                _logger.Set(LogKind.Error, null, 9000022, path);
            }

            //TraceDlg = new TraceDlg(this, (mainForm != null) ? mainForm.getFrame() : null); //�g���[�X�\��
            TraceDlg = new TraceDlg(this); //�g���[�X�\��

            switch (RunMode){
                case RunMode.Normal:
                    MenuOnClick("StartStop_Start"); //���j���[�I��C�x���g
                    break;
                case RunMode.Remote:
                    RemoteClient = new RemoteClient(this);
                    RemoteClient.Start();
                    break;
                //Java fix Ver5.8.3
                case RunMode.NormalRegist:
                case RunMode.Service:
                    break;
                default:
                    Util.RuntimeException("Kernel.defaultInitialize() not implement (RunMode)");
                    break;
            }

            //Java fix Ver5.8.3
            View.SetColor();//�E�C���h�F�̏�����

        }

        //�T�[�o�ċN���ŁA�ēx���s����鏉����
        public void ListInitialize(){
            //Logger���g�p�ł��Ȃ��Ԃ̃��O�́A������ɕۑ����āA���Logger�ɑ���
            var tmpLogger = new TmpLogger();

            //************************************************************
            // �j��
            //************************************************************
            if (ListOption != null){
                ListOption.Dispose();
                ListOption = null;
            }
            //Java fix
            if (ListTool != null){
                ListTool.Dispose();
                ListTool = null;
            }
            if (ListServer != null){
                ListServer.Dispose();
                ListServer = null;
            }
            if (MailBox != null){
                MailBox = null;
            }
            if (LogFile != null){
                LogFile.Dispose();
                LogFile = null;
            }

            //************************************************************
            // ������
            //************************************************************
            //ListPlugin �́BListOption��ListServer�����������Ԃ�����������
            //isTest=true�̏ꍇ�A�p�X��""�ɂ��āA�v���O�C��0�ŏ�������������

            //ListPlugin listPlugin = new ListPlugin((isTest) ? "" : string.Format("%s\\plugins", getProgDir()));
            var listPlugin = new ListPlugin(ProgDir());
            foreach (var o in listPlugin){
                //�����[�g�N���C�A���g�̏ꍇ�A���̃��O�́A��₱�����̂ŕ\�����Ȃ�
                if (RunMode == RunMode.Normal){
                    tmpLogger.Set(LogKind.Detail, null, 9000008, string.Format("{0}Server", o.Name));
                }
            }

            //ListOption�Ŋe�I�v�V���������������O�ɁAisJp�����͏��������Ă����K�v������̂�
            //�ŏ���OptionBasic��lang������ǂݏo��
            //Ver5.8.6 Java fix
            //_isJp = OptionIni.GetInstance().IsJp();
            _isJp = IniDb.IsJp();

            ListOption = new ListOption(this, listPlugin);

            //Ver5.9.1
            //���߂Ă�����ʉ߂���Ƃ��A�ߋ��̃o�[�W������Option��ǂݍ��ނ�
            //���I�v�V�����̓I�u�W�F�N�g�̒���OneOption�ɂ̂ݕێ������
            //���̏�ԂŁA�����̃I�v�V�����w���OK����ƁA���̃I�v�V�����ȊO��
            //Option.ini�ɕۑ�����Ȃ����ߔj������Ă��܂�
            //���̖��ɑΏ����邽�߁A�����ň�x�AOption.ini��ۑ����邱�Ƃɂ���
            if (!_isTest){
                ListOption.Save(IniDb);
            }


            //OptionBasic
            var confBasic = new Conf(ListOption.Get("Basic"));
            EditBrowse = (bool) confBasic.Get("editBrowse");

            //OptionLog
            var confOption = new Conf(ListOption.Get("Log"));
            LogView.SetFont((Font) confOption.Get("font"));

            if (RunMode == RunMode.Normal || RunMode == RunMode.Service){
                //LogFile�̏�����
                var saveDirectory = (String) confOption.Get("saveDirectory");
                saveDirectory = ReplaceOptionEnv(saveDirectory);
                var normalLogKind = (int) confOption.Get("normalLogKind");
                var secureLogKind = (int) confOption.Get("secureLogKind");
                var saveDays = (int) confOption.Get("saveDays");
                //Ver6.0.7
                var useLogFile = (bool)confOption.Get("useLogFile");
                var useLogClear = (bool) confOption.Get("useLogClear");
                if (!useLogClear){
                    saveDays = 0; //���O�̎����폜�������ȏꍇ�AsaveDays��0��Z�b�g����
                }
                if (saveDirectory == ""){
                    tmpLogger.Set(LogKind.Error, null, 9000045, "It is not appointed");
                } else{
                    tmpLogger.Set(LogKind.Detail, null, 9000032, saveDirectory);
                    try{
                        LogFile = new LogFile(saveDirectory, normalLogKind, secureLogKind, saveDays,useLogFile);
                    } catch (IOException e){
                        LogFile = null;
                        tmpLogger.Set(LogKind.Error, null, 9000031, e.Message);
                    }
                }

                //Ver5.8.7 Java fix
                //mailBox������
                foreach (var o in ListOption) {
                    //SmtpServer�Ⴕ���́APop3Server���g�p�����ꍇ�̂݃��[���{�b�N�X�����������                
                    if (o.NameTag == "Smtp" || o.NameTag == "Pop3") {
                        if (o.UseServer) {
                            var conf = new Conf(ListOption.Get("MailBox"));
                            var dir = ReplaceOptionEnv((String) conf.Get("dir"));
                            var datUser = (Dat) conf.Get("user");
                            var logger = CreateLogger("MailBox", (bool)conf.Get("useDetailsLog"), null);
                            MailBox = new MailBox(logger,datUser, dir);
                            break;
                        }
                    }
                }

            }
            _logger = CreateLogger("kernel", true, null);
            tmpLogger.Release(_logger);


            //Ver5.8.7 Java fix �����[�g�N���C�A���g�̏ꍇ����[���{�b�N�X��쐬���Ă��܂��o�O��C��
//            //mailBox������
//            foreach (var o in ListOption){
//                //SmtpServer�Ⴕ���́APop3Server���g�p�����ꍇ�̂݃��[���{�b�N�X�����������                
//                if (o.NameTag == "Smtp" || o.NameTag == "Pop3"){
//                    if (o.UseServer){
//                        var conf = new Conf(ListOption.Get("MailBox"));
//                        MailBox = new MailBox(this, conf);
//                        break;
//                    }
//                }
//            }

            ListServer = new ListServer(this, listPlugin);

            ListTool = new ListTool();
            ListTool.Initialize(this);

            View.SetColumnText(); //Log�r���[�̃J�����e�L�X�g�̏�����
            Menu.Initialize(IsJp()); //���j���[�\�z�i����e�[�u���̏������j

            WebApi = new WebApi();

        }

        //Conf�̐���
        //���O��ListOption������������Ă���K�v������
        public Conf CreateConf(String nameTag){
            if (ListOption == null){
                Util.RuntimeException("createConf() ListOption==null");
                return null;
            }
            var oneOption = ListOption.Get(nameTag);
            if (oneOption != null){
                return new Conf(oneOption);
            }
            return null;
        }

        //Logger�̐���
        //���O��ListOption������������Ă���K�v������
        public Logger CreateLogger(String nameTag, bool useDetailsLog, ILogger logger){
            if (ListOption == null){
                Util.RuntimeException("CreateLogger() ListOption==null || LogFile==null");
            }
            var conf = CreateConf("Log");
            if (conf == null){
                //CreateLogger��g�p����ۂɁAOptionLog�������ł��Ȃ��̂́A�݌v��̖�肪����
                Util.RuntimeException("CreateLogger() conf==null");
                return null;
            }
            var dat = (Dat) conf.Get("limitString");
            var isDisplay = ((int) conf.Get("isDisplay")) == 0;
            var logLimit = new LogLimit(dat, isDisplay);

            var useLimitString = (bool) conf.Get("useLimitString");
            return new Logger(this,logLimit, LogFile, LogView, _isJp, nameTag, useDetailsLog, useLimitString, logger);
        }

        //�I������
        public void Dispose(){

            //	        if (RunMode != RunMode.Service && RunMode != RunMode.Remote) {
            //	            //**********************************************
            //	            // ��U�t�@�C����폜���Č��ݗL���Ȃ�̂���������߂�
            //	            //**********************************************
            //	            var iniDb = new IniDb(ProgDir(),"Option");
            //	            iniDb.DeleteIni();

            //Ver5.8.6 Java fix 
            if (RunMode == RunMode.Normal) {
                var iniTmp = new IniDb(ProgDir(), "$tmp");//�o�b�N�A�b�v��쐬����ini�t�@�C����폜����
                //��U�A�ʃt�@�C���Ɍ��ݗL���Ȃ�̂���������߂�
                ListOption.Save(iniTmp);
                //�㏑������
                File.Copy(iniTmp.Path, IniDb.Path,true);
                iniTmp.Delete();
            }else if (RunMode == RunMode.Remote){
                IniDb.Delete(); //$Remote.ini�̍폜
            }
            

            //**********************************************
            // �j��
            //**********************************************
            ListServer.Dispose(); //�e�T�[�o�͒�~�����
            ListOption.Dispose();
            ListTool.Dispose();
            MailBox = null;
            //	        }
            if (RemoteClient != null)
                RemoteClient.Dispose();

            View.Dispose();
            if (TraceDlg != null){
                TraceDlg.Dispose();
            }
            if (Menu != null){
                Menu.Dispose();
            }
            if (WindowSize != null){
                View.Save(WindowSize);
                WindowSize.Dispose(); //Dispose���Ȃ���Reg.Dispose(�ۑ�)����Ȃ�
            }
        }

        public string ProgDir(){
            if (_isTest){
                var dir = Directory.GetCurrentDirectory(); //�e�X�g�v���O�����̃f�B���N�g��
                var src = Directory.GetParent(dir).Parent.FullName; //�e�X�g�R�[�h�̃f�B���N�g��
                return Directory.GetParent(src) + "\\BJD\\out"; //���v���O�����̃f�B���N�g��
            }
            return Path.GetDirectoryName(Application.ExecutablePath);
        }

        //�I�v�V�����Ŏw�肳���ϐ���u���ς���

        public String ReplaceOptionEnv(String str){
            var executablePath = ProgDir();
            executablePath = executablePath.Replace("\\\\", "\\\\\\\\");
            str = str.Replace("%ExecutablePath%", executablePath);
            return str;
        }

        //public string Env(string str) {
        //    str = Util.SwapStr("%ExecutablePath%", ProgDir(), str);
        //    return str;
        //}


        private void Start(){

            //�T�[�r�X�o�^����Ă���ꍇ�̏���
            if (RunMode == RunMode.NormalRegist){
                //            var setupService = new SetupService(this);
                //            if (setupService.Status != ServiceControllerStatus.Running) {
                //                setupService.Job(ServiceCmd.Start);
                //            }
            } else{
                if (ListServer.Count == 0){
                    _logger.Set(LogKind.Error, null, 9000030, "");
                } else{
                    ListServer.Start();
                }
            }
        }

        private void Stop(){

            //�T�[�r�X�o�^����Ă���ꍇ�̏���
            if (RunMode == RunMode.NormalRegist){
                //            var setupService = new SetupService(this);
                //            if (setupService.Status == ServiceControllerStatus.Running) {
                //                setupService.Job(ServiceCmd.Stop);
                //            }
            } else{
                ListServer.Stop();
            }
        }

        //�����[�g����(�f�[�^�̎擾)
        public string Cmd(string cmdStr){
            var sb = new StringBuilder();


            sb.Append(IsJp() ? "(1) �T�[�r�X���" : "(1) Service Status");
            sb.Append("\b");

            foreach (var sv in ListServer){
                sb.Append("  " + sv);
                sb.Append("\b");
            }
            sb.Append(" \b");

            sb.Append(IsJp() ? "(2) ���[�J���A�h���X" : "(2) Local address");
            sb.Append("\b");
            foreach (string addr in Define.ServerAddressList()){
                sb.Append(string.Format("  {0}", addr));
                sb.Append("\b");
            }

            return sb.ToString();
        }


        //���j���[�I����̏���
        public void MenuOnClick(String cmd){
            if (cmd.IndexOf("Option_") == 0){
                if (RunMode == RunMode.Remote){
                    //Java fix RunMOde==Remote�̏ꍇ�̃��j���[����
                    RemoteClient.MenuOnClick(cmd);
                } else{
                    var oneOption = ListOption.Get(cmd.Substring(7));
                    if (oneOption != null){
                        var dlg = new OptionDlg(this, oneOption);
                        if (DialogResult.OK == dlg.ShowDialog()){
                            //Ver5.8.6 Java fix
                            //oneOption.Save(OptionIni.GetInstance());
                            oneOption.Save(IniDb);
                            MenuOnClick("StartStop_Reload");
                        }
                    }
                }
            } else if (cmd.IndexOf("Tool_") == 0){
                if (RunMode == RunMode.Remote){
                    //Java fix RunMOde==Remote�̏ꍇ�̃��j���[����
                    RemoteClient.MenuOnClick(cmd);
                } else{
                    var nameTag = cmd.Substring(5);
                    var oneTool = ListTool.Get(nameTag);
                    if (oneTool == null)
                        return;

                    //BJD.EXE�ȊO�̏ꍇ�A�T�[�o�I�u�W�F�N�g�ւ̃|�C���^���K�v�ɂȂ�
                    OneServer oneServer = null;
                    if (nameTag != "BJD"){
                        oneServer = ListServer.Get(nameTag);
                        if (oneServer == null){
                            return;
                        }
                    }

                    ToolDlg dlg = oneTool.CreateDlg(oneServer);
                    dlg.ShowDialog();
                }
            } else if (cmd.IndexOf("StartStop_") == 0){
                if (RunMode == RunMode.Remote){
                    //Java fix RunMOde==Remote�̏ꍇ�̃��j���[����
                    RemoteClient.MenuOnClick(cmd);
                } else{
                    switch (cmd){
                        case "StartStop_Start":
                            Start();
                            break;
                        case "StartStop_Stop":
                            Stop();
                            break;
                        case "StartStop_Restart":
                            Stop();
                            Thread.Sleep(300);
                            Start();
                            break;
                        case "StartStop_Reload":
                            Stop();
                            ListInitialize();
                            Start();
                            break;
                        case "StartStop_Service":
                            SetupService(); //�T�[�r�X�̐ݒ�
                            break;
                        default:
                            Util.RuntimeException(string.Format("cmd={0}", cmd));
                            break;

                    }
                    View.SetColor(); //�E�C���h�̃J���[������
                    Menu.SetEnable(); //��Ԃɉ������L���E����
                }
            } else{
                switch (cmd){
                    case "File_LogClear":
                        LogView.Clear();
                        break;
                    case "File_LogCopy":
                        LogView.SetClipboard();
                        break;
                    case "File_Trace":
                        TraceDlg.Open();
                        break;
                    case "File_Exit":
                        View.MainForm.Close();
                        break;
                    case "Help_Version":
                        var dlg = new VersionDlg(this);
                        dlg.ShowDialog();
                        break;
                    case "Help_Homepage":
                        Process.Start(Define.WebHome());
                        break;
                    case "Help_Document":
                        Process.Start(Define.WebDocument());
                        break;
                    case "Help_Support":
                        Process.Start(Define.WebSupport());
                        break;
                }
            }

        }

        public String ChangeTag(String src){
            var tagList = new[]{"$h", "$v", "$p", "$d", "$a", "$s"};

            foreach (var tag in tagList){
                while (true){
                    var index = src.IndexOf(tag);
                    if (index == -1){
                        break;
                    }
                    var tmp1 = src.Substring(0, index);
                    var tmp2 = "";
                    switch (tag){
                        case "$h":
                            var serverName = ServerName;
                            tmp2 = serverName == "" ? "localhost" : serverName;
                            break;
                        case "$v":
                            tmp2 = Ver.Version();
                            break;
                        case "$p":
                            tmp2 = Define.ApplicationName();
                            break;
                        case "$d":
                            tmp2 = Define.Date();
                            break;
                        case "$a":
                            var localAddress = LocalAddress.GetInstance();
                            tmp2 = localAddress.RemoteStr();
                            //tmp2 = Define.ServerAddress();
                            break;
                        case "$s":
                            tmp2 = ServerName;
                            break;
                        default:
                            Util.RuntimeException(string.Format("undefind tag = {0}", tag));
                            break;
                    }
                    var tmp3 = src.Substring(index + 2);
                    src = tmp1 + tmp2 + tmp3;
                }
            }
            return src;
        }

        //IP�A�h���X�̈ꗗ�擾
        public List<Ip> GetIpList(String hostName){
            var ar = new List<Ip>();
            try{
                var ip = new Ip(hostName);
                ar.Add(ip);
            } catch (ValidObjException){
                ar = DnsCache.GetAddress(hostName).ToList();
            }
            return ar;
        }

        //�f�B���N�g�����擾�i�����[�g�N���C�A���g�p�j
        public string GetBrowseInfo(string path){
            var sb = new StringBuilder();
            try{
                if (path == ""){
//�h���C�u�ꗗ�擾
                    var drives = Directory.GetLogicalDrives();
                    foreach (string s in drives){
                        var driveName = s.ToUpper().Substring(0, 1);
                        var info = new DriveInfo(driveName);

                        var name = driveName + ":";
                        const int size = 0;
                        var dt = new DateTime(0);
                        BrowseKind browseKind;

                        if (info.DriveType == DriveType.Fixed){
                            browseKind = BrowseKind.DriveFixed;
                        } else if (info.DriveType == DriveType.CDRom){
                            browseKind = BrowseKind.DriveCdrom;
                        } else if (info.DriveType == DriveType.Removable){
                            browseKind = BrowseKind.DriveRemovable;
                        } else{
                            continue;
                        }
                        var p = new OneBrowse(browseKind, name, size, dt); //�P�f�[�^����
                        sb.Append(p + "\t"); //���M�����񐶐�

                    }
                } else{
                    string[] dirs = Directory.GetDirectories(path);
                    Array.Sort(dirs);
                    foreach (string s in dirs){
                        var name = s.Substring(path.Length);
                        var info = new DirectoryInfo(s);
                        const long size = 0;
                        var dt = info.LastWriteTime;
                        var p = new OneBrowse(BrowseKind.Dir, name, size, dt); //�P�f�[�^����
                        sb.Append(p + "\t"); //���M�����񐶐�
                    }
                    var files = Directory.GetFiles(path);
                    Array.Sort(files);
                    foreach (var s in files){
                        var name = s.Substring(path.Length);
                        var info = new FileInfo(s);
                        var size = info.Length;
                        var dt = info.LastWriteTime;
                        var p = new OneBrowse(BrowseKind.File, name, size, dt); //�P�f�[�^����
                        sb.Append(p + "\t"); //���M�����񐶐�
                    }
                }
            } catch{
                sb.Length = 0;
            }
            return sb.ToString();
        }

        public void SetupService(){
            //�ݒ�p�_�C�A���O�̕\��
            var dlg = new SetupServiceDlg(this);
            dlg.ShowDialog();
        }
    }
}


