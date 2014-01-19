namespace HocusImgView
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
            this.panel1 = new System.Windows.Forms.Panel();
            this.PalettePanel = new System.Windows.Forms.Panel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.numericUpDown1 = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.panel3 = new System.Windows.Forms.Panel();
            this.TilePicture = new System.Windows.Forms.Panel();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadPaletteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.importToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exportPNGToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.recentFilesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.noneToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).BeginInit();
            this.panel3.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new System.Drawing.Size(145, 6);
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new System.Drawing.Size(145, 6);
            // 
            // panel1
            // 
            this.panel1.AutoScroll = true;
            this.panel1.Controls.Add(this.PalettePanel);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Right;
            this.panel1.Location = new System.Drawing.Point(257, 24);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(128, 232);
            this.panel1.TabIndex = 2;
            // 
            // PalettePanel
            // 
            this.PalettePanel.Location = new System.Drawing.Point(0, 0);
            this.PalettePanel.Margin = new System.Windows.Forms.Padding(0);
            this.PalettePanel.Name = "PalettePanel";
            this.PalettePanel.Size = new System.Drawing.Size(128, 128);
            this.PalettePanel.TabIndex = 0;
            this.PalettePanel.Paint += new System.Windows.Forms.PaintEventHandler(this.PalettePanel_Paint);
            this.PalettePanel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.PalettePanel_MouseDown);
            // 
            // panel2
            // 
            this.panel2.AutoSize = true;
            this.panel2.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.panel2.Controls.Add(this.numericUpDown1);
            this.panel2.Controls.Add(this.label1);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel2.Location = new System.Drawing.Point(0, 24);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(257, 29);
            this.panel2.TabIndex = 8;
            // 
            // numericUpDown1
            // 
            this.numericUpDown1.Location = new System.Drawing.Point(55, 6);
            this.numericUpDown1.Maximum = new decimal(new int[] {
            16,
            0,
            0,
            0});
            this.numericUpDown1.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDown1.Name = "numericUpDown1";
            this.numericUpDown1.Size = new System.Drawing.Size(37, 20);
            this.numericUpDown1.TabIndex = 4;
            this.numericUpDown1.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDown1.ValueChanged += new System.EventHandler(this.numericUpDown1_ValueChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 8);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(37, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Zoom:";
            // 
            // panel3
            // 
            this.panel3.AutoScroll = true;
            this.panel3.AutoScrollMargin = new System.Drawing.Size(3, 3);
            this.panel3.BackColor = System.Drawing.SystemColors.ControlDark;
            this.panel3.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panel3.Controls.Add(this.TilePicture);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel3.Location = new System.Drawing.Point(0, 53);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(257, 203);
            this.panel3.TabIndex = 10;
            // 
            // TilePicture
            // 
            this.TilePicture.Location = new System.Drawing.Point(3, 3);
            this.TilePicture.Name = "TilePicture";
            this.TilePicture.Size = new System.Drawing.Size(8, 8);
            this.TilePicture.TabIndex = 2;
            this.TilePicture.Paint += new System.Windows.Forms.PaintEventHandler(this.TilePicture_Paint);
            this.TilePicture.MouseMove += new System.Windows.Forms.MouseEventHandler(this.TilePicture_MouseMove);
            this.TilePicture.MouseDown += new System.Windows.Forms.MouseEventHandler(this.TilePicture_MouseDown);
            this.TilePicture.Resize += new System.EventHandler(this.TilePicture_Resize);
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(385, 24);
            this.menuStrip1.TabIndex = 11;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.loadPaletteToolStripMenuItem,
            this.importToolStripMenuItem,
            this.exportPNGToolStripMenuItem,
            this.saveToolStripMenuItem,
            this.saveAsToolStripMenuItem,
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
            this.openToolStripMenuItem.Size = new System.Drawing.Size(148, 22);
            this.openToolStripMenuItem.Text = "&Open...";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
            // 
            // loadPaletteToolStripMenuItem
            // 
            this.loadPaletteToolStripMenuItem.Name = "loadPaletteToolStripMenuItem";
            this.loadPaletteToolStripMenuItem.Size = new System.Drawing.Size(148, 22);
            this.loadPaletteToolStripMenuItem.Text = "Load &Palette...";
            this.loadPaletteToolStripMenuItem.Click += new System.EventHandler(this.loadPaletteToolStripMenuItem_Click);
            // 
            // importToolStripMenuItem
            // 
            this.importToolStripMenuItem.Enabled = false;
            this.importToolStripMenuItem.Name = "importToolStripMenuItem";
            this.importToolStripMenuItem.Size = new System.Drawing.Size(148, 22);
            this.importToolStripMenuItem.Text = "&Import...";
            this.importToolStripMenuItem.Click += new System.EventHandler(this.importToolStripMenuItem_Click);
            // 
            // exportPNGToolStripMenuItem
            // 
            this.exportPNGToolStripMenuItem.Enabled = false;
            this.exportPNGToolStripMenuItem.Name = "exportPNGToolStripMenuItem";
            this.exportPNGToolStripMenuItem.Size = new System.Drawing.Size(148, 22);
            this.exportPNGToolStripMenuItem.Text = "&Export PNG...";
            this.exportPNGToolStripMenuItem.Click += new System.EventHandler(this.exportPNGToolStripMenuItem_Click);
            // 
            // saveToolStripMenuItem
            // 
            this.saveToolStripMenuItem.Enabled = false;
            this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            this.saveToolStripMenuItem.Size = new System.Drawing.Size(148, 22);
            this.saveToolStripMenuItem.Text = "&Save";
            this.saveToolStripMenuItem.Click += new System.EventHandler(this.saveToolStripMenuItem_Click);
            // 
            // saveAsToolStripMenuItem
            // 
            this.saveAsToolStripMenuItem.Enabled = false;
            this.saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            this.saveAsToolStripMenuItem.Size = new System.Drawing.Size(148, 22);
            this.saveAsToolStripMenuItem.Text = "Save &As...";
            this.saveAsToolStripMenuItem.Click += new System.EventHandler(this.saveAsToolStripMenuItem_Click);
            // 
            // recentFilesToolStripMenuItem
            // 
            this.recentFilesToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.noneToolStripMenuItem});
            this.recentFilesToolStripMenuItem.Name = "recentFilesToolStripMenuItem";
            this.recentFilesToolStripMenuItem.Size = new System.Drawing.Size(148, 22);
            this.recentFilesToolStripMenuItem.Text = "&Recent Files";
            this.recentFilesToolStripMenuItem.DropDownItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.recentFilesToolStripMenuItem_DropDownItemClicked);
            // 
            // noneToolStripMenuItem
            // 
            this.noneToolStripMenuItem.Enabled = false;
            this.noneToolStripMenuItem.Name = "noneToolStripMenuItem";
            this.noneToolStripMenuItem.Size = new System.Drawing.Size(109, 22);
            this.noneToolStripMenuItem.Text = "(none)";
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(148, 22);
            this.exitToolStripMenuItem.Text = "E&xit";
            // 
            // MainForm
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(385, 256);
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MainForm";
            this.Text = "Hocus Pocus Image Viewer";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.panel1.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).EndInit();
            this.panel3.ResumeLayout(false);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        internal System.Windows.Forms.Panel panel1;
        internal System.Windows.Forms.Panel PalettePanel;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.NumericUpDown numericUpDown1;
        private System.Windows.Forms.Label label1;
        internal System.Windows.Forms.Panel TilePicture;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadPaletteToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveAsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem recentFilesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem noneToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem importToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exportPNGToolStripMenuItem;
    }
}

