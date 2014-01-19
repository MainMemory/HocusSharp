using System.IO;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System;

namespace HocusSharp
{
    public struct Rectangle
    {
        public ushort Left { get; set; }
        public ushort Top { get; set; }
        public ushort Right { get; set; }
        public ushort Bottom { get; set; }

        public Rectangle(Stream stream)
            : this()
        {
            BinaryReader br = new BinaryReader(stream);
            Left = br.ReadUInt16();
            Top = br.ReadUInt16();
            Right = br.ReadUInt16();
            Bottom = br.ReadUInt16();
        }

        public void Write(Stream stream)
        {
            BinaryWriter bw = new BinaryWriter(stream);
            bw.Write(Left);
            bw.Write(Top);
            bw.Write(Right);
            bw.Write(Bottom);
        }

        public System.Drawing.Rectangle ToPixels()
        {
            return System.Drawing.Rectangle.FromLTRB(Left * 16, Top * 16, Right * 16 + 16, Bottom * 16 + 16);
        }

        public bool IsEmpty
        {
            get
            {
                if (Left == 0xFFFF & Top == 0xFFFF & Right == 0xFFFF & Bottom == 0xFFFF) return true;
                return Left == 0 & Top == 0 & Right == 0 & Bottom == 0;
            }
        }
    }

    public class SpriteData
    {
        internal long SpriteOffset { get; set; }
        public string Name { get; set; }
        public ushort Width4 { get; set; }
        public ushort Height { get; set; }
        public ushort StandFrame { get; set; }
        public ushort StandFrame2 { get; set; }
        public ushort WalkStartFrame { get; set; }
        public ushort WalkEndFrame { get; set; }
        public ushort JumpFrame { get; set; }
        public ushort FallFrame { get; set; }
        public ushort Shoot1DashStartFrame { get; set; }
        public ushort Shoot2DashEndFrame { get; set; }
        public ushort Unknown1 { get; set; }
        public ushort Unknown2 { get; set; }
        public ushort ProjectileYOff { get; set; }
        public ushort ProjectileFrame { get; set; }
        public ushort ProjectileFrame2 { get; set; }
        public ReadOnlyCollection<BitmapBits[]> Sprites { get; private set; }

        public SpriteData(Stream stream, long baseOff)
        {
            BinaryReader br = new BinaryReader(stream);
            long spriteOff = SpriteOffset = br.ReadUInt32() + baseOff;
            Name = br.ReadString(22);
            Width4 = br.ReadUInt16();
            Height = br.ReadUInt16();
            StandFrame = br.ReadUInt16();
            StandFrame2 = br.ReadUInt16();
            WalkStartFrame = br.ReadUInt16();
            WalkEndFrame = br.ReadUInt16();
            JumpFrame = br.ReadUInt16();
            FallFrame = br.ReadUInt16();
            Shoot1DashStartFrame = br.ReadUInt16();
            Shoot2DashEndFrame = br.ReadUInt16();
            Unknown1 = br.ReadUInt16();
            Unknown2 = br.ReadUInt16();
            ProjectileYOff = br.ReadUInt16();
            ProjectileFrame = br.ReadUInt16();
            ProjectileFrame2 = br.ReadUInt16();
            long pixelOff = br.ReadUInt16() + spriteOff;
            ushort pixelSize = br.ReadUInt16();
            long[][] spriteOffs = new long[20][];
            for (int i = 0; i < 20; i++)
                spriteOffs[i] = new long[2];
            for (int i = 0; i < 2; i++)
                for (int j = 0; j < 20; j++)
                    spriteOffs[j][i] = br.ReadUInt16() + spriteOff;
            ushort[][] pixelOffs = new ushort[20][];
            for (int i = 0; i < 20; i++)
                pixelOffs[i] = new ushort[2];
            for (int i = 0; i < 2; i++)
                for (int j = 0; j < 20; j++)
                    pixelOffs[j][i] = br.ReadUInt16();
            long next = stream.Position;
            stream.Seek(pixelOff, SeekOrigin.Begin);
            byte[] pixelData = br.ReadBytes(pixelSize);
            using (MemoryStream ps = new MemoryStream(pixelData, false))
            using (BinaryReader pr = new BinaryReader(ps))
            {
                BitmapBits[][] sprites = new BitmapBits[20][];
                for (int i = 0; i < 20; i++)
                {
                    sprites[i] = new BitmapBits[2];
                    for (int j = 0; j < 2; j++)
                    {
                        stream.Seek(spriteOffs[i][j], SeekOrigin.Begin);
                        ps.Seek(pixelOffs[i][j] * 4, SeekOrigin.Begin);
                        BitmapBits buf = new BitmapBits(320, Height);
                        int pixi = 0;
                        byte tr = 0;
                        try
                        {
                            while (true)
                                switch (br.ReadByte())
                                {
                                    case 0:
                                        tr = br.ReadByte();
                                        pixi = 0;
                                        break;
                                    case 1:
                                        pixi += br.ReadUInt16() * 4;
                                        break;
                                    case 2:
                                        for (int k = 3; k >= 0; k--)
                                        {
                                            byte p = pr.ReadByte();
                                            /*if (((tr >> k) & 1) == 1)*/
                                            buf.Bits[pixi++] = p;
                                            //else pixi++;
                                        }
                                        break;
                                    case 3:
                                        goto spritedone;
                                    default:
                                        goto endloop;
                                }
                        spritedone: sprites[i][j] = buf.GetSection(0, 0, Width4 * 4, Height);
                        }
                        catch { }
                    endloop: ;
                    }
                }
                Sprites = new ReadOnlyCollection<BitmapBits[]>(sprites);
            }
            stream.Seek(next, SeekOrigin.Begin);
        }

