namespace loadData
{
    partial class Form1
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
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.btGo = new System.Windows.Forms.Button();
            this.btBrowseDirs = new System.Windows.Forms.Button();
            this.rdXml = new System.Windows.Forms.RadioButton();
            this.rdCsv = new System.Windows.Forms.RadioButton();
            this.textBox3 = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txDirectoryRoot = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.lbResult = new System.Windows.Forms.ListBox();
            this.fBrowse = new System.Windows.Forms.FolderBrowserDialog();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.btGo);
            this.splitContainer1.Panel1.Controls.Add(this.btBrowseDirs);
            this.splitContainer1.Panel1.Controls.Add(this.rdXml);
            this.splitContainer1.Panel1.Controls.Add(this.rdCsv);
            this.splitContainer1.Panel1.Controls.Add(this.textBox3);
            this.splitContainer1.Panel1.Controls.Add(this.label3);
            this.splitContainer1.Panel1.Controls.Add(this.textBox2);
            this.splitContainer1.Panel1.Controls.Add(this.label2);
            this.splitContainer1.Panel1.Controls.Add(this.txDirectoryRoot);
            this.splitContainer1.Panel1.Controls.Add(this.label1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.lbResult);
            this.splitContainer1.Size = new System.Drawing.Size(834, 918);
            this.splitContainer1.SplitterDistance = 218;
            this.splitContainer1.TabIndex = 0;
            // 
            // btGo
            // 
            this.btGo.Location = new System.Drawing.Point(700, 85);
            this.btGo.Name = "btGo";
            this.btGo.Size = new System.Drawing.Size(77, 23);
            this.btGo.TabIndex = 9;
            this.btGo.Text = "Go";
            this.btGo.UseVisualStyleBackColor = true;
            this.btGo.Click += new System.EventHandler(this.btGo_Click);
            // 
            // btBrowseDirs
            // 
            this.btBrowseDirs.Location = new System.Drawing.Point(428, 40);
            this.btBrowseDirs.Name = "btBrowseDirs";
            this.btBrowseDirs.Size = new System.Drawing.Size(28, 22);
            this.btBrowseDirs.TabIndex = 8;
            this.btBrowseDirs.Text = "...";
            this.btBrowseDirs.UseVisualStyleBackColor = true;
            this.btBrowseDirs.Click += new System.EventHandler(this.btBrowseDirs_Click);
            // 
            // rdXml
            // 
            this.rdXml.AutoSize = true;
            this.rdXml.Location = new System.Drawing.Point(486, 92);
            this.rdXml.Name = "rdXml";
            this.rdXml.Size = new System.Drawing.Size(40, 17);
            this.rdXml.TabIndex = 7;
            this.rdXml.TabStop = true;
            this.rdXml.Text = "xml";
            this.rdXml.UseVisualStyleBackColor = true;
            this.rdXml.CheckedChanged += new System.EventHandler(this.rdXml_CheckedChanged);
            // 
            // rdCsv
            // 
            this.rdCsv.AutoSize = true;
            this.rdCsv.Location = new System.Drawing.Point(549, 92);
            this.rdCsv.Name = "rdCsv";
            this.rdCsv.Size = new System.Drawing.Size(42, 17);
            this.rdCsv.TabIndex = 6;
            this.rdCsv.TabStop = true;
            this.rdCsv.Text = "csv";
            this.rdCsv.UseVisualStyleBackColor = true;
            this.rdCsv.CheckedChanged += new System.EventHandler(this.rdCsv_CheckedChanged);
            // 
            // textBox3
            // 
            this.textBox3.Location = new System.Drawing.Point(533, 40);
            this.textBox3.Name = "textBox3";
            this.textBox3.Size = new System.Drawing.Size(244, 20);
            this.textBox3.TabIndex = 5;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(483, 43);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(35, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "label3";
            // 
            // textBox2
            // 
            this.textBox2.Location = new System.Drawing.Point(178, 92);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(244, 20);
            this.textBox2.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(128, 95);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(35, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "label2";
            // 
            // txDirectoryRoot
            // 
            this.txDirectoryRoot.Location = new System.Drawing.Point(178, 40);
            this.txDirectoryRoot.Name = "txDirectoryRoot";
            this.txDirectoryRoot.Size = new System.Drawing.Size(244, 20);
            this.txDirectoryRoot.TabIndex = 1;
            this.txDirectoryRoot.Text = "Y:\\kwtn\\qos";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(128, 43);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(49, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Directory";
            // 
            // lbResult
            // 
            this.lbResult.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbResult.FormattingEnabled = true;
            this.lbResult.HorizontalScrollbar = true;
            this.lbResult.Location = new System.Drawing.Point(0, 0);
            this.lbResult.Name = "lbResult";
            this.lbResult.ScrollAlwaysVisible = true;
            this.lbResult.Size = new System.Drawing.Size(834, 696);
            this.lbResult.TabIndex = 0;
            // 
            // fBrowse
            // 
            this.fBrowse.RootFolder = System.Environment.SpecialFolder.Recent;
            this.fBrowse.ShowNewFolderButton = false;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(834, 918);
            this.Controls.Add(this.splitContainer1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Button btBrowseDirs;
        private System.Windows.Forms.RadioButton rdXml;
        private System.Windows.Forms.RadioButton rdCsv;
        private System.Windows.Forms.TextBox textBox3;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txDirectoryRoot;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.FolderBrowserDialog fBrowse;
        private System.Windows.Forms.Button btGo;
        private System.Windows.Forms.ListBox lbResult;
    }
}

