using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using HocusSharp;
using System.Collections.ObjectModel;

namespace HocusText
{
    public partial class MainForm : Form
    {
        Properties.Settings Settings = Properties.Settings.Default;

        public MainForm()
        {
            InitializeComponent();
        }

        const string ProgramName = "Hocus Pocus Text Editor";
        
        TextBox[] textcontrols;
        ComboBox[] alignmentcontrols;
        NumericUpDown[] colorcontrols;

        private void MainForm_Load(object sender, EventArgs e)
        {
            previewGfx = previewPanel.CreateGraphics();
            previewGfx.SetOptions();
            textcontrols = new TextBox[TextPage.LineCount];
            textcontrols[0] = text0;
            alignmentcontrols = new ComboBox[TextPage.LineCount];
            alignmentcontrols[0] = alignment0;
            colorcontrols = new NumericUpDown[TextPage.LineCount];
            colorcontrols[0] = color0;
            tableLayoutPanel1.SuspendLayout();
            for (int i = 1; i < TextPage.LineCount; i++)
            {
                TextBox textbox = new TextBox();
                textbox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                textbox.MaxLength = TextPage.LineLength;
                textbox.Name = "text" + i;
                textbox.TextChanged += new EventHandler(text_TextChanged);
                tableLayoutPanel1.Controls.Add(textbox, 0, i);
                textcontrols[i] = textbox;
                Panel panel = new Panel();
                panel.AutoSize = true;
                panel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                panel.Margin = new Padding(0);
                panel.Controls.Add(new Label() { AutoSize = true, Text = "Alignment:", Location = new Point(3, 6) });
                ComboBox combobox = new ComboBox();
                combobox.DropDownStyle = ComboBoxStyle.DropDownList;
                combobox.Items.AddRange(new object[] { "Centered", "Left" });
                combobox.Location = alignment0.Location;
                combobox.Name = "alignment" + i;
                combobox.Width = alignment0.Width;
                combobox.SelectedIndexChanged += new EventHandler(alignment_SelectedIndexChanged);
                panel.Controls.Add(combobox);
                alignmentcontrols[i] = combobox;
                panel.Controls.Add(new Label() { AutoSize = true, Text = "Color:", Location = new Point(149, 6) });
                NumericUpDown numericupdown = new NumericUpDown();
                numericupdown.Location = color0.Location;
                numericupdown.Maximum = 8;
                numericupdown.Name = "color" + i;
                numericupdown.Width = color0.Width;
                numericupdown.ValueChanged += new EventHandler(color_ValueChanged);
                panel.Controls.Add(numericupdown);
                colorcontrols[i] = numericupdown;
                tableLayoutPanel1.Controls.Add(panel, 1, i);
            }
            tableLayoutPanel1.ResumeLayout();
            if (Settings.RecentFiles == null)
                Settings.RecentFiles = new System.Collections.Specialized.StringCollection();
            System.Collections.Specialized.StringCollection mru = new System.Collections.Specialized.StringCollection();
            foreach (string item in Settings.RecentFiles)
                if (File.Exists(item))
                {
                    mru.Add(item);
                    recentFilesToolStripMenuItem.DropDownItems.Add(item.Replace("&", "&&"));
                }
            Settings.RecentFiles = mru;
            if (mru.Count > 0)
                recentFilesToolStripMenuItem.DropDownItems.Remove(noneToolStripMenuItem);
            if (Program.Arguments.Length > 0)
                LoadFile(Program.Arguments[0]);
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (filename != null)
                switch (MessageBox.Show(this, "Do you want to save?", ProgramName, MessageBoxButtons.YesNoCancel))
                {
                    case DialogResult.Yes:
                        saveToolStripMenuItem_Click(sender, EventArgs.Empty);
                        break;
                    case DialogResult.Cancel:
                        e.Cancel = true;
                        return;
                }
            Settings.Save();
        }

        private void UpdateMRU(string filename)
        {
            if (Settings.RecentFiles.Count == 0)
                recentFilesToolStripMenuItem.DropDownItems.Remove(noneToolStripMenuItem);
            if (Settings.RecentFiles.Contains(filename))
            {
                recentFilesToolStripMenuItem.DropDownItems.RemoveAt(Settings.RecentFiles.IndexOf(filename));
                Settings.RecentFiles.Remove(filename);
            }
            Settings.RecentFiles.Insert(0, filename);
            recentFilesToolStripMenuItem.DropDownItems.Insert(0, new ToolStripMenuItem(filename.Replace("&", "&&")));
        }

        TextPage page;
        string filename;
        ReadOnlyCollection<BitmapBits> font;
        Color[] palette;
        const double VGAMultiplier = 4.04761904761905;