        public static SpriteData[] Read(Stream stream)
        {
            long baseOff = stream.Position;
            List<SpriteData> sprites = new List<SpriteData>(1);
            sprites.Add(new SpriteData(stream, baseOff));
            long end = sprites[0].SpriteOffset;
            while (stream.Position < end)
            {
                SpriteData spr = new SpriteData(stream, baseOff);
                end = Math.Min(end, spr.SpriteOffset);
                sprites.Add(spr);
            }
            return sprites.ToArray();
        }
    }

    public class StartPosition
    {
        public ushort Null { get; set; }
        public ushort X { get; set; }
        public ushort Y { get; set; }
        public ushort Unknown { get; set; }

        public StartPosition(Stream stream)
        {
            BinaryReader br = new BinaryReader(stream);
            Null = br.ReadUInt16();
            X = br.ReadUInt16();
            Y = br.ReadUInt16();
            Unknown = br.ReadUInt16();
        }

        public void Write(Stream stream)
        {
            BinaryWriter bw = new BinaryWriter(stream);
            bw.Write(Null);
            bw.Write(X);
            bw.Write(Y);
            bw.Write(Unknown);
        }
    }

    public class TileSettings
    {
        public byte BackgroundTile { get; set; }
        public byte SwitchDownTile { get; set; }
        public byte SwitchUpTile { get; set; }
        public byte ShootableTile { get; set; }
        public AnimationData[] Animations { get; private set; }

        public TileSettings(Stream stream)
        {
            BinaryReader br = new BinaryReader(stream);
            BackgroundTile = br.ReadByte();
            SwitchDownTile = br.ReadByte();
            SwitchUpTile = br.ReadByte();
            ShootableTile = br.ReadByte();
            Animations = new AnimationData[240];
            for (int i = 0; i < 240; i++)
                Animations[i] = new AnimationData(stream);
        }

        public void Write(Stream stream)
        {
            BinaryWriter bw = new BinaryWriter(stream);
            bw.Write(BackgroundTile);
            bw.Write(SwitchDownTile);
            bw.Write(SwitchUpTile);
            bw.Write(ShootableTile);
            foreach (AnimationData item in Animations)
                item.Write(stream);
        }
    }

    public class AnimationData
    {
        public byte FirstIndex { get; set; }
        public byte LastIndex { get; set; }
        public AnimationType Type { get; set; }

        internal AnimationData(Stream stream)
        {
            BinaryReader br = new BinaryReader(stream);
            FirstIndex = br.ReadByte();
            LastIndex = br.ReadByte();
            Type = (AnimationType)br.ReadByte();
        }

        internal void Write(Stream stream)
        {
            BinaryWriter bw = new BinaryWriter(stream);
            bw.Write(FirstIndex);
            bw.Write(LastIndex);
            bw.Write((byte)Type);
        }
    }

    public enum AnimationType : byte
    {
        None,
        Constant,
        Random
    }

    public class TerexinMessage
    {
        public ushort X { get; set; }
        public ushort Y { get; set; }
        public string[] Lines { get; private set; }

        public TerexinMessage(Stream stream)
        {
            BinaryReader br = new BinaryReader(stream);
            X = br.ReadUInt16();
            Y = br.ReadUInt16();
            Lines = new string[10];
            for (int i = 0; i < 10; i++)
                Lines[i] = br.ReadString(50);
        }

        public void Write(Stream stream)
        {
            BinaryWriter bw = new BinaryWriter(stream);
            bw.Write(X);
            bw.Write(Y);
            foreach (string item in Lines)
                bw.WriteString(item, 50);
        }

