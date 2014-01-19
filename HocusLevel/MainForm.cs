﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using HocusSharp;

namespace HocusLevel
{
    public partial class MainForm : Form
    {
        public static MainForm Instance { get; private set; }
        Properties.Settings Settings = Properties.Settings.Default;

        public MainForm()
        {
            Instance = this;
            InitializeComponent();
        }

        ImageAttributes imageTransparency = new ImageAttributes();
        Bitmap LevelBmp;
        Graphics LevelGfx, Panel1Gfx, Panel2Gfx, Panel3Gfx;
        internal bool loaded;
        internal byte SelectedChunk;
        internal List<Entry> SelectedItems;
        ObjectList ObjectSelect;
        EditingMode FGMode, BGMode;
        Rectangle FGSelection, BGSelection;
        internal ColorPalette LevelImgPalette;
        double ZoomLevel = 1;
        bool objdrag = false;
        bool dragdrop = false;
        byte dragobj;
        Point dragpoint;
        bool selecting = false;
        Point selpoint;
        List<Point> locs = new List<Point>();
        List<byte> tiles = new List<byte>();
        Point lastchunkpoint;
        Point lastmouse;
        Stack<UndoAction> UndoList;
        Stack<UndoAction> RedoList;
        Dictionary<string, ToolStripMenuItem> levelMenuItems;
        Dictionary<char, BitmapBits> HUDLetters, HUDNumbers;

        private void MainForm_Load(object sender, EventArgs e)
        {
            System.Drawing.Imaging.ColorMatrix x = new System.Drawing.Imaging.ColorMatrix();
            x.Matrix33 = 0.75f;
            imageTransparency.SetColorMatrix(x, System.Drawing.Imaging.ColorMatrixFlag.Default, System.Drawing.Imaging.ColorAdjustType.Bitmap);
            hUDToolStripMenuItem.Checked = Settings.ShowHUD;
            enableGridToolStripMenuItem.Checked = Settings.ShowGrid;
            if (Settings.MRUList == null)
                Settings.MRUList = new System.Collections.Specialized.StringCollection();
            System.Collections.Specialized.StringCollection mru = new System.Collections.Specialized.StringCollection();
            foreach (string item in Settings.MRUList)
                if (File.Exists(item))
                {
                    mru.Add(item);
                    recentProjectsToolStripMenuItem.DropDownItems.Add(item.Replace("&", "&&"));
                }
            Settings.MRUList = mru;
            if (mru.Count > 0) recentProjectsToolStripMenuItem.DropDownItems.Remove(noneToolStripMenuItem2);
            if (Program.Arguments.Length > 0)
                LoadINI(Program.Arguments[0]);
        }

        private void LoadINI(string filename)
        {
            Panel1Gfx = panel1.CreateGraphics();
            Panel1Gfx.SetOptions();
            Panel2Gfx = panel2.CreateGraphics();
            Panel2Gfx.SetOptions();
            Panel3Gfx = panel3.CreateGraphics();
            Panel3Gfx.SetOptions();
            LevelData.LoadGame(filename);
            changeLevelToolStripMenuItem.DropDownItems.Clear();
            levelMenuItems = new Dictionary<string, ToolStripMenuItem>();
            foreach (KeyValuePair<string, LevelInfo> item in LevelData.Game.Levels)
            {
                if (!string.IsNullOrEmpty(item.Key))
                {
                    string[] itempath = item.Key.Split('\\');
                    ToolStripMenuItem parent = changeLevelToolStripMenuItem;
                    string curpath = string.Empty;
                    for (int i = 0; i < itempath.Length - 1; i++)
                    {
                        if (i - 1 >= 0)
                            curpath += @"\";
                        curpath += itempath[i];
                        if (!levelMenuItems.ContainsKey(curpath))
                        {
                            ToolStripMenuItem it = new ToolStripMenuItem(itempath[i].Replace("&", "&&"));
                            levelMenuItems.Add(curpath, it);
                            parent.DropDownItems.Add(it);
                            parent = it;
                        }
                        else
                            parent = levelMenuItems[curpath];
                    }
                    ToolStripMenuItem ts = new ToolStripMenuItem(itempath[itempath.Length - 1], null, new EventHandler(LevelToolStripMenuItem_Clicked)) { Tag = item.Key };
                    levelMenuItems.Add(item.Key, ts);
                    parent.DropDownItems.Add(ts);
                }
            }
            timeZoneToolStripMenuItem.Visible = false;
            switch (LevelData.Game.EngineVersion)
            {
                case EngineVersion.S1:
                    Icon = Properties.Resources.gogglemon;
                    break;
                case EngineVersion.SCDPC:
                    Icon = Properties.Resources.clockmon;
                    timeZoneToolStripMenuItem.Visible = true;
                    break;
                case EngineVersion.S2:
                case EngineVersion.S2NA:
                    Icon = Properties.Resources.Tailsmon2;
                    break;
                case EngineVersion.S3K:
                    Icon = Properties.Resources.watermon;
                    break;
                case EngineVersion.SKC:
                    Icon = Properties.Resources.lightningmon;
                    break;
                default:
                    throw new NotImplementedException("Game type " + LevelData.Game.EngineVersion.ToString() + " is not supported!");
            }
            Text = "SonLVL - " + LevelData.Game.EngineVersion.ToString();
            buildAndRunToolStripMenuItem.Enabled = LevelData.Game.BuildScript != null & (LevelData.Game.ROMFile != null | LevelData.Game.RunCommand != null);
            if (Settings.MRUList.Count == 0)
                recentProjectsToolStripMenuItem.DropDownItems.Remove(noneToolStripMenuItem2);
            if (Settings.MRUList.Contains(filename))
            {
                recentProjectsToolStripMenuItem.DropDownItems.RemoveAt(Settings.MRUList.IndexOf(filename));
                Settings.MRUList.Remove(filename);
            }
            Settings.MRUList.Insert(0, filename);
            recentProjectsToolStripMenuItem.DropDownItems.Insert(0, new ToolStripMenuItem(filename));
        }

        #region Main Menu
        #region File Menu
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (loaded)
            {
                switch (MessageBox.Show(this, "Do you want to save?", Text, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question))
                {
                    case DialogResult.Yes:
                        saveToolStripMenuItem_Click(this, EventArgs.Empty);
                        break;
                    case DialogResult.Cancel:
                        return;
                }
            }
            using (OpenFileDialog a = new OpenFileDialog()
            {
                DefaultExt = "ini",
                Filter = "INI Files|*.ini|All Files|*.*"
            })
                if (a.ShowDialog(this) == DialogResult.OK)
                {
                    loaded = false;
                    LoadINI(a.FileName);
                }
        }

        private void LevelToolStripMenuItem_Clicked(object sender, EventArgs e)
        {
            if (loaded)
            {
                fileToolStripMenuItem.DropDown.Hide();
                switch (MessageBox.Show(this, "Do you want to save?", Text, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question))
                {
                    case DialogResult.Yes:
                        saveToolStripMenuItem_Click(this, EventArgs.Empty);
                        break;
                    case DialogResult.Cancel:
                        return;
                }
            }
            loaded = false;
            foreach (KeyValuePair<string, ToolStripMenuItem> item in levelMenuItems)
                item.Value.Checked = false;
            ((ToolStripMenuItem)sender).Checked = true;
            Enabled = false;
            Text = "SonLVL - " + LevelData.Game.EngineVersion + " - Loading " + LevelData.Game.GetLevelInfo((string)((ToolStripMenuItem)sender).Tag).DisplayName + "...";
#if !DEBUG
            backgroundLevelLoader.RunWorkerAsync(((ToolStripMenuItem)sender).Tag);
#else
            backgroundLevelLoader_DoWork(null, new DoWorkEventArgs(((ToolStripMenuItem)sender).Tag));
            backgroundLevelLoader_RunWorkerCompleted(null, null);
#endif
        }

        Exception initerror = null;
        private void backgroundLevelLoader_DoWork(object sender, DoWorkEventArgs e)
        {
#if !DEBUG
            try
            {
#endif
                SelectedChunk = 0;
                UndoList = new Stack<UndoAction>();
                RedoList = new Stack<UndoAction>();
                LevelData.LoadLevel((string)e.Argument, true);
                LevelImgPalette = new Bitmap(1, 1, PixelFormat.Format8bppIndexed).Palette;
                for (int i = 0; i < 64; i++)
                    LevelImgPalette.Entries[i] = LevelData.PaletteToColor(i / 16, i % 16, true);
                for (int i = 64; i < 256; i++)
                    LevelImgPalette.Entries[i] = Color.Black;
                LevelImgPalette.Entries[0] = LevelData.PaletteToColor(2, 0, false);
                LevelImgPalette.Entries[64] = Color.White;
                LevelImgPalette.Entries[65] = Color.Yellow;
                LevelImgPalette.Entries[66] = Color.Black;
                LevelImgPalette.Entries[67] = Settings.GridColor;
                curpal = new Color[16];
                for (int i = 0; i < 16; i++)
                    curpal[i] = LevelData.PaletteToColor(0, i, false);
#if !DEBUG
            }
            catch (Exception ex) { initerror = ex; }
#endif
        }

