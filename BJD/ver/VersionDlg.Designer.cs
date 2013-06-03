namespace Bjd {
    partial class VersionDlg {
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
            this.buttonOk = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            this.labelDotnetRunning = new System.Windows.Forms.Label();
            this.labelDotnetInstall = new System.Windows.Forms.Label();
            this.labelVersion = new System.Windows.Forms.Label();
            this.labelCopyright = new System.Windows.Forms.Label();
            this.pictureBoxIcon = new System.Windows.Forms.PictureBox();
            this.labelApplicationName = new System.Windows.Forms.Label();
            this.pictureBox = new System.Windows.Forms.PictureBox();
            this.textBoxDotnetInstall = new System.Windows.Forms.TextBox();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxIcon)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // buttonOk
            // 
            this.buttonOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonOk.Location = new System.Drawing.Point(173, 210);
            this.buttonOk.Name = "buttonOk";
            this.buttonOk.Size = new System.Drawing.Size(75, 23);
            this.buttonOk.TabIndex = 0;
            this.buttonOk.Text = "OK";
            this.buttonOk.UseVisualStyleBackColor = true;
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.White;
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panel1.Controls.Add(this.textBoxDotnetInstall);
            this.panel1.Controls.Add(this.labelDotnetRunning);
            this.panel1.Controls.Add(this.labelDotnetInstall);
            this.panel1.Controls.Add(this.labelVersion);
            this.panel1.Controls.Add(this.labelCopyright);
            this.panel1.Controls.Add(this.pictureBoxIcon);
            this.panel1.Controls.Add(this.labelApplicationName);
            this.panel1.Controls.Add(this.pictureBox);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(417, 199);
            this.panel1.TabIndex = 1;
            // 
            // labelDotnetRunning
            // 
            this.labelDotnetRunning.AutoSize = true;
            this.labelDotnetRunning.Location = new System.Drawing.Point(82, 132);
            this.labelDotnetRunning.Name = "labelDotnetRunning";
            this.labelDotnetRunning.Size = new System.Drawing.Size(104, 12);
            this.labelDotnetRunning.TabIndex = 6;
            this.labelDotnetRunning.Text = "labelDotnetRunning";
            // 
            // labelDotnetInstall
            // 
            this.labelDotnetInstall.AutoSize = true;
            this.labelDotnetInstall.Location = new System.Drawing.Point(82, 149);
            this.labelDotnetInstall.Name = "labelDotnetInstall";
            this.labelDotnetInstall.Size = new System.Drawing.Size(29, 12);
            this.labelDotnetInstall.TabIndex = 5;
            this.labelDotnetInstall.Text = ".NET";
            // 
            // labelVersion
            // 
            this.labelVersion.AutoSize = true;
            this.labelVersion.Location = new System.Drawing.Point(82, 115);
            this.labelVersion.Name = "labelVersion";
            this.labelVersion.Size = new System.Drawing.Size(68, 12);
            this.labelVersion.TabIndex = 4;
            this.labelVersion.Text = "labelVersion";
            // 
            // labelCopyright
            // 
            this.labelCopyright.AutoSize = true;
            this.labelCopyright.Location = new System.Drawing.Point(82, 98);
            this.labelCopyright.Name = "labelCopyright";
            this.labelCopyright.Size = new System.Drawing.Size(78, 12);
            this.labelCopyright.TabIndex = 3;
            this.labelCopyright.Text = "labelCopyright";
            // 
            // pictureBoxIcon
            // 
            this.pictureBoxIcon.Image = global::Bjd.Properties.Resources.icon;
            this.pictureBoxIcon.Location = new System.Drawing.Point(31, 90);
            this.pictureBoxIcon.Name = "pictureBoxIcon";
            this.pictureBoxIcon.Size = new System.Drawing.Size(32, 37);
            this.pictureBoxIcon.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBoxIcon.TabIndex = 2;
            this.pictureBoxIcon.TabStop = false;
            // 
            // labelApplicationName
            // 
            this.labelApplicationName.AutoSize = true;
            this.labelApplicationName.Location = new System.Drawing.Point(82, 82);
            this.labelApplicationName.Name = "labelApplicationName";
            this.labelApplicationName.Size = new System.Drawing.Size(86, 12);
            this.labelApplicationName.TabIndex = 1;
            this.labelApplicationName.Text = "labelApplication";
            // 
            // pictureBox
            // 
            this.pictureBox.Cursor = System.Windows.Forms.Cursors.Hand;
            this.pictureBox.Image = global::Bjd.Properties.Resources.spw;
            this.pictureBox.Location = new System.Drawing.Point(62, 12);
            this.pictureBox.Name = "pictureBox";
            this.pictureBox.Size = new System.Drawing.Size(286, 63);
            this.pictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBox.TabIndex = 0;
            this.pictureBox.TabStop = false;
            this.pictureBox.Click += new System.EventHandler(this.PictureBoxClick);
            // 
            // textBoxDotnetInstall
            // 
            this.textBoxDotnetInstall.BackColor = System.Drawing.SystemColors.Window;
            this.textBoxDotnetInstall.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBoxDotnetInstall.Location = new System.Drawing.Point(112, 149);
            this.textBoxDotnetInstall.Multiline = true;
            this.textBoxDotnetInstall.Name = "textBoxDotnetInstall";
            this.textBoxDotnetInstall.ReadOnly = true;
            this.textBoxDotnetInstall.Size = new System.Drawing.Size(236, 43);
            this.textBoxDotnetInstall.TabIndex = 7;
            // 
            // VersionDlg
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(417, 240);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.buttonOk);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "VersionDlg";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "VersionDlg";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxIcon)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button buttonOk;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.PictureBox pictureBox;
        private System.Windows.Forms.PictureBox pictureBoxIcon;
        private System.Windows.Forms.Label labelApplicationName;
        private System.Windows.Forms.Label labelVersion;
        private System.Windows.Forms.Label labelCopyright;
        private System.Windows.Forms.Label labelDotnetInstall;
        private System.Windows.Forms.Label labelDotnetRunning;
        private System.Windows.Forms.TextBox textBoxDotnetInstall;
    }
}