        private void LoadFile(string filename)
        {
            using (Stream str = File.OpenRead(filename))
                page = new TextPage(str);
            Text = ProgramName + " - " + Path.GetFileName(filename);
            this.filename = filename;
            UpdateMRU(filename);
            Environment.CurrentDirectory = Path.GetDirectoryName(filename);
            if (File.Exists("FONT.MSK"))
                using (Stream str = File.OpenRead("FONT.MSK"))
                {
                    BitmapBits[] font = new BitmapBits[90];
                    for (int i = 0; i < 90; i++)
                    {
                        font[i] = BitmapBits.Read1bppTile(str);
                        for (int x = 7; x >= 0; x--)
                        {
                            bool found = false;
                            for (int y = 0; y < 8; y++)
                                if (font[i][x, y] != 0)
                                {
                                    found = true;
                                    font[i] = font[i].GetSection(0, 0, x + 1, 8);
                                    break;
                                }
                            if (found) break;
                        }
                        font[i].ReplaceColor(1, 255);
                    }
                    this.font = new ReadOnlyCollection<BitmapBits>(font);
                }
            else
                MessageBox.Show(this, "FONT.MSK not found.\n\nPreview has been disabled.", ProgramName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            using (Bitmap bmp = new Bitmap(1, 1, PixelFormat.Format8bppIndexed))
                palette = bmp.Palette.Entries;
            if (File.Exists("GAMEPAL.PAL"))
                using (Stream str = File.OpenRead("GAMEPAL.PAL"))
                using (BinaryReader br = new BinaryReader(str))
                    for (int i = 0; i < 128; i++)
                        palette[i] = Color.FromArgb((int)(br.ReadByte() * VGAMultiplier), (int)(br.ReadByte() * VGAMultiplier), (int)(br.ReadByte() * VGAMultiplier));
            if (File.Exists("MENUPAL.PAL"))
                using (Stream str = File.OpenRead("MENUPAL.PAL"))
                using (BinaryReader br = new BinaryReader(str))
                    for (int i = 128; i < 256; i++)
                        palette[i] = Color.FromArgb((int)(br.ReadByte() * VGAMultiplier), (int)(br.ReadByte() * VGAMultiplier), (int)(br.ReadByte() * VGAMultiplier));
            saveToolStripMenuItem.Enabled = saveAsToolStripMenuItem.Enabled = importtxtToolStripMenuItem.Enabled = exporttxtToolStripMenuItem.Enabled = unknown1.Enabled = unknown2.Enabled = tableLayoutPanel1.Enabled = true;
            unknown1.Value = page.Unknown1;
            unknown2.Value = page.Unknown2;
            for (int i = 0; i < TextPage.LineCount; i++)
            {
                textcontrols[i].Text = page.Lines[i];
                alignmentcontrols[i].SelectedIndex = page.Attributes[i].Alignment == 0xA ? 1 : 0;
                colorcontrols[i].Value = page.Attributes[i].TextColor;
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (filename != null)
                switch (MessageBox.Show(this, "Do you want to save?", ProgramName, MessageBoxButtons.YesNoCancel))
                {
                    case DialogResult.Yes:
                        saveToolStripMenuItem_Click(sender, e);
                        break;
                    case DialogResult.Cancel:
                        return;
                }
            using (OpenFileDialog fd = new OpenFileDialog() { DefaultExt = "pag", Filter = "Text Files|*.pag|All Files|*.*", RestoreDirectory = true })
                if (fd.ShowDialog(this) == DialogResult.OK)
                    LoadFile(fd.FileName);
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (Stream str = File.Create(filename))
                page.Write(str);
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog fd = new SaveFileDialog() { DefaultExt = "pag", FileName = Path.GetFileName(filename), Filter = "Text Files|*.pag|All Files|*.*", InitialDirectory = Path.GetDirectoryName(filename), RestoreDirectory = true })
                if (fd.ShowDialog(this) == DialogResult.OK)
                {
                    using (Stream str = File.Create(fd.FileName))
                        page.Write(str);
                    Text = ProgramName + " - " + Path.GetFileName(fd.FileName);
                    filename = fd.FileName;
                    UpdateMRU(fd.FileName);
                }
        }

        private void importtxtToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog fd = new OpenFileDialog() { DefaultExt = "txt", Filter = "Text Files|*.txt|All Files|*.*", RestoreDirectory = true })
                if (fd.ShowDialog(this) == DialogResult.OK)
                {
                    string[] lines = File.ReadAllLines(fd.FileName);
                    for (int i = 0; i < Math.Min(TextPage.LineCount, lines.Length); i++)
                        textcontrols[i].Text = lines[i].Substring(0, Math.Min(TextPage.LineLength, lines[i].Length));
                }
        }