        private void backgroundLevelLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (initerror != null)
            {
                Log(initerror.ToString().Split(new string[] { Environment.NewLine }, StringSplitOptions.None));
                System.IO.File.WriteAllLines("SonLVL.log", LogFile.ToArray());
                using (ErrorDialog ed = new ErrorDialog(initerror.GetType().Name + ": " + initerror.Message + "\nLog file has been saved to " + System.IO.Path.Combine(Environment.CurrentDirectory, "SonLVL.log") + ".\nSend this to MainMemory on the Sonic Retro forums.", true))
                    if (ed.ShowDialog(this) == System.Windows.Forms.DialogResult.Cancel) Close();
                Enabled = true;
                return;
            }
            Log("Load completed.");
            ChunkSelector.Images = LevelData.CompChunkBmps;
            ChunkSelector.ImageSize = LevelData.chunksz;
            BlockSelector.Images = LevelData.CompBlockBmps;
            BlockSelector.ChangeSize();
            CollisionSelector.Images = new List<Bitmap>(LevelData.ColBmps);
            CollisionSelector.ChangeSize();
            ChunkSelector.SelectedIndex = 0;
            ChunkPicture.Size = new Size(LevelData.chunksz, LevelData.chunksz);
            BlockSelector.SelectedIndex = 0;
            TileSelector.Images.Clear();
            for (int i = 0; i < LevelData.Tiles.Count; i++)
                TileSelector.Images.Add(LevelData.TileToBmp4bpp(LevelData.Tiles[i], 0, SelectedColor.Y));
            TileSelector.SelectedIndex = 0;
            TileSelector.ChangeSize();
            switch (LevelData.Level.ChunkFormat)
            {
                case EngineVersion.S1:
                case EngineVersion.SCD:
                case EngineVersion.SCDPC:
                    BlockCollision2.Visible = false;
                    button2.Visible = false;
                    path2ToolStripMenuItem.Visible = false;
                    break;
                case EngineVersion.S2:
                case EngineVersion.S2NA:
                case EngineVersion.S3K:
                case EngineVersion.SKC:
                    BlockCollision2.Visible = true;
                    button2.Visible = true;
                    path2ToolStripMenuItem.Visible = true;
                    break;
            }
            if (ObjectSelect == null)
            {
                ObjectSelect = new ObjectList();
                ObjectSelect.listView1.SelectedIndexChanged += new EventHandler(ObjectSelect_listView1_SelectedIndexChanged);
                ObjectSelect.listView2.SelectedIndexChanged += new EventHandler(ObjectSelect_listView2_SelectedIndexChanged);
            }
            ObjectSelect.listView1.Items.Clear();
            ObjectSelect.imageList1.Images.Clear();
            objectTypeList.Items.Clear();
            objectTypeImages.Images.Clear();
            foreach (KeyValuePair<byte, ObjectDefinition> item in LevelData.ObjTypes)
            {
                Bitmap image = item.Value.Image.Image.ToBitmap(LevelData.BmpPal);
                ObjectSelect.imageList1.Images.Add(image.Resize(ObjectSelect.imageList1.ImageSize));
                ObjectSelect.listView1.Items.Add(new ListViewItem(item.Value.Name, ObjectSelect.imageList1.Images.Count - 1) { Tag = item.Key });
                objectTypeImages.Images.Add(image.Resize(objectTypeImages.ImageSize));
                objectTypeList.Items.Add(new ListViewItem(item.Value.Name, objectTypeImages.Images.Count - 1) { Tag = item.Key });
            }
            ObjectSelect.listView2.Items.Clear();
            ObjectSelect.imageList2.Images.Clear();
            ColIndBox.Visible = collisionToolStripMenuItem.Visible = LevelData.ColInds1.Count > 0;
            Text = "SonLVL - " + LevelData.Game.EngineVersion + " - " + LevelData.Level.DisplayName;
            UpdateScrollBars();
            hScrollBar1.Value = 0;
            hScrollBar1.SmallChange = 16;
            hScrollBar1.LargeChange = 128;
            vScrollBar1.Value = 0;
            vScrollBar1.SmallChange = 16;
            vScrollBar1.LargeChange = 128;
            hScrollBar1.Enabled = true;
            vScrollBar1.Enabled = true;
            hScrollBar2.Value = 0;
            hScrollBar2.SmallChange = 16;
            hScrollBar2.LargeChange = 128;
            vScrollBar2.Value = 0;
            vScrollBar2.SmallChange = 16;
            vScrollBar2.LargeChange = 128;
            hScrollBar2.Enabled = true;
            vScrollBar2.Enabled = true;
            hScrollBar3.Value = 0;
            hScrollBar3.SmallChange = 16;
            hScrollBar3.LargeChange = 128;
            vScrollBar3.Value = 0;
            vScrollBar3.SmallChange = 16;
            vScrollBar3.LargeChange = 128;
            hScrollBar3.Enabled = true;
            vScrollBar3.Enabled = true;
            loaded = true;
            SelectedItems = new List<Entry>();
            undoCtrlZToolStripMenuItem.DropDownItems.Clear();
            redoCtrlYToolStripMenuItem.DropDownItems.Clear();
            saveToolStripMenuItem.Enabled = true;
            editToolStripMenuItem.Enabled = true;
            exportToolStripMenuItem.Enabled = true;
            paletteToolStripMenuItem2.DropDownItems.Clear();
            foreach (string item in LevelData.PalName)
                paletteToolStripMenuItem2.DropDownItems.Add(new ToolStripMenuItem(item));
            ((ToolStripMenuItem)paletteToolStripMenuItem2.DropDownItems[0]).Checked = true;
            blendAlternatePaletteToolStripMenuItem.Enabled = LevelData.Palette.Count > 1;
            timeZoneToolStripMenuItem.Visible = LevelData.Level.TimeZone != API.TimeZone.None;
            SelectedObjectChanged();
            Enabled = true;
            DrawLevel();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LevelData.SaveLevel();
        }

        private void buildAndRunToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Path.Combine(Environment.CurrentDirectory, LevelData.Game.BuildScript)) { WorkingDirectory = Path.GetDirectoryName(Path.Combine(Environment.CurrentDirectory, LevelData.Game.BuildScript)) }).WaitForExit();
            string romfile = LevelData.Game.ROMFile ?? LevelData.Game.RunCommand;
            if (LevelData.Game.UseEmulator)
                if (!string.IsNullOrEmpty(Settings.Emulator))
                    System.Diagnostics.Process.Start(Settings.Emulator, '"' + Path.Combine(Environment.CurrentDirectory, romfile) + '"');
                else
                    MessageBox.Show("You must set up an emulator before you can run the ROM, use File -> Setup Emulator.");
            else
                System.Diagnostics.Process.Start(Path.Combine(Environment.CurrentDirectory, romfile));
        }

        private void setupEmulatorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog opn = new OpenFileDialog() { DefaultExt = "exe", Filter = "EXE Files|*.exe|All Files|*.*", RestoreDirectory = true })
            {
                if (!string.IsNullOrEmpty(Settings.Emulator))
                {
                    opn.FileName = Path.GetFileName(Settings.Emulator);
                    opn.InitialDirectory = Path.GetDirectoryName(Settings.Emulator);
                }
                if (opn.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    Settings.Emulator = opn.FileName;
                    setupEmulatorToolStripMenuItem.Checked = true;
                }
            }
        }

        private void recentProjectsToolStripMenuItem_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            loaded = false;
            LoadINI(Settings.MRUList[recentProjectsToolStripMenuItem.DropDownItems.IndexOf(e.ClickedItem)]);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }
        #endregion

        #region Edit Menu
        public void AddUndo(UndoAction action)
        {
            UndoList.Push(action);
            undoCtrlZToolStripMenuItem.DropDownItems.Insert(0, new ToolStripMenuItem(action.Name));
            if (UndoList.Count > 100)
            {
                UndoAction[] l = UndoList.ToArray();
                UndoList.Clear();
                for (int i = 99; i >= 0; i--)
                {
                    UndoList.Push(l[i]);
                }
                undoCtrlZToolStripMenuItem.DropDownItems.RemoveAt(100);
            }
            RedoList.Clear();
            redoCtrlYToolStripMenuItem.DropDownItems.Clear();
        }

        private void undoCtrlZToolStripMenuItem_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            DoUndo(undoCtrlZToolStripMenuItem.DropDownItems.IndexOf(e.ClickedItem) + 1);
        }

        private void redoCtrlYToolStripMenuItem_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            DoRedo(redoCtrlYToolStripMenuItem.DropDownItems.IndexOf(e.ClickedItem) + 1);
        }

        private void DoUndo(int num)
        {
            for (int i = 0; i < num; i++)
            {
                undoCtrlZToolStripMenuItem.DropDownItems.RemoveAt(0);
                UndoAction act = UndoList.Pop();
                act.Undo();
                RedoList.Push(act);
                redoCtrlYToolStripMenuItem.DropDownItems.Insert(0, new ToolStripMenuItem(act.Name));
            }
            SelectedItems.Clear();
            SelectedObjectChanged();
            DrawLevel();
        }

        private void DoRedo(int num)
        {
            for (int i = 0; i < num; i++)
            {
                redoCtrlYToolStripMenuItem.DropDownItems.RemoveAt(0);
                UndoAction act = RedoList.Pop();
                act.Redo();
                UndoList.Push(act);
                undoCtrlZToolStripMenuItem.DropDownItems.Insert(0, new ToolStripMenuItem(act.Name));
            }
            SelectedItems.Clear();
            SelectedObjectChanged();
            DrawLevel();
        }

        private void blendAlternatePaletteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (AlternatePaletteDialog dlg = new AlternatePaletteDialog())
            {
                if (dlg.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
                {
                    int underwater = LevelData.CurPal;
                    LevelData.CurPal = 0;
                    Color[,] pal = new Color[4, 16];
                    for (int l = 0; l < 4; l++)
                        for (int i = 0; i < 16; i++)
                        {
                            Color col = LevelData.PaletteToColor(l, i, false);
                            if (dlg.radioButton1.Checked)
                                pal[l, i] = col.Blend(dlg.BlendColor);
                            else if (dlg.radioButton2.Checked)
                                pal[l, i] = Color.FromArgb(Math.Min(col.R + dlg.BlendColor.R, 255), Math.Min(col.G + dlg.BlendColor.G, 255), Math.Min(col.B + dlg.BlendColor.B, 255));
                            else
                                pal[l, i] = Color.FromArgb(Math.Max(col.R - dlg.BlendColor.R, 0), Math.Max(col.G - dlg.BlendColor.G, 0), Math.Max(col.B - dlg.BlendColor.B, 0));
                        }
                    LevelData.CurPal = dlg.paletteIndex.SelectedIndex + 1;
                    for (int l = 0; l < 4; l++)
                        for (int i = 0; i < 16; i++)
                            LevelData.ColorToPalette(l, i, pal[l, i]);
                    LevelData.CurPal = underwater;
                    LevelData.PaletteChanged();
                }
            }
        }
        #endregion

        #region View Menu
        private void objectsAboveHighPlaneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            objectsAboveHighPlaneToolStripMenuItem.Checked = !objectsAboveHighPlaneToolStripMenuItem.Checked;
            DrawLevel();
        }

        private void paletteToolStripMenuItem2_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            foreach (ToolStripMenuItem item in paletteToolStripMenuItem2.DropDownItems)
                item.Checked = false;
            ((ToolStripMenuItem)e.ClickedItem).Checked = true;
            LevelData.CurPal = paletteToolStripMenuItem2.DropDownItems.IndexOf(e.ClickedItem);
            LevelData.PaletteChanged();
        }

        private void lowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DrawLevel();
        }

        private void highToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DrawLevel();
        }

        private void collisionToolStripMenuItem_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (collisionToolStripMenuItem.DropDownItems.IndexOf(e.ClickedItem) < 3)
            {
                bool angles = anglesToolStripMenuItem.Checked;
                foreach (ToolStripItem item in collisionToolStripMenuItem.DropDownItems)
                    if (item is ToolStripMenuItem)
                        ((ToolStripMenuItem)item).Checked = false;
                ((ToolStripMenuItem)e.ClickedItem).Checked = true;
                anglesToolStripMenuItem.Checked = angles;
                DrawLevel();
            }
        }

        private void anglesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            anglesToolStripMenuItem.Checked = !anglesToolStripMenuItem.Checked;
            DrawLevel();
        }

        private void currentOnlyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            currentOnlyToolStripMenuItem.Checked = true;
            allToolStripMenuItem.Checked = false;
            DrawLevel();
        }

        private void allToolStripMenuItem_Click(object sender, EventArgs e)
        {
            allToolStripMenuItem.Checked = true;
            currentOnlyToolStripMenuItem.Checked = false;
            DrawLevel();
        }

        private void gridColorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ColorDialog a = new ColorDialog
            {
                AllowFullOpen = true,
                AnyColor = true,
                FullOpen = true,
                SolidColorOnly = true,
                Color = Settings.GridColor
            };
            if (cols != null)
                a.CustomColors = cols;
            if (a.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Settings.GridColor = a.Color;
                if (loaded)
                {
                    LevelImgPalette.Entries[67] = a.Color;
                    DrawLevel();
                }
            }
            cols = a.CustomColors;
            a.Dispose();
        }

        private void zoomToolStripMenuItem_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            foreach (ToolStripMenuItem item in zoomToolStripMenuItem.DropDownItems)
                item.Checked = false;
            ((ToolStripMenuItem)e.ClickedItem).Checked = true;
            switch (zoomToolStripMenuItem.DropDownItems.IndexOf(e.ClickedItem))
            {
                case 0: // 1/2x
                    ZoomLevel = 0.5;
                    break;
                default:
                    ZoomLevel = zoomToolStripMenuItem.DropDownItems.IndexOf(e.ClickedItem);
                    break;
            }
            if (!loaded) return;
            loaded = false;
            UpdateScrollBars();
            loaded = true;
            DrawLevel();
        }

        private void logToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LogWindow = new LogWindow();
            LogWindow.Show(this);
            logToolStripMenuItem.Enabled = false;
        }
        #endregion

        #region Export Menu
        private void paletteToolStripMenuItem_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            exportToolStripMenuItem.DropDown.Hide();
            SaveFileDialog a = new SaveFileDialog() { DefaultExt = "png", Filter = "PNG Files|*.png", RestoreDirectory = true };
            if (a.ShowDialog() == DialogResult.OK)
            {
                int line = paletteToolStripMenuItem.DropDownItems.IndexOf(e.ClickedItem);
                if (line < 4)
                {
                    BitmapBits bmp = new BitmapBits(16 * 8, 8);
                    Color[] pal = new Color[16];
                    for (int i = 0; i < 16; i++)
                    {
                        pal[i] = LevelData.PaletteToColor(line, i, false);
                        bmp.FillRectangle((byte)i, i * 8, 0, 8, 8);
                    }
                    bmp.ToBitmap(pal).Save(a.FileName);
                }
                else
                {
                    BitmapBits bmp = new BitmapBits(16 * 8, 4 * 8);
                    Color[] pal = new Color[256];
                    for (int i = 0; i < 64; i++)
                        pal[i] = LevelData.PaletteToColor(i / 16, i % 16, false);
                    for (int i = 64; i < 256; i++)
                        pal[i] = Color.Black;
                    for (int y = 0; y < 4; y++)
                        for (int x = 0; x < 16; x++)
                            bmp.FillRectangle((byte)((y * 16) + x), x * 8, y * 8, 8, 8);
                    bmp.ToBitmap(pal).Save(a.FileName);
                }
            }
        }

        private void tilesToolStripMenuItem_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            exportToolStripMenuItem.DropDown.Hide();
            using (FolderBrowserDialog a = new FolderBrowserDialog() { SelectedPath = Environment.CurrentDirectory })
                if (a.ShowDialog() == DialogResult.OK)
                    for (int i = 0; i < LevelData.Tiles.Count; i++)
                        LevelData.TileToBmp4bpp(LevelData.Tiles[i], 0, tilesToolStripMenuItem.DropDownItems.IndexOf(e.ClickedItem)).Save(Path.Combine(a.SelectedPath, i + ".png"));
        }

        private void blocksToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!highToolStripMenuItem.Checked & !lowToolStripMenuItem.Checked)
            {
                MessageBox.Show(this, "Cannot export blocks with no planes visible.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            using (FolderBrowserDialog a = new FolderBrowserDialog() { SelectedPath = Environment.CurrentDirectory })
                if (a.ShowDialog() == DialogResult.OK)
                {
                    Color[] palette = new Color[256];
                    for (int i = 0; i < 64; i++)
                        palette[i] = LevelData.PaletteToColor(i / 16, i % 16, false);
                    for (int i = 64; i < 256; i++)
                        palette[i] = Color.Black;
                    palette[0] = LevelData.PaletteToColor(2, 0, false);
                    for (int i = 0; i < LevelData.Blocks.Count; i++)
                    {
                        BitmapBits bits = null;
                        if (highToolStripMenuItem.Checked & lowToolStripMenuItem.Checked)
                            bits = new BitmapBits(LevelData.CompBlockBmpBits[i]);
                        else if (lowToolStripMenuItem.Checked)
                            bits = new BitmapBits(LevelData.BlockBmpBits[i][0]);
                        else
                            bits = new BitmapBits(LevelData.BlockBmpBits[i][1]);
                        if (path1ToolStripMenuItem.Checked)
                            bits.DrawBitmapComposited(LevelData.ColBmpBits[LevelData.ColInds1[i]], 0, 0);
                        else if (path2ToolStripMenuItem.Checked)
                            bits.DrawBitmapComposited(LevelData.ColBmpBits[LevelData.ColInds2[i]], 0, 0);
                        bits.ToBitmap(LevelImgPalette).Save(Path.Combine(a.SelectedPath, i + ".png"));
                    }
                }
        }

        private void chunksToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!highToolStripMenuItem.Checked & !lowToolStripMenuItem.Checked)
            {
                MessageBox.Show(this, "Cannot export chunks with no planes visible.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            using (FolderBrowserDialog a = new FolderBrowserDialog() { SelectedPath = Environment.CurrentDirectory })
                if (a.ShowDialog() == DialogResult.OK)
                {
                    for (int i = 0; i < LevelData.Chunks.Count; i++)
                    {
                        BitmapBits bits = null;
                        if (highToolStripMenuItem.Checked & lowToolStripMenuItem.Checked)
                            bits = new BitmapBits(LevelData.CompChunkBmpBits[i]);
                        else if (lowToolStripMenuItem.Checked)
                            bits = new BitmapBits(LevelData.ChunkBmpBits[i][0]);
                        else
                            bits = new BitmapBits(LevelData.ChunkBmpBits[i][1]);
                        if (path1ToolStripMenuItem.Checked)
                            bits.DrawBitmapComposited(LevelData.ChunkColBmpBits[i][0], 0, 0);
                        else if (path2ToolStripMenuItem.Checked)
                            bits.DrawBitmapComposited(LevelData.ChunkColBmpBits[i][1], 0, 0);
                        bits.ToBitmap(LevelImgPalette).Save(Path.Combine(a.SelectedPath, i + ".png"));
                    }
                }
        }

        private void foregroundToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog a = new SaveFileDialog()
                        {
                            DefaultExt = "png",
                            Filter = "PNG Files|*.png",
                            RestoreDirectory = true
                        })
                if (a.ShowDialog() == DialogResult.OK)
                {
                    BitmapBits bmp = LevelData.DrawForeground(null, includeobjectsWithFGToolStripMenuItem.Checked, !hideDebugObjectsToolStripMenuItem.Checked, objectsAboveHighPlaneToolStripMenuItem.Checked, lowToolStripMenuItem.Checked, highToolStripMenuItem.Checked, path1ToolStripMenuItem.Checked, path2ToolStripMenuItem.Checked, allToolStripMenuItem.Checked);
                    for (int i = 0; i < bmp.Bits.Length; i++)
                        if (bmp.Bits[i] == 0)
                            bmp.Bits[i] = 32;
                    Bitmap res = bmp.ToBitmap();
                    ColorPalette pal = res.Palette;
                    for (int i = 0; i < 64; i++)
                        pal.Entries[i] = LevelData.PaletteToColor(i / 16, i % 16, transparentBackFGBGToolStripMenuItem.Checked);
                    pal.Entries[64] = Color.White;
                    pal.Entries[65] = Color.Yellow;
                    pal.Entries[66] = Color.Black;
                    for (int i = 67; i < 256; i++)
                        pal.Entries[i] = Color.Black;
                    pal.Entries[0] = LevelData.PaletteToColor(2, 0, transparentBackFGBGToolStripMenuItem.Checked);
                    res.Palette = pal;
                    res.Save(a.FileName);
                }
        }

        private void backgroundToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog a = new SaveFileDialog()
                        {
                            DefaultExt = "png",
                            Filter = "PNG Files|*.png",
                            RestoreDirectory = true
                        })
                if (a.ShowDialog() == DialogResult.OK)
                {
                    BitmapBits bmp = LevelData.DrawBackground(null, lowToolStripMenuItem.Checked, highToolStripMenuItem.Checked, path1ToolStripMenuItem.Checked, path2ToolStripMenuItem.Checked);
                    for (int i = 0; i < bmp.Bits.Length; i++)
                        if (bmp.Bits[i] == 0)
                            bmp.Bits[i] = 32;
                    Bitmap res = bmp.ToBitmap();
                    ColorPalette pal = res.Palette;
                    for (int i = 0; i < 64; i++)
                        pal.Entries[i] = LevelData.PaletteToColor(i / 16, i % 16, transparentBackFGBGToolStripMenuItem.Checked);
                    pal.Entries[64] = Color.White;
                    pal.Entries[65] = Color.Yellow;
                    pal.Entries[66] = Color.Black;
                    for (int i = 67; i < 256; i++)
                        pal.Entries[i] = Color.Black;
                    pal.Entries[0] = LevelData.PaletteToColor(2, 0, transparentBackFGBGToolStripMenuItem.Checked);
                    res.Palette = pal;
                    res.Save(a.FileName);
                }
        }
        #endregion

        #region Help Menu
        private void viewReadmeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "readme.txt"));
        }

        private void reportBugToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (BugReportDialog err = new BugReportDialog())
                err.ShowDialog();
        }
        #endregion
        #endregion

        void ObjectSelect_listView2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!loaded) return;
            if (ObjectSelect.listView1.SelectedIndices.Count == 0) return;
            if (ObjectSelect.listView2.SelectedIndices.Count == 0) return;
            ObjectSelect.numericUpDown2.Value = (byte)ObjectSelect.listView2.SelectedItems[0].Tag;
        }

        void ObjectSelect_listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!loaded) return;
            if (ObjectSelect.listView1.SelectedIndices.Count == 0) return;
            byte ID = (byte)ObjectSelect.listView1.SelectedItems[0].Tag;
            ObjectSelect.numericUpDown1.Value = ID;
            ObjectSelect.numericUpDown2.Value = LevelData.ObjTypes[ID].DefaultSubtype;
            ObjectSelect.listView2.Items.Clear();
            ObjectSelect.imageList2.Images.Clear();
            foreach (byte item in LevelData.ObjTypes[ID].Subtypes)
            {
                ObjectSelect.imageList2.Images.Add(LevelData.ObjTypes[ID].SubtypeImage(item).Image.ToBitmap(LevelData.BmpPal).Resize(ObjectSelect.imageList2.ImageSize));
                ObjectSelect.listView2.Items.Add(new ListViewItem(LevelData.ObjTypes[ID].SubtypeName(item), ObjectSelect.imageList2.Images.Count - 1) { Tag = item, Selected = item == LevelData.ObjTypes[ID].DefaultSubtype });
            }
        }

        List<object> oldvalues;
        void ObjectProperties_SelectedGridItemChanged(object sender, SelectedGridItemChangedEventArgs e)
        {
            /*if (e.NewSelection.PropertyDescriptor == null || e.NewSelection.PropertyDescriptor.IsReadOnly) return;
            oldvalues = new List<object>();
            if (SelectedItems.Count > 1)
                oldvalues.Add(e.NewSelection.PropertyDescriptor.GetValue(SelectedItems.ToArray()));
            else
                oldvalues.Add(e.NewSelection.PropertyDescriptor.GetValue(SelectedItems[0]));*/
        }

        void ObjectProperties_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            /*AddUndo(new ObjectPropertyChangedUndoAction(new List<Entry>(SelectedItems), oldvalues, e.ChangedItem.PropertyDescriptor));
            oldvalues = new List<object>();*/
            foreach (Entry item in SelectedItems)
            {
                //oldvalues.Add(e.ChangedItem.PropertyDescriptor.GetValue(item));
                item.UpdateSprite();
            }
            DrawLevel();
        }

        BitmapBits LevelImg8bpp;
        internal void DrawLevel()
        {
            if (!loaded) return;
            Point pnlcur;
            Point camera;
            switch (tabControl1.SelectedIndex)
            {
                case 0:
                    pnlcur = panel1.PointToClient(Cursor.Position);
                    camera = new Point(hScrollBar1.Value, vScrollBar1.Value);
                    LevelImg8bpp = LevelData.DrawForeground(new Rectangle(camera.X, camera.Y, (int)(panel1.Width / ZoomLevel), (int)(panel1.Height / ZoomLevel)), true, true, objectsAboveHighPlaneToolStripMenuItem.Checked, lowToolStripMenuItem.Checked, highToolStripMenuItem.Checked, path1ToolStripMenuItem.Checked, path2ToolStripMenuItem.Checked, allToolStripMenuItem.Checked);
                    if (enableGridToolStripMenuItem.Checked)
                        for (int y = Math.Max(camera.Y / LevelData.chunksz, 0); y <= Math.Min(((camera.Y + (panel1.Height - 1) / ZoomLevel)) / LevelData.chunksz, LevelData.FGLayout.GetLength(1) - 1); y++)
                            for (int x = Math.Max(camera.X / LevelData.chunksz, 0); x <= Math.Min(((camera.X + (panel1.Width - 1) / ZoomLevel)) / LevelData.chunksz, LevelData.FGLayout.GetLength(0) - 1); x++)
                            {
                                LevelImg8bpp.DrawLine(67, x * LevelData.chunksz - camera.X, y * LevelData.chunksz - camera.Y, x * LevelData.chunksz - camera.X + LevelData.chunksz - 1, y * LevelData.chunksz - camera.Y);
                                LevelImg8bpp.DrawLine(67, x * LevelData.chunksz - camera.X, y * LevelData.chunksz - camera.Y, x * LevelData.chunksz - camera.X, y * LevelData.chunksz - camera.Y + LevelData.chunksz - 1);
                            }
                    if (anglesToolStripMenuItem.Checked & !noneToolStripMenuItem1.Checked)
                        for (int y = Math.Max(camera.Y / LevelData.chunksz, 0); y <= Math.Min(((camera.Y + (panel1.Height - 1) / ZoomLevel)) / LevelData.chunksz, LevelData.FGLayout.GetLength(1) - 1); y++)
                            for (int x = Math.Max(camera.X / LevelData.chunksz, 0); x <= Math.Min(((camera.X + (panel1.Width - 1) / ZoomLevel)) / LevelData.chunksz, LevelData.FGLayout.GetLength(0) - 1); x++)
                                for (int b = 0; b < LevelData.chunksz / 16; b++)
                                    for (int a = 0; a < LevelData.chunksz / 16; a++)
                                    {
                                        ChunkBlock blk = LevelData.Chunks[LevelData.FGLayout[x, y]].blocks[a, b];
                                        Solidity solid;
                                        if (path2ToolStripMenuItem.Checked)
                                            solid = ((S2ChunkBlock)blk).Solid2;
                                        else
                                            solid = blk.Solid1;
                                        if (solid == Solidity.NotSolid) continue;
                                        {
                                            byte coli;
                                            if (path2ToolStripMenuItem.Checked)
                                                coli = LevelData.ColInds2[blk.Block];
                                            else
                                                coli = LevelData.ColInds1[blk.Block];
                                            DrawHUDNum(x * LevelData.chunksz + a * 16 - camera.X, y * LevelData.chunksz + b * 16 - camera.Y, LevelData.Angles[coli].ToString("X2"));
                                        }
                                    }
                    Rectangle hudbnd = Rectangle.Empty;
                    Rectangle tmpbnd;
                    int ringcnt = 0;
                    switch (LevelData.Level.RingFormat)
                    {
                        case EngineVersion.S1:
                            foreach (ObjectEntry item in LevelData.Objects)
                                if (item.ID == 0x25)
                                    ringcnt += Math.Min(6, item.SubType & 7) + 1;
                            break;
                        case EngineVersion.S2:
                        case EngineVersion.S2NA:
                            foreach (RingEntry item in LevelData.Rings)
                                ringcnt += ((S2RingEntry)item).Count;
                            break;
                        case EngineVersion.S3K:
                        case EngineVersion.SKC:
                            ringcnt = LevelData.Rings.Count;
                            break;
                    }
                    if (hUDToolStripMenuItem.Checked)
                    {
                        tmpbnd = hudbnd = DrawHUDStr(8, 8, "Screen Pos: ");
                        hudbnd = Rectangle.Union(hudbnd, DrawHUDNum(tmpbnd.Right, tmpbnd.Top, camera.X.ToString("X4") + ' ' + camera.Y.ToString("X4")));
                        tmpbnd = DrawHUDStr(hudbnd.Left, hudbnd.Bottom, "Level Size: ");
                        hudbnd = Rectangle.Union(hudbnd, tmpbnd);
                        hudbnd = Rectangle.Union(hudbnd, DrawHUDNum(tmpbnd.Right, tmpbnd.Top, (LevelData.FGLayout.GetLength(0) * LevelData.chunksz).ToString("X4") + ' ' + (LevelData.FGLayout.GetLength(1) * LevelData.chunksz).ToString("X4")));
                        hudbnd = Rectangle.Union(hudbnd, DrawHUDStr(hudbnd.Left, hudbnd.Bottom,
                            "Objects: " + LevelData.Objects.Count + '\n' +
                            "Rings: " + ringcnt));
                    }
                    if (dragdrop)
                        LevelImg8bpp.DrawSprite(LevelData.GetObjectDefinition(dragobj).Image, dragpoint);
                    LevelBmp = LevelImg8bpp.ToBitmap(LevelImgPalette).To32bpp();
                    LevelGfx = Graphics.FromImage(LevelBmp);
                    LevelGfx.SetOptions();
                    foreach (Entry item in SelectedItems)
                    {
                        if (item is ObjectEntry)
                        {
                            ObjectEntry objitem = (ObjectEntry)item;
                            Rectangle objbnd = LevelData.GetObjectDefinition(objitem.ID).GetBounds(objitem, camera);
                            LevelGfx.FillRectangle(new SolidBrush(Color.FromArgb(128, Color.Cyan)), objbnd);
                            LevelGfx.DrawRectangle(new Pen(Color.FromArgb(128, Color.Black)) { DashStyle = DashStyle.Dot }, objbnd);
                        }
                        else if (item is S2RingEntry)
                        {
                            S2RingEntry rngitem = (S2RingEntry)item;
                            Rectangle bnd = LevelData.S2RingDef.GetBounds(rngitem, camera);
                            LevelGfx.FillRectangle(new SolidBrush(Color.FromArgb(128, Color.Yellow)), bnd);
                            LevelGfx.DrawRectangle(new Pen(Color.FromArgb(128, Color.Black)) { DashStyle = DashStyle.Dot }, bnd);
                        }
                        else if (item is S3KRingEntry)
                        {
                            S3KRingEntry rngitem = (S3KRingEntry)item;
                            Rectangle bnd = LevelData.S3KRingDef.GetBounds(rngitem, camera);
                            LevelGfx.FillRectangle(new SolidBrush(Color.FromArgb(128, Color.Yellow)), bnd);
                            LevelGfx.DrawRectangle(new Pen(Color.FromArgb(128, Color.Black)) { DashStyle = DashStyle.Dot }, bnd);
                        }
                        else if (item is CNZBumperEntry)
                        {
                            LevelGfx.FillRectangle(new SolidBrush(Color.FromArgb(128, Color.Cyan)), LevelData.unkobj.GetBounds(new S2ObjectEntry() { X = item.X, Y = item.Y }, new Point(camera.X, camera.Y)));
                            LevelGfx.DrawRectangle(new Pen(Color.FromArgb(128, Color.Black)) { DashStyle = DashStyle.Dot }, LevelData.unkobj.GetBounds(new S2ObjectEntry() { X = item.X, Y = item.Y }, new Point(camera.X, camera.Y)));
                        }
                        else if (item is StartPositionEntry)
                        {
                            StartPositionEntry strtitem = (StartPositionEntry)item;
                            Rectangle bnd = LevelData.StartPosDefs[LevelData.StartPositions.IndexOf(strtitem)].GetBounds(strtitem, camera);
                            LevelGfx.FillRectangle(new SolidBrush(Color.FromArgb(128, Color.Red)), bnd);
                            LevelGfx.DrawRectangle(new Pen(Color.FromArgb(128, Color.Black)) { DashStyle = DashStyle.Dot }, bnd);
                        }
                    }
                    if (LevelData.Level.LayoutFormat == EngineVersion.S1)
                        for (int y = Math.Max(camera.Y / LevelData.chunksz, 0); y <= Math.Min(((camera.Y + (panel1.Height - 1) / ZoomLevel)) / LevelData.chunksz, LevelData.FGLayout.GetLength(1) - 1); y++)
                            for (int x = Math.Max(camera.X / LevelData.chunksz, 0); x <= Math.Min(((camera.X + (panel1.Width - 1) / ZoomLevel)) / LevelData.chunksz, LevelData.FGLayout.GetLength(0) - 1); x++)
                                if (LevelData.FGLoop[x, y])
                                    LevelGfx.DrawRectangle(new Pen(Color.FromArgb(128, Color.Yellow)) { Width = 3 }, x * LevelData.chunksz - camera.X, y * LevelData.chunksz - camera.Y, LevelData.chunksz, LevelData.chunksz);
                    if (selecting)
                    {
                        Rectangle selbnds = Rectangle.FromLTRB(
                        Math.Min(selpoint.X, lastmouse.X) - camera.X,
                        Math.Min(selpoint.Y, lastmouse.Y) - camera.Y,
                        Math.Max(selpoint.X, lastmouse.X) - camera.X,
                        Math.Max(selpoint.Y, lastmouse.Y) - camera.Y);
                        LevelGfx.FillRectangle(new SolidBrush(Color.FromArgb(128, Color.White)), selbnds);
                        LevelGfx.DrawRectangle(new Pen(Color.FromArgb(128, Color.Black)) { DashStyle = DashStyle.Dot }, selbnds);
                    }
                    Panel1Gfx.DrawImage(LevelBmp, 0, 0, panel1.Width, panel1.Height);
                    break;
                case 1:
                    pnlcur = panel2.PointToClient(Cursor.Position);
                    camera = new Point(hScrollBar2.Value, vScrollBar2.Value);
                    LevelImg8bpp = LevelData.DrawForeground(new Rectangle(camera.X, camera.Y, (int)(panel2.Width / ZoomLevel), (int)(panel2.Height / ZoomLevel)), true, true, objectsAboveHighPlaneToolStripMenuItem.Checked, lowToolStripMenuItem.Checked, highToolStripMenuItem.Checked, path1ToolStripMenuItem.Checked, path2ToolStripMenuItem.Checked, allToolStripMenuItem.Checked);
                    if (enableGridToolStripMenuItem.Checked)
                        for (int y = Math.Max(camera.Y / LevelData.chunksz, 0); y <= Math.Min(((camera.Y + (panel2.Height - 1) / ZoomLevel)) / LevelData.chunksz, LevelData.FGLayout.GetLength(1) - 1); y++)
                            for (int x = Math.Max(camera.X / LevelData.chunksz, 0); x <= Math.Min(((camera.X + (panel2.Width - 1) / ZoomLevel)) / LevelData.chunksz, LevelData.FGLayout.GetLength(0) - 1); x++)
                            {
                                LevelImg8bpp.DrawLine(67, x * LevelData.chunksz - camera.X, y * LevelData.chunksz - camera.Y, x * LevelData.chunksz - camera.X + LevelData.chunksz - 1, y * LevelData.chunksz - camera.Y);
                                LevelImg8bpp.DrawLine(67, x * LevelData.chunksz - camera.X, y * LevelData.chunksz - camera.Y, x * LevelData.chunksz - camera.X, y * LevelData.chunksz - camera.Y + LevelData.chunksz - 1);
                            }
                    ringcnt = 0;
                    switch (LevelData.Level.RingFormat)
                    {
                        case EngineVersion.S1:
                            foreach (ObjectEntry item in LevelData.Objects)
                                if (item.ID == 0x25)
                                    ringcnt += Math.Min(6, item.SubType & 7) + 1;
                            break;
                        case EngineVersion.S2:
                        case EngineVersion.S2NA:
                            foreach (RingEntry item in LevelData.Rings)
                                ringcnt += ((S2RingEntry)item).Count;
                            break;
                        case EngineVersion.S3K:
                        case EngineVersion.SKC:
                            ringcnt = LevelData.Rings.Count;
                            break;
                    }
                    if (hUDToolStripMenuItem.Checked)
                    {
                        tmpbnd = hudbnd = DrawHUDStr(8, 8, "Screen Pos: ");
                        hudbnd = Rectangle.Union(hudbnd, DrawHUDNum(tmpbnd.Right, tmpbnd.Top, camera.X.ToString("X4") + ' ' + camera.Y.ToString("X4")));
                        tmpbnd = DrawHUDStr(hudbnd.Left, hudbnd.Bottom, "Level Size: ");
                        hudbnd = Rectangle.Union(hudbnd, tmpbnd);
                        hudbnd = Rectangle.Union(hudbnd, DrawHUDNum(tmpbnd.Right, tmpbnd.Top, (LevelData.FGLayout.GetLength(0) * LevelData.chunksz).ToString("X4") + ' ' + (LevelData.FGLayout.GetLength(1) * LevelData.chunksz).ToString("X4")));
                        hudbnd = Rectangle.Union(hudbnd, DrawHUDStr(hudbnd.Left, hudbnd.Bottom,
                            "Objects: " + LevelData.Objects.Count + '\n' +
                            "Rings: " + ringcnt));
                        tmpbnd = DrawHUDStr(hudbnd.Left, hudbnd.Bottom, "Chunk: ");
                        hudbnd = Rectangle.Union(hudbnd, tmpbnd);
                        hudbnd = Rectangle.Union(hudbnd, DrawHUDNum(tmpbnd.Right, tmpbnd.Top, SelectedChunk.ToString("X2")));
                    }
                    LevelBmp = LevelImg8bpp.ToBitmap(LevelImgPalette).To32bpp();
                    LevelGfx = Graphics.FromImage(LevelBmp);
                    LevelGfx.SetOptions();
                    if (LevelData.Level.LayoutFormat == EngineVersion.S1)
                        for (int y = Math.Max(camera.Y / LevelData.chunksz, 0); y <= Math.Min(((camera.Y + (panel2.Height - 1) / ZoomLevel)) / LevelData.chunksz, LevelData.FGLayout.GetLength(1) - 1); y++)
                            for (int x = Math.Max(camera.X / LevelData.chunksz, 0); x <= Math.Min(((camera.X + (panel2.Width - 1) / ZoomLevel)) / LevelData.chunksz, LevelData.FGLayout.GetLength(0) - 1); x++)
                                if (LevelData.FGLoop[x, y])
                                    LevelGfx.DrawRectangle(new Pen(Color.FromArgb(128, Color.Yellow)) { Width = (int)(3 * ZoomLevel) }, x * LevelData.chunksz - camera.X, y * LevelData.chunksz - camera.Y, LevelData.chunksz, LevelData.chunksz);
                    switch (FGMode)
                    {
                        case EditingMode.Draw:
                            LevelGfx.DrawImage(LevelData.CompChunkBmps[SelectedChunk],
                            new Rectangle(((((int)(pnlcur.X / ZoomLevel) + camera.X) / LevelData.chunksz) * LevelData.chunksz) - camera.X, ((((int)(pnlcur.Y / ZoomLevel) + camera.Y) / LevelData.chunksz) * LevelData.chunksz) - camera.Y, LevelData.chunksz, LevelData.chunksz),
                            0, 0, LevelData.chunksz, LevelData.chunksz,
                            GraphicsUnit.Pixel, imageTransparency);
                            break;
                        case EditingMode.Select:
                            if (!FGSelection.IsEmpty)
                            {
                                Rectangle selbnds = FGSelection.Scale(LevelData.chunksz);
                                selbnds.Offset(-camera.X, -camera.Y);
                                LevelGfx.FillRectangle(new SolidBrush(Color.FromArgb(128, Color.White)), selbnds);
                                LevelGfx.DrawRectangle(new Pen(Color.FromArgb(128, Color.Black)) { DashStyle = DashStyle.Dot }, selbnds);
                            }
                            break;
                    }
                    Panel2Gfx.DrawImage(LevelBmp, 0, 0, panel2.Width, panel2.Height);
                    break;
                case 2:
                    pnlcur = panel3.PointToClient(Cursor.Position);
                    camera = new Point(hScrollBar3.Value, vScrollBar3.Value);
                    LevelImg8bpp = LevelData.DrawBackground(new Rectangle(camera.X, camera.Y, (int)(panel3.Width / ZoomLevel), (int)(panel3.Height / ZoomLevel)), lowToolStripMenuItem.Checked, highToolStripMenuItem.Checked, path1ToolStripMenuItem.Checked, path2ToolStripMenuItem.Checked);
                    if (enableGridToolStripMenuItem.Checked)
                        for (int y = Math.Max(camera.Y / LevelData.chunksz, 0); y <= Math.Min(((camera.Y + (panel2.Height - 1) / ZoomLevel)) / LevelData.chunksz, LevelData.FGLayout.GetLength(1) - 1); y++)
                            for (int x = Math.Max(camera.X / LevelData.chunksz, 0); x <= Math.Min(((camera.X + (panel2.Width - 1) / ZoomLevel)) / LevelData.chunksz, LevelData.FGLayout.GetLength(0) - 1); x++)
                            {
                                LevelImg8bpp.DrawLine(67, x * LevelData.chunksz - camera.X, y * LevelData.chunksz - camera.Y, x * LevelData.chunksz - camera.X + LevelData.chunksz - 1, y * LevelData.chunksz - camera.Y);
                                LevelImg8bpp.DrawLine(67, x * LevelData.chunksz - camera.X, y * LevelData.chunksz - camera.Y, x * LevelData.chunksz - camera.X, y * LevelData.chunksz - camera.Y + LevelData.chunksz - 1);
                            }
                    if (hUDToolStripMenuItem.Checked)
                    {
                        tmpbnd = hudbnd = DrawHUDStr(8, 8, "Screen Pos: ");
                        hudbnd = Rectangle.Union(hudbnd, DrawHUDNum(tmpbnd.Right, tmpbnd.Top, camera.X.ToString("X4") + ' ' + camera.Y.ToString("X4")));
                        tmpbnd = DrawHUDStr(hudbnd.Left, hudbnd.Bottom, "Level Size: ");
                        hudbnd = Rectangle.Union(hudbnd, tmpbnd);
                        hudbnd = Rectangle.Union(hudbnd, DrawHUDNum(tmpbnd.Right, tmpbnd.Top, (LevelData.BGLayout.GetLength(0) * LevelData.chunksz).ToString("X4") + ' ' + (LevelData.BGLayout.GetLength(1) * LevelData.chunksz).ToString("X4")));
                        tmpbnd = DrawHUDStr(hudbnd.Left, hudbnd.Bottom, "Chunk: ");
                        hudbnd = Rectangle.Union(hudbnd, tmpbnd);
                        hudbnd = Rectangle.Union(hudbnd, DrawHUDNum(tmpbnd.Right, tmpbnd.Top, SelectedChunk.ToString("X2")));
                    }
                    LevelBmp = LevelImg8bpp.ToBitmap(LevelImgPalette).To32bpp();
                    LevelGfx = Graphics.FromImage(LevelBmp);
                    LevelGfx.SetOptions();
                    if (LevelData.Level.LayoutFormat == EngineVersion.S1)
                        for (int y = Math.Max(camera.Y / LevelData.chunksz, 0); y <= Math.Min(((camera.Y + (panel3.Height - 1) / ZoomLevel)) / LevelData.chunksz, LevelData.BGLayout.GetLength(1) - 1); y++)
                            for (int x = Math.Max(camera.X / LevelData.chunksz, 0); x <= Math.Min(((camera.X + (panel3.Width - 1) / ZoomLevel)) / LevelData.chunksz, LevelData.BGLayout.GetLength(0) - 1); x++)
                                if (LevelData.BGLoop[x, y])
                                    LevelGfx.DrawRectangle(new Pen(Color.FromArgb(128, Color.Yellow)) { Width = (int)(3 * ZoomLevel) }, x * LevelData.chunksz - camera.X, y * LevelData.chunksz - camera.Y, LevelData.chunksz, LevelData.chunksz);
                    switch (BGMode)
                    {
                        case EditingMode.Draw:
                            LevelGfx.DrawImage(LevelData.CompChunkBmps[SelectedChunk],
                            new Rectangle(((((int)(pnlcur.X / ZoomLevel) + camera.X) / LevelData.chunksz) * LevelData.chunksz) - camera.X, ((((int)(pnlcur.Y / ZoomLevel) + camera.Y) / LevelData.chunksz) * LevelData.chunksz) - camera.Y, LevelData.chunksz, LevelData.chunksz),
                            0, 0, LevelData.chunksz, LevelData.chunksz,
                            GraphicsUnit.Pixel, imageTransparency);
                            break;
                        case EditingMode.Select:
                            if (!BGSelection.IsEmpty)
                            {
                                Rectangle selbnds = BGSelection.Scale(LevelData.chunksz);
                                selbnds.Offset(-camera.X, -camera.Y);
                                LevelGfx.FillRectangle(new SolidBrush(Color.FromArgb(128, Color.White)), selbnds);
                                LevelGfx.DrawRectangle(new Pen(Color.FromArgb(128, Color.Black)) { DashStyle = DashStyle.Dot }, selbnds);
                            }
                            break;
                    }
                    Panel3Gfx.DrawImage(LevelBmp, 0, 0, panel3.Width, panel3.Height);
                    break;
            }
        }

        public Rectangle DrawHUDStr(int X, int Y, string str)
        {
            BitmapBits curimg;
            int curX = X;
            int curY = Y;
            Rectangle bounds = new Rectangle();
            bounds.X = X;
            bounds.Y = Y;
            int maxX = X;
            foreach (string line in str.Split(new char[] {'\n'}, StringSplitOptions.None))
            {
                int maxY = 0;
                foreach (char ch in line)
                {
                    curimg = HUDLetters[char.ToUpper(ch)];
                    LevelImg8bpp.DrawBitmapComposited(curimg, new Point(curX, curY));
                    curX += curimg.Width;
                    maxX = Math.Max(maxX, curX);
                    maxY = Math.Max(maxY, curimg.Height);
                }
                curY += maxY;
                curX = X;
            }
            bounds.Width = maxX - X;
            bounds.Height = curY - Y;
            return bounds;
        }

        public Rectangle DrawHUDNum(int X, int Y, string str)
        {
            BitmapBits curimg;
            int curX = X;
            int curY = Y;
            Rectangle bounds = new Rectangle();
            bounds.X = X;
            bounds.Y = Y;
            int maxX = X;
            foreach (string line in str.Split(new char[] { '\n' }, StringSplitOptions.None))
            {
                int maxY = 0;
                foreach (char ch in line)
                {
                    curimg = HUDNumbers[char.ToUpper(ch)];
                    LevelImg8bpp.DrawBitmapComposited(curimg, new Point(curX, curY));
                    curX += curimg.Width;
                    maxX = Math.Max(maxX, curX);
                    maxY = Math.Max(maxY, curimg.Height);
                }
                curY += maxY;
                curX = X;
            }
            bounds.Height = curY - Y;
            return bounds;
        }

        private void panel_Paint(object sender, PaintEventArgs e)
        {
            DrawLevel();
        }

        private void UpdateScrollBars()
        {
            hScrollBar1.Maximum = (int)Math.Max(((LevelData.FGLayout.GetLength(0) + 1) * LevelData.chunksz) - (panel1.Width / ZoomLevel), 0);
            vScrollBar1.Maximum = (int)Math.Max(((LevelData.FGLayout.GetLength(1) + 1) * LevelData.chunksz) - (panel1.Height / ZoomLevel), 0);
            hScrollBar2.Maximum = (int)Math.Max(((LevelData.FGLayout.GetLength(0) + 1) * LevelData.chunksz) - (panel2.Width / ZoomLevel), 0);
            vScrollBar2.Maximum = (int)Math.Max(((LevelData.FGLayout.GetLength(1) + 1) * LevelData.chunksz) - (panel2.Height / ZoomLevel), 0);
            hScrollBar3.Maximum = (int)Math.Max(((LevelData.BGLayout.GetLength(0) + 1) * LevelData.chunksz) - (panel3.Width / ZoomLevel), 0);
            vScrollBar3.Maximum = (int)Math.Max(((LevelData.BGLayout.GetLength(1) + 1) * LevelData.chunksz) - (panel3.Height / ZoomLevel), 0);
        }

        Rectangle prevbnds;
        FormWindowState prevstate;
        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.O:
                    if (e.Control)
                        openToolStripMenuItem_Click(sender, EventArgs.Empty);
                    break;
                case Keys.R:
                    if (!loaded | !(e.Control & e.Alt)) return;
                    if (MessageBox.Show(this, "Do you really want to randomize this level? This action cannot be undone.", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
                    Random rand = new Random();
                    int w = LevelData.FGLayout.GetLength(0);
                    int h = LevelData.FGLayout.GetLength(1);
                    for (int y = 0; y < h; y++)
                        for (int x = 0; x < w; x++)
                            LevelData.FGLayout[x, y] = (byte)rand.Next(LevelData.Chunks.Count);
                    if (LevelData.FGLoop != null) Array.Clear(LevelData.FGLoop, 0, LevelData.FGLoop.Length);
                    for (int y = 0; y < LevelData.BGLayout.GetLength(1); y++)
                        for (int x = 0; x < LevelData.BGLayout.GetLength(0); x++)
                            LevelData.BGLayout[x, y] = (byte)rand.Next(LevelData.Chunks.Count);
                    if (LevelData.BGLoop != null) Array.Clear(LevelData.BGLoop, 0, LevelData.BGLoop.Length);
                    w *= LevelData.chunksz;
                    h *= LevelData.chunksz;
                    LevelData.Objects.Clear();
                    List<KeyValuePair<byte, ObjectDefinition>> objdefs = new List<KeyValuePair<byte, ObjectDefinition>>();
                    foreach (KeyValuePair<byte, ObjectDefinition> item in LevelData.ObjTypes)
                        objdefs.Add(item);
                    int o = rand.Next(256);
                    for (int i = 0; i < o; i++)
                    {
                        byte ID = objdefs[rand.Next(objdefs.Count)].Key;
                        byte sub = LevelData.ObjTypes[ID].DefaultSubtype;
                        System.Collections.ObjectModel.ReadOnlyCollection<byte> subs = LevelData.ObjTypes[ID].Subtypes;
                        if (subs.Count > 0)
                            sub = subs[rand.Next(subs.Count)];
                        switch (LevelData.Level.ObjectFormat)
                        {
                            case EngineVersion.S1:
                                S1ObjectEntry ent1 = (S1ObjectEntry)LevelData.CreateObject(ID);
                                LevelData.Objects.Add(ent1);
                                ent1.SubType = sub;
                                ent1.X = (ushort)(rand.Next(w));
                                ent1.Y = (ushort)(rand.Next(h));
                                ent1.RememberState = LevelData.GetObjectDefinition(ID).RememberState;
                                ent1.UpdateSprite();
                                break;
                            case EngineVersion.S2:
                            case EngineVersion.S2NA:
                                S2ObjectEntry ent = (S2ObjectEntry)LevelData.CreateObject(ID);
                                LevelData.Objects.Add(ent);
                                ent.SubType = sub;
                                ent.X = (ushort)(rand.Next(w));
                                ent.Y = (ushort)(rand.Next(h));
                                ent.RememberState = LevelData.GetObjectDefinition(ID).RememberState;
                                ent.UpdateSprite();
                                break;
                            case EngineVersion.S3K:
                            case EngineVersion.SKC:
                                S3KObjectEntry ent3 = (S3KObjectEntry)LevelData.CreateObject(ID);
                                LevelData.Objects.Add(ent3);
                                ent3.SubType = sub;
                                ent3.X = (ushort)(rand.Next(w));
                                ent3.Y = (ushort)(rand.Next(h));
                                ent3.UpdateSprite();
                                break;
                            case EngineVersion.SCDPC:
                                SCDObjectEntry entcd = (SCDObjectEntry)LevelData.CreateObject(ID);
                                LevelData.Objects.Add(entcd);
                                entcd.SubType = sub;
                                entcd.X = (ushort)(rand.Next(w));
                                entcd.Y = (ushort)(rand.Next(h));
                                entcd.RememberState = LevelData.GetObjectDefinition(ID).RememberState;
                                entcd.UpdateSprite();
                                break;
                        }
                    }
                    LevelData.Objects.Sort();
                    LevelData.Rings.Clear();
                    o = rand.Next(256);
                    for (int i = 0; i < o; i++)
                    {
                        switch (LevelData.Level.RingFormat)
                        {
                            case EngineVersion.S2:
                            case EngineVersion.S2NA:
                                S2RingEntry ent = new S2RingEntry();
                                ent.X = (ushort)(rand.Next(w));
                                ent.Y = (ushort)(rand.Next(h));
                                ent.Count = (byte)rand.Next(1, 9);
                                ent.Direction = (Direction)rand.Next(2);
                                ent.UpdateSprite();
                                LevelData.Rings.Add(ent);
                                break;
                            case EngineVersion.S3K:
                            case EngineVersion.SKC:
                                S3KRingEntry ent3 = new S3KRingEntry();
                                ent3.X = (ushort)(rand.Next(w));
                                ent3.Y = (ushort)(rand.Next(h));
                                ent3.UpdateSprite();
                                LevelData.Rings.Add(ent3);
                                break;
                        }
                    }
                    LevelData.Rings.Sort();
                    DrawLevel();
                    break;
                case Keys.S:
                    if (!loaded) return;
                    if (e.Control)
                        saveToolStripMenuItem_Click(sender, EventArgs.Empty);
                    break;
                case Keys.Y:
                    if (!loaded) return;
                    if (e.Control && RedoList.Count > 0) DoRedo(1);
                    break;
                case Keys.Z:
                    if (!loaded) return;
                    if (e.Control && UndoList.Count > 0)
                        DoUndo(1);
                    break;
                case Keys.Enter:
                    if (e.Alt)
                    {
                        if (!TopMost)
                        {
                            prevbnds = Bounds;
                            prevstate = WindowState;
                            TopMost = true;
                            WindowState = FormWindowState.Normal;
                            FormBorderStyle = FormBorderStyle.None;
                            Bounds = Screen.FromControl(this).Bounds;
                        }
                        else
                        {
                            TopMost = false;
                            WindowState = prevstate;
                            FormBorderStyle = FormBorderStyle.Sizable;
                            Bounds = prevbnds;
                        }
                    }
                    break;
                case Keys.F5:
                    menuStrip1.ShowHide();
                    break;
            }
        }

        private void panel1_KeyDown(object sender, KeyEventArgs e)
        {
            long step = e.Shift ? LevelData.chunksz : 16;
            step = e.Control ? int.MaxValue : step;
            switch (e.KeyCode)
            {
                case Keys.Up:
                    if (!loaded) return;
                    vScrollBar1.Value = (int)Math.Max(vScrollBar1.Value - step, vScrollBar1.Minimum);
                    break;
                case Keys.Down:
                    if (!loaded) return;
                    vScrollBar1.Value = (int)Math.Min(vScrollBar1.Value + step, vScrollBar1.Maximum - LevelData.chunksz + 1);
                    break;
                case Keys.Left:
                    if (!loaded) return;
                    hScrollBar1.Value = (int)Math.Max(hScrollBar1.Value - step, hScrollBar1.Minimum);
                    break;
                case Keys.Right:
                    if (!loaded) return;
                    hScrollBar1.Value = (int)Math.Min(hScrollBar1.Value + step, hScrollBar1.Maximum - LevelData.chunksz + 1);
                    break;
                case Keys.Delete:
                    if (!loaded) return;
                    if (SelectedItems.Count > 0)
                        deleteToolStripMenuItem_Click(sender, EventArgs.Empty);
                    break;
                case Keys.A:
                    if (!loaded) return;
                    for (int i = 0; i < SelectedItems.Count; i++)
                    {
                        if (SelectedItems[i] is S1ObjectEntry)
                        {
                            S1ObjectEntry oi = SelectedItems[i] as S1ObjectEntry;
                            oi.ID = (byte)(oi.ID == 0 ? 0x7F : oi.ID - 1);
                            SelectedItems[i].UpdateSprite();
                        }
                        else if (SelectedItems[i] is SCDObjectEntry)
                        {
                            SCDObjectEntry oi = SelectedItems[i] as SCDObjectEntry;
                            oi.ID = (byte)(oi.ID == 0 ? 0x7F : oi.ID - 1);
                            SelectedItems[i].UpdateSprite();
                        }
                        else if (SelectedItems[i] is ObjectEntry)
                        {
                            ObjectEntry oi = SelectedItems[i] as ObjectEntry;
                            oi.ID = (byte)(oi.ID == 0 ? 255 : oi.ID - 1);
                            SelectedItems[i].UpdateSprite();
                        }
                        else if (SelectedItems[i] is S2RingEntry)
                        {
                            S2RingEntry ri = SelectedItems[i] as S2RingEntry;
                            if (ri.Count == 1)
                            {
                                ri.Direction = (ri.Direction == Direction.Vertical ? Direction.Horizontal : Direction.Vertical);
                                ri.Count = 8;
                            }
                            else
                            {
                                ri.Count -= 1;
                            }
                            ri.UpdateSprite();
                        }
                        else if (SelectedItems[i] is CNZBumperEntry)
                        {
                            CNZBumperEntry ci = SelectedItems[i] as CNZBumperEntry;
                            ci.ID = (ushort)(ci.ID == 0 ? 65535 : ci.ID - 1);
                            ci.UpdateSprite();
                        }
                    }
                    DrawLevel();
                    break;
                case Keys.C:
                    if (!loaded) return;
                    if (e.Control)
                        if (SelectedItems.Count > 0)
                            copyToolStripMenuItem_Click(sender, EventArgs.Empty);
                    break;
                case Keys.S:
                    if (!loaded) return;
                    if (!e.Control)
                    {
                        foreach (Entry item in SelectedItems)
                        {
                            if (item is ObjectEntry)
                            {
                                ObjectEntry oi = item as ObjectEntry;
                                oi.SubType = (byte)(oi.SubType == 0 ? 255 : oi.SubType - 1);
                                oi.UpdateSprite();
                            }
                        }
                        DrawLevel();
                    }
                    break;
                case Keys.V:
                    if (!loaded) return;
                    if (e.Control)
                    {
                        menuLoc = new Point(panel1.Width / 2, panel1.Height / 2);
                        pasteToolStripMenuItem_Click(sender, EventArgs.Empty);
                    }
                    break;
                case Keys.X:
                    if (!loaded) return;
                    if (e.Control)
                    {
                        if (SelectedItems.Count > 0)
                            cutToolStripMenuItem_Click(sender, EventArgs.Empty);
                    }
                    else
                    {
                        foreach (Entry item in SelectedItems)
                        {
                            if (item is ObjectEntry)
                            {
                                ObjectEntry oi = item as ObjectEntry;
                                oi.SubType = (byte)(oi.SubType == 255 ? 0 : oi.SubType + 1);
                                oi.UpdateSprite();
                            }
                        }
                        DrawLevel();
                    }
                    break;
                case Keys.Z:
                    if (!loaded) return;
                    if (!e.Control)
                    {
                        for (int i = 0; i < SelectedItems.Count; i++)
                        {
                            if (SelectedItems[i] is S1ObjectEntry)
                            {
                                S1ObjectEntry oi = SelectedItems[i] as S1ObjectEntry;
                                oi.ID = (byte)(oi.ID == 0x7F ? 0 : oi.ID + 1);
                                SelectedItems[i].UpdateSprite();
                            }
                            else if (SelectedItems[i] is SCDObjectEntry)
                            {
                                SCDObjectEntry oi = SelectedItems[i] as SCDObjectEntry;
                                oi.ID = (byte)(oi.ID == 0x7F ? 0 : oi.ID + 1);
                                SelectedItems[i].UpdateSprite();
                            }
                            else if (SelectedItems[i] is ObjectEntry)
                            {
                                ObjectEntry oi = SelectedItems[i] as ObjectEntry;
                                oi.ID = (byte)(oi.ID == 255 ? 0 : oi.ID + 1);
                                SelectedItems[i].UpdateSprite();
                            }
                            else if (SelectedItems[i] is S2RingEntry)
                            {
                                S2RingEntry ri = SelectedItems[i] as S2RingEntry;
                                if (ri.Count == 8)
                                {
                                    ri.Direction = (ri.Direction == Direction.Vertical ? Direction.Horizontal : Direction.Vertical);
                                    ri.Count = 1;
                                }
                                else
                                {
                                    ri.Count += 1;
                                }
                                ri.UpdateSprite();
                            }
                            else if (SelectedItems[i] is CNZBumperEntry)
                            {
                                CNZBumperEntry ci = SelectedItems[i] as CNZBumperEntry;
                                ci.ID = (ushort)(ci.ID == 0 ? 65535 : ci.ID - 1);
                                ci.UpdateSprite();
                            }
                        }
                        DrawLevel();
                    }
                    break;
            }
            panel_KeyDown(sender, e);
        }

        private void panel2_KeyDown(object sender, KeyEventArgs e)
        {
            long step = e.Shift ? LevelData.chunksz : 16;
            step = e.Control ? int.MaxValue : step;
            switch (e.KeyCode)
            {
                case Keys.Up:
                    if (!loaded) return;
                    vScrollBar2.Value = (int)Math.Max(vScrollBar2.Value - step, vScrollBar2.Minimum);
                    break;
                case Keys.Down:
                    if (!loaded) return;
                    vScrollBar2.Value = (int)Math.Min(vScrollBar2.Value + step, vScrollBar2.Maximum - LevelData.chunksz + 1);
                    break;
                case Keys.Left:
                    if (!loaded) return;
                    hScrollBar2.Value = (int)Math.Max(hScrollBar2.Value - step, hScrollBar2.Minimum);
                    break;
                case Keys.Right:
                    if (!loaded) return;
                    hScrollBar2.Value = (int)Math.Min(hScrollBar2.Value + step, hScrollBar2.Maximum - LevelData.chunksz + 1);
                    break;
                case Keys.A:
                    if (!loaded) return;
                    SelectedChunk = (byte)(SelectedChunk == 0 ? LevelData.Chunks.Count - 1 : SelectedChunk - 1);
                    DrawLevel();
                    break;
                case Keys.Z:
                    if (!loaded) return;
                    if (!e.Control)
                    {
                        SelectedChunk = (byte)(SelectedChunk == LevelData.Chunks.Count - 1 ? 0 : SelectedChunk + 1);
                        DrawLevel();
                    }
                    break;
            }
            panel_KeyDown(sender, e);
        }

        private void panel3_KeyDown(object sender, KeyEventArgs e)
        {
            long step = e.Shift ? LevelData.chunksz : 16;
            step = e.Control ? int.MaxValue : step;
            switch (e.KeyCode)
            {
                case Keys.Up:
                    if (!loaded) return;
                    vScrollBar3.Value = (int)Math.Max(vScrollBar3.Value - step, vScrollBar3.Minimum);
                    break;
                case Keys.Down:
                    if (!loaded) return;
                    vScrollBar3.Value = (int)Math.Min(vScrollBar3.Value + step, vScrollBar3.Maximum - LevelData.chunksz + 1);
                    break;
                case Keys.Left:
                    if (!loaded) return;
                    hScrollBar3.Value = (int)Math.Max(hScrollBar3.Value - step, hScrollBar3.Minimum);
                    break;
                case Keys.Right:
                    if (!loaded) return;
                    hScrollBar3.Value = (int)Math.Min(hScrollBar3.Value + step, hScrollBar3.Maximum - LevelData.chunksz + 1);
                    break;
                case Keys.A:
                    if (!loaded) return;
                    SelectedChunk = (byte)(SelectedChunk == 0 ? LevelData.Chunks.Count - 1 : SelectedChunk - 1);
                    DrawLevel();
                    break;
                case Keys.Z:
                    if (!loaded) return;
                    if (!e.Control)
                    {
                        SelectedChunk = (byte)(SelectedChunk == LevelData.Chunks.Count - 1 ? 0 : SelectedChunk + 1);
                        DrawLevel();
                    }
                    break;
            }
            panel_KeyDown(sender, e);
        }

        private void panel_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Q:
                    bool angles = anglesToolStripMenuItem.Checked;
                    foreach (ToolStripItem item in collisionToolStripMenuItem.DropDownItems)
                        if (item is ToolStripMenuItem)
                            ((ToolStripMenuItem)item).Checked = false;
                    noneToolStripMenuItem1.Checked = true;
                    anglesToolStripMenuItem.Checked = angles;
                    DrawLevel();
                    break;
                case Keys.W:
                    angles = anglesToolStripMenuItem.Checked;
                    foreach (ToolStripItem item in collisionToolStripMenuItem.DropDownItems)
                        if (item is ToolStripMenuItem)
                            ((ToolStripMenuItem)item).Checked = false;
                    path1ToolStripMenuItem.Checked = true;
                    anglesToolStripMenuItem.Checked = angles;
                    DrawLevel();
                    break;
                case Keys.E:
                    switch (LevelData.Level.ChunkFormat)
                    {
                        case EngineVersion.S1:
                        case EngineVersion.SCD:
                        case EngineVersion.SCDPC:
                            break;
                        case EngineVersion.S2:
                        case EngineVersion.S2NA:
                        case EngineVersion.S3K:
                        case EngineVersion.SKC:
                            angles = anglesToolStripMenuItem.Checked;
                            foreach (ToolStripItem item in collisionToolStripMenuItem.DropDownItems)
                                if (item is ToolStripMenuItem)
                                    ((ToolStripMenuItem)item).Checked = false;
                            path2ToolStripMenuItem.Checked = true;
                            anglesToolStripMenuItem.Checked = angles;
                            DrawLevel();
                            break;
                    }
                    break;
                case Keys.R:
                    if (!(e.Alt & e.Control))
                    {
                        anglesToolStripMenuItem.Checked = !anglesToolStripMenuItem.Checked;
                        DrawLevel();
                    }
                    break;
                case Keys.T:
                    objectsAboveHighPlaneToolStripMenuItem.Checked = !objectsAboveHighPlaneToolStripMenuItem.Checked;
                    DrawLevel();
                    break;
                case Keys.Y:
                    if (!e.Control)
                    {
                        lowToolStripMenuItem.Checked = !lowToolStripMenuItem.Checked;
                        DrawLevel();
                    }
                    break;
                case Keys.U:
                    highToolStripMenuItem.Checked = !highToolStripMenuItem.Checked;
                    DrawLevel();
                    break;
                case Keys.I:
                    enableGridToolStripMenuItem.Checked = !enableGridToolStripMenuItem.Checked;
                    DrawLevel();
                    break;
                case Keys.O:
                    if (!e.Control)
                    {
                        hUDToolStripMenuItem.Checked = !hUDToolStripMenuItem.Checked;
                        DrawLevel();
                    }
                    break;
                case Keys.P:
                    switch (LevelData.Game.EngineVersion)
                    {
                        case EngineVersion.SCD:
                        case EngineVersion.SCDPC:
                            currentOnlyToolStripMenuItem.Checked = !currentOnlyToolStripMenuItem.Checked;
                            allToolStripMenuItem.Checked = !allToolStripMenuItem.Checked;
                            DrawLevel();
                            break;
                    }
                    break;
                case Keys.OemOpenBrackets:
                    LevelData.CurPal--;
                    if (LevelData.CurPal == -1)
                        LevelData.CurPal = LevelData.Palette.Count - 1;
                    LevelData.PaletteChanged();
                    DrawLevel();
                    break;
                case Keys.OemCloseBrackets:
                    LevelData.CurPal++;
                    if (LevelData.CurPal == LevelData.Palette.Count)
                        LevelData.CurPal = 0;
                    LevelData.PaletteChanged();
                    DrawLevel();
                    break;
                case Keys.OemMinus:
                case Keys.Subtract:
                    for (int i = 1; i < zoomToolStripMenuItem.DropDownItems.Count; i++)
                        if (((ToolStripMenuItem)zoomToolStripMenuItem.DropDownItems[i]).Checked)
                        {
                            zoomToolStripMenuItem_DropDownItemClicked(sender, new ToolStripItemClickedEventArgs(zoomToolStripMenuItem.DropDownItems[i - 1]));
                            break;
                        }
                    break;
                case Keys.Oemplus:
                case Keys.Add:
                    for (int i = 0; i < zoomToolStripMenuItem.DropDownItems.Count - 1; i++)
                        if (((ToolStripMenuItem)zoomToolStripMenuItem.DropDownItems[i]).Checked)
                        {
                            zoomToolStripMenuItem_DropDownItemClicked(sender, new ToolStripItemClickedEventArgs(zoomToolStripMenuItem.DropDownItems[i + 1]));
                            break;
                        }
                    break;
            }
        }

        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            if (!loaded) return;
            int curx = (int)(e.X / ZoomLevel) + hScrollBar1.Value;
            int cury = (int)(e.Y / ZoomLevel) + vScrollBar1.Value;
            switch (e.Button)
            {
                case MouseButtons.Left:
                    if (e.Clicks == 2)
                    {
                        if (ModifierKeys != Keys.Shift)
                        {
                            if (ModifierKeys != Keys.Control || LevelData.Bumpers == null)
                            {
                                if (ObjectSelect.ShowDialog(this) == DialogResult.OK)
                                {
                                    byte ID = (byte)ObjectSelect.numericUpDown1.Value;
                                    byte sub = (byte)ObjectSelect.numericUpDown2.Value;
                                    switch (LevelData.Level.ObjectFormat)
                                    {
                                        case EngineVersion.S2:
                                        case EngineVersion.S2NA:
                                            S2ObjectEntry ent = (S2ObjectEntry)LevelData.CreateObject(ID);
                                            LevelData.Objects.Add(ent);
                                            ent.SubType = sub;
                                            ent.X = (ushort)(curx);
                                            ent.Y = (ushort)(e.Y /  ZoomLevel + vScrollBar1.Value);
                                            ent.RememberState = LevelData.GetObjectDefinition(ID).RememberState;
                                            ent.UpdateSprite();
                                            SelectedItems.Clear();
                                            SelectedItems.Add(ent);
                                            SelectedObjectChanged();
                                            AddUndo(new ObjectAddedUndoAction(ent));
                                            break;
                                        case EngineVersion.S1:
                                            S1ObjectEntry ent1 = (S1ObjectEntry)LevelData.CreateObject(ID);
                                            LevelData.Objects.Add(ent1);
                                            ent1.SubType = sub;
                                            ent1.X = (ushort)(curx);
                                            ent1.Y = (ushort)(cury);
                                            ent1.RememberState = LevelData.GetObjectDefinition(ID).RememberState;
                                            ent1.UpdateSprite();
                                            SelectedItems.Clear();
                                            SelectedItems.Add(ent1);
                                            SelectedObjectChanged();
                                            AddUndo(new ObjectAddedUndoAction(ent1));
                                            break;
                                        case EngineVersion.S3K:
                                        case EngineVersion.SKC:
                                            S3KObjectEntry ent3 = (S3KObjectEntry)LevelData.CreateObject(ID);
                                            LevelData.Objects.Add(ent3);
                                            ent3.SubType = sub;
                                            ent3.X = (ushort)(curx);
                                            ent3.Y = (ushort)(cury);
                                            ent3.UpdateSprite();
                                            SelectedItems.Clear();
                                            SelectedItems.Add(ent3);
                                            SelectedObjectChanged();
                                            AddUndo(new ObjectAddedUndoAction(ent3));
                                            break;
                                        case EngineVersion.SCD:
                                        case EngineVersion.SCDPC:
                                            SCDObjectEntry entcd = (SCDObjectEntry)LevelData.CreateObject(ID);
                                            LevelData.Objects.Add(entcd);
                                            entcd.SubType = sub;
                                            entcd.X = (ushort)(curx);
                                            entcd.Y = (ushort)(cury);
                                            entcd.RememberState = LevelData.GetObjectDefinition(ID).RememberState;
                                            switch (LevelData.Level.TimeZone)
                                            {
                                                case API.TimeZone.Past:
                                                    entcd.ShowPast = true;
                                                    break;
                                                case API.TimeZone.Present:
                                                    entcd.ShowPresent = true;
                                                    break;
                                                case API.TimeZone.Future:
                                                    entcd.ShowFuture = true;
                                                    break;
                                            }
                                            entcd.UpdateSprite();
                                            SelectedItems.Clear();
                                            SelectedItems.Add(entcd);
                                            SelectedObjectChanged();
                                            AddUndo(new ObjectAddedUndoAction(entcd));
                                            break;
                                    }
                                    LevelData.Objects.Sort();
                                    DrawLevel();
                                }
                            }
                            else
                            {
                                LevelData.Bumpers.Add(new CNZBumperEntry() { X = (ushort)(curx), Y = (ushort)(cury) });
                                LevelData.Bumpers[LevelData.Bumpers.Count - 1].UpdateSprite();
                                SelectedItems.Clear();
                                SelectedItems.Add(LevelData.Bumpers[LevelData.Bumpers.Count - 1]);
                                SelectedObjectChanged();
                                AddUndo(new ObjectAddedUndoAction(LevelData.Bumpers[LevelData.Bumpers.Count - 1]));
                                LevelData.Bumpers.Sort();
                                DrawLevel();
                            }
                        }
                        else if (LevelData.Level.RingFormat == EngineVersion.S2 | LevelData.Level.RingFormat == EngineVersion.S2NA)
                        {
                            LevelData.Rings.Add(new S2RingEntry() { X = (ushort)(curx), Y = (ushort)(cury) });
                            LevelData.Rings[LevelData.Rings.Count - 1].UpdateSprite();
                            SelectedItems.Clear();
                            SelectedItems.Add(LevelData.Rings[LevelData.Rings.Count - 1]);
                            SelectedObjectChanged();
                            AddUndo(new ObjectAddedUndoAction(LevelData.Rings[LevelData.Rings.Count - 1]));
                            LevelData.Rings.Sort();
                            DrawLevel();
                        }
                        else if (LevelData.Level.RingFormat == EngineVersion.S3K | LevelData.Level.RingFormat == EngineVersion.SKC)
                        {
                            LevelData.Rings.Add(new S3KRingEntry() { X = (ushort)(curx), Y = (ushort)(cury) });
                            LevelData.Rings[LevelData.Rings.Count - 1].UpdateSprite();
                            SelectedItems.Clear();
                            SelectedItems.Add(LevelData.Rings[LevelData.Rings.Count - 1]);
                            SelectedObjectChanged();
                            AddUndo(new ObjectAddedUndoAction(LevelData.Rings[LevelData.Rings.Count - 1]));
                            LevelData.Rings.Sort();
                            DrawLevel();
                        }
                    }
                    foreach (ObjectEntry item in LevelData.Objects)
                    {
                        ObjectDefinition dat = LevelData.GetObjectDefinition(item.ID);
                        Rectangle bound = dat.GetBounds(item, Point.Empty);
                        if (LevelData.ObjectVisible(item, allToolStripMenuItem.Checked) && bound.Contains(curx, cury))
                        {
                            if (ModifierKeys == Keys.Control)
                            {
                                if (SelectedItems.Contains(item))
                                    SelectedItems.Remove(item);
                                else
                                    SelectedItems.Add(item);
                            }
                            else if (!SelectedItems.Contains(item))
                            {
                                SelectedItems.Clear();
                                SelectedItems.Add(item);
                            }
                            SelectedObjectChanged();
                            objdrag = true;
                            DrawLevel();
                            break;
                        }
                    }
                    if (!objdrag)
                        foreach (RingEntry ritem in LevelData.Rings)
                        {
                            if (ritem is S2RingEntry)
                            {
                                S2RingEntry item = ritem as S2RingEntry;
                                Rectangle bound = LevelData.S2RingDef.GetBounds(item, Point.Empty);
                                if (bound.Contains(curx, cury))
                                {
                                    if (ModifierKeys == Keys.Control)
                                    {
                                        if (SelectedItems.Contains(item))
                                            SelectedItems.Remove(item);
                                        else
                                            SelectedItems.Add(item);
                                    }
                                    else if (!SelectedItems.Contains(item))
                                    {
                                        SelectedItems.Clear();
                                        SelectedItems.Add(item);
                                    }
                                    SelectedObjectChanged();
                                    objdrag = true;
                                    DrawLevel();
                                    break;
                                }
                            }
                            else if (ritem is S3KRingEntry)
                            {
                                S3KRingEntry item = ritem as S3KRingEntry;
                                Rectangle bound = LevelData.S3KRingDef.GetBounds(item, Point.Empty);
                                if (bound.Contains(curx, cury))
                                {
                                    if (ModifierKeys == Keys.Control)
                                    {
                                        if (SelectedItems.Contains(item))
                                            SelectedItems.Remove(item);
                                        else
                                            SelectedItems.Add(item);
                                    }
                                    else if (!SelectedItems.Contains(item))
                                    {
                                        SelectedItems.Clear();
                                        SelectedItems.Add(item);
                                    }
                                    SelectedObjectChanged();
                                    objdrag = true;
                                    DrawLevel();
                                    break;
                                }
                            }
                        }
                    if (!objdrag && LevelData.Bumpers != null)
                        foreach (CNZBumperEntry item in LevelData.Bumpers)
                        {
                            Rectangle bound = LevelData.unkobj.GetBounds(new S2ObjectEntry() { X = item.X, Y = item.Y }, Point.Empty);
                            if (bound.Contains(curx, cury))
                            {
                                if (ModifierKeys == Keys.Control)
                                {
                                    if (SelectedItems.Contains(item))
                                        SelectedItems.Remove(item);
                                    else
                                        SelectedItems.Add(item);
                                }
                                if (!SelectedItems.Contains(item))
                                {
                                    SelectedItems.Clear();
                                    SelectedItems.Add(item);
                                }
                                SelectedObjectChanged();
                                objdrag = true;
                                DrawLevel();
                                break;
                            }
                        }
                    if (!objdrag)
                        foreach (StartPositionEntry item in LevelData.StartPositions)
                        {
                            Rectangle bound = LevelData.StartPosDefs[LevelData.StartPositions.IndexOf(item)].GetBounds(item, Point.Empty);
                            if (bound.Contains(curx, cury))
                            {
                                if (ModifierKeys == Keys.Control)
                                {
                                    if (SelectedItems.Contains(item))
                                        SelectedItems.Remove(item);
                                    else
                                        SelectedItems.Add(item);
                                }
                                if (!SelectedItems.Contains(item))
                                {
                                    SelectedItems.Clear();
                                    SelectedItems.Add(item);
                                }
                                SelectedObjectChanged();
                                objdrag = true;
                                DrawLevel();
                                break;
                            }
                        }
                    if (!objdrag)
                    {
                        selecting = true;
                        selpoint = new Point(curx, cury);
                        SelectedItems.Clear();
                        SelectedObjectChanged();
                    }
                    else
                    {
                        locs = new List<Point>();
                        foreach (Entry item in SelectedItems)
                            locs.Add(new Point(item.X, item.Y));
                    }
                    break;
                case MouseButtons.Right:
                    menuLoc = e.Location;
                    foreach (ObjectEntry item in LevelData.Objects)
                    {
                        ObjectDefinition dat = LevelData.GetObjectDefinition(item.ID);
                        Rectangle bound = dat.GetBounds(item, Point.Empty);
                        if (LevelData.ObjectVisible(item, allToolStripMenuItem.Checked) && bound.Contains(curx, cury))
                        {
                            if (!SelectedItems.Contains(item))
                            {
                                SelectedItems.Clear();
                                SelectedItems.Add(item);
                            }
                            SelectedObjectChanged();
                            objdrag = true;
                            DrawLevel();
                            break;
                        }
                    }
                    if (!objdrag)
                        foreach (RingEntry ritem in LevelData.Rings)
                        {
                            if (ritem is S2RingEntry)
                            {
                                S2RingEntry item = ritem as S2RingEntry;
                                Rectangle bound = LevelData.S2RingDef.GetBounds(item, Point.Empty);
                                if (bound.Contains(curx, cury))
                                {
                                    if (!SelectedItems.Contains(item))
                                    {
                                        SelectedItems.Clear();
                                        SelectedItems.Add(item);
                                    }
                                    SelectedObjectChanged();
                                    objdrag = true;
                                    DrawLevel();
                                    break;
                                }
                            }
                            else if (ritem is S3KRingEntry)
                            {
                                S3KRingEntry item = ritem as S3KRingEntry;
                                Rectangle bound = LevelData.S3KRingDef.GetBounds(item, Point.Empty);
                                if (bound.Contains(curx, cury))
                                {
                                    if (!SelectedItems.Contains(item))
                                    {
                                        SelectedItems.Clear();
                                        SelectedItems.Add(item);
                                    }
                                    SelectedObjectChanged();
                                    objdrag = true;
                                    DrawLevel();
                                    break;
                                }
                            }
                        }
                    if (!objdrag && LevelData.Bumpers != null)
                        foreach (CNZBumperEntry item in LevelData.Bumpers)
                        {
                            Rectangle bound = LevelData.unkobj.GetBounds(new S2ObjectEntry() { X = item.X, Y = item.Y }, Point.Empty);
                            if (bound.Contains(curx, cury))
                            {
                                if (!SelectedItems.Contains(item))
                                {
                                    SelectedItems.Clear();
                                    SelectedItems.Add(item);
                                }
                                SelectedObjectChanged();
                                objdrag = true;
                                DrawLevel();
                                break;
                            }
                        }
                    if (!objdrag)
                        foreach (StartPositionEntry item in LevelData.StartPositions)
                        {
                            Rectangle bound = LevelData.StartPosDefs[LevelData.StartPositions.IndexOf(item)].GetBounds(item, Point.Empty);
                            if (bound.Contains(curx, cury))
                            {
                                if (!SelectedItems.Contains(item))
                                {
                                    SelectedItems.Clear();
                                    SelectedItems.Add(item);
                                }
                                SelectedObjectChanged();
                                objdrag = true;
                                DrawLevel();
                                break;
                            }
                        }
                    objdrag = false;
                    if (SelectedItems.Count > 0)
                    {
                        cutToolStripMenuItem.Enabled = true;
                        copyToolStripMenuItem.Enabled = true;
                        deleteToolStripMenuItem.Enabled = true;
                    }
                    else
                    {
                        cutToolStripMenuItem.Enabled = false;
                        copyToolStripMenuItem.Enabled = false;
                        deleteToolStripMenuItem.Enabled = false;
                    }
                    pasteToolStripMenuItem.Enabled = Clipboard.ContainsData(typeof(List<Entry>).AssemblyQualifiedName);
                    objectContextMenuStrip.Show(panel1, menuLoc);
                    break;
            }
        }

        private void panel1_MouseMove(object sender, MouseEventArgs e)
        {
            if (!loaded) return;
            Rectangle bnd = panel1.Bounds;
            bnd.Offset(-panel1.Location.X, -panel1.Location.Y);
            if (!bnd.Contains(panel1.PointToClient(Cursor.Position))) return;
            Point mouse = new Point((int)(e.X / ZoomLevel) + hScrollBar1.Value, (int)(e.Y / ZoomLevel) + vScrollBar1.Value);
            bool redraw = false;
            switch (e.Button)
            {
                case MouseButtons.Left:
                    if (objdrag)
                    {
                        foreach (Entry item in SelectedItems)
                        {
                            item.X = (ushort)(item.X + (mouse.X - lastmouse.X));
                            item.Y = (ushort)(item.Y + (mouse.Y - lastmouse.Y));
                            item.UpdateSprite();
                        }
                        redraw = true;
                    }
                    else if (selecting)
                    {
                        int selobjs = SelectedItems.Count;
                        SelectedItems.Clear();
                        Rectangle selbnds = Rectangle.FromLTRB(
                        Math.Min(selpoint.X, mouse.X),
                        Math.Min(selpoint.Y, mouse.Y),
                        Math.Max(selpoint.X, mouse.X),
                        Math.Max(selpoint.Y, mouse.Y));
                        foreach (ObjectEntry item in LevelData.Objects)
                        {
                            ObjectDefinition dat = LevelData.GetObjectDefinition(item.ID);
                            Rectangle bound = dat.GetBounds(item, Point.Empty);
                            if (LevelData.ObjectVisible(item, allToolStripMenuItem.Checked) && bound.IntersectsWith(selbnds))
                                SelectedItems.Add(item);
                        }
                        foreach (RingEntry ritem in LevelData.Rings)
                        {
                            if (ritem is S2RingEntry)
                            {
                                S2RingEntry item = ritem as S2RingEntry;
                                Rectangle bound = LevelData.S2RingDef.GetBounds(item, Point.Empty);
                                if (bound.IntersectsWith(selbnds))
                                    SelectedItems.Add(item);
                            }
                            else if (ritem is S3KRingEntry)
                            {
                                S3KRingEntry item = ritem as S3KRingEntry;
                                Rectangle bound = LevelData.S3KRingDef.GetBounds(item, Point.Empty);
                                if (bound.IntersectsWith(selbnds))
                                    SelectedItems.Add(item);
                            }
                        }
                        if (LevelData.Bumpers != null)
                            foreach (CNZBumperEntry item in LevelData.Bumpers)
                            {
                                Rectangle bound = LevelData.unkobj.GetBounds(new S2ObjectEntry() { X = item.X, Y = item.Y }, Point.Empty);
                                if (bound.IntersectsWith(selbnds))
                                    SelectedItems.Add(item);
                            }
                        foreach (StartPositionEntry item in LevelData.StartPositions)
                        {
                            Rectangle bound = LevelData.StartPosDefs[LevelData.StartPositions.IndexOf(item)].GetBounds(item, Point.Empty);
                            if (bound.IntersectsWith(selbnds))
                                SelectedItems.Add(item);
                        }
                        if (selobjs != SelectedItems.Count) SelectedObjectChanged();
                        redraw = true;
                    }
                    break;
            }
            Cursor cur = Cursors.Default;
            foreach (ObjectEntry item in LevelData.Objects)
            {
                ObjectDefinition dat = LevelData.GetObjectDefinition(item.ID);
                Rectangle bound = dat.GetBounds(item, Point.Empty);
                if (LevelData.ObjectVisible(item, allToolStripMenuItem.Checked) && bound.Contains(mouse))
                {
                    cur = Cursors.SizeAll;
                    break;
                }
            }
            foreach (RingEntry item in LevelData.Rings)
            {
                switch (LevelData.Level.RingFormat)
                {
                    case EngineVersion.S2:
                    case EngineVersion.S2NA:
                        Rectangle bound = LevelData.S2RingDef.GetBounds((S2RingEntry)item, Point.Empty);
                        if (bound.Contains(mouse))
                        {
                            cur = Cursors.SizeAll;
                            break;
                        }
                        break;
                    case EngineVersion.S3K:
                    case EngineVersion.SKC:
                        bound = LevelData.S3KRingDef.GetBounds((S3KRingEntry)item, Point.Empty);
                        if (bound.Contains(mouse))
                        {
                            cur = Cursors.SizeAll;
                            break;
                        }
                        break;
                }
            }
            if (LevelData.Bumpers != null)
                foreach (CNZBumperEntry item in LevelData.Bumpers)
                {
                    Rectangle bound = LevelData.unkobj.GetBounds(new S2ObjectEntry() { X = item.X, Y = item.Y }, Point.Empty);
                    if (bound.Contains(mouse))
                    {
                        cur = Cursors.SizeAll;
                        break;
                    }
                }
            foreach (StartPositionEntry item in LevelData.StartPositions)
            {
                Rectangle bound = LevelData.StartPosDefs[LevelData.StartPositions.IndexOf(item)].GetBounds(item, Point.Empty);
                if (bound.Contains(mouse))
                {
                    cur = Cursors.SizeAll;
                    break;
                }
            }
            panel1.Cursor = cur;
            if (redraw) DrawLevel();
            lastmouse = mouse;
        }

        private void panel1_MouseUp(object sender, MouseEventArgs e)
        {
            if (objdrag)
            {
                if (ModifierKeys == Keys.Shift)
                {
                    foreach (Entry item in SelectedItems)
                    {
                        item.X = (ushort)(Math.Round(item.X / 8.0, MidpointRounding.AwayFromZero) * 8);
                        item.Y = (ushort)(Math.Round(item.Y / 8.0, MidpointRounding.AwayFromZero) * 8);
                        item.UpdateSprite();
                    }
                }
                bool moved = false;
                for (int i = 0; i < SelectedItems.Count; i++)
                    if (SelectedItems[i].X != locs[i].X | SelectedItems[i].Y != locs[i].Y)
                        moved = true;
                if (moved)
                    AddUndo(new ObjectMoveUndoAction(new List<Entry>(SelectedItems), locs));
                ObjectProperties.SelectedObjects = SelectedItems.ToArray();
                LevelData.Objects.Sort();
                LevelData.Rings.Sort();
                if (LevelData.Bumpers != null) LevelData.Bumpers.Sort();
            }
            objdrag = false;
            selecting = false;
            DrawLevel();
        }

        private void panel2_MouseDown(object sender, MouseEventArgs e)
        {
            if (!loaded) return;
            Point chunkpoint = new Point(((int)(e.X / ZoomLevel) + hScrollBar2.Value) / LevelData.chunksz, ((int)(e.Y / ZoomLevel) + vScrollBar2.Value) / LevelData.chunksz);
            if (chunkpoint.X >= LevelData.FGLayout.GetLength(0) | chunkpoint.Y >= LevelData.FGLayout.GetLength(1)) return;
            switch (FGMode)
            {
                case EditingMode.Draw:
                    switch (e.Button)
                    {
                        case MouseButtons.Left:
                            if (LevelData.Level.LayoutFormat == EngineVersion.S1 && e.Clicks >= 2)
                                LevelData.FGLoop[chunkpoint.X, chunkpoint.Y] = !LevelData.FGLoop[chunkpoint.X, chunkpoint.Y];
                            else
                            {
                                locs = new List<Point>();
                                tiles = new List<byte>();
                                byte t = LevelData.FGLayout[chunkpoint.X, chunkpoint.Y];
                                if (t != SelectedChunk)
                                {
                                    locs.Add(chunkpoint);
                                    tiles.Add(t);
                                    LevelData.FGLayout[chunkpoint.X, chunkpoint.Y] = SelectedChunk;
                                    DrawLevel();
                                }
                            }
                            break;
                        case MouseButtons.Right:
                            SelectedChunk = LevelData.FGLayout[chunkpoint.X, chunkpoint.Y];
                            if (SelectedChunk < LevelData.Chunks.Count)
                                ChunkSelector.SelectedIndex = SelectedChunk;
                            DrawLevel();
                            break;
                    }
                    break;
                case EditingMode.Select:
                    switch (e.Button)
                    {
                        case MouseButtons.Left:
                            selecting = true;
                            FGSelection = new Rectangle(chunkpoint, new Size(1, 1));
                            DrawLevel();
                            break;
                        case MouseButtons.Right:
                            menuLoc = chunkpoint;
                            if (!FGSelection.Contains(chunkpoint))
                            {
                                FGSelection = new Rectangle(chunkpoint, new Size(1, 1));
                                DrawLevel();
                            }
                            pasteOnceToolStripMenuItem.Enabled = pasteRepeatingToolStripMenuItem.Enabled = Clipboard.ContainsData(typeof(LayoutSection).AssemblyQualifiedName);
                            layoutContextMenuStrip.Show(panel2, e.Location);
                            break;
                    }
                    break;
            }
        }

        private void panel2_MouseMove(object sender, MouseEventArgs e)
        {
            if (!loaded) return;
            Rectangle bnd = panel2.Bounds;
            bnd.Offset(-panel2.Location.X, -panel2.Location.Y);
            if (!bnd.Contains(panel2.PointToClient(Cursor.Position))) return;
            Point mouse = new Point((int)(e.X / ZoomLevel) + hScrollBar2.Value, (int)(e.Y / ZoomLevel) + vScrollBar2.Value);
            Point chunkpoint = new Point(mouse.X / LevelData.chunksz, mouse.Y / LevelData.chunksz);
            if (chunkpoint.X >= LevelData.FGLayout.GetLength(0) | chunkpoint.Y >= LevelData.FGLayout.GetLength(1)) return;
            switch (FGMode)
            {
                case EditingMode.Draw:
                    if (e.Button == MouseButtons.Left)
                    {
                        byte t = LevelData.FGLayout[chunkpoint.X, chunkpoint.Y];
                        if (t != SelectedChunk)
                        {
                            locs.Add(chunkpoint);
                            tiles.Add(t);
                            LevelData.FGLayout[chunkpoint.X, chunkpoint.Y] = SelectedChunk;
                        }
                    }
                    break;
                case EditingMode.Select:
                    if (e.Button == MouseButtons.Left & selecting)
                        FGSelection = new Rectangle(Math.Min(FGSelection.X, chunkpoint.X), Math.Min(FGSelection.Y, chunkpoint.Y), Math.Abs(FGSelection.X - chunkpoint.X) + 1, Math.Abs(FGSelection.Y - chunkpoint.Y) + 1);
                    break;
            }
            if (chunkpoint != lastchunkpoint) DrawLevel();
            lastchunkpoint = chunkpoint;
            lastmouse = mouse;
        }

        private void panel2_MouseUp(object sender, MouseEventArgs e)
        {
            switch (FGMode)
            {
                case EditingMode.Draw:
                    if (locs.Count > 0) AddUndo(new LayoutEditUndoAction(1, locs, tiles));
                    DrawLevel();
                    break;
                case EditingMode.Select:
                    selecting = false;
                    break;
            }
        }

        private void panel3_MouseDown(object sender, MouseEventArgs e)
        {
            if (!loaded) return;
            Point chunkpoint = new Point(((int)(e.X / ZoomLevel) + hScrollBar3.Value) / LevelData.chunksz, ((int)(e.Y / ZoomLevel) + vScrollBar3.Value) / LevelData.chunksz);
            if (chunkpoint.X >= LevelData.BGLayout.GetLength(0) | chunkpoint.Y >= LevelData.BGLayout.GetLength(1)) return;
            switch (BGMode)
            {
                case EditingMode.Draw:
                    switch (e.Button)
                    {
                        case MouseButtons.Left:
                            if (LevelData.Level.LayoutFormat == EngineVersion.S1 && e.Clicks >= 2)
                                LevelData.BGLoop[chunkpoint.X, chunkpoint.Y] = !LevelData.BGLoop[chunkpoint.X, chunkpoint.Y];
                            else
                            {
                                locs = new List<Point>();
                                tiles = new List<byte>();
                                byte tb = LevelData.BGLayout[chunkpoint.X, chunkpoint.Y];
                                if (tb != SelectedChunk)
                                {
                                    locs.Add(chunkpoint);
                                    tiles.Add(tb);
                                    LevelData.BGLayout[chunkpoint.X, chunkpoint.Y] = SelectedChunk;
                                    DrawLevel();
                                }
                            }
                            break;
                        case MouseButtons.Right:
                            SelectedChunk = LevelData.BGLayout[chunkpoint.X, chunkpoint.Y];
                            if (SelectedChunk < LevelData.Chunks.Count)
                                ChunkSelector.SelectedIndex = SelectedChunk;
                            DrawLevel();
                            break;
                    }
                    break;
                case EditingMode.Select:
                    switch (e.Button)
                    {
                        case MouseButtons.Left:
                            selecting = true;
                            BGSelection = new Rectangle(chunkpoint, new Size(1, 1));
                            DrawLevel();
                            break;
                        case MouseButtons.Right:
                            menuLoc = chunkpoint;
                            if (!BGSelection.Contains(chunkpoint))
                            {
                                BGSelection = new Rectangle(chunkpoint, new Size(1, 1));
                                DrawLevel();
                            }
                            pasteOnceToolStripMenuItem.Enabled = pasteRepeatingToolStripMenuItem.Enabled = Clipboard.ContainsData(typeof(LayoutSection).AssemblyQualifiedName);
                            layoutContextMenuStrip.Show(panel3, e.Location);
                            break;
                    }
                    break;
            }
        }

        private void panel3_MouseMove(object sender, MouseEventArgs e)
        {
            if (!loaded) return;
            Rectangle bnd = panel3.Bounds;
            bnd.Offset(-panel3.Location.X, -panel3.Location.Y);
            if (!bnd.Contains(panel3.PointToClient(Cursor.Position))) return;
            Point mouse = new Point((int)(e.X / ZoomLevel) + hScrollBar3.Value, (int)(e.Y / ZoomLevel) + vScrollBar3.Value);
            Point chunkpoint = new Point(mouse.X / LevelData.chunksz, mouse.Y / LevelData.chunksz);
            if (chunkpoint.X >= LevelData.BGLayout.GetLength(0) | chunkpoint.Y >= LevelData.BGLayout.GetLength(1)) return;
            switch (BGMode)
            {
                case EditingMode.Draw:
                    if (e.Button == MouseButtons.Left)
                    {
                        byte t = LevelData.BGLayout[chunkpoint.X, chunkpoint.Y];
                        if (t != SelectedChunk)
                        {
                            locs.Add(chunkpoint);
                            tiles.Add(t);
                            LevelData.BGLayout[chunkpoint.X, chunkpoint.Y] = SelectedChunk;
                        }
                    }
                    break;
                case EditingMode.Select:
                    if (e.Button == MouseButtons.Left & selecting)
                        BGSelection = new Rectangle(Math.Min(BGSelection.X, chunkpoint.X), Math.Min(BGSelection.Y, chunkpoint.Y), Math.Abs(BGSelection.X - chunkpoint.X) + 1, Math.Abs(BGSelection.Y - chunkpoint.Y) + 1);
                    break;
            }
            if (chunkpoint != lastchunkpoint) DrawLevel();
            lastchunkpoint = chunkpoint;
            lastmouse = mouse;
        }

        private void panel3_MouseUp(object sender, MouseEventArgs e)
        {
            switch (BGMode)
            {
                case EditingMode.Draw:
                    if (locs.Count > 0) AddUndo(new LayoutEditUndoAction(1, locs, tiles));
                    DrawLevel();
                    break;
                case EditingMode.Select:
                    selecting = false;
                    break;
            }
        }

        private void ChunkSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!loaded) return;
            if (ChunkSelector.SelectedIndex == -1 | ChunkSelector.SelectedIndex >= LevelData.Chunks.Count) return;
            SelectedChunk = (byte)ChunkSelector.SelectedIndex;
            SelectedChunkBlock = new Point();
            ChunkBlockPropertyGrid.SelectedObject = LevelData.Chunks[SelectedChunk].blocks[0, 0];
            ChunkPicture.Invalidate();
            ChunkID.Text = SelectedChunk.ToString("X2");
            ChunkCount.Text = LevelData.Chunks.Count.ToString("X") + " / 100";
            DrawLevel();
        }

        private void SelectedObjectChanged()
        {
            ObjectProperties.SelectedObjects = SelectedItems.ToArray();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (loaded)
            {
                switch (MessageBox.Show(this, "Do you want to save?", Text, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question))
                {
                    case DialogResult.Yes:
                        saveToolStripMenuItem_Click(this, EventArgs.Empty);
                        break;
                    case DialogResult.Cancel:
                        e.Cancel = true;
                        break;
                }
            }
            Settings.ShowHUD = hUDToolStripMenuItem.Checked;
            Settings.ShowGrid = enableGridToolStripMenuItem.Checked;
            Settings.IncludeObjectsInForegroundSelection = includeObjectsWithForegroundSelectionToolStripMenuItem.Checked;
            Settings.Save();
        }

        private void ScrollBar_ValueChanged(object sender, EventArgs e)
        {
            if (!loaded) return;
            loaded = false;
            switch (tabControl1.SelectedIndex)
            {
                case 0:
                    hScrollBar2.Value = Math.Min(hScrollBar1.Value, hScrollBar2.Maximum);
                    vScrollBar2.Value = Math.Min(vScrollBar1.Value, vScrollBar2.Maximum);
                    break;
                case 1:
                    hScrollBar1.Value = Math.Min(hScrollBar2.Value, hScrollBar1.Maximum);
                    vScrollBar1.Value = Math.Min(vScrollBar2.Value, vScrollBar1.Maximum);
                    break;
            }
            loaded = true;
            DrawLevel();
        }

        private void panel_Resize(object sender, EventArgs e)
        {
            if (!loaded) return;
            Panel1Gfx = panel1.CreateGraphics();
            Panel1Gfx.SetOptions();
            Panel2Gfx = panel2.CreateGraphics();
            Panel2Gfx.SetOptions();
            Panel3Gfx = panel3.CreateGraphics();
            Panel3Gfx.SetOptions();
            loaded = false;
            UpdateScrollBars();
            loaded = true;
            DrawLevel();
        }

        Point menuLoc;
        private void addObjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ObjectSelect.ShowDialog(this) == DialogResult.OK)
            {
                byte ID = (byte)ObjectSelect.numericUpDown1.Value;
                byte sub = (byte)ObjectSelect.numericUpDown2.Value;
                switch (LevelData.Level.ObjectFormat)
                {
                    case EngineVersion.S1:
                        S1ObjectEntry ent1 = (S1ObjectEntry)LevelData.CreateObject(ID);
                        LevelData.Objects.Add(ent1);
                        ent1.SubType = sub;
                        ent1.X = (ushort)((menuLoc.X * ZoomLevel) + hScrollBar1.Value);
                        ent1.Y = (ushort)((menuLoc.Y * ZoomLevel) + vScrollBar1.Value);
                        ent1.RememberState = LevelData.GetObjectDefinition(ID).RememberState;
                        ent1.UpdateSprite();
                        SelectedItems.Clear();
                        SelectedItems.Add(ent1);
                        SelectedObjectChanged();
                        AddUndo(new ObjectAddedUndoAction(ent1));
                        break;
                    case EngineVersion.S2:
                    case EngineVersion.S2NA:
                        S2ObjectEntry ent = (S2ObjectEntry)LevelData.CreateObject(ID);
                        LevelData.Objects.Add(ent);
                        ent.SubType = sub;
                        ent.X = (ushort)((menuLoc.X * ZoomLevel) + hScrollBar1.Value);
                        ent.Y = (ushort)((menuLoc.Y * ZoomLevel) + vScrollBar1.Value);
                        ent.RememberState = LevelData.GetObjectDefinition(ID).RememberState;
                        ent.UpdateSprite();
                        SelectedItems.Clear();
                        SelectedItems.Add(ent);
                        SelectedObjectChanged();
                        AddUndo(new ObjectAddedUndoAction(ent));
                        break;
                    case EngineVersion.S3K:
                    case EngineVersion.SKC:
                        S3KObjectEntry ent3 = (S3KObjectEntry)LevelData.CreateObject(ID);
                        LevelData.Objects.Add(ent3);
                        ent3.SubType = sub;
                        ent3.X = (ushort)((menuLoc.X * ZoomLevel) + hScrollBar1.Value);
                        ent3.Y = (ushort)((menuLoc.Y * ZoomLevel) + vScrollBar1.Value);
                        ent3.UpdateSprite();
                        SelectedItems.Clear();
                        SelectedItems.Add(ent3);
                        SelectedObjectChanged();
                        AddUndo(new ObjectAddedUndoAction(ent3));
                        break;
                    case EngineVersion.SCDPC:
                        SCDObjectEntry entcd = (SCDObjectEntry)LevelData.CreateObject(ID);
                        LevelData.Objects.Add(entcd);
                        entcd.SubType = sub;
                        entcd.X = (ushort)((menuLoc.X * ZoomLevel) + hScrollBar1.Value);
                        entcd.Y = (ushort)((menuLoc.Y * ZoomLevel) + vScrollBar1.Value);
                        entcd.RememberState = LevelData.GetObjectDefinition(ID).RememberState;
                        switch (LevelData.Level.TimeZone)
                        {
                            case API.TimeZone.Present:
                                entcd.ShowPresent = true;
                                break;
                            case API.TimeZone.Past:
                                entcd.ShowPast = true;
                                break;
                            case API.TimeZone.Future:
                                entcd.ShowFuture = true;
                                break;
                        }
                        entcd.UpdateSprite();
                        SelectedItems.Clear();
                        SelectedItems.Add(entcd);
                        SelectedObjectChanged();
                        AddUndo(new ObjectAddedUndoAction(entcd));
                        break;
                }
                LevelData.Objects.Sort();
                DrawLevel();
            }
        }

        private void addRingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            switch (LevelData.Level.RingFormat)
            {
                case EngineVersion.S1:
                    LevelData.Objects.Add(new S1ObjectEntry() { X = (ushort)((menuLoc.X * ZoomLevel) + hScrollBar1.Value), Y = (ushort)((menuLoc.Y * ZoomLevel) + vScrollBar1.Value), ID = 0x25 });
                    LevelData.Objects[LevelData.Objects.Count - 1].UpdateSprite();
                    SelectedItems.Clear();
                    SelectedItems.Add(LevelData.Objects[LevelData.Objects.Count - 1]);
                    SelectedObjectChanged();
                    AddUndo(new ObjectAddedUndoAction(LevelData.Objects[LevelData.Objects.Count - 1]));
                    LevelData.Objects.Sort();
                    break;
                case EngineVersion.S2:
                case EngineVersion.S2NA:
                    LevelData.Rings.Add(new S2RingEntry() { X = (ushort)((menuLoc.X * ZoomLevel) + hScrollBar1.Value), Y = (ushort)((menuLoc.Y * ZoomLevel) + vScrollBar1.Value) });
                    LevelData.Rings[LevelData.Rings.Count - 1].UpdateSprite();
                    SelectedItems.Clear();
                    SelectedItems.Add(LevelData.Rings[LevelData.Rings.Count - 1]);
                    SelectedObjectChanged();
                    AddUndo(new ObjectAddedUndoAction(LevelData.Rings[LevelData.Rings.Count - 1]));
                    LevelData.Rings.Sort();
                    break;
                case EngineVersion.S3K:
                case EngineVersion.SKC:
                    LevelData.Rings.Add(new S3KRingEntry() { X = (ushort)((menuLoc.X * ZoomLevel) + hScrollBar1.Value), Y = (ushort)((menuLoc.Y * ZoomLevel) + vScrollBar1.Value) });
                    LevelData.Rings[LevelData.Rings.Count - 1].UpdateSprite();
                    SelectedItems.Clear();
                    SelectedItems.Add(LevelData.Rings[LevelData.Rings.Count - 1]);
                    SelectedObjectChanged();
                    AddUndo(new ObjectAddedUndoAction(LevelData.Rings[LevelData.Rings.Count - 1]));
                    LevelData.Rings.Sort();
                    break;
            }
            DrawLevel();
        }

        private void addGroupOfObjectsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ObjectSelect.ShowDialog(this) == DialogResult.OK)
            {
                byte ID = (byte)ObjectSelect.numericUpDown1.Value;
                byte sub = (byte)ObjectSelect.numericUpDown2.Value;
                using (AddGroupDialog dlg = new AddGroupDialog())
                {
                    dlg.Text = "Add Group of Objects";
                    dlg.XDist.Value = LevelData.GetObjectDefinition(ID).GetBounds(new S2ObjectEntry() { SubType = sub }, Point.Empty).Width;
                    dlg.YDist.Value = LevelData.GetObjectDefinition(ID).GetBounds(new S2ObjectEntry() { SubType = sub }, Point.Empty).Height;
                    if (dlg.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
                    {
                        Point pt = new Point((int)(menuLoc.X * ZoomLevel) + hScrollBar1.Value, (int)(menuLoc.Y * ZoomLevel) + vScrollBar1.Value);
                        int xst = pt.X;
                        Size xsz = new Size((int)dlg.XDist.Value, 0);
                        Size ysz = new Size(0, (int)dlg.YDist.Value);
                        SelectedItems.Clear();
                        for (int y = 0; y < dlg.Rows.Value; y++)
                        {
                            for (int x = 0; x < dlg.Columns.Value; x++)
                            {
                                switch (LevelData.Level.ObjectFormat)
                                {
                                    case EngineVersion.S1:
                                        S1ObjectEntry ent1 = (S1ObjectEntry)LevelData.CreateObject(ID);
                                        LevelData.Objects.Add(ent1);
                                        ent1.SubType = sub;
                                        ent1.X = (ushort)(pt.X);
                                        ent1.Y = (ushort)(pt.Y);
                                        ent1.RememberState = LevelData.GetObjectDefinition(ID).RememberState;
                                        ent1.UpdateSprite();
                                        SelectedItems.Add(ent1);
                                        break;
                                    case EngineVersion.S2:
                                    case EngineVersion.S2NA:
                                        S2ObjectEntry ent = (S2ObjectEntry)LevelData.CreateObject(ID);
                                        LevelData.Objects.Add(ent);
                                        ent.SubType = sub;
                                        ent.X = (ushort)(pt.X);
                                        ent.Y = (ushort)(pt.Y);
                                        ent.RememberState = LevelData.GetObjectDefinition(ID).RememberState;
                                        ent.UpdateSprite();
                                        SelectedItems.Add(ent);
                                        break;
                                    case EngineVersion.S3K:
                                    case EngineVersion.SKC:
                                        S3KObjectEntry ent3 = (S3KObjectEntry)LevelData.CreateObject(ID);
                                        LevelData.Objects.Add(ent3);
                                        ent3.SubType = sub;
                                        ent3.X = (ushort)(pt.X);
                                        ent3.Y = (ushort)(pt.Y);
                                        ent3.UpdateSprite();
                                        SelectedItems.Add(ent3);
                                        break;
                                    case EngineVersion.SCDPC:
                                        SCDObjectEntry entcd = (SCDObjectEntry)LevelData.CreateObject(ID);
                                        LevelData.Objects.Add(entcd);
                                        entcd.SubType = sub;
                                        entcd.X = (ushort)(pt.X);
                                        entcd.Y = (ushort)(pt.Y);
                                        entcd.RememberState = LevelData.GetObjectDefinition(ID).RememberState;
                                        switch (LevelData.Level.TimeZone)
                                        {
                                            case API.TimeZone.Present:
                                                entcd.ShowPresent = true;
                                                break;
                                            case API.TimeZone.Past:
                                                entcd.ShowPast = true;
                                                break;
                                            case API.TimeZone.Future:
                                                entcd.ShowFuture = true;
                                                break;
                                        }
                                        entcd.UpdateSprite();
                                        SelectedItems.Add(entcd);
                                        break;
                                }
                                pt += xsz;
                            }
                            pt.X = xst;
                            pt += ysz;
                        }
                        SelectedObjectChanged();
                        LevelData.Objects.Sort();
                        DrawLevel();
                    }
                }
            }
        }

        private void addGroupOfRingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (AddGroupDialog dlg = new AddGroupDialog())
            {
                dlg.Text = "Add Group of Rings";
                switch (LevelData.Level.RingFormat)
                {
                    case EngineVersion.S2:
                    case EngineVersion.S2NA:
                        dlg.XDist.Value = LevelData.S2RingDef.GetBounds(new S2RingEntry(), Point.Empty).Width;
                        dlg.YDist.Value = LevelData.S2RingDef.GetBounds(new S2RingEntry(), Point.Empty).Height;
                        break;
                    case EngineVersion.S3K:
                    case EngineVersion.SKC:
                        dlg.XDist.Value = LevelData.S3KRingDef.GetBounds(new S3KRingEntry(), Point.Empty).Width;
                        dlg.YDist.Value = LevelData.S3KRingDef.GetBounds(new S3KRingEntry(), Point.Empty).Height;
                        break;
                    default:
                        return;
                }
                if (dlg.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
                {
                    Point pt = new Point((int)(menuLoc.X * ZoomLevel) + hScrollBar1.Value, (int)(menuLoc.Y * ZoomLevel) + vScrollBar1.Value);
                    int xst = pt.X;
                    Size xsz = new Size((int)dlg.XDist.Value, 0);
                    Size ysz = new Size(0, (int)dlg.YDist.Value);
                    SelectedItems.Clear();
                    for (int y = 0; y < dlg.Rows.Value; y++)
                    {
                        for (int x = 0; x < dlg.Columns.Value; x++)
                        {
                            switch (LevelData.Level.RingFormat)
                            {
                                case EngineVersion.S2:
                                case EngineVersion.S2NA:
                                    LevelData.Rings.Add(new S2RingEntry() { X = (ushort)(pt.X), Y = (ushort)(pt.Y) });
                                    LevelData.Rings[LevelData.Rings.Count - 1].UpdateSprite();
                                    SelectedItems.Add(LevelData.Rings[LevelData.Rings.Count - 1]);
                                    break;
                                case EngineVersion.S3K:
                                case EngineVersion.SKC:
                                    LevelData.Rings.Add(new S3KRingEntry() { X = (ushort)(pt.X), Y = (ushort)(pt.Y) });
                                    LevelData.Rings[LevelData.Rings.Count - 1].UpdateSprite();
                                    SelectedItems.Add(LevelData.Rings[LevelData.Rings.Count - 1]);
                                    break;
                            }
                            pt += xsz;
                        }
                        pt.X = xst;
                        pt += ysz;
                    }
                    SelectedObjectChanged();
                    LevelData.Rings.Sort();
                    DrawLevel();
                }
            }
        }

        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<Entry> selitems = new List<Entry>();
            foreach (Entry item in SelectedItems)
            {
                if (item is ObjectEntry)
                {
                    LevelData.Objects.Remove((ObjectEntry)item);
                    selitems.Add(item);
                }
                else if (item is RingEntry)
                {
                    LevelData.Rings.Remove((RingEntry)item);
                    selitems.Add(item);
                }
                else if (item is CNZBumperEntry)
                {
                    LevelData.Bumpers.Remove((CNZBumperEntry)item);
                    selitems.Add(item);
                }
            }
            if (selitems.Count == 0) return;
            Clipboard.SetData(typeof(List<Entry>).AssemblyQualifiedName, selitems);
            AddUndo(new ObjectsDeletedUndoAction(selitems));
            SelectedItems.Clear();
            SelectedObjectChanged();
            DrawLevel();
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<Entry> selitems = new List<Entry>();
            foreach (Entry item in SelectedItems)
            {
                if (item is ObjectEntry)
                    selitems.Add(item);
                else if (item is RingEntry)
                    selitems.Add(item);
                else if (item is CNZBumperEntry)
                    selitems.Add(item);
            }
            if (selitems.Count == 0) return;
            Clipboard.SetData(typeof(List<Entry>).AssemblyQualifiedName, selitems);
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<Entry> objs = Clipboard.GetData(typeof(List<Entry>).AssemblyQualifiedName) as List<Entry>;
            Point upleft = new Point(int.MaxValue, int.MaxValue);
            foreach (Entry item in objs)
            {
                upleft.X = Math.Min(upleft.X, item.X);
                upleft.Y = Math.Min(upleft.Y, item.Y);
            }
            Size off = new Size((menuLoc.X + hScrollBar1.Value) - upleft.X, (menuLoc.Y + vScrollBar1.Value) - upleft.Y);
            SelectedItems = new List<Entry>(objs);
            foreach (Entry item in objs)
            {
                item.X += (ushort)off.Width;
                item.Y += (ushort)off.Height;
                if (item is ObjectEntry)
                    LevelData.Objects.Add((ObjectEntry)item);
                else if (item is RingEntry)
                    LevelData.Rings.Add((RingEntry)item);
                else if (item is CNZBumperEntry)
                    LevelData.Bumpers.Add((CNZBumperEntry)item);
                item.UpdateSprite();
            }
            AddUndo(new ObjectsPastedUndoAction(new List<Entry>(SelectedItems)));
            SelectedObjectChanged();
            LevelData.Objects.Sort();
            LevelData.Rings.Sort();
            if (LevelData.Bumpers != null) LevelData.Bumpers.Sort();
            DrawLevel();
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<Entry> selitems = new List<Entry>();
            foreach (Entry item in SelectedItems)
            {
                if (item is ObjectEntry)
                {
                    LevelData.Objects.Remove((ObjectEntry)item);
                    selitems.Add(item);
                }
                else if (item is RingEntry)
                {
                    LevelData.Rings.Remove((RingEntry)item);
                    selitems.Add(item);
                }
                else if (item is CNZBumperEntry)
                {
                    LevelData.Bumpers.Remove((CNZBumperEntry)item);
                    selitems.Add(item);
                }
            }
            if (selitems.Count == 0) return;
            AddUndo(new ObjectsDeletedUndoAction(selitems));
            SelectedItems.Clear();
            SelectedObjectChanged();
            DrawLevel();
        }

        private void panel_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Down:
                case Keys.Left:
                case Keys.Right:
                case Keys.Up:
                    e.IsInputKey = true;
                    break;
            }
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            selecting = false;
            switch (tabControl1.SelectedIndex)
            {
                case 0:
                    panel1.Focus();
                    break;
                case 1:
                    splitContainer2.Panel2.Controls.Add(ChunkSelector);
                    panel2.Focus();
                    break;
                case 2:
                    splitContainer3.Panel2.Controls.Add(ChunkSelector);
                    panel3.Focus();
                    break;
                case 3:
                    panel10.Controls.Add(ChunkSelector);
                    break;
            }
            DrawLevel();
        }

        public int SelectedBlock, SelectedTile;
        public Point SelectedChunkBlock, SelectedBlockTile, SelectedColor;

        private void ChunkPicture_MouseClick(object sender, MouseEventArgs e)
        {
            if (!loaded) return;
            SelectedChunkBlock = new Point(e.X / 16, e.Y / 16);
            ChunkBlockPropertyGrid.SelectedObject = LevelData.Chunks[SelectedChunk].blocks[e.X / 16, e.Y / 16];
            ChunkPicture.Invalidate();
        }

        private void ChunkBlockPropertyGrid_PropertyValueChanged(object sender, PropertyValueChangedEventArgs e)
        {
            LevelData.RedrawChunk(SelectedChunk);
            DrawLevel();
            ChunkPicture.Invalidate();
        }

        private void ChunkPicture_Paint(object sender, PaintEventArgs e)
        {
            if (!loaded) return;
            e.Graphics.Clear(LevelData.PaletteToColor(2, 0, false));
            if (lowToolStripMenuItem.Checked)
                e.Graphics.DrawImage(LevelData.ChunkBmps[SelectedChunk][0], 0, 0, LevelData.chunksz, LevelData.chunksz);
            if (highToolStripMenuItem.Checked)
                e.Graphics.DrawImage(LevelData.ChunkBmps[SelectedChunk][1], 0, 0, LevelData.chunksz, LevelData.chunksz);
            if (path1ToolStripMenuItem.Checked)
                e.Graphics.DrawImage(LevelData.ChunkColBmps[SelectedChunk][0], 0, 0, LevelData.chunksz, LevelData.chunksz);
            if (path2ToolStripMenuItem.Checked)
                e.Graphics.DrawImage(LevelData.ChunkColBmps[SelectedChunk][1], 0, 0, LevelData.chunksz, LevelData.chunksz);
            e.Graphics.DrawRectangle(Pens.White, SelectedChunkBlock.X * 16 - 1, SelectedChunkBlock.Y * 16 - 1, 18, 18);
        }

        private void BlockPicture_MouseClick(object sender, MouseEventArgs e)
        {
            if (!loaded) return;
            SelectedBlockTile = new Point(e.X / 32, e.Y / 32);
            BlockTilePropertyGrid.SelectedObject = LevelData.Blocks[SelectedBlock].tiles[e.X / 32, e.Y / 32];
            BlockPicture.Invalidate();
        }

        private void BlockTilePropertyGrid_PropertyValueChanged(object sender, PropertyValueChangedEventArgs e)
        {
            LevelData.RedrawBlock(SelectedBlock, true);
            DrawLevel();
            BlockPicture.Invalidate();
        }

        private void BlockSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (BlockSelector.SelectedIndex > -1)
            {
                SelectedBlock = BlockSelector.SelectedIndex;
                SelectedBlockTile = new Point();
                BlockTilePropertyGrid.SelectedObject = LevelData.Blocks[SelectedBlock].tiles[0, 0];
                if (LevelData.ColInds1.Count > 0)
                {
                    BlockCollision1.Value = LevelData.ColInds1[SelectedBlock];
                    BlockCollision2.Value = LevelData.ColInds2[SelectedBlock];
                }
                BlockID.Text = SelectedBlock.ToString("X3");
                int blockmax = 0x400;
                switch (LevelData.Game.EngineVersion)
                {
                    case EngineVersion.S2:
                    case EngineVersion.S2NA:
                        blockmax = 0x340;
                        break;
                    case EngineVersion.S3K:
                    case EngineVersion.SKC:
                        blockmax = 0x300;
                        break;
                }
                BlockCount.Text = LevelData.Blocks.Count.ToString("X") + " / " + blockmax.ToString("X");
                BlockPicture.Invalidate();
            }
        }

        private void BlockPicture_Paint(object sender, PaintEventArgs e)
        {
            if (!loaded) return;
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            e.Graphics.Clear(LevelData.PaletteToColor(2, 0, false));
            if (lowToolStripMenuItem.Checked)
                e.Graphics.DrawImage(LevelData.BlockBmpBits[SelectedBlock][0].Scale(4).ToBitmap(LevelData.BmpPal), 0, 0, 64, 64);
            if (highToolStripMenuItem.Checked)
                e.Graphics.DrawImage(LevelData.BlockBmpBits[SelectedBlock][1].Scale(4).ToBitmap(LevelData.BmpPal), 0, 0, 64, 64);
            if (path1ToolStripMenuItem.Checked)
                e.Graphics.DrawImage(LevelData.ColBmpBits[LevelData.ColInds1[SelectedBlock]].Scale(4).ToBitmap(Color.Transparent, Color.White), 0, 0, 64, 64);
            if (path2ToolStripMenuItem.Checked)
                e.Graphics.DrawImage(LevelData.ColBmpBits[LevelData.ColInds2[SelectedBlock]].Scale(4).ToBitmap(Color.Transparent, Color.White), 0, 0, 64, 64);
            e.Graphics.DrawRectangle(Pens.White, SelectedBlockTile.X * 32, SelectedBlockTile.Y * 32, 31, 31);
        }

        private void PalettePanel_Paint(object sender, PaintEventArgs e)
        {
            if (!loaded) return;
            e.Graphics.Clear(Color.Black);
            for (int y = 0; y <= 3; y++)
                for (int x = 0; x <= 15; x++)
                {
                    e.Graphics.FillRectangle(new SolidBrush(LevelData.PaletteToColor(y, x, false)), x * 32, y * 32, 32, 32);
                    e.Graphics.DrawRectangle(Pens.White, x * 32, y * 32, 31, 31);
                }
            e.Graphics.DrawRectangle(new Pen(Color.Yellow, 2), SelectedColor.X * 32, SelectedColor.Y * 32, 32, 32);
        }

        int[] cols;
        private void PalettePanel_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (!loaded) return;
            int line = e.Y / 32;
            int index = e.X / 32;
            ColorDialog a = new ColorDialog
            {
                AllowFullOpen = true,
                AnyColor = true,
                FullOpen = true,
                SolidColorOnly = true,
                Color = LevelData.PaletteToColor(line, index, false)
            };
            if (cols != null)
                a.CustomColors = cols;
            if (a.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                LevelData.ColorToPalette(line, index, a.Color);
                PalettePanel.Invalidate();
                LevelData.PaletteChanged();
                ChunkSelector.Invalidate();
                ChunkPicture.Invalidate();
                BlockSelector.Invalidate();
                BlockPicture.Invalidate();
                TileSelector.Invalidate();
                TilePicture.Invalidate();
            }
            cols = a.CustomColors;
        }

        private void BlockCollision1_ValueChanged(object sender, EventArgs e)
        {
            if (!loaded) return;
            LevelData.ColInds1[SelectedBlock] = (byte)BlockCollision1.Value;
            if (Object.ReferenceEquals(LevelData.ColInds1, LevelData.ColInds2))
                BlockCollision2.Value = BlockCollision1.Value;
        }

        void BlockCollision1_TextChanged(object sender, EventArgs e)
        {
            if (!loaded) return;
            byte value;
            if (byte.TryParse(BlockCollision1.Text, System.Globalization.NumberStyles.HexNumber, null, out value))
                BlockCollision1.Value = value;
        }

        private void BlockCollision2_ValueChanged(object sender, EventArgs e)
        {
            if (!loaded) return;
            LevelData.ColInds2[SelectedBlock] = (byte)BlockCollision2.Value;
            if (Object.ReferenceEquals(LevelData.ColInds1, LevelData.ColInds2))
                BlockCollision1.Value = BlockCollision2.Value;
        }

        void BlockCollision2_TextChanged(object sender, EventArgs e)
        {
            if (!loaded) return;
            byte value;
            if (byte.TryParse(BlockCollision2.Text, System.Globalization.NumberStyles.HexNumber, null, out value))
                BlockCollision2.Value = value;
        }

        private Color[] curpal;
        private void PalettePanel_MouseDown(object sender, MouseEventArgs e)
        {
            if (!loaded) return;
            bool newpal = e.Y / 32 != SelectedColor.Y;
            SelectedColor = new Point(e.X / 32, e.Y / 32);
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
                paletteContextMenuStrip.Show(PalettePanel, e.Location);
            PalettePanel.Invalidate();
            if (newpal)
            {
                curpal = new Color[16];
                for (int i = 0; i < 16; i++)
                    curpal[i] = LevelData.PaletteToColor(SelectedColor.Y, i, false);
            }
            TilePicture.Invalidate();
            TileSelector.Images.Clear();
            for (int i = 0; i < LevelData.Tiles.Count; i++)
                TileSelector.Images.Add(LevelData.TileToBmp4bpp(LevelData.Tiles[i], 0, SelectedColor.Y));
            TileSelector.SelectedIndex = SelectedTile;
            TileSelector.Invalidate();
        }

        private void importToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog a = new OpenFileDialog())
            {
                a.DefaultExt = "bin";
                a.Filter = "MD Palettes|*.bin|Image Files|*.bmp;*.png;*.jpg;*.gif";
                a.RestoreDirectory = true;
                if (a.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
                {
                    int l = SelectedColor.Y;
                    int x = SelectedColor.X;
                    switch (System.IO.Path.GetExtension(a.FileName))
                    {
                        case ".bin":
                            byte[] file = System.IO.File.ReadAllBytes(a.FileName);
                            for (int i = 0; i < file.Length; i += 2)
                            {
                                LevelData.Palette[LevelData.CurPal][l, x] = new SonLVLColor(BitConverter.ToUInt16(file, i));
                                x++;
                                if (x == 16)
                                {
                                    x = 0;
                                    l++;
                                    if (l == 4)
                                        break;
                                }
                            }
                            break;
                        case ".bmp":
                        case ".png":
                        case ".jpg":
                        case ".gif":
                            Bitmap bmp = new Bitmap(a.FileName);
                            if ((bmp.PixelFormat & System.Drawing.Imaging.PixelFormat.Indexed) == System.Drawing.Imaging.PixelFormat.Indexed)
                            {
                                Color[] pal = bmp.Palette.Entries;
                                for (int i = 0; i < pal.Length; i++)
                                {
                                    LevelData.ColorToPalette(l, x, pal[i]);
                                    x++;
                                    if (x == 16)
                                    {
                                        x = 0;
                                        l++;
                                        if (l == 4)
                                            break;
                                    }
                                }
                            }
                            break;
                    }
                }
            }
            LevelData.PaletteChanged();
            ChunkSelector.Invalidate();
            ChunkPicture.Invalidate();
            BlockSelector.Invalidate();
            BlockPicture.Invalidate();
            TileSelector.Invalidate();
            TilePicture.Invalidate();
        }

        private BitmapBits tile;
        private void TileSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (TileSelector.SelectedIndex > -1)
            {
                SelectedTile = TileSelector.SelectedIndex;
                tile = BitmapBits.FromTile(LevelData.Tiles[SelectedTile], 0);
                TileID.Text = SelectedTile.ToString("X3");
                TileCount.Text = LevelData.Tiles.Count.ToString("X") + " / 800";
                TilePicture.Invalidate();
            }
        }

        private void TilePicture_Paint(object sender, PaintEventArgs e)
        {
            if (TileSelector.SelectedIndex == -1) return;
            e.Graphics.SetOptions();
            e.Graphics.DrawImage(tile.Scale(16).ToBitmap(curpal), 0, 0, 128, 128);
        }

        private void TilePicture_MouseDown(object sender, MouseEventArgs e)
        {
            if (TileSelector.SelectedIndex == -1) return;
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                tile.Bits[((e.Y / 16) * 8) + (e.X / 16)] = (byte)SelectedColor.X;
                TilePicture.Invalidate();
            }
        }

        private void TilePicture_MouseMove(object sender, MouseEventArgs e)
        {
            if (TileSelector.SelectedIndex == -1) return;
            if (e.Button == System.Windows.Forms.MouseButtons.Left && new Rectangle(Point.Empty, TilePicture.Size).Contains(e.Location))
            {
                tile.Bits[((e.Y / 16) * 8) + (e.X / 16)] = (byte)SelectedColor.X;
                TilePicture.Invalidate();
            }
        }

        private void TilePicture_MouseUp(object sender, MouseEventArgs e)
        {
            if (TileSelector.SelectedIndex == -1) return;
            LevelData.Tiles[SelectedTile] = tile.ToTile();
            LevelData.Tiles[SelectedTile].CopyTo(LevelData.TileArray, SelectedTile * 32);
            for (int i = 0; i < LevelData.Blocks.Count; i++)
            {
                bool dr = false;
                for (int y = 0; y < 2; y++)
                    for (int x = 0; x < 2; x++)
                        if (LevelData.Blocks[i].tiles[x, y].Tile == SelectedTile)
                            dr = true;
                if (dr)
                    LevelData.RedrawBlock(i, true);
            }
            TileSelector.Images[SelectedTile] = LevelData.TileToBmp4bpp(LevelData.Tiles[SelectedTile], 0, SelectedColor.Y);
        }

        private void ChunkSelector_MouseDown(object sender, MouseEventArgs e)
        {
            if (!loaded) return;
            if (tabControl1.SelectedIndex == 3 & e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                pasteBeforeToolStripMenuItem.Enabled = Clipboard.ContainsData(typeof(Chunk).AssemblyQualifiedName) & LevelData.Chunks.Count < 256;
                pasteAfterToolStripMenuItem.Enabled = pasteBeforeToolStripMenuItem.Enabled;
                importToolStripMenuItem.Enabled = LevelData.Chunks.Count < 256;
                drawToolStripMenuItem.Enabled = importToolStripMenuItem.Enabled;
                insertAfterToolStripMenuItem.Enabled = importToolStripMenuItem.Enabled;
                insertBeforeToolStripMenuItem.Enabled = importToolStripMenuItem.Enabled;
                tileContextMenuStrip.Show(ChunkSelector, e.Location);
            }
        }

        private void BlockSelector_MouseDown(object sender, MouseEventArgs e)
        {
            if (!loaded) return;
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                int blockmax = 0x400;
                switch (LevelData.Game.EngineVersion)
                {
                    case EngineVersion.S2:
                    case EngineVersion.S2NA:
                        blockmax = 0x340;
                        break;
                    case EngineVersion.S3K:
                    case EngineVersion.SKC:
                        blockmax = 0x300;
                        break;
                }
                pasteBeforeToolStripMenuItem.Enabled = Clipboard.ContainsData(typeof(Block).AssemblyQualifiedName) & LevelData.Blocks.Count < blockmax;
                pasteAfterToolStripMenuItem.Enabled = pasteBeforeToolStripMenuItem.Enabled;
                importToolStripMenuItem.Enabled = LevelData.Blocks.Count < blockmax;
                drawToolStripMenuItem.Enabled = importToolStripMenuItem.Enabled;
                insertAfterToolStripMenuItem.Enabled = importToolStripMenuItem.Enabled;
                insertBeforeToolStripMenuItem.Enabled = importToolStripMenuItem.Enabled;
                tileContextMenuStrip.Show(BlockSelector, e.Location);
            }
        }

        private void TileSelector_MouseDown(object sender, MouseEventArgs e)
        {
            if (!loaded) return;
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                pasteBeforeToolStripMenuItem.Enabled = Clipboard.ContainsData("SonLVLTile") & LevelData.Tiles.Count < 0x800;
                pasteAfterToolStripMenuItem.Enabled = pasteBeforeToolStripMenuItem.Enabled;
                importToolStripMenuItem.Enabled = LevelData.Tiles.Count < 0x800;
                drawToolStripMenuItem.Enabled = importToolStripMenuItem.Enabled;
                insertAfterToolStripMenuItem.Enabled = importToolStripMenuItem.Enabled;
                insertBeforeToolStripMenuItem.Enabled = importToolStripMenuItem.Enabled;
                tileContextMenuStrip.Show(TileSelector, e.Location);
            }
        }

        private void cutTilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            switch (tabControl1.SelectedIndex)
            {
                case 3: // Chunks
                    Clipboard.SetData(typeof(Chunk).AssemblyQualifiedName, LevelData.Chunks[SelectedChunk].GetBytes());
                    LevelData.Chunks.RemoveAt(SelectedChunk);
                    LevelData.ChunkBmpBits.RemoveAt(SelectedChunk);
                    LevelData.ChunkBmps.RemoveAt(SelectedChunk);
                    LevelData.ChunkColBmpBits.RemoveAt(SelectedChunk);
                    LevelData.ChunkColBmps.RemoveAt(SelectedChunk);
                    LevelData.CompChunkBmps.RemoveAt(SelectedChunk);
                    LevelData.CompChunkBmpBits.RemoveAt(SelectedChunk);
                    SelectedChunk = (byte)Math.Min(SelectedChunk, LevelData.Chunks.Count - 1);
                    for (int y = 0; y < LevelData.FGLayout.GetLength(1); y++)
                        for (int x = 0; x < LevelData.FGLayout.GetLength(0); x++)
                            if (LevelData.FGLayout[x, y] > SelectedChunk)
                                LevelData.FGLayout[x, y]--;
                    for (int y = 0; y < LevelData.BGLayout.GetLength(1); y++)
                        for (int x = 0; x < LevelData.BGLayout.GetLength(0); x++)
                            if (LevelData.BGLayout[x, y] > SelectedChunk)
                                LevelData.BGLayout[x, y]--;
                    ChunkSelector.SelectedIndex = Math.Min(ChunkSelector.SelectedIndex, LevelData.Chunks.Count - 1);
                    break;
                case 4: // Blocks
                    Clipboard.SetData(typeof(Block).AssemblyQualifiedName, LevelData.Blocks[SelectedBlock].GetBytes());
                    LevelData.Blocks.RemoveAt(SelectedBlock);
                    LevelData.BlockBmps.RemoveAt(SelectedBlock);
                    LevelData.BlockBmpBits.RemoveAt(SelectedBlock);
                    LevelData.CompBlockBmps.RemoveAt(SelectedBlock);
                    LevelData.CompBlockBmpBits.RemoveAt(SelectedBlock);
                    LevelData.ColInds1.RemoveAt(SelectedBlock);
                    if (LevelData.Game.EngineVersion == EngineVersion.S2 || LevelData.Game.EngineVersion == EngineVersion.S2NA || LevelData.Game.EngineVersion == EngineVersion.S3K || LevelData.Game.EngineVersion == EngineVersion.SKC)
                        if (!Object.ReferenceEquals(LevelData.ColInds1, LevelData.ColInds2))
                            LevelData.ColInds2.RemoveAt(SelectedBlock);
                    for (int i = 0; i < LevelData.Chunks.Count; i++)
                    {
                        bool dr = false;
                        for (int y = 0; y < LevelData.chunksz / 16; y++)
                            for (int x = 0; x < LevelData.chunksz / 16; x++)
                                if (LevelData.Chunks[i].blocks[x, y].Block == SelectedBlock)
                                    dr = true;
                                else if (LevelData.Chunks[i].blocks[x, y].Block > SelectedBlock)
                                    LevelData.Chunks[i].blocks[x, y].Block--;
                        if (dr)
                            LevelData.RedrawChunk(i);
                    }
                    BlockSelector.SelectedIndex = Math.Min(BlockSelector.SelectedIndex, LevelData.Blocks.Count - 1);
                    break;
                case 5: // Tiles
                    Clipboard.SetData("SonLVLTile", LevelData.Tiles[SelectedTile]);
                    LevelData.Tiles.RemoveAt(SelectedTile);
                    LevelData.UpdateTileArray();
                    TileSelector.Images.RemoveAt(SelectedTile);
                    for (int i = 0; i < LevelData.Blocks.Count; i++)
                    {
                        bool dr = false;
                        for (int y = 0; y < 2; y++)
                            for (int x = 0; x < 2; x++)
                                if (LevelData.Blocks[i].tiles[x, y].Tile == SelectedTile)
                                    dr = true;
                                else if (LevelData.Blocks[i].tiles[x, y].Tile > SelectedTile)
                                    LevelData.Blocks[i].tiles[x, y].Tile--;
                        if (dr)
                            LevelData.RedrawBlock(i, true);
                    }
                    TileSelector.SelectedIndex = Math.Min(TileSelector.SelectedIndex, TileSelector.Images.Count - 1);
                    break;
            }
        }

        private void copyTilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            switch (tabControl1.SelectedIndex)
            {
                case 3: // Chunks
                    Clipboard.SetData(typeof(Chunk).AssemblyQualifiedName, LevelData.Chunks[SelectedChunk].GetBytes());
                    break;
                case 4: // Blocks
                    Clipboard.SetData(typeof(Block).AssemblyQualifiedName, LevelData.Blocks[SelectedBlock].GetBytes());
                    break;
                case 5: // Tiles
                    Clipboard.SetData("SonLVLTile", LevelData.Tiles[SelectedTile]);
                    break;
            }
        }

        private void pasteBeforeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            switch (tabControl1.SelectedIndex)
            {
                case 3: // Chunks
                    LevelData.Chunks.InsertBefore(SelectedChunk, new Chunk((byte[])Clipboard.GetData(typeof(Chunk).AssemblyQualifiedName), 0));
                    LevelData.ChunkBmpBits.Insert(SelectedChunk, new BitmapBits[2]);
                    LevelData.ChunkBmps.Insert(SelectedChunk, new Bitmap[2]);
                    LevelData.ChunkColBmpBits.Insert(SelectedChunk, new BitmapBits[2]);
                    LevelData.ChunkColBmps.Insert(SelectedChunk, new Bitmap[2]);
                    LevelData.CompChunkBmps.Insert(SelectedChunk, null);
                    LevelData.CompChunkBmpBits.Insert(SelectedChunk, null);
                    for (int y = 0; y < LevelData.FGLayout.GetLength(1); y++)
                        for (int x = 0; x < LevelData.FGLayout.GetLength(0); x++)
                            if (LevelData.FGLayout[x, y] >= SelectedChunk)
                                LevelData.FGLayout[x, y]++;
                    for (int y = 0; y < LevelData.BGLayout.GetLength(1); y++)
                        for (int x = 0; x < LevelData.BGLayout.GetLength(0); x++)
                            if (LevelData.BGLayout[x, y] >= SelectedChunk)
                                LevelData.BGLayout[x, y]++;
                    LevelData.RedrawChunk(SelectedChunk);
                    ChunkSelector.SelectedIndex = SelectedChunk;
                    break;
                case 4: // Blocks
                    LevelData.Blocks.InsertBefore(SelectedBlock, new Block((byte[])Clipboard.GetData(typeof(Block).AssemblyQualifiedName), 0));
                    LevelData.BlockBmps.Insert(SelectedBlock, new Bitmap[2]);
                    LevelData.BlockBmpBits.Insert(SelectedBlock, new BitmapBits[2]);
                    LevelData.CompBlockBmps.Insert(SelectedBlock, null);
                    LevelData.CompBlockBmpBits.Insert(SelectedBlock, null);
                    LevelData.ColInds1.Insert(SelectedBlock, 0);
                    if (LevelData.Game.EngineVersion == EngineVersion.S2 || LevelData.Game.EngineVersion == EngineVersion.S2NA || LevelData.Game.EngineVersion == EngineVersion.S3K || LevelData.Game.EngineVersion == EngineVersion.SKC)
                        if (!Object.ReferenceEquals(LevelData.ColInds1, LevelData.ColInds2))
                            LevelData.ColInds2.Insert(SelectedBlock, 0);
                    for (int i = 0; i < LevelData.Chunks.Count; i++)
                        for (int y = 0; y < LevelData.chunksz / 16; y++)
                            for (int x = 0; x < LevelData.chunksz / 16; x++)
                                if (LevelData.Chunks[i].blocks[x, y].Block >= SelectedBlock)
                                    LevelData.Chunks[i].blocks[x, y].Block++;
                    LevelData.RedrawBlock(SelectedBlock, false);
                    BlockSelector.SelectedIndex = SelectedBlock;
                    break;
                case 5: // Tiles
                    byte[] t = (byte[])Clipboard.GetData("SonLVLTile");
                    LevelData.Tiles.InsertBefore(SelectedTile, t);
                    LevelData.UpdateTileArray();
                    TileSelector.Images.Insert(SelectedTile, LevelData.TileToBmp4bpp(LevelData.Tiles[SelectedTile], 0, SelectedColor.Y));
                    for (int i = 0; i < LevelData.Blocks.Count; i++)
                        for (int y = 0; y < 2; y++)
                            for (int x = 0; x < 2; x++)
                                if (LevelData.Blocks[i].tiles[x, y].Tile >= SelectedTile)
                                    LevelData.Blocks[i].tiles[x, y].Tile++;
                    TileSelector.SelectedIndex = SelectedTile;
                    break;
            }
        }

        private void pasteAfterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            switch (tabControl1.SelectedIndex)
            {
                case 3: // Chunks
                    LevelData.Chunks.InsertAfter(SelectedChunk, new Chunk((byte[])Clipboard.GetData(typeof(Chunk).AssemblyQualifiedName), 0));
                    SelectedChunk++;
                    LevelData.ChunkBmpBits.Insert(SelectedChunk, new BitmapBits[2]);
                    LevelData.ChunkBmps.Insert(SelectedChunk, new Bitmap[2]);
                    LevelData.ChunkColBmpBits.Insert(SelectedChunk, new BitmapBits[2]);
                    LevelData.ChunkColBmps.Insert(SelectedChunk, new Bitmap[2]);
                    LevelData.CompChunkBmps.Insert(SelectedChunk, null);
                    LevelData.CompChunkBmpBits.Insert(SelectedChunk, null);
                    for (int y = 0; y < LevelData.FGLayout.GetLength(1); y++)
                        for (int x = 0; x < LevelData.FGLayout.GetLength(0); x++)
                            if (LevelData.FGLayout[x, y] >= SelectedChunk)
                                LevelData.FGLayout[x, y]++;
                    for (int y = 0; y < LevelData.BGLayout.GetLength(1); y++)
                        for (int x = 0; x < LevelData.BGLayout.GetLength(0); x++)
                            if (LevelData.BGLayout[x, y] >= SelectedChunk)
                                LevelData.BGLayout[x, y]++;
                    LevelData.RedrawChunk(SelectedChunk);
                    ChunkSelector.SelectedIndex = SelectedChunk;
                    break;
                case 4: // Blocks
                    LevelData.Blocks.InsertAfter(SelectedBlock, new Block((byte[])Clipboard.GetData(typeof(Block).AssemblyQualifiedName), 0));
                    SelectedBlock++;
                    LevelData.BlockBmps.Insert(SelectedBlock, new Bitmap[2]);
                    LevelData.BlockBmpBits.Insert(SelectedBlock, new BitmapBits[2]);
                    LevelData.CompBlockBmps.Insert(SelectedBlock, null);
                    LevelData.CompBlockBmpBits.Insert(SelectedBlock, null);
                    LevelData.ColInds1.Insert(SelectedBlock, 0);
                    if (LevelData.Game.EngineVersion == EngineVersion.S2 || LevelData.Game.EngineVersion == EngineVersion.S2NA || LevelData.Game.EngineVersion == EngineVersion.S3K || LevelData.Game.EngineVersion == EngineVersion.SKC)
                        if (!Object.ReferenceEquals(LevelData.ColInds1, LevelData.ColInds2))
                            LevelData.ColInds2.Insert(SelectedBlock, 0);
                    for (int i = 0; i < LevelData.Chunks.Count; i++)
                        for (int y = 0; y < LevelData.chunksz / 16; y++)
                            for (int x = 0; x < LevelData.chunksz / 16; x++)
                                if (LevelData.Chunks[i].blocks[x, y].Block >= SelectedBlock)
                                    LevelData.Chunks[i].blocks[x, y].Block++;
                    LevelData.RedrawBlock(SelectedBlock, false);
                    BlockSelector.SelectedIndex = SelectedBlock;
                    break;
                case 5: // Tiles
                    byte[] t = (byte[])Clipboard.GetData("SonLVLTile");
                    LevelData.Tiles.InsertAfter(SelectedTile, t);
                    SelectedTile++;
                    LevelData.UpdateTileArray();
                    TileSelector.Images.Insert(SelectedTile, LevelData.TileToBmp4bpp(LevelData.Tiles[SelectedTile], 0, SelectedColor.Y));
                    for (int i = 0; i < LevelData.Blocks.Count; i++)
                        for (int y = 0; y < 2; y++)
                            for (int x = 0; x < 2; x++)
                                if (LevelData.Blocks[i].tiles[x, y].Tile >= SelectedTile)
                                    LevelData.Blocks[i].tiles[x, y].Tile++;
                    TileSelector.SelectedIndex = SelectedTile;
                    break;
            }
        }

        private void insertBeforeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            switch (tabControl1.SelectedIndex)
            {
                case 3: // Chunks
                    LevelData.Chunks.InsertBefore(SelectedChunk, new Chunk());
                    LevelData.ChunkBmpBits.Insert(SelectedChunk, new BitmapBits[2]);
                    LevelData.ChunkBmps.Insert(SelectedChunk, new Bitmap[2]);
                    LevelData.ChunkColBmpBits.Insert(SelectedChunk, new BitmapBits[2]);
                    LevelData.ChunkColBmps.Insert(SelectedChunk, new Bitmap[2]);
                    LevelData.CompChunkBmps.Insert(SelectedChunk, null);
                    LevelData.CompChunkBmpBits.Insert(SelectedChunk, null);
                    for (int y = 0; y < LevelData.FGLayout.GetLength(1); y++)
                        for (int x = 0; x < LevelData.FGLayout.GetLength(0); x++)
                            if (LevelData.FGLayout[x, y] >= SelectedChunk)
                                LevelData.FGLayout[x, y]++;
                    for (int y = 0; y < LevelData.BGLayout.GetLength(1); y++)
                        for (int x = 0; x < LevelData.BGLayout.GetLength(0); x++)
                            if (LevelData.BGLayout[x, y] >= SelectedChunk)
                                LevelData.BGLayout[x, y]++;
                    LevelData.RedrawChunk(SelectedChunk);
                    ChunkSelector.SelectedIndex = SelectedChunk;
                    break;
                case 4: // Blocks
                    LevelData.Blocks.InsertBefore(SelectedBlock, new Block());
                    LevelData.BlockBmps.Insert(SelectedBlock, new Bitmap[2]);
                    LevelData.BlockBmpBits.Insert(SelectedBlock, new BitmapBits[2]);
                    LevelData.CompBlockBmps.Insert(SelectedBlock, null);
                    LevelData.CompBlockBmpBits.Insert(SelectedBlock, null);
                    LevelData.ColInds1.Insert(SelectedBlock, 0);
                    if (LevelData.Game.EngineVersion == EngineVersion.S2 || LevelData.Game.EngineVersion == EngineVersion.S2NA || LevelData.Game.EngineVersion == EngineVersion.S3K || LevelData.Game.EngineVersion == EngineVersion.SKC)
                        if (!Object.ReferenceEquals(LevelData.ColInds1, LevelData.ColInds2))
                            LevelData.ColInds2.Insert(SelectedBlock, 0);
                    for (int i = 0; i < LevelData.Chunks.Count; i++)
                        for (int y = 0; y < LevelData.chunksz / 16; y++)
                            for (int x = 0; x < LevelData.chunksz / 16; x++)
                                if (LevelData.Chunks[i].blocks[x, y].Block >= SelectedBlock)
                                    LevelData.Chunks[i].blocks[x, y].Block++;
                    LevelData.RedrawBlock(SelectedBlock, false);
                    BlockSelector.SelectedIndex = SelectedBlock;
                    break;
                case 5: // Tiles
                    LevelData.Tiles.InsertBefore(SelectedTile, new byte[32]);
                    LevelData.UpdateTileArray();
                    TileSelector.Images.Insert(SelectedTile, LevelData.TileToBmp4bpp(LevelData.Tiles[SelectedTile], 0, SelectedColor.Y));
                    for (int i = 0; i < LevelData.Blocks.Count; i++)
                        for (int y = 0; y < 2; y++)
                            for (int x = 0; x < 2; x++)
                                if (LevelData.Blocks[i].tiles[x, y].Tile >= SelectedTile)
                                    LevelData.Blocks[i].tiles[x, y].Tile++;
                    TileSelector.SelectedIndex = SelectedTile;
                    break;
            }
        }

        private void insertAfterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            switch (tabControl1.SelectedIndex)
            {
                case 3: // Chunks

                    LevelData.Chunks.InsertAfter(SelectedChunk, new Chunk());
                    SelectedChunk++;
                    LevelData.ChunkBmpBits.Insert(SelectedChunk, new BitmapBits[2]);
                    LevelData.ChunkBmps.Insert(SelectedChunk, new Bitmap[2]);
                    LevelData.ChunkColBmpBits.Insert(SelectedChunk, new BitmapBits[2]);
                    LevelData.ChunkColBmps.Insert(SelectedChunk, new Bitmap[2]);
                    LevelData.CompChunkBmps.Insert(SelectedChunk, null);
                    LevelData.CompChunkBmpBits.Insert(SelectedChunk, null);
                    for (int y = 0; y < LevelData.FGLayout.GetLength(1); y++)
                        for (int x = 0; x < LevelData.FGLayout.GetLength(0); x++)
                            if (LevelData.FGLayout[x, y] >= SelectedChunk)
                                LevelData.FGLayout[x, y]++;
                    for (int y = 0; y < LevelData.BGLayout.GetLength(1); y++)
                        for (int x = 0; x < LevelData.BGLayout.GetLength(0); x++)
                            if (LevelData.BGLayout[x, y] >= SelectedChunk)
                                LevelData.BGLayout[x, y]++;
                    LevelData.RedrawChunk(SelectedChunk);
                    ChunkSelector.SelectedIndex = SelectedChunk;
                    break;
                case 4: // Blocks
                    LevelData.Blocks.InsertAfter(SelectedBlock, new Block());
                    SelectedBlock++;
                    LevelData.BlockBmps.Insert(SelectedBlock, new Bitmap[2]);
                    LevelData.BlockBmpBits.Insert(SelectedBlock, new BitmapBits[2]);
                    LevelData.CompBlockBmps.Insert(SelectedBlock, null);
                    LevelData.CompBlockBmpBits.Insert(SelectedBlock, null);
                    LevelData.ColInds1.Insert(SelectedBlock, 0);
                    if (LevelData.Game.EngineVersion == EngineVersion.S2 || LevelData.Game.EngineVersion == EngineVersion.S2NA || LevelData.Game.EngineVersion == EngineVersion.S3K || LevelData.Game.EngineVersion == EngineVersion.SKC)
                        if (!Object.ReferenceEquals(LevelData.ColInds1, LevelData.ColInds2))
                            LevelData.ColInds2.Insert(SelectedBlock, 0);
                    for (int i = 0; i < LevelData.Chunks.Count; i++)
                        for (int y = 0; y < LevelData.chunksz / 16; y++)
                            for (int x = 0; x < LevelData.chunksz / 16; x++)
                                if (LevelData.Chunks[i].blocks[x, y].Block >= SelectedBlock)
                                    LevelData.Chunks[i].blocks[x, y].Block++;
                    LevelData.RedrawBlock(SelectedBlock, false);
                    BlockSelector.SelectedIndex = SelectedBlock;
                    break;
                case 5: // Tiles
                    LevelData.Tiles.InsertAfter(SelectedTile, new byte[32]);
                    SelectedTile++;
                    LevelData.UpdateTileArray();
                    TileSelector.Images.Insert(SelectedTile, LevelData.TileToBmp4bpp(LevelData.Tiles[SelectedTile], 0, SelectedColor.Y));
                    for (int i = 0; i < LevelData.Blocks.Count; i++)
                        for (int y = 0; y < 2; y++)
                            for (int x = 0; x < 2; x++)
                                if (LevelData.Blocks[i].tiles[x, y].Tile >= SelectedTile)
                                    LevelData.Blocks[i].tiles[x, y].Tile++;
                    TileSelector.SelectedIndex = SelectedTile;
                    break;
            }
        }

        private void deleteTilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            switch (tabControl1.SelectedIndex)
            {
                case 3: // Chunks
                    LevelData.Chunks.RemoveAt(SelectedChunk);
                    LevelData.ChunkBmpBits.RemoveAt(SelectedChunk);
                    LevelData.ChunkBmps.RemoveAt(SelectedChunk);
                    LevelData.ChunkColBmpBits.RemoveAt(SelectedChunk);
                    LevelData.ChunkColBmps.RemoveAt(SelectedChunk);
                    LevelData.CompChunkBmps.RemoveAt(SelectedChunk);
                    LevelData.CompChunkBmpBits.RemoveAt(SelectedChunk);
                    SelectedChunk = (byte)Math.Min(SelectedChunk, LevelData.Chunks.Count - 1);
                    for (int y = 0; y < LevelData.FGLayout.GetLength(1); y++)
                        for (int x = 0; x < LevelData.FGLayout.GetLength(0); x++)
                            if (LevelData.FGLayout[x, y] > SelectedChunk)
                                LevelData.FGLayout[x, y]--;
                    for (int y = 0; y < LevelData.BGLayout.GetLength(1); y++)
                        for (int x = 0; x < LevelData.BGLayout.GetLength(0); x++)
                            if (LevelData.BGLayout[x, y] > SelectedChunk)
                                LevelData.BGLayout[x, y]--;
                    ChunkSelector.SelectedIndex = Math.Min(ChunkSelector.SelectedIndex, LevelData.Chunks.Count - 1);
                    break;
                case 4: // Blocks
                    LevelData.Blocks.RemoveAt(SelectedBlock);
                    LevelData.BlockBmps.RemoveAt(SelectedBlock);
                    LevelData.BlockBmpBits.RemoveAt(SelectedBlock);
                    LevelData.CompBlockBmps.RemoveAt(SelectedBlock);
                    LevelData.CompBlockBmpBits.RemoveAt(SelectedBlock);
                    LevelData.ColInds1.RemoveAt(SelectedBlock);
                    if (LevelData.Game.EngineVersion == EngineVersion.S2 || LevelData.Game.EngineVersion == EngineVersion.S2NA || LevelData.Game.EngineVersion == EngineVersion.S3K || LevelData.Game.EngineVersion == EngineVersion.SKC)
                        if (!Object.ReferenceEquals(LevelData.ColInds1, LevelData.ColInds2))
                            LevelData.ColInds2.RemoveAt(SelectedBlock);
                    for (int i = 0; i < LevelData.Chunks.Count; i++)
                    {
                        bool dr = false;
                        for (int y = 0; y < LevelData.chunksz / 16; y++)
                            for (int x = 0; x < LevelData.chunksz / 16; x++)
                                if (LevelData.Chunks[i].blocks[x, y].Block == SelectedBlock)
                                    dr = true;
                                else if (LevelData.Chunks[i].blocks[x, y].Block > SelectedBlock)
                                    LevelData.Chunks[i].blocks[x, y].Block--;
                        if (dr)
                            LevelData.RedrawChunk(i);
                    }
                    BlockSelector.SelectedIndex = Math.Min(BlockSelector.SelectedIndex, LevelData.Blocks.Count - 1);
                    break;
                case 5: // Tiles
                    LevelData.Tiles.RemoveAt(SelectedTile);
                    LevelData.UpdateTileArray();
                    TileSelector.Images.RemoveAt(SelectedTile);
                    for (int i = 0; i < LevelData.Blocks.Count; i++)
                    {
                        bool dr = false;
                        for (int y = 0; y < 2; y++)
                            for (int x = 0; x < 2; x++)
                                if (LevelData.Blocks[i].tiles[x, y].Tile == SelectedTile)
                                    dr = true;
                                else if (LevelData.Blocks[i].tiles[x, y].Tile > SelectedTile)
                                    LevelData.Blocks[i].tiles[x, y].Tile--;
                        if (dr)
                            LevelData.RedrawBlock(i, true);
                    }
                    TileSelector.SelectedIndex = Math.Min(TileSelector.SelectedIndex, TileSelector.Images.Count - 1);
                    break;
            }
        }

        private void importToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog opendlg = new OpenFileDialog())
            {
                opendlg.DefaultExt = "png";
                opendlg.Filter = "Image Files|*.bmp;*.png;*.jpg;*.gif";
                opendlg.RestoreDirectory = true;
                if (opendlg.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
                    ImportImage(new Bitmap(opendlg.FileName));
            }
        }

        private void ImportImage(Bitmap bmp)
        {
            int w = bmp.Width;
            int h = bmp.Height;
            int pal = 0;
            bool match = false;
            List<BitmapBits> tiles = new List<BitmapBits>();
            List<Block> blocks = new List<Block>();
            List<Chunk> chunks = new List<Chunk>();
            byte[] tile;
            int curtilecnt = LevelData.Tiles.Count;
            int curblkcnt = LevelData.Blocks.Count;
            switch (tabControl1.SelectedIndex)
            {
                case 3: // Chunks
                    for (int cy = 0; cy < h / LevelData.chunksz; cy++)
                        for (int cx = 0; cx < w / LevelData.chunksz; cx++)
                        {
                            Chunk cnk = new Chunk();
                            for (int by = 0; by < LevelData.chunksz / 16; by++)
                                for (int bx = 0; bx < LevelData.chunksz / 16; bx++)
                                {
                                    Block blk = new Block();
                                    for (int y = 0; y < 2; y++)
                                        for (int x = 0; x < 2; x++)
                                        {
                                            tile = LevelData.BmpToTile(bmp.Clone(new Rectangle((cx * 16) + (bx * 16) + (x * 8), (cy * 16) + (by * 16) + (y * 8), 8, 8), bmp.PixelFormat), out pal);
                                            blk.tiles[x, y].Palette = (byte)pal;
                                            BitmapBits bits = BitmapBits.FromTile(tile, 0);
                                            match = false;
                                            for (int i = 0; i < tiles.Count; i++)
                                            {
                                                if (tiles[i].Equals(bits))
                                                {
                                                    match = true;
                                                    blk.tiles[x, y].Tile = (ushort)(i + curtilecnt);
                                                    break;
                                                }
                                                BitmapBits flip = new BitmapBits(bits);
                                                flip.Flip(true, false);
                                                if (tiles[i].Equals(flip))
                                                {
                                                    match = true;
                                                    blk.tiles[x, y].Tile = (ushort)(i + curtilecnt);
                                                    blk.tiles[x, y].XFlip = true;
                                                    break;
                                                }
                                                flip = new BitmapBits(bits);
                                                flip.Flip(false, true);
                                                if (tiles[i].Equals(flip))
                                                {
                                                    match = true;
                                                    blk.tiles[x, y].Tile = (ushort)(i + curtilecnt);
                                                    blk.tiles[x, y].YFlip = true;
                                                    break;
                                                }
                                                flip = new BitmapBits(bits);
                                                flip.Flip(true, true);
                                                if (tiles[i].Equals(flip))
                                                {
                                                    match = true;
                                                    blk.tiles[x, y].Tile = (ushort)(i + curtilecnt);
                                                    blk.tiles[x, y].XFlip = true;
                                                    blk.tiles[x, y].YFlip = true;
                                                    break;
                                                }
                                            }
                                            if (match) continue;
                                            tiles.Add(bits);
                                            LevelData.Tiles.Add(tile);
                                            SelectedTile = LevelData.Tiles.Count - 1;
                                            blk.tiles[x, y].Tile = (ushort)SelectedTile;
                                            LevelData.UpdateTileArray();
                                            TileSelector.Images.Add(LevelData.TileToBmp4bpp(LevelData.Tiles[SelectedTile], 0, SelectedColor.Y));
                                        }
                                    match = false;
                                    for (int i = 0; i < blocks.Count; i++)
                                    {
                                        if (blk.Equals(blocks[i]))
                                        {
                                            match = true;
                                            cnk.blocks[bx, by].Block = (ushort)i;
                                            break;
                                        }
                                    }
                                    if (match) continue;
                                    blocks.Add(blk);
                                    LevelData.Blocks.Add(blk);
                                    LevelData.ColInds1.Add(0);
                                    if (LevelData.Game.EngineVersion == EngineVersion.S2 || LevelData.Game.EngineVersion == EngineVersion.S2NA || LevelData.Game.EngineVersion == EngineVersion.S3K || LevelData.Game.EngineVersion == EngineVersion.SKC)
                                        if (!Object.ReferenceEquals(LevelData.ColInds1, LevelData.ColInds2))
                                            LevelData.ColInds2.Add(0);
                                    SelectedBlock = LevelData.Blocks.Count - 1;
                                    LevelData.BlockBmps.Add(new Bitmap[2]);
                                    LevelData.BlockBmpBits.Add(new BitmapBits[2]);
                                    LevelData.CompBlockBmps.Add(null);
                                    LevelData.CompBlockBmpBits.Add(null);
                                    LevelData.RedrawBlock(SelectedBlock, false);
                                    cnk.blocks[bx, by].Block = (ushort)SelectedBlock;
                                }
                            match = false;
                            for (int i = 0; i < chunks.Count; i++)
                            {
                                if (cnk.Equals(chunks[i]))
                                {
                                    match = true;
                                    break;
                                }
                            }
                            if (match) continue;
                            chunks.Add(cnk);
                            LevelData.Chunks.Add(cnk);
                            SelectedChunk = (byte)(LevelData.Chunks.Count - 1);
                            LevelData.ChunkBmpBits.Add(new BitmapBits[2]);
                            LevelData.ChunkBmps.Add(new Bitmap[2]);
                            LevelData.ChunkColBmpBits.Add(new BitmapBits[2]);
                            LevelData.ChunkColBmps.Add(new Bitmap[2]);
                            LevelData.CompChunkBmps.Add(null);
                            LevelData.CompChunkBmpBits.Add(null);
                            LevelData.RedrawChunk(SelectedChunk);
                        }
                    TileSelector.SelectedIndex = SelectedTile;
                    BlockSelector.SelectedIndex = SelectedBlock;
                    ChunkSelector.SelectedIndex = SelectedChunk;
                    break;
                case 4: // Blocks
                    for (int by = 0; by < h / 16; by++)
                        for (int bx = 0; bx < w / 16; bx++)
                        {
                            Block blk = new Block();
                            for (int y = 0; y < 2; y++)
                                for (int x = 0; x < 2; x++)
                                {
                                    tile = LevelData.BmpToTile(bmp.Clone(new Rectangle((bx * 16) + (x * 8), (by * 16) + (y * 8), 8, 8), bmp.PixelFormat), out pal);
                                    blk.tiles[x, y].Palette = (byte)pal;
                                    BitmapBits bits = BitmapBits.FromTile(tile, 0);
                                    match = false;
                                    for (int i = 0; i < tiles.Count; i++)
                                    {
                                        if (tiles[i].Equals(bits))
                                        {
                                            match = true;
                                            blk.tiles[x, y].Tile = (ushort)(i + curtilecnt);
                                            break;
                                        }
                                        BitmapBits flip = new BitmapBits(bits);
                                        flip.Flip(true, false);
                                        if (tiles[i].Equals(flip))
                                        {
                                            match = true;
                                            blk.tiles[x, y].Tile = (ushort)(i + curtilecnt);
                                            blk.tiles[x, y].XFlip = true;
                                            break;
                                        }
                                        flip = new BitmapBits(bits);
                                        flip.Flip(false, true);
                                        if (tiles[i].Equals(flip))
                                        {
                                            match = true;
                                            blk.tiles[x, y].Tile = (ushort)(i + curtilecnt);
                                            blk.tiles[x, y].YFlip = true;
                                            break;
                                        }
                                        flip = new BitmapBits(bits);
                                        flip.Flip(true, true);
                                        if (tiles[i].Equals(flip))
                                        {
                                            match = true;
                                            blk.tiles[x, y].Tile = (ushort)(i + curtilecnt);
                                            blk.tiles[x, y].XFlip = true;
                                            blk.tiles[x, y].YFlip = true;
                                            break;
                                        }
                                    }
                                    if (match) continue;
                                    tiles.Add(bits);
                                    LevelData.Tiles.Add(tile);
                                    SelectedTile = LevelData.Tiles.Count - 1;
                                    blk.tiles[x, y].Tile = (ushort)SelectedTile;
                                    LevelData.UpdateTileArray();
                                    TileSelector.Images.Add(LevelData.TileToBmp4bpp(LevelData.Tiles[SelectedTile], 0, SelectedColor.Y));
                                }
                            match = false;
                            for (int i = 0; i < blocks.Count; i++)
                                if (blk.Equals(blocks[i]))
                                {
                                    match = true;
                                    break;
                                }
                            if (match) continue;
                            blocks.Add(blk);
                            LevelData.Blocks.Add(blk);
                            LevelData.ColInds1.Add(0);
                            if (LevelData.Game.EngineVersion == EngineVersion.S2 || LevelData.Game.EngineVersion == EngineVersion.S2NA || LevelData.Game.EngineVersion == EngineVersion.S3K || LevelData.Game.EngineVersion == EngineVersion.SKC)
                                if (!Object.ReferenceEquals(LevelData.ColInds1, LevelData.ColInds2))
                                    LevelData.ColInds2.Add(0);
                            SelectedBlock = LevelData.Blocks.Count - 1;
                            LevelData.BlockBmps.Add(new Bitmap[2]);
                            LevelData.BlockBmpBits.Add(new BitmapBits[2]);
                            LevelData.CompBlockBmps.Add(null);
                            LevelData.CompBlockBmpBits.Add(null);
                            LevelData.RedrawBlock(SelectedBlock, false);
                        }
                    TileSelector.SelectedIndex = SelectedTile;
                    BlockSelector.SelectedIndex = SelectedBlock;
                    break;
                case 5: // Tiles
                    for (int y = 0; y < h / 8; y++)
                        for (int x = 0; x < w / 8; x++)
                        {
                            tile = LevelData.BmpToTile(bmp.Clone(new Rectangle(x * 8, y * 8, 8, 8), bmp.PixelFormat), out pal);
                            BitmapBits bits = BitmapBits.FromTile(tile, 0);
                            match = false;
                            for (int i = 0; i < tiles.Count; i++)
                            {
                                if (tiles[i].Equals(bits))
                                {
                                    match = true;
                                    break;
                                }
                                BitmapBits flip = new BitmapBits(bits);
                                flip.Flip(true, false);
                                if (tiles[i].Equals(flip))
                                {
                                    match = true;
                                    break;
                                }
                                flip = new BitmapBits(bits);
                                flip.Flip(false, true);
                                if (tiles[i].Equals(flip))
                                {
                                    match = true;
                                    break;
                                }
                                flip = new BitmapBits(bits);
                                flip.Flip(true, true);
                                if (tiles[i].Equals(flip))
                                {
                                    match = true;
                                    break;
                                }
                            }
                            if (match) continue;
                            tiles.Add(bits);
                            LevelData.Tiles.Add(tile);
                            SelectedTile = LevelData.Tiles.Count - 1;
                            LevelData.UpdateTileArray();
                            TileSelector.Images.Add(LevelData.TileToBmp4bpp(LevelData.Tiles[SelectedTile], 0, SelectedColor.Y));
                        }
                    TileSelector.SelectedIndex = SelectedTile;
                    break;
            }
            bmp.Dispose();
        }

        private int SelectedCol;
        private void CollisionSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (CollisionSelector.SelectedIndex > -1)
            {
                SelectedCol = CollisionSelector.SelectedIndex;
                ColAngle.Value = LevelData.Angles[SelectedCol];
                ColID.Text = SelectedCol.ToString("X2");
                ColPicture.Invalidate();
            }
        }

        private void ColPicture_Paint(object sender, PaintEventArgs e)
        {
            if (CollisionSelector.SelectedIndex == -1) return;
            e.Graphics.SetOptions();
            e.Graphics.DrawImage(LevelData.ColBmpBits[SelectedCol].Scale(4).ToBitmap(Color.Black, Color.White), 0, 0, 64, 64);
        }

        private void ColPicture_MouseDown(object sender, MouseEventArgs e)
        {
            if (CollisionSelector.SelectedIndex == -1) return;
            int x = e.X / 4;
            int y = e.Y / 4;
            switch (e.Button)
            {
                case MouseButtons.Left:
                    LevelData.ColArr1[SelectedCol][x] = (sbyte)(16 - y);
                    break;
                case MouseButtons.Right:
                    if (y == 16)
                        LevelData.ColArr1[SelectedCol][x] = 0;
                    else
                        LevelData.ColArr1[SelectedCol][x] = (sbyte)(-y - 1);
                    break;
            }
            LevelData.RedrawCol(SelectedCol, false);
            ColPicture.Invalidate();
            CollisionSelector.Images[SelectedCol] = LevelData.ColBmps[SelectedCol];
        }

        private void ColPicture_MouseMove(object sender, MouseEventArgs e)
        {
            if (CollisionSelector.SelectedIndex == -1) return;
            int x = e.X / 4;
            if (x < 0 | x > 15) return;
            int y = e.Y / 4;
            if (y < 0 | y > 16) return;
            switch (e.Button)
            {
                case MouseButtons.Left:
                    LevelData.ColArr1[SelectedCol][x] = (sbyte)(16 - y);
                    LevelData.RedrawCol(SelectedCol, false);
                    ColPicture.Invalidate();
                    CollisionSelector.Images[SelectedCol] = LevelData.ColBmps[SelectedCol];
                    break;
                case MouseButtons.Right:
                    if (y == 16)
                        LevelData.ColArr1[SelectedCol][x] = 0;
                    else
                        LevelData.ColArr1[SelectedCol][x] = (sbyte)(-y - 1);
                    LevelData.RedrawCol(SelectedCol, false);
                    ColPicture.Invalidate();
                    CollisionSelector.Images[SelectedCol] = LevelData.ColBmps[SelectedCol];
                    break;
            }
        }

        private void ColPicture_MouseUp(object sender, KeyEventArgs e)
        {
            if (CollisionSelector.SelectedIndex == -1) return;
            LevelData.RedrawCol(SelectedCol, true);
            ColPicture.Invalidate();
            CollisionSelector.Images[SelectedCol] = LevelData.ColBmps[SelectedCol];
        }

        private void ColAngle_ValueChanged(object sender, EventArgs e)
        {
            if (CollisionSelector.SelectedIndex == -1) return;
            LevelData.Angles[SelectedCol] = (byte)ColAngle.Value;
        }

        private void ColAngle_TextChanged(object sender, EventArgs e)
        {
            if (!loaded) return;
            byte value;
            if (byte.TryParse(ColAngle.Text, System.Globalization.NumberStyles.HexNumber, null, out value))
                ColAngle.Value = value;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!loaded) return;
            CollisionSelector sel = new CollisionSelector();
            sel.ShowDialog(this);
            BlockCollision1.Value = sel.Selection;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (!loaded) return;
            CollisionSelector sel = new CollisionSelector();
            sel.ShowDialog(this);
            BlockCollision2.Value = sel.Selection;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (TileSelector.SelectedIndex == -1) return;
            tile.Rotate(3);
            LevelData.Tiles[SelectedTile] = tile.ToTile();
            LevelData.Tiles[SelectedTile].CopyTo(LevelData.TileArray, SelectedTile * 32);
            for (int i = 0; i < LevelData.Blocks.Count; i++)
            {
                bool dr = false;
                for (int y = 0; y < 2; y++)
                    for (int x = 0; x < 2; x++)
                        if (LevelData.Blocks[i].tiles[x, y].Tile == SelectedTile)
                            dr = true;
                if (dr)
                    LevelData.RedrawBlock(i, true);
            }
            TileSelector.Images[SelectedTile] = LevelData.TileToBmp4bpp(LevelData.Tiles[SelectedTile], 0, SelectedColor.Y);
            TilePicture.Invalidate();
        }

        private void drawToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (DrawTileDialog dlg = new DrawTileDialog())
            {
                switch (tabControl1.SelectedIndex)
                {
                    case 3: // Chunks
                        dlg.tile = new BitmapBits(LevelData.chunksz, LevelData.chunksz);
                        break;
                    case 4: // Blocks
                        dlg.tile = new BitmapBits(16, 16);
                        break;
                    case 5: // Tiles
                        dlg.tile = new BitmapBits(8, 8);
                        break;
                }
                if (dlg.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
                    ImportImage(dlg.tile.ToBitmap(LevelData.BmpPal));
            }
        }

        private void TileList_KeyDown(object sender, KeyEventArgs e)
        {
            if (tabControl1.SelectedIndex > 2)
            {
                switch (e.KeyCode)
                {
                    case Keys.C:
                        if (e.Control)
                            copyTilesToolStripMenuItem_Click(sender, EventArgs.Empty);
                        break;
                    case Keys.Delete:
                        deleteTilesToolStripMenuItem_Click(sender, EventArgs.Empty);
                        break;
                    case Keys.Insert:
                        switch (tabControl1.SelectedIndex)
                        {
                            case 3: // Chunks
                                if (LevelData.Chunks.Count < 0x100)
                                    insertBeforeToolStripMenuItem_Click(sender, EventArgs.Empty);
                                break;
                            case 4: // Blocks
                                int blockmax = 0x400;
                                switch (LevelData.Game.EngineVersion)
                                {
                                    case EngineVersion.S2:
                                    case EngineVersion.S2NA:
                                        blockmax = 0x340;
                                        break;
                                    case EngineVersion.S3K:
                                    case EngineVersion.SKC:
                                        blockmax = 0x300;
                                        break;
                                }
                                if (LevelData.Blocks.Count < blockmax)
                                    insertBeforeToolStripMenuItem_Click(sender, EventArgs.Empty);
                                break;
                            case 5: // Tiles
                                if (LevelData.Tiles.Count < 0x800)
                                    insertBeforeToolStripMenuItem_Click(sender, EventArgs.Empty);
                                break;
                        }
                        break;
                    case Keys.V:
                        if (e.Control)
                            switch (tabControl1.SelectedIndex)
                            {
                                case 3: // Chunks
                                    if (Clipboard.GetDataObject().GetDataPresent(typeof(Chunk).AssemblyQualifiedName) & LevelData.Chunks.Count < 0x100)
                                        pasteAfterToolStripMenuItem_Click(sender, EventArgs.Empty);
                                    break;
                                case 4: // Blocks
                                    int blockmax = 0x400;
                                    switch (LevelData.Game.EngineVersion)
                                    {
                                        case EngineVersion.S2:
                                        case EngineVersion.S2NA:
                                            blockmax = 0x340;
                                            break;
                                        case EngineVersion.S3K:
                                        case EngineVersion.SKC:
                                            blockmax = 0x300;
                                            break;
                                    }
                                    if (Clipboard.GetDataObject().GetDataPresent(typeof(Block).AssemblyQualifiedName) & LevelData.Blocks.Count < blockmax)
                                        pasteAfterToolStripMenuItem_Click(sender, EventArgs.Empty);
                                    break;
                                case 5: // Tiles
                                    if (Clipboard.GetDataObject().GetDataPresent("SonLVLTile") & LevelData.Tiles.Count < 0x800)
                                        pasteAfterToolStripMenuItem_Click(sender, EventArgs.Empty);
                                    break;
                            }
                        break;
                    case Keys.X:
                        if (e.Control)
                            cutTilesToolStripMenuItem_Click(sender, EventArgs.Empty);
                        break;
                }
            }
        }

        private void selectAllObjectsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Entry[] items = new Entry[LevelData.Objects.Count];
            for (int i = 0; i < items.Length; i++)
                items[i] = LevelData.Objects[i];
            SelectedItems = new List<Entry>(items);
            SelectedObjectChanged();
        }

        private void selectAllRingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<Entry> items = new List<Entry>();
            switch (LevelData.Level.RingFormat)
            {
                case EngineVersion.S2:
                case EngineVersion.S2NA:
                case EngineVersion.S3K:
                case EngineVersion.SKC:
                    items = new List<Entry>(LevelData.Rings.Count);
                    for (int i = 0; i < items.Count; i++)
                        items.Add(LevelData.Rings[i]);
                    break;
                case EngineVersion.S1:
                case EngineVersion.SCD:
                case EngineVersion.SCDPC:
                    foreach (ObjectEntry item in LevelData.Objects)
                        if (item.ID == 0x25)
                            items.Add(item);
                    break;
            }
            SelectedItems = items;
            SelectedObjectChanged();
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            toolStripButton2.Checked = true;
            toolStripButton1.Checked = false;
            FGMode = EditingMode.Draw;
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            toolStripButton2.Checked = false;
            toolStripButton1.Checked = true;
            FGMode = EditingMode.Select;
        }

        private void cutToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            byte[,] layout;
            bool[,] loop;
            Rectangle selection;
            if (tabControl1.SelectedIndex == 2)
            {
                layout = LevelData.BGLayout;
                loop = LevelData.BGLoop;
                selection = BGSelection;
            }
            else
            {
                layout = LevelData.FGLayout;
                loop = LevelData.FGLoop;
                selection = FGSelection;
            }
            byte[,] layoutsection = new byte[selection.Width, selection.Height];
            bool[,] loopsection = loop == null ? null : new bool[selection.Width, selection.Height];
            for (int y = 0; y < selection.Height; y++)
                for (int x = 0; x < selection.Width; x++)
                {
                    layoutsection[x, y] = layout[x + selection.X, y + selection.Y];
                    layout[x + selection.X, y + selection.Y] = 0;
                    if (loop != null)
                    {
                        loopsection[x, y] = loop[x + selection.X, y + selection.Y];
                        loop[x + selection.X, y + selection.Y] = false;
                    }
                }
            List<Entry> objectselection = new List<Entry>();
            if (includeObjectsWithForegroundSelectionToolStripMenuItem.Checked & tabControl1.SelectedIndex == 1)
            {
                if (LevelData.Objects != null)
                    foreach (ObjectEntry item in LevelData.Objects)
                        if (item.Y >= selection.Top * LevelData.chunksz & item.Y < selection.Bottom * LevelData.chunksz
                            & item.X >= selection.Left * LevelData.chunksz & item.X < selection.Right * LevelData.chunksz)
                            objectselection.Add(item);
                if (LevelData.Rings != null)
                    foreach (RingEntry item in LevelData.Rings)
                        if (item.Y >= selection.Top * LevelData.chunksz & item.Y < selection.Bottom * LevelData.chunksz
                            & item.X >= selection.Left * LevelData.chunksz & item.X < selection.Right * LevelData.chunksz)
                            objectselection.Add(item);
                if (LevelData.Bumpers != null)
                    foreach (CNZBumperEntry item in LevelData.Bumpers)
                        if (item.Y >= selection.Top * LevelData.chunksz & item.Y < selection.Bottom * LevelData.chunksz
                            & item.X >= selection.Left * LevelData.chunksz & item.X < selection.Right * LevelData.chunksz)
                            objectselection.Add(item);
                foreach (Entry item in objectselection)
                {
                    if (item is ObjectEntry)
                        LevelData.Objects.Remove((ObjectEntry)item);
                    if (item is RingEntry)
                        LevelData.Rings.Remove((RingEntry)item);
                    if (item is CNZBumperEntry)
                        LevelData.Bumpers.Remove((CNZBumperEntry)item);
                    if (SelectedItems.Contains(item))
                        SelectedItems.Remove(item);
                }
                SelectedObjectChanged();
            }
            Clipboard.SetData(typeof(LayoutSection).AssemblyQualifiedName, new LayoutSection(layoutsection, loopsection, new Point(selection.X * LevelData.chunksz, selection.Y * LevelData.chunksz), objectselection));
            DrawLevel();
        }

        private void copyToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            byte[,] layout;
            bool[,] loop;
            Rectangle selection;
            if (tabControl1.SelectedIndex == 2)
            {
                layout = LevelData.BGLayout;
                loop = LevelData.BGLoop;
                selection = BGSelection;
            }
            else
            {
                layout = LevelData.FGLayout;
                loop = LevelData.FGLoop;
                selection = FGSelection;
            }
            byte[,] layoutsection = new byte[selection.Width, selection.Height];
            bool[,] loopsection = loop == null ? null : new bool[selection.Width, selection.Height];
            for (int y = 0; y < selection.Height; y++)
                for (int x = 0; x < selection.Width; x++)
                {
                    layoutsection[x, y] = layout[x + selection.X, y + selection.Y];
                    if (loop != null)
                        loopsection[x, y] = loop[x + selection.X, y + selection.Y];
                }
            List<Entry> objectselection = new List<Entry>();
            if (includeObjectsWithForegroundSelectionToolStripMenuItem.Checked & tabControl1.SelectedIndex == 1)
            {
                if (LevelData.Objects != null)
                    foreach (ObjectEntry item in LevelData.Objects)
                        if (item.Y >= selection.Top * LevelData.chunksz & item.Y < selection.Bottom * LevelData.chunksz
                            & item.X >= selection.Left * LevelData.chunksz & item.X < selection.Right * LevelData.chunksz)
                            objectselection.Add(item);
                if (LevelData.Rings != null)
                    foreach (RingEntry item in LevelData.Rings)
                        if (item.Y >= selection.Top * LevelData.chunksz & item.Y < selection.Bottom * LevelData.chunksz
                            & item.X >= selection.Left * LevelData.chunksz & item.X < selection.Right * LevelData.chunksz)
                            objectselection.Add(item);
                if (LevelData.Bumpers != null)
                    foreach (CNZBumperEntry item in LevelData.Bumpers)
                        if (item.Y >= selection.Top * LevelData.chunksz & item.Y < selection.Bottom * LevelData.chunksz
                            & item.X >= selection.Left * LevelData.chunksz & item.X < selection.Right * LevelData.chunksz)
                            objectselection.Add(item);
            }
            Clipboard.SetData(typeof(LayoutSection).AssemblyQualifiedName, new LayoutSection(layoutsection, loopsection, new Point(selection.X * LevelData.chunksz, selection.Y * LevelData.chunksz), objectselection));
        }

        private void pasteOnceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            byte[,] layout;
            bool[,] loop;
            if (tabControl1.SelectedIndex == 2)
            {
                layout = LevelData.BGLayout;
                loop = LevelData.BGLoop;
            }
            else
            {
                layout = LevelData.FGLayout;
                loop = LevelData.FGLoop;
            }
            LayoutSection section = (LayoutSection)Clipboard.GetData(typeof(LayoutSection).AssemblyQualifiedName);
            for (int y = 0; y < section.Layout.GetLength(1); y++)
                for (int x = 0; x < section.Layout.GetLength(0); x++)
                {
                    layout[x + menuLoc.X, y + menuLoc.Y] = section.Layout[x, y];
                    if (loop != null)
                    {
                        if (section.Loop == null)
                            loop[x + menuLoc.X, y + menuLoc.Y] = false;
                        else
                            loop[x + menuLoc.X, y + menuLoc.Y] = section.Loop[x, y];
                    }
                }
            if (includeObjectsWithForegroundSelectionToolStripMenuItem.Checked & tabControl1.SelectedIndex == 1)
            {
                Size off = new Size((menuLoc.X * LevelData.chunksz) - section.Position.X, (menuLoc.Y * LevelData.chunksz) - section.Position.Y);
                foreach (Entry item in section.Objects)
                {
                    item.X = (ushort)(item.X + off.Width);
                    item.Y = (ushort)(item.Y + off.Height);
                    if (item is ObjectEntry)
                        LevelData.Objects.Add((ObjectEntry)item);
                    else if (item is RingEntry)
                        LevelData.Rings.Add((RingEntry)item);
                    else if (item is CNZBumperEntry)
                        LevelData.Bumpers.Add((CNZBumperEntry)item);
                    item.UpdateSprite();
                }
                LevelData.Objects.Sort();
                LevelData.Rings.Sort();
                if (LevelData.Bumpers != null)
                    LevelData.Bumpers.Sort();
            }
            DrawLevel();
        }

        private void pasteRepeatingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            byte[,] layout;
            bool[,] loop;
            Rectangle selection;
            if (tabControl1.SelectedIndex == 2)
            {
                layout = LevelData.BGLayout;
                loop = LevelData.BGLoop;
                selection = BGSelection;
            }
            else
            {
                layout = LevelData.FGLayout;
                loop = LevelData.FGLoop;
                selection = FGSelection;
            }
            LayoutSection section = (LayoutSection)Clipboard.GetData(typeof(LayoutSection).AssemblyQualifiedName);
            int width = section.Layout.GetLength(0);
            int height = section.Layout.GetLength(1);
            for (int y = 0; y < selection.Height; y++)
                for (int x = 0; x < selection.Width; x++)
                {
                    layout[x + selection.X, y + selection.Y] = section.Layout[x % width, y % height];
                    if (loop != null)
                    {
                        if (section.Loop == null)
                            loop[x + selection.X, y + selection.Y] = false;
                        else
                            loop[x + selection.X, y + selection.Y] = section.Loop[x % width, y % height];
                    }
                }
            if (includeObjectsWithForegroundSelectionToolStripMenuItem.Checked & tabControl1.SelectedIndex == 1)
            {
                int w = (int)Math.Ceiling(selection.Width / (double)width);
                int h = (int)Math.Ceiling(selection.Height / (double)height);
                Point bottomright = new Point(selection.Right * LevelData.chunksz, selection.Bottom * LevelData.chunksz);
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        Size off = new Size(((selection.X + (x * width)) * LevelData.chunksz) - section.Position.X, ((selection.Y + (y * height)) * LevelData.chunksz) - section.Position.Y);
                        foreach (Entry item in section.Objects)
                        {
                            Entry it2 = (Entry)Activator.CreateInstance(item.GetType());
                            it2.FromBytes(item.GetBytes());
                            it2.X = (ushort)(it2.X + off.Width);
                            it2.Y = (ushort)(it2.Y + off.Height);
                            if (it2.X < bottomright.X & it2.Y < bottomright.Y)
                            {
                                if (it2 is ObjectEntry)
                                    LevelData.Objects.Add((ObjectEntry)it2);
                                else if (it2 is RingEntry)
                                    LevelData.Rings.Add((RingEntry)it2);
                                else if (it2 is CNZBumperEntry)
                                    LevelData.Bumpers.Add((CNZBumperEntry)it2);
                                it2.UpdateSprite();
                            }
                        }
                    }
                LevelData.Objects.Sort();
                LevelData.Rings.Sort();
                if (LevelData.Bumpers != null)
                    LevelData.Bumpers.Sort();
            }
            DrawLevel();
        }

        private void deleteToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            byte[,] layout;
            bool[,] loop;
            Rectangle selection;
            if (tabControl1.SelectedIndex == 2)
            {
                layout = LevelData.BGLayout;
                loop = LevelData.BGLoop;
                selection = BGSelection;
            }
            else
            {
                layout = LevelData.FGLayout;
                loop = LevelData.FGLoop;
                selection = FGSelection;
            }
            for (int y = selection.Top; y < selection.Bottom; y++)
                for (int x = selection.Left; x < selection.Right; x++)
                {
                    layout[x, y] = 0;
                    if (loop != null)
                        loop[x, y] = false;
                }
            if (includeObjectsWithForegroundSelectionToolStripMenuItem.Checked & tabControl1.SelectedIndex == 1)
            {
                List<Entry> objectselection = new List<Entry>();
                if (LevelData.Objects != null)
                    foreach (ObjectEntry item in LevelData.Objects)
                        if (item.Y >= selection.Top * LevelData.chunksz & item.Y < selection.Bottom * LevelData.chunksz
                            & item.X >= selection.Left * LevelData.chunksz & item.X < selection.Right * LevelData.chunksz)
                            objectselection.Add(item);
                if (LevelData.Rings != null)
                    foreach (RingEntry item in LevelData.Rings)
                        if (item.Y >= selection.Top * LevelData.chunksz & item.Y < selection.Bottom * LevelData.chunksz
                            & item.X >= selection.Left * LevelData.chunksz & item.X < selection.Right * LevelData.chunksz)
                            objectselection.Add(item);
                if (LevelData.Bumpers != null)
                    foreach (CNZBumperEntry item in LevelData.Bumpers)
                        if (item.Y >= selection.Top * LevelData.chunksz & item.Y < selection.Bottom * LevelData.chunksz
                            & item.X >= selection.Left * LevelData.chunksz & item.X < selection.Right * LevelData.chunksz)
                            objectselection.Add(item);
                foreach (Entry item in objectselection)
                {
                    if (item is ObjectEntry)
                        LevelData.Objects.Remove((ObjectEntry)item);
                    if (item is RingEntry)
                        LevelData.Rings.Remove((RingEntry)item);
                    if (item is CNZBumperEntry)
                        LevelData.Bumpers.Remove((CNZBumperEntry)item);
                    if (SelectedItems.Contains(item))
                        SelectedItems.Remove(item);
                }
                SelectedObjectChanged();
            }
            DrawLevel();
        }

        private void fillToolStripMenuItem_Click(object sender, EventArgs e)
        {
            byte[,] layout;
            bool[,] loop;
            Rectangle selection;
            if (tabControl1.SelectedIndex == 2)
            {
                layout = LevelData.BGLayout;
                loop = LevelData.BGLoop;
                selection = BGSelection;
            }
            else
            {
                layout = LevelData.FGLayout;
                loop = LevelData.FGLoop;
                selection = FGSelection;
            }
            for (int y = selection.Top; y < selection.Bottom; y++)
                for (int x = selection.Left; x < selection.Right; x++)
                {
                    layout[x, y] = SelectedChunk;
                    if (loop != null)
                        loop[x, y] = false;
                }
            DrawLevel();
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            toolStripButton3.Checked = true;
            toolStripButton4.Checked = false;
            BGMode = EditingMode.Draw;
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            toolStripButton3.Checked = false;
            toolStripButton4.Checked = true;
            BGMode = EditingMode.Select;
        }

        private void resizeLevelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (ResizeLevelDialog dg = new ResizeLevelDialog(tabControl1.SelectedIndex != 2))
            {
                dg.levelWidth.Minimum = 1;
                dg.levelWidth.Minimum = 1;
                Size? maxsize = LevelData.LevelSizeLimit();
                if (maxsize.HasValue)
                {
                    dg.levelWidth.Maximum = maxsize.Value.Width;
                    dg.levelHeight.Maximum = maxsize.Value.Height;
                    Size cursize;
                    if (tabControl1.SelectedIndex == 2)
                        cursize = LevelData.BGSize;
                    else
                        cursize = LevelData.FGSize;
                    dg.levelWidth.Value = cursize.Width;
                    dg.levelHeight.Value = cursize.Height;
                    if (dg.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
                    {
                        if (tabControl1.SelectedIndex == 2)
                            LevelData.ResizeBG((int)dg.levelWidth.Value, (int)dg.levelHeight.Value);
                        else
                            LevelData.ResizeFG((int)dg.levelWidth.Value, (int)dg.levelHeight.Value);
                        loaded = false;
                        UpdateScrollBars();
                        loaded = true;
                        DrawLevel();
                    }
                    else
                        MessageBox.Show("The current game does not allow you to resize levels!");
                }
            }
        }

        private void objectTypeList_ItemDrag(object sender, ItemDragEventArgs e)
        {
            objectTypeList.DoDragDrop(new DataObject("HocusLevel.ObjectDrop", ((ListViewItem)e.Item).Tag), DragDropEffects.Copy);
        }

        private void panel1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("HocusLevel.ObjectDrop"))
            {
                e.Effect = DragDropEffects.All;
                dragdrop = true;
                dragobj = (byte)e.Data.GetData("HocusLevel.ObjectDrop");
                dragpoint = panel1.PointToClient(new Point(e.X, e.Y));
                DrawLevel();
            }
            else
                dragdrop = false;
        }

        private void panel1_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("HocusLevel.ObjectDrop"))
            {
                e.Effect = DragDropEffects.All;
                dragdrop = true;
                dragobj = (byte)e.Data.GetData("HocusLevel.ObjectDrop");
                dragpoint = panel1.PointToClient(new Point(e.X, e.Y));
                DrawLevel();
            }
            else
                dragdrop = false;
        }

        private void panel1_DragLeave(object sender, EventArgs e)
        {
            dragdrop = false;
            DrawLevel();
        }

        private void panel1_DragDrop(object sender, DragEventArgs e)
        {
            dragdrop = false;
            if (e.Data.GetDataPresent("HocusLevel.ObjectDrop"))
            {
                Point clientPoint = panel1.PointToClient(new Point(e.X, e.Y));
                ObjectEntry obj = LevelData.CreateObject((byte)e.Data.GetData("HocusLevel.ObjectDrop"));
                obj.X = (ushort)(clientPoint.X - hScrollBar1.Value);
                obj.Y = (ushort)(clientPoint.Y - vScrollBar1.Value);
                obj.UpdateSprite();
                LevelData.Objects.Add(obj);
                LevelData.Objects.Sort();
                SelectedItems = new List<Entry>(1) { obj };
                SelectedObjectChanged();
                DrawLevel();
            }
        }

        private void insertLayoutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (InsertDeleteDialog dlg = new InsertDeleteDialog())
            {
                dlg.Text = "Insert";
                dlg.moveObjects.Visible = dlg.moveObjects.Checked = tabControl1.SelectedIndex == 1;
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                Rectangle selection;
                if (tabControl1.SelectedIndex == 2)
                    selection = BGSelection;
                else
                    selection = FGSelection;
                Size? maxsize = LevelData.LevelSizeLimit();
                if (dlg.shiftH.Checked)
                {
                    byte[,] layout;
                    bool[,] loop;
                    if (tabControl1.SelectedIndex == 2)
                    {
                        if (maxsize.HasValue && maxsize.Value.Width > LevelData.BGWidth)
                            LevelData.ResizeBG(Math.Min(maxsize.Value.Width, LevelData.BGWidth + selection.Width), LevelData.BGHeight);
                        layout = LevelData.BGLayout;
                        loop = LevelData.BGLoop;
                    }
                    else
                    {
                        if (maxsize.HasValue && maxsize.Value.Width > LevelData.FGWidth)
                            LevelData.ResizeFG(Math.Min(maxsize.Value.Width, LevelData.FGWidth + selection.Width), LevelData.FGHeight);
                        layout = LevelData.FGLayout;
                        loop = LevelData.FGLoop;
                    }
                    for (int y = selection.Top; y < selection.Bottom; y++)
                        for (int x = layout.GetLength(0) - selection.Width - 1; x >= selection.Left; x--)
                        {
                            layout[x + selection.Width, y] = layout[x, y];
                            if (loop != null)
                                loop[x + selection.Width, y] = loop[x, y];
                        }
                    for (int y = selection.Top; y < selection.Bottom; y++)
                        for (int x = selection.Left; x < selection.Right; x++)
                        {
                            layout[x, y] = 0;
                            if (loop != null)
                                loop[x, y] = false;
                        }
                    if (dlg.moveObjects.Checked)
                    {
                        if (LevelData.Objects != null)
                            foreach (ObjectEntry item in LevelData.Objects)
                                if (item.Y >= selection.Top * LevelData.chunksz & item.Y < selection.Bottom * LevelData.chunksz & item.X >= selection.Left * LevelData.chunksz)
                                {
                                    item.X += (ushort)(selection.Width * LevelData.chunksz);
                                    item.UpdateSprite();
                                }
                        if (LevelData.Rings != null)
                            foreach (RingEntry item in LevelData.Rings)
                                if (item.Y >= selection.Top * LevelData.chunksz & item.Y < selection.Bottom * LevelData.chunksz & item.X >= selection.Left * LevelData.chunksz)
                                {
                                    item.X += (ushort)(selection.Width * LevelData.chunksz);
                                    item.UpdateSprite();
                                }
                        if (LevelData.Bumpers != null)
                            foreach (CNZBumperEntry item in LevelData.Bumpers)
                                if (item.Y >= selection.Top * LevelData.chunksz & item.Y < selection.Bottom * LevelData.chunksz & item.X >= selection.Left * LevelData.chunksz)
                                {
                                    item.X += (ushort)(selection.Width * LevelData.chunksz);
                                    item.UpdateSprite();
                                }
                        if (LevelData.StartPositions != null)
                            foreach (StartPositionEntry item in LevelData.StartPositions)
                                if (item.Y >= selection.Top * LevelData.chunksz & item.Y < selection.Bottom * LevelData.chunksz & item.X >= selection.Left * LevelData.chunksz)
                                {
                                    item.X += (ushort)(selection.Width * LevelData.chunksz);
                                    item.UpdateSprite();
                                }
                    }
                }
                else if (dlg.shiftV.Checked)
                {
                    byte[,] layout;
                    bool[,] loop;
                    if (tabControl1.SelectedIndex == 2)
                    {
                        if (maxsize.HasValue && maxsize.Value.Height > LevelData.BGHeight)
                            LevelData.ResizeBG(LevelData.BGWidth, Math.Min(maxsize.Value.Height, LevelData.BGHeight + selection.Height));
                        layout = LevelData.BGLayout;
                        loop = LevelData.BGLoop;
                    }
                    else
                    {
                        if (maxsize.HasValue && maxsize.Value.Height > LevelData.FGHeight)
                            LevelData.ResizeFG(LevelData.FGWidth, Math.Min(maxsize.Value.Height, LevelData.FGHeight + selection.Height));
                        layout = LevelData.FGLayout;
                        loop = LevelData.FGLoop;
                    }
                    for (int x = selection.Left; x < selection.Right; x++)
                        for (int y = layout.GetLength(1) - selection.Height - 1; y >= selection.Top; y--)
                        {
                            layout[x, y + selection.Height] = layout[x, y];
                            if (loop != null)
                                loop[x, y + selection.Height] = loop[x, y];
                        }
                    for (int x = selection.Left; x < selection.Right; x++)
                        for (int y = selection.Top; y < selection.Bottom; y++)
                        {
                            layout[x, y] = 0;
                            if (loop != null)
                                loop[x, y] = false;
                        }
                    if (dlg.moveObjects.Checked)
                    {
                        if (LevelData.Objects != null)
                            foreach (ObjectEntry item in LevelData.Objects)
                                if (item.X >= selection.Left * LevelData.chunksz & item.X < selection.Right * LevelData.chunksz & item.Y >= selection.Top * LevelData.chunksz)
                                {
                                    item.Y += (ushort)(selection.Height * LevelData.chunksz);
                                    item.UpdateSprite();
                                }
                        if (LevelData.Rings != null)
                            foreach (RingEntry item in LevelData.Rings)
                                if (item.X >= selection.Left * LevelData.chunksz & item.X < selection.Right * LevelData.chunksz & item.Y >= selection.Top * LevelData.chunksz)
                                {
                                    item.Y += (ushort)(selection.Height * LevelData.chunksz);
                                    item.UpdateSprite();
                                }
                        if (LevelData.Bumpers != null)
                            foreach (CNZBumperEntry item in LevelData.Bumpers)
                                if (item.X >= selection.Left * LevelData.chunksz & item.X < selection.Right * LevelData.chunksz & item.Y >= selection.Top * LevelData.chunksz)
                                {
                                    item.Y += (ushort)(selection.Height * LevelData.chunksz);
                                    item.UpdateSprite();
                                }
                        if (LevelData.StartPositions != null)
                            foreach (StartPositionEntry item in LevelData.StartPositions)
                                if (item.X >= selection.Left * LevelData.chunksz & item.X < selection.Right * LevelData.chunksz & item.Y >= selection.Top * LevelData.chunksz)
                                {
                                    item.Y += (ushort)(selection.Height * LevelData.chunksz);
                                    item.UpdateSprite();
                                }
                    }
                }
                else if (dlg.entireRow.Checked)
                {
                    byte[,] layout;
                    bool[,] loop;
                    if (tabControl1.SelectedIndex == 2)
                    {
                        if (maxsize.HasValue && maxsize.Value.Height > LevelData.BGHeight)
                            LevelData.ResizeBG(LevelData.BGWidth, Math.Min(maxsize.Value.Height, LevelData.BGHeight + selection.Height));
                        layout = LevelData.BGLayout;
                        loop = LevelData.BGLoop;
                    }
                    else
                    {
                        if (maxsize.HasValue && maxsize.Value.Height > LevelData.FGHeight)
                            LevelData.ResizeFG(LevelData.FGWidth, Math.Min(maxsize.Value.Height, LevelData.FGHeight + selection.Height));
                        layout = LevelData.FGLayout;
                        loop = LevelData.FGLoop;
                    }
                    for (int x = 0; x < layout.GetLength(0); x++)
                        for (int y = layout.GetLength(1) - selection.Height - 1; y >= selection.Top; y--)
                        {
                            layout[x, y + selection.Height] = layout[x, y];
                            if (loop != null)
                                loop[x, y + selection.Height] = loop[x, y];
                        }
                    for (int x = 0; x < layout.GetLength(0); x++)
                        for (int y = selection.Top; y < selection.Bottom; y++)
                        {
                            layout[x, y] = 0;
                            if (loop != null)
                                loop[x, y] = false;
                        }
                    if (dlg.moveObjects.Checked)
                    {
                        if (LevelData.Objects != null)
                            foreach (ObjectEntry item in LevelData.Objects)
                                if (item.Y >= selection.Top * LevelData.chunksz)
                                {
                                    item.Y += (ushort)(selection.Height * LevelData.chunksz);
                                    item.UpdateSprite();
                                }
                        if (LevelData.Rings != null)
                            foreach (RingEntry item in LevelData.Rings)
                                if (item.Y >= selection.Top * LevelData.chunksz)
                                {
                                    item.Y += (ushort)(selection.Height * LevelData.chunksz);
                                    item.UpdateSprite();
                                }
                        if (LevelData.Bumpers != null)
                            foreach (CNZBumperEntry item in LevelData.Bumpers)
                                if (item.Y >= selection.Top * LevelData.chunksz)
                                {
                                    item.Y += (ushort)(selection.Height * LevelData.chunksz);
                                    item.UpdateSprite();
                                }
                        if (LevelData.StartPositions != null)
                            foreach (StartPositionEntry item in LevelData.StartPositions)
                                if (item.Y >= selection.Top * LevelData.chunksz)
                                {
                                    item.Y += (ushort)(selection.Height * LevelData.chunksz);
                                    item.UpdateSprite();
                                }
                    }
                }
                else if (dlg.entireColumn.Checked)
                {
                    byte[,] layout;
                    bool[,] loop;
                    if (tabControl1.SelectedIndex == 2)
                    {
                        if (maxsize.HasValue && maxsize.Value.Width > LevelData.BGWidth)
                            LevelData.ResizeBG(Math.Min(maxsize.Value.Width, LevelData.BGWidth + selection.Width), LevelData.BGHeight);
                        layout = LevelData.BGLayout;
                        loop = LevelData.BGLoop;
                    }
                    else
                    {
                        if (maxsize.HasValue && maxsize.Value.Width > LevelData.FGWidth)
                            LevelData.ResizeFG(Math.Min(maxsize.Value.Width, LevelData.FGWidth + selection.Width), LevelData.FGHeight);
                        layout = LevelData.FGLayout;
                        loop = LevelData.FGLoop;
                    }
                    for (int y = 0; y < layout.GetLength(1); y++)
                        for (int x = layout.GetLength(0) - selection.Width - 1; x >= selection.Left; x--)
                        {
                            layout[x + selection.Width, y] = layout[x, y];
                            if (loop != null)
                                loop[x + selection.Width, y] = loop[x, y];
                        }
                    for (int y = 0; y < layout.GetLength(1); y++)
                        for (int x = selection.Left; x < selection.Right; x++)
                        {
                            layout[x, y] = 0;
                            if (loop != null)
                                loop[x, y] = false;
                        }
                    if (dlg.moveObjects.Checked)
                    {
                        if (LevelData.Objects != null)
                            foreach (ObjectEntry item in LevelData.Objects)
                                if (item.X >= selection.Left * LevelData.chunksz)
                                {
                                    item.X += (ushort)(selection.Width * LevelData.chunksz);
                                    item.UpdateSprite();
                                }
                        if (LevelData.Rings != null)
                            foreach (RingEntry item in LevelData.Rings)
                                if (item.X >= selection.Left * LevelData.chunksz)
                                {
                                    item.X += (ushort)(selection.Width * LevelData.chunksz);
                                    item.UpdateSprite();
                                }
                        if (LevelData.Bumpers != null)
                            foreach (CNZBumperEntry item in LevelData.Bumpers)
                                if (item.X >= selection.Left * LevelData.chunksz)
                                {
                                    item.X += (ushort)(selection.Width * LevelData.chunksz);
                                    item.UpdateSprite();
                                }
                        if (LevelData.StartPositions != null)
                            foreach (StartPositionEntry item in LevelData.StartPositions)
                                if (item.X >= selection.Left * LevelData.chunksz)
                                {
                                    item.X += (ushort)(selection.Width * LevelData.chunksz);
                                    item.UpdateSprite();
                                }
                    }
                }
            }
        }

        private void deleteLayoutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (InsertDeleteDialog dlg = new InsertDeleteDialog())
            {
                dlg.Text = "Delete";
                dlg.shiftH.Text = "Shift cells left";
                dlg.shiftV.Text = "Shift cells up";
                dlg.moveObjects.Visible = dlg.moveObjects.Checked = tabControl1.SelectedIndex == 1;
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                Rectangle selection;
                if (tabControl1.SelectedIndex == 2)
                    selection = BGSelection;
                else
                    selection = FGSelection;
                Size? maxsize = LevelData.LevelSizeLimit();
                if (dlg.shiftH.Checked)
                {
                    byte[,] layout;
                    bool[,] loop;
                    if (tabControl1.SelectedIndex == 2)
                    {
                        layout = LevelData.BGLayout;
                        loop = LevelData.BGLoop;
                    }
                    else
                    {
                        layout = LevelData.FGLayout;
                        loop = LevelData.FGLoop;
                    }
                    for (int y = selection.Top; y < selection.Bottom; y++)
                        for (int x = selection.Left; x < layout.GetLength(0) - selection.Width; x++)
                        {
                            layout[x, y] = layout[x + selection.Width, y];
                            if (loop != null)
                                loop[x, y] = loop[x + selection.Width, y];
                        }
                    for (int y = selection.Top; y < selection.Bottom; y++)
                        for (int x = layout.GetLength(0) - selection.Width; x < layout.GetLength(0); x++)
                        {
                            layout[x, y] = 0;
                            if (loop != null)
                                loop[x, y] = false;
                        }
                    if (dlg.moveObjects.Checked)
                    {
                        if (LevelData.Objects != null)
                            foreach (ObjectEntry item in LevelData.Objects)
                                if (item.Y >= selection.Top * LevelData.chunksz & item.Y < selection.Bottom * LevelData.chunksz & item.X >= selection.Right * LevelData.chunksz)
                                {
                                    item.X -= (ushort)(selection.Width * LevelData.chunksz);
                                    item.UpdateSprite();
                                }
                        if (LevelData.Rings != null)
                            foreach (RingEntry item in LevelData.Rings)
                                if (item.Y >= selection.Top * LevelData.chunksz & item.Y < selection.Bottom * LevelData.chunksz & item.X >= selection.Right * LevelData.chunksz)
                                {
                                    item.X -= (ushort)(selection.Width * LevelData.chunksz);
                                    item.UpdateSprite();
                                }
                        if (LevelData.Bumpers != null)
                            foreach (CNZBumperEntry item in LevelData.Bumpers)
                                if (item.Y >= selection.Top * LevelData.chunksz & item.Y < selection.Bottom * LevelData.chunksz & item.X >= selection.Right * LevelData.chunksz)
                                {
                                    item.X -= (ushort)(selection.Width * LevelData.chunksz);
                                    item.UpdateSprite();
                                }
                        if (LevelData.StartPositions != null)
                            foreach (StartPositionEntry item in LevelData.StartPositions)
                                if (item.Y >= selection.Top * LevelData.chunksz & item.Y < selection.Bottom * LevelData.chunksz & item.X >= selection.Right * LevelData.chunksz)
                                {
                                    item.X -= (ushort)(selection.Width * LevelData.chunksz);
                                    item.UpdateSprite();
                                }
                    }
                }
                else if (dlg.shiftV.Checked)
                {
                    byte[,] layout;
                    bool[,] loop;
                    if (tabControl1.SelectedIndex == 2)
                    {
                        layout = LevelData.BGLayout;
                        loop = LevelData.BGLoop;
                    }
                    else
                    {
                        layout = LevelData.FGLayout;
                        loop = LevelData.FGLoop;
                    }
                    for (int x = selection.Left; x < selection.Right; x++)
                        for (int y = selection.Top; y < layout.GetLength(1) - selection.Height; y++)
                        {
                            layout[x, y] = layout[x, y + selection.Height];
                            if (loop != null)
                                loop[x, y] = loop[x, y + selection.Height];
                        }
                    for (int x = selection.Left; x < selection.Right; x++)
                        for (int y = layout.GetLength(1) - selection.Height; y < layout.GetLength(1); y++)
                        {
                            layout[x, y] = 0;
                            if (loop != null)
                                loop[x, y] = false;
                        }
                    if (dlg.moveObjects.Checked)
                    {
                        if (LevelData.Objects != null)
                            foreach (ObjectEntry item in LevelData.Objects)
                                if (item.X >= selection.Left * LevelData.chunksz & item.X < selection.Right * LevelData.chunksz & item.Y >= selection.Bottom * LevelData.chunksz)
                                {
                                    item.Y -= (ushort)(selection.Height * LevelData.chunksz);
                                    item.UpdateSprite();
                                }
                        if (LevelData.Rings != null)
                            foreach (RingEntry item in LevelData.Rings)
                                if (item.X >= selection.Left * LevelData.chunksz & item.X < selection.Right * LevelData.chunksz & item.Y >= selection.Bottom * LevelData.chunksz)
                                {
                                    item.Y -= (ushort)(selection.Height * LevelData.chunksz);
                                    item.UpdateSprite();
                                }
                        if (LevelData.Bumpers != null)
                            foreach (CNZBumperEntry item in LevelData.Bumpers)
                                if (item.X >= selection.Left * LevelData.chunksz & item.X < selection.Right * LevelData.chunksz & item.Y >= selection.Bottom * LevelData.chunksz)
                                {
                                    item.Y -= (ushort)(selection.Height * LevelData.chunksz);
                                    item.UpdateSprite();
                                }
                        if (LevelData.StartPositions != null)
                            foreach (StartPositionEntry item in LevelData.StartPositions)
                                if (item.X >= selection.Left * LevelData.chunksz & item.X < selection.Right * LevelData.chunksz & item.Y >= selection.Bottom * LevelData.chunksz)
                                {
                                    item.Y -= (ushort)(selection.Height * LevelData.chunksz);
                                    item.UpdateSprite();
                                }
                    }
                }
                else if (dlg.entireRow.Checked)
                {
                    byte[,] layout;
                    bool[,] loop;
                    if (tabControl1.SelectedIndex == 2)
                    {
                        layout = LevelData.BGLayout;
                        loop = LevelData.BGLoop;
                    }
                    else
                    {
                        layout = LevelData.FGLayout;
                        loop = LevelData.FGLoop;
                    }
                    for (int x = 0; x < layout.GetLength(0); x++)
                        for (int y = selection.Top; y < layout.GetLength(1) - selection.Height; y++)
                        {
                            layout[x, y] = layout[x, y + selection.Height];
                            if (loop != null)
                                loop[x, y] = loop[x, y + selection.Height];
                        }
                    for (int x = 0; x < layout.GetLength(0); x++)
                        for (int y = layout.GetLength(1) - selection.Height; y < layout.GetLength(1); y++)
                        {
                            layout[x, y] = 0;
                            if (loop != null)
                                loop[x, y] = false;
                        }
                    if (dlg.moveObjects.Checked)
                    {
                        if (LevelData.Objects != null)
                            foreach (ObjectEntry item in LevelData.Objects)
                                if (item.Y >= selection.Bottom * LevelData.chunksz)
                                {
                                    item.Y -= (ushort)(selection.Height * LevelData.chunksz);
                                    item.UpdateSprite();
                                }
                        if (LevelData.Rings != null)
                            foreach (RingEntry item in LevelData.Rings)
                                if (item.Y >= selection.Bottom * LevelData.chunksz)
                                {
                                    item.Y -= (ushort)(selection.Height * LevelData.chunksz);
                                    item.UpdateSprite();
                                }
                        if (LevelData.Bumpers != null)
                            foreach (CNZBumperEntry item in LevelData.Bumpers)
                                if (item.Y >= selection.Bottom * LevelData.chunksz)
                                {
                                    item.Y -= (ushort)(selection.Height * LevelData.chunksz);
                                    item.UpdateSprite();
                                }
                        if (LevelData.StartPositions != null)
                            foreach (StartPositionEntry item in LevelData.StartPositions)
                                if (item.Y >= selection.Bottom * LevelData.chunksz)
                                {
                                    item.Y -= (ushort)(selection.Height * LevelData.chunksz);
                                    item.UpdateSprite();
                                }
                    }
                    if (tabControl1.SelectedIndex == 2)
                    {
                        if (maxsize.HasValue && LevelData.BGHeight > selection.Height)
                            LevelData.ResizeBG(LevelData.BGHeight - selection.Height, LevelData.BGWidth);
                    }
                    else
                    {
                        if (maxsize.HasValue && LevelData.FGHeight > selection.Height)
                            LevelData.ResizeFG(LevelData.FGHeight - selection.Height, LevelData.FGWidth);
                    }
                }
                else if (dlg.entireColumn.Checked)
                {
                    byte[,] layout;
                    bool[,] loop;
                    if (tabControl1.SelectedIndex == 2)
                    {
                        layout = LevelData.BGLayout;
                        loop = LevelData.BGLoop;
                    }
                    else
                    {
                        layout = LevelData.FGLayout;
                        loop = LevelData.FGLoop;
                    }
                    for (int y = 0; y < layout.GetLength(1); y++)
                        for (int x = selection.Left; x < layout.GetLength(0) - selection.Width; x++)
                        {
                            layout[x, y] = layout[x + selection.Width, y];
                            if (loop != null)
                                loop[x, y] = loop[x + selection.Width, y];
                        }
                    for (int y = 0; y < layout.GetLength(1); y++)
                        for (int x = layout.GetLength(0) - selection.Width; x < layout.GetLength(0); x++)
                        {
                            layout[x, y] = 0;
                            if (loop != null)
                                loop[x, y] = false;
                        }
                    if (dlg.moveObjects.Checked)
                    {
                        if (LevelData.Objects != null)
                            foreach (ObjectEntry item in LevelData.Objects)
                                if (item.X >= selection.Right * LevelData.chunksz)
                                {
                                    item.X -= (ushort)(selection.Width * LevelData.chunksz);
                                    item.UpdateSprite();
                                }
                        if (LevelData.Rings != null)
                            foreach (RingEntry item in LevelData.Rings)
                                if (item.X >= selection.Right * LevelData.chunksz)
                                {
                                    item.X -= (ushort)(selection.Width * LevelData.chunksz);
                                    item.UpdateSprite();
                                }
                        if (LevelData.Bumpers != null)
                            foreach (CNZBumperEntry item in LevelData.Bumpers)
                                if (item.X >= selection.Right * LevelData.chunksz)
                                {
                                    item.X -= (ushort)(selection.Width * LevelData.chunksz);
                                    item.UpdateSprite();
                                }
                        if (LevelData.StartPositions != null)
                            foreach (StartPositionEntry item in LevelData.StartPositions)
                                if (item.X >= selection.Right * LevelData.chunksz)
                                {
                                    item.X -= (ushort)(selection.Width * LevelData.chunksz);
                                    item.UpdateSprite();
                                }
                    }
                    if (tabControl1.SelectedIndex == 2)
                    {
                        if (maxsize.HasValue && LevelData.BGWidth > selection.Width)
                            LevelData.ResizeBG(LevelData.BGWidth - selection.Width, LevelData.BGHeight);
                    }
                    else
                    {
                        if (maxsize.HasValue && LevelData.FGWidth > selection.Width)
                            LevelData.ResizeFG(LevelData.FGWidth - selection.Width, LevelData.FGHeight);
                    }
                }
            }
        }
    }

    internal enum EditingMode { Draw, Select }

    [Serializable]
    public class LayoutSection
    {
        public byte[,] Layout { get; set; }
        public bool[,] Loop { get; set; }
        public Point Position { get; set; }
        public List<Entry> Objects { get; set; }

        public LayoutSection(byte[,] layout, bool[,] loop, Point position, List<Entry> objects)
        {
            Layout = layout;
            Loop = loop;
            Position = position;
            Objects = objects;
        }
    }

    public abstract class UndoAction
    {
        public string Name { get; protected set; }
        public abstract void Undo();
        public abstract void Redo();
    }

    internal class LayoutEditUndoAction : UndoAction
    {
        private List<Point> locations;
        private List<byte> oldtiles;
        private int mode;

        public LayoutEditUndoAction(int Mode, List<Point> Locations, List<byte> OldTiles)
        {
            Name = "Layout Edit" + " (" + Locations.Count + " chunk" + (Locations.Count > 1 ? "s" : "") + ")";
            mode = Mode;
            locations = Locations;
            oldtiles = OldTiles;
        }

        public override void Undo()
        {
            byte t;
            switch (mode)
            {
                case 1:
                    for (int i = 0; i < locations.Count; i++)
                    {
                        t = LevelData.FGLayout[locations[i].X, locations[i].Y];
                        LevelData.FGLayout[locations[i].X, locations[i].Y] = oldtiles[i];
                        oldtiles[i] = t;
                    }
                    break;
                case 2:
                    for (int i = 0; i < locations.Count; i++)
                    {
                        t = LevelData.FGLayout[locations[i].X, locations[i].Y];
                        LevelData.BGLayout[locations[i].X, locations[i].Y] = oldtiles[i];
                        oldtiles[i] = t;
                    }
                    break;
            }
        }

        public override void Redo()
        {
            Undo();
        }
    }

    internal class ObjectPropertyChangedUndoAction : UndoAction
    {
        private List<Entry> objects;
        private List<object> values;
        private PropertyDescriptor prop;

        public ObjectPropertyChangedUndoAction(List<Entry> Objects, List<object> Values, PropertyDescriptor Property)
        {
            Name = "Change of property " + Property.DisplayName + " (" + Objects.Count + " object" + (Objects.Count > 1 ? "s" : "") + ")";
            objects = Objects;
            values = Values;
            prop = Property;
        }

        public override void Undo()
        {
            object val;
            for (int i = 0; i < objects.Count; i++)
            {
                val = prop.GetValue(objects[i]);
                prop.SetValue(objects[i], values[i]);
                values[i] = val;
                objects[i].UpdateSprite();
            }
        }

        public override void Redo()
        {
            Undo();
        }
    }

    internal class ObjectMoveUndoAction : UndoAction
    {
        private List<Entry> objects;
        private List<Point> locations;
        
        public ObjectMoveUndoAction(List<Entry> Objects, List<Point> Locations)
        {
            Name = "Moved " + Objects.Count + " object" + (Objects.Count > 1 ? "s" : "");
            objects = Objects;
            locations = Locations;
        }

        public override void Undo()
        {
            Point pt;
            for (int i = 0; i < objects.Count; i++)
            {
                pt = new Point(objects[i].X, objects[i].Y);
                objects[i].X = (ushort)locations[i].X;
                objects[i].Y = (ushort)locations[i].Y;
                locations[i] = pt;
                objects[i].UpdateSprite();
            }
        }

        public override void Redo()
        {
            Undo();
        }
    }

    internal class ObjectAddedUndoAction : UndoAction
    {
        private Entry obj;

        public ObjectAddedUndoAction(Entry Obj)
        {
            Name = "Added object";
            obj = Obj;
        }

        public override void Redo()
        {
            if (obj is ObjectEntry)
            {
                LevelData.Objects.Add((ObjectEntry)obj);
            }
            else if (obj is RingEntry)
            {
                LevelData.Rings.Add((RingEntry)obj);
            }
            else if (obj is CNZBumperEntry)
            {
                LevelData.Bumpers.Add((CNZBumperEntry)obj);
            }
        }

        public override void Undo()
        {
            if (obj is ObjectEntry)
            {
                LevelData.Objects.Remove((ObjectEntry)obj);
            }
            else if (obj is RingEntry)
            {
                LevelData.Rings.Remove((RingEntry)obj);
            }
            else if (obj is CNZBumperEntry)
            {
                LevelData.Bumpers.Remove((CNZBumperEntry)obj);
            }
        }
    }

    internal class ObjectsDeletedUndoAction : UndoAction
    {
        private List<Entry> objs;

        public ObjectsDeletedUndoAction(List<Entry> Objects)
        {
            Name = "Deleted " + Objects.Count + " object" + (Objects.Count > 1 ? "s" : "");
            objs = Objects;
        }

        public override void Undo()
        {
            foreach (Entry item in objs)
            {
                if (item is ObjectEntry)
                {
                    LevelData.Objects.Add((ObjectEntry)item);
                }
                else if (item is RingEntry)
                {
                    LevelData.Rings.Add((RingEntry)item);
                }
                else if (item is CNZBumperEntry)
                {
                    LevelData.Bumpers.Add((CNZBumperEntry)item);
                }
            }
        }

        public override void Redo()
        {
            foreach (Entry item in objs)
            {
                if (item is ObjectEntry)
                {
                    LevelData.Objects.Remove((ObjectEntry)item);
                }
                else if (item is RingEntry)
                {
                    LevelData.Rings.Remove((RingEntry)item);
                }
                else if (item is CNZBumperEntry)
                {
                    LevelData.Bumpers.Remove((CNZBumperEntry)item);
                }
            }
        }
    }


    internal class ObjectsPastedUndoAction : UndoAction
    {
        private List<Entry> objs;

        public ObjectsPastedUndoAction(List<Entry> Objects)
        {
            Name = "Pasted " + Objects.Count + " object" + (Objects.Count > 1 ? "s" : "");
            objs = Objects;
        }

        public override void Redo()
        {
            foreach (Entry item in objs)
            {
                if (item is ObjectEntry)
                {
                    LevelData.Objects.Add((ObjectEntry)item);
                }
                else if (item is RingEntry)
                {
                    LevelData.Rings.Add((RingEntry)item);
                }
                else if (item is CNZBumperEntry)
                {
                    LevelData.Bumpers.Add((CNZBumperEntry)item);
                }
            }
        }

        public override void Undo()
        {
            foreach (Entry item in objs)
            {
                if (item is ObjectEntry)
                {
                    LevelData.Objects.Remove((ObjectEntry)item);
                }
                else if (item is RingEntry)
                {
                    LevelData.Rings.Remove((RingEntry)item);
                }
                else if (item is CNZBumperEntry)
                {
                    LevelData.Bumpers.Remove((CNZBumperEntry)item);
                }
            }
        }
    }
}