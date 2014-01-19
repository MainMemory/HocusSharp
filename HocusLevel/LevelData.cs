using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using HocusSharp;

namespace HocusLevel
{
    public static class LevelData
    {
        public const double VGAMultiplier = 4.04761904761905;
        public const int LevelWidth = 0xF0;
        public const int LevelHeight = 0x3C;
        public const int LevelSize = LevelWidth * LevelHeight;
        public static ReadOnlyCollection<BitmapBits> Font { get; private set; }
        public static Color[] Palette { get; private set; }
        public static BitmapBits[] Tiles { get; private set; }
        public static SpriteData[] Sprites { get; private set; }
        public static StartPosition StartPosition { get; set; }
        public static TileSettings TileSettings { get; set; }
        public static TerexinMessage[] TerexinMessages { get; private set; }
        public static WarpEntry[] Warps { get; private set; }
        public static SwitchEntry[] Switches { get; private set; }
        public static KeyholeEntry[] Keyholes1 { get; private set; }
        public static KeyholeEntry[] Keyholes2 { get; private set; }
        public static EnemyType[] EnemyTypes { get; private set; }
        public static EnemyGroup[] Enemies { get; private set; }
        public static byte[,] LowPlane { get; private set; }
        public static byte[,] HighPlane { get; private set; }
        public static byte[,] HighPlane2 { get; private set; }
        public static ushort[,] TileProperties { get; private set; }
        internal static readonly bool IsMonoRuntime = Type.GetType("Mono.Runtime") != null;
        internal static readonly bool IsWindows = !(Environment.OSVersion.Platform == PlatformID.MacOSX | Environment.OSVersion.Platform == PlatformID.Unix | Environment.OSVersion.Platform == PlatformID.Xbox);

