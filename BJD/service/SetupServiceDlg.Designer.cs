namespace Bjd.service {
    partial class SetupServiceDlg {
        /// <summary>
        /// 必要なデザイナ変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージ リソースが破棄される場合 true、破棄されない場合は false です。</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナで生成されたコード

        /// <summary>
        /// デザイナ サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディタで変更しないでください。
        /// </summary>
        private void InitializeComponent() {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SetupServiceDlg));
            this.groupBoxInstall = new System.Windows.Forms.GroupBox();
            this.textBoxInstall = new System.Windows.Forms.TextBox();
            this.buttonUninstall = new System.Windows.Forms.Button();
            this.buttonInstall = new System.Windows.Forms.Button();
            this.groupBoxStatus = new System.Windows.Forms.GroupBox();
            this.buttonRestart = new System.Windows.Forms.Button();
            this.textBoxStatus = new System.Windows.Forms.TextBox();
            this.buttonStop = new System.Windows.Forms.Button();
            this.buttonStart = new System.Windows.Forms.Button();
            this.groupBoxStartupType = new System.Windows.Forms.GroupBox();
            this.buttonDisable = new System.Windows.Forms.Button();
            this.textBoxStartupType = new System.Windows.Forms.TextBox();
            this.buttonAutomatic = new System.Windows.Forms.Button();
            this.buttonManual = new System.Windows.Forms.Button();
            this.buttonOk = new System.Windows.Forms.Button();
            this.groupBoxInstall.SuspendLayout();
            this.groupBoxStatus.SuspendLayout();
            this.groupBoxStartupType.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBoxInstall
            // 
            this.groupBoxInstall.Controls.Add(this.textBoxInstall);
            this.groupBoxInstall.Controls.Add(this.buttonUninstall);
            this.groupBoxInstall.Controls.Add(this.buttonInstall);
            this.groupBoxInstall.Location = new System.Drawing.Point(8, 3);
            this.groupBoxInstall.Name = "groupBoxInstall";
            this.groupBoxInstall.Size = new System.Drawing.Size(296, 52);
            this.groupBoxInstall.TabIndex = 11;
            this.groupBoxInstall.TabStop = false;
            this.groupBoxInstall.Text = "サービスへのインストール";
            // 
            // textBoxInstall
            // 
            this.textBoxInstall.Location = new System.Drawing.Point(183, 20);
            this.textBoxInstall.Name = "textBoxInstall";
            this.textBoxInstall.ReadOnly = true;
            this.textBoxInstall.Size = new System.Drawing.Size(94, 19);
            this.textBoxInstall.TabIndex = 13;
            // 
            // buttonUninstall
            // 
            this.buttonUninstall.Location = new System.Drawing.Point(87, 18);
            this.buttonUninstall.Name = "buttonUninstall";
            this.buttonUninstall.Size = new System.Drawing.Size(75, 23);
            this.buttonUninstall.TabIndex = 12;
            this.buttonUninstall.Text = "削除";
            this.buttonUninstall.UseVisualStyleBackColor = true;
            this.buttonUninstall.Click += new System.EventHandler(this.ButtonUninstallClick);
            // 
            // buttonInstall
            // 
            this.buttonInstall.Location = new System.Drawing.Point(6, 18);
            this.buttonInstall.Name = "buttonInstall";
            this.buttonInstall.Size = new System.Drawing.Size(75, 23);
            this.buttonInstall.TabIndex = 11;
            this.buttonInstall.Text = "登録";
            this.buttonInstall.UseVisualStyleBackColor = true;
            this.buttonInstall.Click += new System.EventHandler(this.ButtonInstallClick);
            // 
            // groupBoxStatus
            // 
            this.groupBoxStatus.Controls.Add(this.buttonRestart);
            this.groupBoxStatus.Controls.Add(this.textBoxStatus);
            this.groupBoxStatus.Controls.Add(this.buttonStop);
            this.groupBoxStatus.Controls.Add(this.buttonStart);
            this.groupBoxStatus.Location = new System.Drawing.Point(8, 61);
            this.groupBoxStatus.Name = "groupBoxStatus";
            this.groupBoxStatus.Size = new System.Drawing.Size(296, 52);
            this.groupBoxStatus.TabIndex = 14;
            this.groupBoxStatus.TabStop = false;
            this.groupBoxStatus.Text = "状態";
            // 
            // buttonRestart
            // 
            this.buttonRestart.Location = new System.Drawing.Point(142, 18);
            this.buttonRestart.Name = "buttonRestart";
            this.buttonRestart.Size = new System.Drawing.Size(62, 23);
            this.buttonRestart.TabIndex = 14;
            this.buttonRestart.Text = "再起動";
            this.buttonRestart.UseVisualStyleBackColor = true;
            this.buttonRestart.Click += new System.EventHandler(this.ButtonRestartClick);
            // 
            // textBoxStatus
            // 
            this.textBoxStatus.Location = new System.Drawing.Point(216, 20);
            this.textBoxStatus.Name = "textBoxStatus";
            this.textBoxStatus.ReadOnly = true;
            this.textBoxStatus.Size = new System.Drawing.Size(61, 19);
            this.textBoxStatus.TabIndex = 13;
            // 
            // buttonStop
            // 
            this.buttonStop.Location = new System.Drawing.Point(74, 18);
            this.buttonStop.Name = "buttonStop";
            this.buttonStop.Size = new System.Drawing.Size(62, 23);
            this.buttonStop.TabIndex = 12;
            this.buttonStop.Text = "停止";
            this.buttonStop.UseVisualStyleBackColor = true;
            this.buttonStop.Click += new System.EventHandler(this.ButtonStopClick);
            // 
            // buttonStart
            // 
            this.buttonStart.Location = new System.Drawing.Point(6, 18);
            this.buttonStart.Name = "buttonStart";
            this.buttonStart.Size = new System.Drawing.Size(62, 23);
            this.buttonStart.TabIndex = 11;
            this.buttonStart.Text = "開始";
            this.buttonStart.UseVisualStyleBackColor = true;
            this.buttonStart.Click += new System.EventHandler(this.ButtonStartClick);
            // 
            // groupBoxStartupType
            // 
            this.groupBoxStartupType.Controls.Add(this.buttonDisable);
            this.groupBoxStartupType.Controls.Add(this.textBoxStartupType);
            this.groupBoxStartupType.Controls.Add(this.buttonAutomatic);
            this.groupBoxStartupType.Controls.Add(this.buttonManual);
            this.groupBoxStartupType.Location = new System.Drawing.Point(8, 119);
            this.groupBoxStartupType.Name = "groupBoxStartupType";
            this.groupBoxStartupType.Size = new System.Drawing.Size(296, 52);
            this.groupBoxStartupType.TabIndex = 15;
            this.groupBoxStartupType.TabStop = false;
            this.groupBoxStartupType.Text = "スタートアップの種類";
            // 
            // buttonDisable
            // 
            this.buttonDisable.Location = new System.Drawing.Point(142, 18);
            this.buttonDisable.Name = "buttonDisable";
            this.buttonDisable.Size = new System.Drawing.Size(62, 23);
            this.buttonDisable.TabIndex = 14;
            this.buttonDisable.Text = "無効";
            this.buttonDisable.UseVisualStyleBackColor = true;
            this.buttonDisable.Click += new System.EventHandler(this.ButtonDisableClick);
            // 
            // textBoxStartupType
            // 
            this.textBoxStartupType.Location = new System.Drawing.Point(216, 20);
            this.textBoxStartupType.Name = "textBoxStartupType";
            this.textBoxStartupType.ReadOnly = true;
            this.textBoxStartupType.Size = new System.Drawing.Size(61, 19);
            this.textBoxStartupType.TabIndex = 13;
            // 
            // buttonAutomatic
            // 
            this.buttonAutomatic.Location = new System.Drawing.Point(6, 18);
            this.buttonAutomatic.Name = "buttonAutomatic";
            this.buttonAutomatic.Size = new System.Drawing.Size(62, 23);
            this.buttonAutomatic.TabIndex = 11;
            this.buttonAutomatic.Text = "自動";
            this.buttonAutomatic.UseVisualStyleBackColor = true;
            this.buttonAutomatic.Click += new System.EventHandler(this.ButtonAutomaticClick);
            // 
            // buttonManual
            // 
            this.buttonManual.Location = new System.Drawing.Point(74, 18);
            this.buttonManual.Name = "buttonManual";
            this.buttonManual.Size = new System.Drawing.Size(62, 23);
            this.buttonManual.TabIndex = 12;
            this.buttonManual.Text = "手動";
            this.buttonManual.UseVisualStyleBackColor = true;
            this.buttonManual.Click += new System.EventHandler(this.ButtonManualClick);
            // 
            // buttonOk
            // 
            this.buttonOk.Location = new System.Drawing.Point(112, 181);
            this.buttonOk.Name = "buttonOk";
            this.buttonOk.Size = new System.Drawing.Size(75, 23);
            this.buttonOk.TabIndex = 16;
            this.buttonOk.Text = "OK";
            this.buttonOk.UseVisualStyleBackColor = true;
            this.buttonOk.Click += new System.EventHandler(this.ButtonOkClick);
            // 
            // SetupServiceDlg
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(309, 210);
            this.Controls.Add(this.buttonOk);
            this.Controls.Add(this.groupBoxStartupType);
            this.Controls.Add(this.groupBoxStatus);
            this.Controls.Add(this.groupBoxInstall);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SetupServiceDlg";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "サービス設定 ダイアログ";
            this.groupBoxInstall.ResumeLayout(false);
            this.groupBoxInstall.PerformLayout();
            this.groupBoxStatus.ResumeLayout(false);
            this.groupBoxStatus.PerformLayout();
            this.groupBoxStartupType.ResumeLayout(false);
            this.groupBoxStartupType.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBoxInstall;
        private System.Windows.Forms.TextBox textBoxInstall;
        private System.Windows.Forms.Button buttonUninstall;
        private System.Windows.Forms.Button buttonInstall;
        private System.Windows.Forms.GroupBox groupBoxStatus;
        private System.Windows.Forms.TextBox textBoxStatus;
        private System.Windows.Forms.Button buttonStop;
        private System.Windows.Forms.Button buttonStart;
        private System.Windows.Forms.GroupBox groupBoxStartupType;
        private System.Windows.Forms.TextBox textBoxStartupType;
        private System.Windows.Forms.Button buttonManual;
        private System.Windows.Forms.Button buttonAutomatic;
        private System.Windows.Forms.Button buttonDisable;
        private System.Windows.Forms.Button buttonOk;
        private System.Windows.Forms.Button buttonRestart;
    }
}                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                        