        public static TerexinMessage[] Read(Stream stream)
        {
            TerexinMessage[] messages = new TerexinMessage[10];
            for (int i = 0; i < 10; i++)
                messages[i] = new TerexinMessage(stream);
            return messages;
        }

        public static void Write(TerexinMessage[] messages, Stream stream)
        {
            foreach (TerexinMessage item in messages)
                item.Write(stream);
        }
    }

    public class WarpEntry
    {
        public ushort StartLocation { get; set; }
        public ushort EndLocation { get; set; }

        public WarpEntry(Stream stream)
        {
            BinaryReader br = new BinaryReader(stream);
            StartLocation = br.ReadUInt16();
            EndLocation = br.ReadUInt16();
        }

        public void Write(Stream stream)
        {
            BinaryWriter bw = new BinaryWriter(stream);
            bw.Write(StartLocation);
            bw.Write(EndLocation);
        }

        public static WarpEntry[] Read(Stream stream)
        {
            WarpEntry[] warps = new WarpEntry[10];
            for (int i = 0; i < 10; i++)
                warps[i] = new WarpEntry(stream);
            return warps;
        }

        public static void Write(WarpEntry[] warps, Stream stream)
        {
            foreach (WarpEntry item in warps)
                item.Write(stream);
        }
    }

    public class SwitchEntry
    {
        public SwitchType Type { get; set; }
        public short[] Locations { get; private set; }
        public byte[] Tiles { get; private set; }
        public Rectangle Area { get; set; }

        public SwitchEntry(Stream stream)
        {
            Locations = new short[4];
            Tiles = new byte[4];
            BinaryReader br = new BinaryReader(stream);
            Type = (SwitchType)br.ReadUInt16();
            for (int i = 0; i < 4; i++)
                Locations[i] = br.ReadInt16();
            for (int i = 0; i < 4; i++)
                Tiles[i] = br.ReadByte();
            Area = new Rectangle(stream);
        }

        public void Write(Stream stream)
        {
            BinaryWriter bw = new BinaryWriter(stream);
            bw.Write((ushort)Type);
            for (int i = 0; i < 4; i++)
                bw.Write(Locations[i]);
            for (int i = 0; i < 4; i++)
                bw.Write(Tiles[i]);
            Area.Write(stream);
        }

        public bool IsEmpty
        {
            get
            {
                for (int i = 0; i < 4; i++)
                    if (i != -1)
                        return Area.IsEmpty;
                return true;
            }
        }

        public static SwitchEntry[] Read(Stream stream)
        {
            SwitchEntry[] switches = new SwitchEntry[23];
            for (int i = 0; i < 23; i++)
                switches[i] = new SwitchEntry(stream);
            return switches;
        }

        public static void Write(SwitchEntry[] switches, Stream stream)
        {
            foreach (SwitchEntry item in switches)
                item.Write(stream);
        }
    }

    public enum SwitchType : ushort
    {
        Erase,
        Create
    }

    public class KeyholeEntry
    {
        public Key Key { get; set; }
        public ushort Tile { get; set; }
        public Rectangle Area { get; set; }

        public KeyholeEntry(Stream stream)
        {
            BinaryReader br = new BinaryReader(stream);
            Key = (Key)br.ReadUInt16();
            Tile = br.ReadUInt16();
            Area = new Rectangle(stream);
        }

        public void Write(Stream stream)
        {
            BinaryWriter bw = new BinaryWriter(stream);
            bw.Write((ushort)Key);
            bw.Write(Tile);
            Area.Write(stream);
        }

        public bool IsEmpty
        {
            get
            {
                return Area.IsEmpty;
            }
        }

        public static KeyholeEntry[] Read(Stream stream)
        {
            KeyholeEntry[] keyholes = new KeyholeEntry[25];
            for (int i = 0; i < 25; i++)
                keyholes[i] = new KeyholeEntry(stream);
            return keyholes;
        }

        public static void Write(KeyholeEntry[] keyholes, Stream stream)
        {
            foreach (KeyholeEntry item in keyholes)
                item.Write(stream);
        }
    }

    public enum Key : ushort
    {
        None,
        Silver,
        Gold
    }

    public class EnemyType
    {
        public ushort Sprites { get; set; }
        public ushort Health { get; set; }
        public short ProjectileHSpeed { get; set; }
        public short ProjectileVSpeed { get; set; }
        public ushort Unknown1 { get; set; }
        public bool ProjectileSlow { get; set; }
        public ushort Unknown2 { get; set; }
        public bool ShootProjectiles { get; set; }
        public ushort Unknown3 { get; set; }
        public bool WavyProjectiles { get; set; }
        public ushort Unknown4 { get; set; }
        public ushort Behavior { get; set; }