        public static void LoadGame(string file)
        {
            Environment.CurrentDirectory = Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file) + " Data Files");
        }

        public static void LoadLevel(int episode, int level)
        {
            int tileset = Math.Min((level - 1) / 2, 3) + ((episode - 1) * 4) + 1;
            using (Stream ms = File.OpenRead("FONT.MSK"))
            using (BinaryReader br = new BinaryReader(ms))
            {
                BitmapBits[] font = new BitmapBits[90];
                for (int i = 0; i < 90; i++)
                {
                    font[i] = BitmapBits.Read1bppTile(ms);
                    font[i].ReplaceColor(1, 255);
                }
                Font = new ReadOnlyCollection<BitmapBits>(font);
            }
            Palette = new Color[256];
            using (Stream ms = File.OpenRead("GAMEPAL.PAL"))
            using (BinaryReader br = new BinaryReader(ms))
                for (int i = 0; i < 128; i++)
                    Palette[i] = Color.FromArgb((int)(br.ReadByte() * VGAMultiplier), (int)(br.ReadByte() * VGAMultiplier), (int)(br.ReadByte() * VGAMultiplier));
            using (Stream ms = File.OpenRead("SPRITES.SPR"))
                Sprites = SpriteData.Read(ms);
            using (Stream ms = File.OpenRead(string.Format("BACK{0:00}.PAL", tileset)))
            using (BinaryReader br = new BinaryReader(ms))
                for (int i = 128; i < 256; i++)
                    Palette[i] = Color.FromArgb((int)(br.ReadByte() * VGAMultiplier), (int)(br.ReadByte() * VGAMultiplier), (int)(br.ReadByte() * VGAMultiplier));
            Tiles = new BitmapBits[256];
            using (Stream ms = File.OpenRead(string.Format("TILES{0:00}.PCX", tileset)))
            {
                BitmapBits tileImg = BitmapBits.ReadPCX(ms);
                for (int y = 0; y < tileImg.Height / 16; y++)
                    for (int x = 0; x < tileImg.Width / 16; x++)
                        Tiles[(y * (tileImg.Width / 16)) + x] = tileImg.GetSection(x * 16, y * 16, 16, 16);
            }
            using (Stream ms = File.OpenRead(string.Format("E{0}L{1}.000", episode, level)))
                StartPosition = new StartPosition(ms);
            using (Stream ms = File.OpenRead(string.Format("E{0}L{1}.001", episode, level)))
                TileSettings = new TileSettings(ms);
            using (Stream ms = File.OpenRead(string.Format("E{0}L{1}.002", episode, level)))
                TerexinMessages = TerexinMessage.Read(ms);
            using (Stream ms = File.OpenRead(string.Format("E{0}L{1}.003", episode, level)))
                Warps = WarpEntry.Read(ms);
            using (Stream ms = File.OpenRead(string.Format("E{0}L{1}.004", episode, level)))
                Switches = SwitchEntry.Read(ms);
            using (Stream ms = File.OpenRead(string.Format("E{0}L{1}.005", episode, level)))
                Keyholes1 = KeyholeEntry.Read(ms);
            using (Stream ms = File.OpenRead(string.Format("E{0}L{1}.006", episode, level)))
                Keyholes2 = KeyholeEntry.Read(ms);
            using (Stream ms = File.OpenRead(string.Format("E{0}L{1}.007", episode, level)))
                EnemyTypes = EnemyType.Read(ms);
            using (Stream ms = File.OpenRead(string.Format("E{0}L{1}.008", episode, level)))
                Enemies = EnemyGroup.Read(ms);
            LowPlane = new byte[LevelWidth, LevelHeight];
            using (Stream ms = File.OpenRead(string.Format("E{0}L{1}.009", episode, level)))
            using (BinaryReader br = new BinaryReader(ms))
                for (int y = 0; y < LevelHeight; y++)
                    for (int x = 0; x < LevelWidth; x++)
                        LowPlane[x, y] = br.ReadByte();
            HighPlane = new byte[LevelWidth, LevelHeight];
            using (Stream ms = File.OpenRead(string.Format("E{0}L{1}.010", episode, level)))
            using (BinaryReader br = new BinaryReader(ms))
                for (int y = 0; y < LevelHeight; y++)
                    for (int x = 0; x < LevelWidth; x++)
                        HighPlane[x, y] = br.ReadByte();
            HighPlane2 = new byte[LevelWidth, LevelHeight];
            using (Stream ms = File.OpenRead(string.Format("E{0}L{1}.011", episode, level)))
            using (BinaryReader br = new BinaryReader(ms))
                for (int y = 0; y < LevelHeight; y++)
                    for (int x = 0; x < LevelWidth; x++)
                        HighPlane2[x, y] = br.ReadByte();
            TileProperties = new ushort[LevelWidth, LevelHeight];
            using (Stream ms = File.OpenRead(string.Format("E{0}L{1}.012", episode, level)))
            using (BinaryReader br = new BinaryReader(ms))
                for (int y = 0; y < LevelHeight; y++)
                    for (int x = 0; x < LevelWidth; x++)
                        TileProperties[x, y] = br.ReadUInt16();
        }

        public static void DrawString(this BitmapBits dest, object str, int x, int y)
        {
            int startx = x;
            foreach (char item in str.ToString())
            {
                if (item >= '!' & item <= 'z')
                    dest.DrawBitmapComposited(Font[item - '!'], x, y);
                x += 8;
                if (item == '\n')
                {
                    x = startx;
                    y += 8;
                }
            }
        }

        public static void DrawString(this BitmapBits dest, object str, Point location) { DrawString(dest, str, location.X, location.Y); }

        public static void DrawStringWrapped(this BitmapBits dest, object str, int x, int y, int width)
        {
            int i = 0;
            int startx = x;
            foreach (char item in str.ToString())
            {
                if (item >= '!' & item <= 'z')
                    dest.DrawBitmapComposited(Font[item - '!'], x, y);
                x += 8;
                i++;
                if (i == width | item == '\n')
                {
                    x = startx;
                    y += 8;
                    i = 0;
                }
            }
        }

        public static void DrawStringWrapped(this BitmapBits dest, object str, Point location, int width) { DrawStringWrapped(dest, str, location.X, location.Y, width); }
    }
}