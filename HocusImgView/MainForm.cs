using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using HocusSharp;

namespace HocusImgView
{
    public partial class MainForm : Form
    {
        Properties.Settings Settings = Properties.Settings.Default;

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            tileGfx = TilePicture.CreateGraphics();
            tileGfx.SetOptions();
            TilePicture.Cursor = new Cursor(new MemoryStream(Properties.Resources.pencilcur));
            using (Bitmap bmp = new Bitmap(1, 1, PixelFormat.Format8bppIndexed))
                Palette = bmp.Palette.Entries;
            if (File.Exists("palette.pal"))
                using (Stream str = File.OpenRead("palette.pal"))
                using (BinaryReader br = new BinaryReader(str))
                    for (int i = 0; i < str.Length / 3; i++)
                        Palette[i] = Color.FromArgb(br.ReadByte(), br.ReadByte(), br.ReadByte());
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
                LoadImage(Program.Arguments[0]);
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

        private Color[] Palette;
        private Point selectedColor;
        private void PalettePanel_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(Color.Black);
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                    e.Graphics.FillRectangle(new SolidBrush(Palette[x + (y * 16)]), x * 8, y * 8, 8, 8);
            e.Graphics.DrawRectangle(new Pen(Color.Yellow, 1), selectedColor.X * 8, selectedColor.Y * 8, 8, 8);
        }

        private void PalettePanel_MouseDown(object sender, MouseEventArgs e)
        {
            selectedColor = new Point(e.X / 8, e.Y / 8);
            PalettePanel.Invalidate();
        }

        public BitmapBits tile;
        string imagefile;
        private void TilePicture_Paint(object sender, PaintEventArgs e)
        {
            DrawTile();
        }