        private void exporttxtToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog fd = new SaveFileDialog() { DefaultExt = "txt", FileName = Path.ChangeExtension(Path.GetFileName(filename), "txt"), InitialDirectory = Path.GetDirectoryName(filename), Filter = "Text Files|*.txt", RestoreDirectory = true })
                if (fd.ShowDialog(this) == DialogResult.OK)
                {
                    string[] lines = new string[page.Lines.CountLines()];
                    Array.Copy(page.Lines, lines, lines.Length);
                    File.WriteAllLines(fd.FileName, lines);
                }
        }

        private void recentFilesToolStripMenuItem_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (filename != null)
                switch (MessageBox.Show(this, "Do you want to save?", ProgramName, MessageBoxButtons.YesNoCancel))
                {
                    case DialogResult.Yes:
                        saveToolStripMenuItem_Click(sender, e);
                        break;
                    case DialogResult.Cancel:
                        return;
                }
            LoadFile(Settings.RecentFiles[recentFilesToolStripMenuItem.DropDownItems.IndexOf(e.ClickedItem)]);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void previewPanel_Paint(object sender, PaintEventArgs e)
        {
            DrawPreview();
        }

        Graphics previewGfx;
        private void DrawPreview()
        {
            if (page == null) return;
            BitmapBits bmp = new BitmapBits(320, 200);
            int count = page.Lines.CountLines();
            int y = 100 - (count * 4);
            for (int i = 0; i < count; i++)
            {
                int x;
                switch (page.Attributes[i].Alignment)
                {
                    case 0xA:
                        x = 0;
                        break;
                    default:
                        x = 160 - (MeasureString(page.Lines[i]) / 2);
                        break;
                }
                DrawStringGradient(bmp, (byte)(0xB8 + (8 * page.Attributes[i].TextColor)), page.Lines[i], x, y);
                y += 8;
            }
            previewGfx.DrawImage(bmp.ToBitmap(palette), 0, 0, 320, 200);
        }

        private void DrawStringGradient(BitmapBits dest, byte color, object str, int x, int y)
        {
            int startx = x;
            foreach (char item in str.ToString())
            {
                if (item >= '!' & item <= 'z')
                {
                    BitmapBits bmp = new BitmapBits(font[item - '!']);
                    byte c = color;
                    for (int v = 0; v < 8; v++)
                    {
                        for (int h = 0; h < bmp.Width; h++)
                            if (bmp[h, v] != 0)
                                bmp[h, v] = c;
                        c++;
                    }
                    dest.DrawBitmapComposited(bmp, x, y);
                    x += bmp.Width + 1;
                }
                else
                    x += 4;
                if (item == '\n')
                {
                    x = startx;
                    y += 8;
                }
            }
        }

        private int MeasureString(object str)
        {
            int width = 0;
            foreach (char item in str.ToString())
                if (item >= '!' & item <= 'z')
                    width += font[item - '!'].Width + 1;
                else
                    width += 4;
            return width;
        }

        private void unknown1_ValueChanged(object sender, EventArgs e)
        {
            page.Unknown1 = (ushort)unknown1.Value;
        }

        private void unknown2_ValueChanged(object sender, EventArgs e)
        {
            page.Unknown2 = (ushort)unknown2.Value;
        }

        private void text_TextChanged(object sender, EventArgs e)
        {
            page.Lines[Array.IndexOf(textcontrols, sender)] = ((TextBox)sender).Text;
            DrawPreview();
        }

        private void alignment_SelectedIndexChanged(object sender, EventArgs e)
        {
            ushort value;
            switch (((ComboBox)sender).SelectedIndex)
            {
                default:
                    value = 0;
                    break;
                case 1:
                    value = 0xA;
                    break;
            }
            page.Attributes[Array.IndexOf(alignmentcontrols, sender)].Alignment = value;
            DrawPreview();
        }

        private void color_ValueChanged(object sender, EventArgs e)
        {
            page.Attributes[Array.IndexOf(colorcontrols, sender)].TextColor = (ushort)((NumericUpDown)sender).Value;
            DrawPreview();
        }
    }

    public static class Extensions
    {
        /// <summary>
        /// Sets options to enable faster rendering.
        /// </summary>
        public static void SetOptions(this Graphics gfx)
        {
            gfx.CompositingQuality = CompositingQuality.HighQuality;
            gfx.InterpolationMode = InterpolationMode.NearestNeighbor;
            gfx.PixelOffsetMode = PixelOffsetMode.None;
            gfx.SmoothingMode = SmoothingMode.HighSpeed;
        }

        public static Bitmap To32bpp(this Bitmap bmp)
        {
            return bmp.Clone(new System.Drawing.Rectangle(Point.Empty, bmp.Size), PixelFormat.Format32bppArgb);
        }

        public static Color FindNearestMatch(this Color col, params Color[] palette)
        {
            Color nearest_color = Color.Empty;
            double distance = 250000;
            foreach (Color o in palette)
            {
                double dbl_test_red = Math.Pow(o.R - col.R, 2.0);
                double dbl_test_green = Math.Pow(o.G - col.G, 2.0);
                double dbl_test_blue = Math.Pow(o.B - col.B, 2.0);
                double temp = dbl_test_blue + dbl_test_green + dbl_test_red;
                if (temp == 0.0)
                {
                    nearest_color = o;
                    break;
                }
                else if (temp < distance)
                {
                    distance = temp;
                    nearest_color = o;
                }
            }
            return nearest_color;
        }
    }
}