        public EnemyType(Stream stream)
        {
            BinaryReader br = new BinaryReader(stream);
            Sprites = br.ReadUInt16();
            Health = br.ReadUInt16();
            ProjectileHSpeed = br.ReadInt16();
            ProjectileVSpeed = br.ReadInt16();
            Unknown1 = br.ReadUInt16();
            ProjectileSlow = br.ReadUInt16() != 0;
            Unknown2 = br.ReadUInt16();
            ShootProjectiles = br.ReadUInt16() != 0;
            Unknown3 = br.ReadUInt16();
            WavyProjectiles = br.ReadUInt16() != 0;
            Unknown4 = br.ReadUInt16();
            Behavior = br.ReadUInt16();
        }

        public void Write(Stream stream)
        {
            BinaryWriter bw = new BinaryWriter(stream);
            bw.Write(Sprites);
            bw.Write(Health);
            bw.Write(ProjectileHSpeed);
            bw.Write(ProjectileVSpeed);
            bw.Write(Unknown1);
            bw.Write((ushort)(ProjectileSlow ? 1 : 0));
            bw.Write(Unknown2);
            bw.Write((ushort)(ShootProjectiles ? 1 : 0));
            bw.Write(Unknown3);
            bw.Write((ushort)(WavyProjectiles ? 1 : 0));
            bw.Write(Unknown4);
            bw.Write(Behavior);
        }

        public static EnemyType[] Read(Stream stream)
        {
            EnemyType[] settings = new EnemyType[10];
            for (int i = 0; i < 10; i++)
                settings[i] = new EnemyType(stream);
            return settings;
        }

        public static void Write(EnemyType[] settings, Stream stream)
        {
            foreach (EnemyType item in settings)
                item.Write(stream);
        }
    }

    public class EnemyGroup
    {
        public short[] Flags { get; private set; }
        public ushort[] Locations { get; private set; }

        public EnemyGroup(Stream stream)
        {
            Flags = new short[8];
            Locations = new ushort[8];
            BinaryReader br = new BinaryReader(stream);
            for (int i = 0; i < 8; i++)
                Flags[i] = br.ReadInt16();
            for (int i = 0; i < 8; i++)
                Locations[i] = br.ReadUInt16();
        }

        public void Write(Stream stream)
        {
            BinaryWriter bw = new BinaryWriter(stream);
            for (int i = 0; i < 8; i++)
                bw.Write(Flags[i]);
            for (int i = 0; i < 8; i++)
                bw.Write(Locations[i]);
        }

        public static EnemyGroup[] Read(Stream stream)
        {
            EnemyGroup[] enemies = new EnemyGroup[250];
            for (int i = 0; i < 250; i++)
                enemies[i] = new EnemyGroup(stream);
            return enemies;
        }

        public static void Write(EnemyGroup[] enemies, Stream stream)
        {
            foreach (EnemyGroup item in enemies)
                item.Write(stream);
        }
    }

    public class TextPage
    {
        public ushort Unknown1 { get; set; }
        public ushort Unknown2 { get; set; }
        public TextAttributes[] Attributes { get; private set; }
        public string[] Lines { get; private set; }

        public const int LineCount = 20;
        public const int LineLength = 80;

        public TextPage(Stream stream)
        {
            BinaryReader br = new BinaryReader(stream);
            br.ReadUInt16();
            Unknown1 = br.ReadUInt16();
            Unknown2 = br.ReadUInt16();
            Attributes = new TextAttributes[LineCount];
            for (int i = 0; i < LineCount; i++)
                Attributes[i] = new TextAttributes(stream);
            Lines = new string[LineCount];
            for (int i = 0; i < LineCount; i++)
                Lines[i] = br.ReadString(LineLength);
        }

        public void Write(Stream stream)
        {
            BinaryWriter bw = new BinaryWriter(stream);
            bw.Write((ushort)Lines.CountLines());
            bw.Write(Unknown1);
            bw.Write(Unknown2);
            foreach (TextAttributes item in Attributes)
                item.Write(stream);
            foreach (string item in Lines)
                bw.WriteString(item, LineLength);
        }
    }

    public class TextAttributes
    {
        public ushort Alignment { get; set; }
        public ushort TextColor { get; set; }

        public TextAttributes(Stream stream)
        {
            BinaryReader br = new BinaryReader(stream);
            Alignment = br.ReadUInt16();
            TextColor = br.ReadUInt16();
        }

        public void Write(Stream stream)
        {
            BinaryWriter bw = new BinaryWriter(stream);
            bw.Write(Alignment);
            bw.Write(TextColor);
        }
    }
}