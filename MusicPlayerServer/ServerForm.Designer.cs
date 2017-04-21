namespace MusicPlayerServer
{
  partial class ServerForm
  {
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
      if (disposing && (components != null))
      {
        components.Dispose();
      }
      base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
      this.TbIp = new System.Windows.Forms.TextBox();
      this.label1 = new System.Windows.Forms.Label();
      this.label2 = new System.Windows.Forms.Label();
      this.label3 = new System.Windows.Forms.Label();
      this.TbPort = new System.Windows.Forms.TextBox();
      this.BtnToggle = new System.Windows.Forms.Button();
      this.TbPath = new System.Windows.Forms.TextBox();
      this.BtnBrowse = new System.Windows.Forms.Button();
      this.splitContainer1 = new System.Windows.Forms.SplitContainer();
      this.TbLog = new System.Windows.Forms.TextBox();
      this.label4 = new System.Windows.Forms.Label();
      this.LvMusics = new System.Windows.Forms.ListView();
      this.columnHeader5 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
      this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
      this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
      this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
      this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
      this.label5 = new System.Windows.Forms.Label();
      this.splitContainer2 = new System.Windows.Forms.SplitContainer();
      this.FdMp3Repo = new System.Windows.Forms.FolderBrowserDialog();
      ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
      this.splitContainer1.Panel1.SuspendLayout();
      this.splitContainer1.Panel2.SuspendLayout();
      this.splitContainer1.SuspendLayout();
      ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
      this.splitContainer2.Panel1.SuspendLayout();
      this.splitContainer2.Panel2.SuspendLayout();
      this.splitContainer2.SuspendLayout();
      this.SuspendLayout();
      // 
      // TbIp
      // 
      this.TbIp.Location = new System.Drawing.Point(53, 16);
      this.TbIp.Name = "TbIp";
      this.TbIp.Size = new System.Drawing.Size(293, 23);
      this.TbIp.TabIndex = 1;
      // 
      // label1
      // 
      this.label1.AutoSize = true;
      this.label1.Location = new System.Drawing.Point(23, 19);
      this.label1.Name = "label1";
      this.label1.Size = new System.Drawing.Size(24, 15);
      this.label1.TabIndex = 2;
      this.label1.Text = "IP :";
      // 
      // label2
      // 
      this.label2.AutoSize = true;
      this.label2.Location = new System.Drawing.Point(352, 19);
      this.label2.Name = "label2";
      this.label2.Size = new System.Drawing.Size(36, 15);
      this.label2.TabIndex = 2;
      this.label2.Text = "Port :";
      // 
      // label3
      // 
      this.label3.AutoSize = true;
      this.label3.Location = new System.Drawing.Point(23, 54);
      this.label3.Name = "label3";
      this.label3.Size = new System.Drawing.Size(134, 15);
      this.label3.TabIndex = 2;
      this.label3.Text = "MP3 File Storage Path :";
      // 
      // TbPort
      // 
      this.TbPort.Location = new System.Drawing.Point(394, 16);
      this.TbPort.Name = "TbPort";
      this.TbPort.Size = new System.Drawing.Size(66, 23);
      this.TbPort.TabIndex = 1;
      this.TbPort.Text = "8888";
      // 
      // BtnToggle
      // 
      this.BtnToggle.Location = new System.Drawing.Point(491, 16);
      this.BtnToggle.Name = "BtnToggle";
      this.BtnToggle.Size = new System.Drawing.Size(122, 23);
      this.BtnToggle.TabIndex = 3;
      this.BtnToggle.Text = "Start";
      this.BtnToggle.UseVisualStyleBackColor = true;
      this.BtnToggle.Click += new System.EventHandler(this.BtnToggleClick);
      // 
      // TbPath
      // 
      this.TbPath.Location = new System.Drawing.Point(163, 51);
      this.TbPath.Name = "TbPath";
      this.TbPath.Size = new System.Drawing.Size(297, 23);
      this.TbPath.TabIndex = 1;
      // 
      // BtnBrowse
      // 
      this.BtnBrowse.Location = new System.Drawing.Point(491, 51);
      this.BtnBrowse.Name = "BtnBrowse";
      this.BtnBrowse.Size = new System.Drawing.Size(122, 23);
      this.BtnBrowse.TabIndex = 3;
      this.BtnBrowse.Text = "Find Path";
      this.BtnBrowse.UseVisualStyleBackColor = true;
      this.BtnBrowse.Click += new System.EventHandler(this.BtnBrowse_Click);
      // 
      // splitContainer1
      // 
      this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
      this.splitContainer1.Location = new System.Drawing.Point(0, 0);
      this.splitContainer1.Name = "splitContainer1";
      // 
      // splitContainer1.Panel1
      // 
      this.splitContainer1.Panel1.Controls.Add(this.TbLog);
      this.splitContainer1.Panel1.Controls.Add(this.label4);
      this.splitContainer1.Panel1.Padding = new System.Windows.Forms.Padding(10, 20, 0, 10);
      // 
      // splitContainer1.Panel2
      // 
      this.splitContainer1.Panel2.Controls.Add(this.LvMusics);
      this.splitContainer1.Panel2.Controls.Add(this.label5);
      this.splitContainer1.Panel2.Padding = new System.Windows.Forms.Padding(0, 20, 10, 10);
      this.splitContainer1.Size = new System.Drawing.Size(750, 510);
      this.splitContainer1.SplitterDistance = 247;
      this.splitContainer1.TabIndex = 4;
      // 
      // TbLog
      // 
      this.TbLog.Dock = System.Windows.Forms.DockStyle.Fill;
      this.TbLog.Location = new System.Drawing.Point(10, 20);
      this.TbLog.Multiline = true;
      this.TbLog.Name = "TbLog";
      this.TbLog.Size = new System.Drawing.Size(237, 480);
      this.TbLog.TabIndex = 0;
      // 
      // label4
      // 
      this.label4.AutoSize = true;
      this.label4.Location = new System.Drawing.Point(13, 2);
      this.label4.Name = "label4";
      this.label4.Size = new System.Drawing.Size(77, 15);
      this.label4.TabIndex = 2;
      this.label4.Text = "Server Status";
      // 
      // LvMusics
      // 
      this.LvMusics.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader5,
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader3,
            this.columnHeader4});
      this.LvMusics.Dock = System.Windows.Forms.DockStyle.Fill;
      this.LvMusics.FullRowSelect = true;
      this.LvMusics.Location = new System.Drawing.Point(0, 20);
      this.LvMusics.MinimumSize = new System.Drawing.Size(100, 100);
      this.LvMusics.Name = "LvMusics";
      this.LvMusics.Size = new System.Drawing.Size(489, 480);
      this.LvMusics.TabIndex = 0;
      this.LvMusics.UseCompatibleStateImageBehavior = false;
      this.LvMusics.View = System.Windows.Forms.View.Details;
      // 
      // columnHeader5
      // 
      this.columnHeader5.Text = "";
      this.columnHeader5.Width = 0;
      // 
      // columnHeader1
      // 
      this.columnHeader1.Text = "Title";
      this.columnHeader1.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
      this.columnHeader1.Width = 193;
      // 
      // columnHeader2
      // 
      this.columnHeader2.Text = "Artist";
      this.columnHeader2.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
      this.columnHeader2.Width = 99;
      // 
      // columnHeader3
      // 
      this.columnHeader3.Text = "Duration";
      this.columnHeader3.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
      this.columnHeader3.Width = 88;
      // 
      // columnHeader4
      // 
      this.columnHeader4.Text = "Bitrate";
      this.columnHeader4.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
      this.columnHeader4.Width = 74;
      // 
      // label5
      // 
      this.label5.AutoSize = true;
      this.label5.Location = new System.Drawing.Point(3, 2);
      this.label5.Name = "label5";
      this.label5.Size = new System.Drawing.Size(61, 15);
      this.label5.TabIndex = 2;
      this.label5.Text = "Music List";
      // 
      // splitContainer2
      // 
      this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
      this.splitContainer2.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
      this.splitContainer2.Location = new System.Drawing.Point(0, 0);
      this.splitContainer2.Name = "splitContainer2";
      this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
      // 
      // splitContainer2.Panel1
      // 
      this.splitContainer2.Panel1.Controls.Add(this.BtnBrowse);
      this.splitContainer2.Panel1.Controls.Add(this.label1);
      this.splitContainer2.Panel1.Controls.Add(this.BtnToggle);
      this.splitContainer2.Panel1.Controls.Add(this.TbIp);
      this.splitContainer2.Panel1.Controls.Add(this.label3);
      this.splitContainer2.Panel1.Controls.Add(this.TbPath);
      this.splitContainer2.Panel1.Controls.Add(this.label2);
      this.splitContainer2.Panel1.Controls.Add(this.TbPort);
      // 
      // splitContainer2.Panel2
      // 
      this.splitContainer2.Panel2.Controls.Add(this.splitContainer1);
      this.splitContainer2.Size = new System.Drawing.Size(750, 603);
      this.splitContainer2.SplitterDistance = 89;
      this.splitContainer2.TabIndex = 3;
      // 
      // FdMp3Repo
      // 
      this.FdMp3Repo.Description = "MP3 파일 폴더를 지정해주세요.";
      // 
      // ServerForm
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(750, 603);
      this.Controls.Add(this.splitContainer2);
      this.Font = new System.Drawing.Font("Malgun Gothic", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
      this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
      this.Name = "ServerForm";
      this.Text = "Music Player - Server";
      this.splitContainer1.Panel1.ResumeLayout(false);
      this.splitContainer1.Panel1.PerformLayout();
      this.splitContainer1.Panel2.ResumeLayout(false);
      this.splitContainer1.Panel2.PerformLayout();
      ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
      this.splitContainer1.ResumeLayout(false);
      this.splitContainer2.Panel1.ResumeLayout(false);
      this.splitContainer2.Panel1.PerformLayout();
      this.splitContainer2.Panel2.ResumeLayout(false);
      ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
      this.splitContainer2.ResumeLayout(false);
      this.ResumeLayout(false);

    }

    #endregion
    private System.Windows.Forms.TextBox TbIp;
    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.Label label2;
    private System.Windows.Forms.Label label3;
    private System.Windows.Forms.TextBox TbPort;
    private System.Windows.Forms.Button BtnToggle;
    private System.Windows.Forms.TextBox TbPath;
    private System.Windows.Forms.Button BtnBrowse;
    private System.Windows.Forms.SplitContainer splitContainer1;
    private System.Windows.Forms.TextBox TbLog;
    private System.Windows.Forms.ListView LvMusics;
    private System.Windows.Forms.ColumnHeader columnHeader1;
    private System.Windows.Forms.ColumnHeader columnHeader2;
    private System.Windows.Forms.ColumnHeader columnHeader3;
    private System.Windows.Forms.ColumnHeader columnHeader4;
    private System.Windows.Forms.Label label4;
    private System.Windows.Forms.Label label5;
    private System.Windows.Forms.ColumnHeader columnHeader5;
    private System.Windows.Forms.SplitContainer splitContainer2;
    private System.Windows.Forms.FolderBrowserDialog FdMp3Repo;
  }
}

