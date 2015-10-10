using System;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using Microsoft.Win32;


namespace Bjd {
    public partial class VersionDlg : Form {
        public VersionDlg(Kernel kernel) {
            InitializeComponent();

            Text = kernel.IsJp() ? "バージョン情報" : "Version";
            labelApplicationName.Text = Define.ApplicationName();
            labelCopyright.Text = Define.Copyright();
            labelVersion.Text = "Version " + kernel.Ver.Version();

            try {
                var installedVersions = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP");
                var versionNames = installedVersions.GetSubKeyNames();

                
                
                var sb = new StringBuilder();
                foreach (string versionName in versionNames) {
                    sb.Append(versionName);
                    try {
                        var sp = Convert.ToInt32(installedVersions.OpenSubKey(versionName).GetValue("SP", 0));
                        if(sp!=0)
                            sb.Append("(SP"+sp+")");
                    }
                    catch {
                    }
                    sb.Append(" ");
                }
                //int sp = Convert.ToInt32(installed_versions.OpenSubKey(version_names[version_names.Length - 1]).GetValue("SP", 0));
                textBoxDotnetInstall.Text = sb.ToString();
                labelDotnetInstall.Text = ".NET";
                
                var runVer = System.Runtime.InteropServices.RuntimeEnvironment.GetSystemVersion();
                labelDotnetRunning.Text = string.Format("Running : {0}",runVer);

            }
            catch {
                labelDotnetInstall.Text = ".NET Framework ??";
            }


        }

        public override sealed string Text{
            get { return base.Text; }
            set { base.Text = value; }
        }

        private void PictureBoxClick(object sender, EventArgs e) {
            Process.Start(Define.WebHome());

        }
    }
}