        private void TilePicture_MouseDown(object sender, MouseEventArgs e)
        {
            if (tile == null) return;
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                tile.Bits[((e.Y / (int)numericUpDown1.Value) * tile.Width) + (e.X / (int)numericUpDown1.Value)] = (byte)((selectedColor.Y * 16) + selectedColor.X);
                lastpoint = new Point(e.X / (int)numericUpDown1.Value, e.Y / (int)numericUpDown1.Value);
                DrawTile();
            }
        }

        Point lastpoint;
        private void TilePicture_MouseMove(object sender, MouseEventArgs e)
        {
            if (tile == null) return;
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                if (new System.Drawing.Rectangle(Point.Empty, TilePicture.Size).Contains(e.Location))
                    tile.DrawLine((byte)((selectedColor.Y * 16) + selectedColor.X), lastpoint, new Point(e.X / (int)numericUpDown1.Value, e.Y / (int)numericUpDown1.Value));
                tile.Bits[((e.Y / (int)numericUpDown1.Value) * tile.Width) + (e.X / (int)numericUpDown1.Value)] = (byte)((selectedColor.Y * 16) + selectedColor.X);
                lastpoint = new Point(e.X / (int)numericUpDown1.Value, e.Y / (int)numericUpDown1.Value);
                DrawTile();
            }
        }

        private Graphics tileGfx;
        private void DrawTile()
        {
            if (tile == null) return;
            tileGfx.DrawImage(tile.Scale((int)numericUpDown1.Value).ToBitmap(Palette), 0, 0, tile.Width * (int)numericUpDown1.Value, tile.Height * (int)numericUpDown1.Value);
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            if (tile == null) return;
            TilePicture.Size = new Size(tile.Width * (int)numericUpDown1.Value, tile.Height * (int)numericUpDown1.Value);
            DrawTile();
        }

        private void TilePicture_Resize(object sender, EventArgs e)
        {
            tileGfx = TilePicture.CreateGraphics();
            tileGfx.SetOptions();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (imagefile != null)
                switch (MessageBox.Show(this, "Do you want to save?", "Hocus Pocus Image Viewer", MessageBoxButtons.YesNoCancel))
                {
                    case DialogResult.Yes:
                        saveToolStripMenuItem_Click(sender, e);
                        break;
                    case DialogResult.Cancel:
                        return;
                }
            using (OpenFileDialog fd = new OpenFileDialog() { DefaultExt = "img", Filter = "Image Files|*.img|All Files|*.*", RestoreDirectory = true })
                if (fd.ShowDialog(this) == DialogResult.OK)
                    LoadImage(fd.FileName);
        }

        const double VGAMultiplier = 4.04761904761905;
        private void loadPaletteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog fd = new OpenFileDialog() { DefaultExt = "pal", Filter = "Palette Files|*.pal|All Files|*.*", RestoreDirectory = true })
                if (fd.ShowDialog(this) == DialogResult.OK)
                    using (PaletteImportDialog impdlg = new PaletteImportDialog() { Index = selectedColor.Y * 16 + selectedColor.X})
                        if (impdlg.ShowDialog(this) == DialogResult.OK)
                        {
                            using (Stream str = fd.OpenFile())
                            using (BinaryReader br = new BinaryReader(str))
                                for (int i = 0; i < str.Length / 3 && i + impdlg.Index < Palette.Length; i++)
                                    Palette[i + impdlg.Index] = Color.FromArgb((int)(br.ReadByte() * VGAMultiplier), (int)(br.ReadByte() * VGAMultiplier), (int)(br.ReadByte() * VGAMultiplier));
                            PalettePanel.Invalidate();
                            DrawTile();
                        }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (Stream str = File.Create(imagefile))
                tile.WriteHocusImage(str);
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog fd = new SaveFileDialog() { DefaultExt = "img", FileName = Path.GetFileName(imagefile), Filter = "Image Files|*.img|All Files|*.*", InitialDirectory = Path.GetDirectoryName(imagefile), RestoreDirectory = true })
                if (fd.ShowDialog(this) == DialogResult.OK)
                {
                    using (Stream str = File.Create(fd.FileName))
                        tile.WriteHocusImage(str);
                    Text = "Hocus Pocus Image Viewer - " + Path.GetFileName(fd.FileName);
                    imagefile = fd.FileName;
                    UpdateMRU(fd.FileName);
                }
        }

        private void recentFilesToolStripMenuItem_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (imagefile != null)
                switch (MessageBox.Show(this, "Do you want to save?", "Hocus Pocus Image Viewer", MessageBoxButtons.YesNoCancel))
                {
                    case DialogResult.Yes:
                        saveToolStripMenuItem_Click(sender, e);
                        break;
                    case DialogResult.Cancel:
                        return;
                }
            LoadImage(Settings.RecentFiles[recentFilesToolStripMenuItem.DropDownItems.IndexOf(e.ClickedItem)]);
        }

        private void LoadImage(string filename)
        {
            using (Stream str = File.OpenRead(filename))
                tile = BitmapBits.ReadHocusImage(str);
            Text = "Hocus Pocus Image Viewer - " + Path.GetFileName(filename);
            imagefile = filename;
            UpdateMRU(filename);
            saveToolStripMenuItem.Enabled = saveAsToolStripMenuItem.Enabled = importToolStripMenuItem.Enabled = exportPNGToolStripMenuItem.Enabled = true;
            TilePicture.Size = new Size(tile.Width * (int)numericUpDown1.Value, tile.Height * (int)numericUpDown1.Value);
            DrawTile();
        }

        private void importToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog opendlg = new OpenFileDialog() { DefaultExt = "png", Filter = "Image Files|*.bmp;*.png;*.jpg;*.gif", RestoreDirectory = true })
                if (opendlg.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
                {
                    Bitmap bmp = new Bitmap(opendlg.FileName);
                    switch (bmp.PixelFormat)
                    {
                        case PixelFormat.Format1bppIndexed:
                        case PixelFormat.Format32bppArgb:
                        case PixelFormat.Format4bppIndexed:
                            break;
                        case PixelFormat.Format8bppIndexed:
                            tile = new BitmapBits(bmp);
                            bmp.Dispose();
                            TilePicture.Size = new Size(tile.Width * (int)numericUpDown1.Value, tile.Height * (int)numericUpDown1.Value);
                            DrawTile();
                            return;
                        default:
                            Bitmap newbmp = bmp.To32bpp();
                            bmp.Dispose();
                            bmp = newbmp;
                            break;
                    }
                    tile = new BitmapBits(bmp.Width, bmp.Height);
                    BitmapData bmpd = bmp.LockBits(new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, bmp.PixelFormat);
                    int stride = bmpd.Stride;
                    byte[] Bits = new byte[Math.Abs(stride) * bmpd.Height];
                    System.Runtime.InteropServices.Marshal.Copy(bmpd.Scan0, Bits, 0, Bits.Length);
                    bmp.UnlockBits(bmpd);
                    bmp.Dispose();
                    switch (bmpd.PixelFormat)
                    {
                        case PixelFormat.Format1bppIndexed:
                            LoadBitmap1BppIndexed(tile, Bits, stride);
                            break;
                        case PixelFormat.Format32bppArgb:
                            LoadBitmap32BppArgb(tile, Bits, stride, Palette);
                            break;
                        case PixelFormat.Format4bppIndexed:
                            LoadBitmap4BppIndexed(tile, Bits, stride);
                            break;
                    }
                    TilePicture.Size = new Size(tile.Width * (int)numericUpDown1.Value, tile.Height * (int)numericUpDown1.Value);
                    DrawTile();
                }
        }

        private static void LoadBitmap1BppIndexed(BitmapBits bmp, byte[] Bits, int Stride)
        {
            for (int y = 0; y < bmp.Height; y++)
            {
                int srcaddr = y * Math.Abs(Stride);
                for (int x = 0; x < bmp.Width; x += 8)
                {
                    bmp[x + 0, y] = (byte)((Bits[srcaddr + (x / 8)] >> 7) & 1);
                    bmp[x + 1, y] = (byte)((Bits[srcaddr + (x / 8)] >> 6) & 1);
                    bmp[x + 2, y] = (byte)((Bits[srcaddr + (x / 8)] >> 5) & 1);
                    bmp[x + 3, y] = (byte)((Bits[srcaddr + (x / 8)] >> 4) & 1);
                    bmp[x + 4, y] = (byte)((Bits[srcaddr + (x / 8)] >> 3) & 1);
                    bmp[x + 5, y] = (byte)((Bits[srcaddr + (x / 8)] >> 2) & 1);
                    bmp[x + 6, y] = (byte)((Bits[srcaddr + (x / 8)] >> 1) & 1);
                    bmp[x + 7, y] = (byte)(Bits[srcaddr + (x / 8)] & 1);
                }
            }
        }

        private static void LoadBitmap32BppArgb(BitmapBits bmp, byte[] Bits, int Stride, Color[] palette)
        {
            for (int y = 0; y < bmp.Height; y++)
            {
                int srcaddr = y * Math.Abs(Stride);
                for (int x = 0; x < bmp.Width; x++)
                {
                    Color col = Color.FromArgb(BitConverter.ToInt32(Bits, srcaddr + (x * 4)));
                    bmp[x, y] = (byte)Array.IndexOf(palette, col.FindNearestMatch(palette));
                }
            }
        }

        public static void LoadBitmap4BppIndexed(BitmapBits bmp, byte[] Bits, int Stride)
        {
            for (int y = 0; y < bmp.Height; y++)
            {
                int srcaddr = y * Math.Abs(Stride);
                for (int x = 0; x < bmp.Width; x += 2)
                {
                    bmp[x, y] = (byte)(Bits[srcaddr + (x / 2)] >> 4);
                    bmp[x + 1, y] = (byte)(Bits[srcaddr + (x / 2)] & 0xF);
                }
            }
        }

        private void exportPNGToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog fd = new SaveFileDialog() { DefaultExt = "png", FileName = Path.ChangeExtension(Path.GetFileName(imagefile), "png"), InitialDirectory = Path.GetDirectoryName(imagefile), Filter = "PNG Files|*.png", RestoreDirectory = true })
                if (fd.ShowDialog(this) == DialogResult.OK)
                    using (Bitmap bmp = tile.ToBitmap(Palette))
                        bmp.Save(fd.FileName);
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (imagefile != null)
                switch (MessageBox.Show(this, "Do you want to save?", "Hocus Pocus Image Viewer", MessageBoxButtons.YesNoCancel))
                {
                    case DialogResult.Yes:
                        saveToolStripMenuItem_Click(sender, EventArgs.Empty);
                        break;
                    case DialogResult.Cancel:
                        e.Cancel = true;
                        return;
                }
            Settings.Save();
            using (Stream str = File.Create("palette.pal"))
            using (BinaryWriter bw = new BinaryWriter(str))
                for (int i = 0; i < Palette.Length; i++)
                {
                    bw.Write(Palette[i].R);
                    bw.Write(Palette[i].G);
                    bw.Write(Palette[i].B);
                }
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