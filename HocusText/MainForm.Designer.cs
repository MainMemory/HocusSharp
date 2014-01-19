namespace HocusText
{
    partial class MainForm
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
            System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
            System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
            System.Windows.Forms.Label label1;
            System.Windows.Forms.Label label2;
            System.Windows.Forms.Label label3;
            System.Windows.Forms.Label label4;
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.importtxtToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exporttxtToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.recentFilesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.noneToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.unknown1 = new System.Windows.Forms.NumericUpDown();
            this.unknown2 = new System.Windows.Forms.NumericUpDown();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.text0 = new System.Windows.Forms.TextBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.alignment0 = new System.Windows.Forms.ComboBox();
            this.color0 = new System.Windows.Forms.NumericUpDown();
            this.panel2 = new System.Windows.Forms.Panel();
            this.previewPanel = new System.Windows.Forms.Panel();
            toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            label1 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            label3 = new System.Windows.Forms.Label();
            label4 = new System.Windows.Forms.Label();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.unknown1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.unknown2)).BeginInit();
            this.tableLayoutPanel1.SuspendLayout();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.color0)).BeginInit();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new System.Drawing.Size(149, 6);
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(584, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.saveToolStripMenuItem,
            this.saveAsToolStripMenuItem,
            this.importtxtToolStripMenuItem,
            this.exporttxtToolStripMenuItem,
            toolStripSeparator1,
            this.recentFilesToolStripMenuItem,
            toolStripSeparator2,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.openToolStripMenuItem.Text = "&Open...";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
            // 
            // saveToolStripMenuItem
            // 
            this.saveToolStripMenuItem.Enabled = false;
            this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            this.saveToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.saveToolStripMenuItem.Text = "&Save";
            this.saveToolStripMenuItem.Click += new System.EventHandler(this.saveToolStripMenuItem_Click);
            // 
            // saveAsToolStripMenuItem
            // 
            this.saveAsToolStripMenuItem.Enabled = false;
            this.saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            this.saveAsToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.saveAsToolStripMenuItem.Text = "Save &As...";
            this.saveAsToolStripMenuItem.Click += new System.EventHandler(this.saveAsToolStripMenuItem_Click);
            // 
            // importtxtToolStripMenuItem
            // 
            this.importtxtToolStripMenuItem.Enabled = false;
            this.importtxtToolStripMenuItem.Name = "importtxtToolStripMenuItem";
            this.importtxtToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.importtxtToolStripMenuItem.Text = "&Import .txt...";
            this.importtxtToolStripMenuItem.Click += new System.EventHandler(this.importtxtToolStripMenuItem_Click);
            // 
            // exporttxtToolStripMenuItem
            // 
            this.exporttxtToolStripMenuItem.Enabled = false;
            this.exporttxtToolStripMenuItem.Name = "exporttxtToolStripMenuItem";
            this.exporttxtToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.exporttxtToolStripMenuItem.Text = "&Export .txt...";
            this.exporttxtToolStripMenuItem.Click += new System.EventHandler(this.exporttxtToolStripMenuItem_Click);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.exitToolStripMenuItem.Text = "E&xit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new System.Drawing.Size(149, 6);
            // 
            // recentFilesToolStripMenuItem
            // 
            this.recentFilesToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.noneToolStripMenuItem});
            this.recentFilesToolStripMenuItem.Name = "recentFilesToolStripMenuItem";
            this.recentFilesToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.recentFilesToolStripMenuItem.Text = "&Recent Files";
            this.recentFilesToolStripMenuItem.DropDownItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.recentFilesToolStripMenuItem_DropDownItemClicked);
            // 
            // noneToolStripMenuItem
            // 
            this.noneToolStripMenuItem.Enabled = false;
            this.noneToolStripMenuItem.Name = "noneToolStripMenuItem";
            this.noneToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.noneToolStripMenuItem.Text = "(none)";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(12, 29);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(56, 13);
            label1.TabIndex = 1;
            label1.Text = "Unknown:";
            // 
            // unknown1
            // 
            this.unknown1.Enabled = false;
            this.unknown1.Hexadecimal = true;
            this.unknown1.Location = new System.Drawing.Point(74, 27);
            this.unknown1.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.unknown1.Name = "unknown1";
            this.unknown1.Size = new System.Drawing.Size(50, 20);
            this.unknown1.TabIndex = 2;
            this.unknown1.ValueChanged += new System.EventHandler(this.unknown1_ValueChanged);
            // 
            // unknown2
            // 
            this.unknown2.Enabled = false;
            this.unknown2.Hexadecimal = true;
            this.unknown2.Location = new System.Drawing.Point(192, 27);
            this.unknown2.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.unknown2.Name = "unknown2";
            this.unknown2.Size = new System.Drawing.Size(50, 20);
            this.unknown2.TabIndex = 4;
            this.unknown2.ValueChanged += new System.EventHandler(this.unknown2_ValueChanged);
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(130, 29);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(56, 13);
            label2.TabIndex = 3;
            label2.Text = "Unknown:";
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel1.AutoSize = true;
            this.tableLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.Controls.Add(this.text0, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.panel1, 1, 0);
            this.tableLayoutPanel1.Enabled = false;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Size = new System.Drawing.Size(584, 27);
            this.tableLayoutPanel1.TabIndex = 5;
            // 
            // text0
            // 
            this.text0.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.text0.Location = new System.Drawing.Point(3, 3);
            this.text0.MaxLength = 80;
            this.text0.Name = "text0";
            this.text0.Size = new System.Drawing.Size(347, 20);
            this.text0.TabIndex = 0;
            this.text0.TextChanged += new System.EventHandler(this.text_TextChanged);
            // 
            // panel1
            // 
            this.panel1.AutoSize = true;
            this.panel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.panel1.Controls.Add(this.color0);
            this.panel1.Controls.Add(label4);
            this.panel1.Controls.Add(this.alignment0);
            this.panel1.Controls.Add(label3);
            this.panel1.Location = new System.Drawing.Point(353, 0);
            this.panel1.Margin = new System.Windows.Forms.Padding(0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(231, 27);
            this.panel1.TabIndex = 1;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(3, 6);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(56, 13);
            label3.TabIndex = 0;
            label3.Text = "Alignment:";
            // 
            // alignment0
            // 
            this.alignment0.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.alignment0.FormattingEnabled = true;
            this.alignment0.Items.AddRange(new object[] {
            "Centered",
            "Left"});
            this.alignment0.Location = new System.Drawing.Point(65, 3);
            this.alignment0.Name = "alignment0";
            this.alignment0.Size = new System.Drawing.Size(78, 21);
            this.alignment0.TabIndex = 1;
            this.alignment0.SelectedIndexChanged += new System.EventHandler(this.alignment_SelectedIndexChanged);
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(149, 6);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(34, 13);
            label4.TabIndex = 2;
            label4.Text = "Color:";
            // 
            // color0
            // 
            this.color0.Hexadecimal = true;
            this.color0.Location = new System.Drawing.Point(189, 4);
            this.color0.Maximum = new decimal(new int[] {
            8,
            0,
            0,
            0});
            this.color0.Name = "color0";
            this.color0.Size = new System.Drawing.Size(39, 20);
            this.color0.TabIndex = 3;
            this.color0.ValueChanged += new System.EventHandler(this.color_ValueChanged);
            // 
            // panel2
            // 
            this.panel2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.panel2.AutoScroll = true;
            this.panel2.Controls.Add(this.tableLayoutPanel1);
            this.panel2.Location = new System.Drawing.Point(0, 53);
            this.panel2.Margin = new System.Windows.Forms.Padding(0, 3, 0, 3);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(584, 291);
            this.panel2.TabIndex = 6;
            // 
            // previewPanel
            // 
            this.previewPanel.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.previewPanel.Location = new System.Drawing.Point(132, 350);
            this.previewPanel.Name = "previewPanel";
            this.previewPanel.Size = new System.Drawing.Size(320, 200);
            this.previewPanel.TabIndex = 7;
            this.previewPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.previewPanel_Paint);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(584, 562);
            this.Controls.Add(this.previewPanel);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.unknown2);
            this.Controls.Add(label2);
            this.Controls.Add(this.unknown1);
            this.Controls.Add(label1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MainForm";
            this.Text = "Hocus Pocus Text Editor";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.unknown1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.unknown2)).EndInit();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.color0)).EndInit();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveAsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem importtxtToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exporttxtToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem recentFilesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem noneToolStripMenuItem;
        private System.Windows.Forms.NumericUpDown unknown1;
        private System.Windows.Forms.NumericUpDown unknown2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TextBox text0;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.ComboBox alignment0;
        private System.Windows.Forms.NumericUpDown color0;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Panel previewPanel;
    }
}

