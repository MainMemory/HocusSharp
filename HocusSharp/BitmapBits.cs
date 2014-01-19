﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.IO;

namespace HocusSharp
{
    /// <summary>
    /// Represents the pixel data of an 8bpp Bitmap.
    /// </summary>
    public class BitmapBits
    {
        public byte[] Bits { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public Size Size { get { return new Size(Width, Height); } }

        public byte this[int x, int y]
        {
            get { return Bits[(y * Width) + x]; }
            set { Bits[(y * Width) + x] = value; }
        }

        public BitmapBits(int width, int height)
        {
            Width = width;
            Height = height;
            Bits = new byte[width * height];
        }

        public BitmapBits(Size size)
            : this(size.Width, size.Height) { }

        public BitmapBits(Bitmap bmp)
        {
            if (bmp.PixelFormat != PixelFormat.Format8bppIndexed)
                throw new ArgumentException();
            BitmapData bmpd = bmp.LockBits(new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, bmp.PixelFormat);
            Width = bmpd.Width;
            Height = bmpd.Height;
            byte[] tmpbits = new byte[Math.Abs(bmpd.Stride) * bmpd.Height];
            Marshal.Copy(bmpd.Scan0, tmpbits, 0, tmpbits.Length);
            Bits = new byte[Width * Height];
            int j = 0;
            for (int i = 0; i < Bits.Length; i += Width)
            {
                Array.Copy(tmpbits, j, Bits, i, Width);
                j += Math.Abs(bmpd.Stride);
            }
            bmp.UnlockBits(bmpd);
        }

        public BitmapBits(BitmapBits source)
        {
            Width = source.Width;
            Height = source.Height;
            Bits = new byte[source.Bits.Length];
            Array.Copy(source.Bits, Bits, Bits.Length);
        }

        public Bitmap ToBitmap()
        {
            if (Size.IsEmpty) return new Bitmap(1, 1, PixelFormat.Format8bppIndexed);
            Bitmap newbmp = new Bitmap(Width, Height, PixelFormat.Format8bppIndexed);
            BitmapData newbmpd = newbmp.LockBits(new System.Drawing.Rectangle(0, 0, Width, Height), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
            byte[] bmpbits = new byte[Math.Abs(newbmpd.Stride) * newbmpd.Height];
            for (int y = 0; y < Height; y++)
                Array.Copy(Bits, y * Width, bmpbits, y * Math.Abs(newbmpd.Stride), Width);
            Marshal.Copy(bmpbits, 0, newbmpd.Scan0, bmpbits.Length);
            newbmp.UnlockBits(newbmpd);
            return newbmp;
        }

        public Bitmap ToBitmap(ColorPalette palette)
        {
            Bitmap newbmp = ToBitmap();
            newbmp.Palette = palette;
            return newbmp;
        }

        public Bitmap ToBitmap(params Color[] palette)
        {
            Bitmap newbmp = ToBitmap();
            ColorPalette pal = newbmp.Palette;
            for (int i = 0; i < Math.Min(palette.Length, 256); i++)
                pal.Entries[i] = palette[i];
            newbmp.Palette = pal;
            return newbmp;
        }

        public void DrawBitmap(BitmapBits source, int x, int y)
        {
            for (int i = 0; i < source.Height; i++)
            {
                int di = ((y + i) * Width) + x;
                int si = i * source.Width;
                Array.Copy(source.Bits, si, Bits, di, source.Width);
            }
        }

        public void DrawBitmap(BitmapBits source, Point location) { DrawBitmap(source, location.X, location.Y); }

        public void DrawBitmapComposited(BitmapBits source, int x, int y)
        {
            int srcl = 0;
            if (x < 0)
                srcl = -x;
            int srct = 0;
            if (y < 0)
                srct = -y;
            int srcr = source.Width;
            if (srcr > Width - x)
                srcr = Width - x;
            int srcb = source.Height;
            if (srcb > Height - y)
                srcb = Height - y;
            for (int c = srct; c < srcb; c++)
                for (int r = srcl; r < srcr; r++)
                    if (source[r, c] != 0)
                        this[x + r, y + c] = source[r, c];
        }

        public void DrawBitmapComposited(BitmapBits source, Point location) { DrawBitmapComposited(source, location.X, location.Y); }

        public void DrawBitmapBounded(BitmapBits source, int x, int y)
        {
            int srcl = 0;
            if (x < 0)
                srcl = -x;
            int srct = 0;
            if (y < 0)
                srct = -y;
            int srcr = source.Width;
            if (srcr > Width - x)
                srcr = Width - x;
            int srcb = source.Height;
            if (srcb > Height - y)
                srcb = Height - y;
            for (int c = srct; c < srcb; c++)
                Array.Copy(source.Bits, c * source.Width + srcl, Bits, ((y + c) * Width) + x + srcl, srcr - srcl);
        }

        public void DrawBitmapBounded(BitmapBits source, Point location) { DrawBitmapComposited(source, location.X, location.Y); }

        public void Flip(bool XFlip, bool YFlip)
        {
            if (!XFlip & !YFlip)
                return;
            if (XFlip)
            {
                for (int y = 0; y < Height; y++)
                {
                    int addr = y * Width;
                    Array.Reverse(Bits, addr, Width);
                }
            }
            if (YFlip)
            {
                byte[] tmppix = new byte[Bits.Length];
                for (int y = 0; y < Height; y++)
                {
                    int srcaddr = y * Width;
                    int dstaddr = (Height - y - 1) * Width;
                    Array.Copy(Bits, srcaddr, tmppix, dstaddr, Width);
                }
                Bits = tmppix;
            }
        }

        public void Clear()
        {
            Array.Clear(Bits, 0, Bits.Length);
        }

        public static BitmapBits FromTile(byte[] art, int index)
        {
            BitmapBits bmp = new BitmapBits(8, 8);
            if (index * 32 + 32 <= art.Length)
            {
                for (int i = 0; i < 32; i++)
                {
                    bmp.Bits[i * 2] = (byte)(art[i + (index * 32)] >> 4);
                    bmp.Bits[(i * 2) + 1] = (byte)(art[i + (index * 32)] & 0xF);
                }
            }
            return bmp;
        }

        public byte[] ToTile()
        {
            List<byte> res = new List<byte>();
            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x += 2)
                    res.Add((byte)(((this[x, y] & 0xF) << 4) | (this[x + 1, y] & 0xF)));
            return res.ToArray();
        }

        public void Rotate(int R)
        {
            byte[] tmppix = new byte[Bits.Length];
            switch (R)
            {
                case 1:
                    for (int y = 0; y < Height; y++)
                    {
                        int srcaddr = y * Width;
                        int dstaddr = (Width * (Width - 1)) + y;
                        for (int x = 0; x < Width; x++)
                        {
                            tmppix[dstaddr] = Bits[srcaddr + x];
                            dstaddr -= Width;
                        }
                    }
                    Bits = tmppix;
                    int h = Height;
                    Height = Width;
                    Width = h;
                    break;
                case 2:
                    Flip(true, true);
                    break;
                case 3:
                    for (int y = 0; y < Height; y++)
                    {
                        int srcaddr = y * Width;
                        int dstaddr = Height - 1 - y;
                        for (int x = 0; x < Width; x++)
                        {
                            tmppix[dstaddr] = Bits[srcaddr + x];
                            dstaddr += Width;
                        }
                    }
                    Bits = tmppix;
                    h = Height;
                    Height = Width;
                    Width = h;
                    break;
            }
        }

        public BitmapBits Scale(int factor)
        {
            BitmapBits res = new BitmapBits(Width * factor, Height * factor);
            for (int y = 0; y < res.Height; y++)
                for (int x = 0; x < res.Width; x++)
                    res[x, y] = this[(x / factor), (y / factor)];
            return res;
        }

        public void DrawLine(byte index, int x1, int y1, int x2, int y2)
        {
            bool steep = Math.Abs(y2 - y1) > Math.Abs(x2 - x1);
            if (steep)
            {
                int tmp = x1;
                x1 = y1;
                y1 = tmp;
                tmp = x2;
                x2 = y2;
                y2 = tmp;
            }
            if (x1 > x2)
            {
                int tmp = x1;
                x1 = x2;
                x2 = tmp;
                tmp = y1;
                y1 = y2;
                y2 = tmp;
            }
            int deltax = x2 - x1;
            int deltay = Math.Abs(y2 - y1);
            double error = 0;
            double deltaerr = (double)deltay / (double)deltax;
            int ystep;
            int y = y1;
            if (y1 < y2) ystep = 1; else ystep = -1;
            for (int x = x1; x <= x2; x++)
            {
                if (steep)
                {
                    if (x >= 0 & x < Height & y >= 0 & y < Width)
                        this[y, x] = index;
                }
                else
                {
                    if (y >= 0 & y < Height & x >= 0 & x < Width)
                        this[x, y] = index;
                }
                error = error + deltaerr;
                if (error >= 0.5)
                {
                    y = y + ystep;
                    error = error - 1.0;
                }
            }
        }

        public void DrawLine(byte index, Point p1, Point p2) { DrawLine(index, p1.X, p1.Y, p2.X, p2.Y); }

        public void DrawRectangle(byte index, int x, int y, int width, int height)
        {
            DrawLine(index, x, y, x + width, y);
            DrawLine(index, x, y, x, y + height);
            DrawLine(index, x + width, y, x + width, y + height);
            DrawLine(index, x, y + height, x + width, y + height);
        }

        public void DrawRectangle(byte index, System.Drawing.Rectangle rect) { DrawRectangle(index, rect.X, rect.Y, rect.Width, rect.Height); }

        public void FillRectangle(byte index, int x, int y, int width, int height)
        {
            int srcl = 0;
            if (x < 0)
                srcl = -x;
            int srct = 0;
            if (y < 0)
                srct = -y;
            int srcr = width;
            if (srcr > Width - x)
                srcr = Width - x;
            int srcb = height;
            if (srcb > Height - y)
                srcb = Height - y;
            for (int c = srct; c < srcb; c++)
                for (int r = srcl; r < srcr; r++)
                    this[x + r, y + c] = index;
        }

        public void FillRectangle(byte index, System.Drawing.Rectangle rect) { FillRectangle(index, rect.X, rect.Y, rect.Width, rect.Height); }

        public override bool Equals(object obj)
        {
            if (base.Equals(obj)) return true;
            BitmapBits other = obj as BitmapBits;
            if (other == null) return false;
            if (Width != other.Width | Height != other.Height) return false;
            return System.Linq.Enumerable.SequenceEqual(Bits, other.Bits);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public BitmapBits GetSection(int x, int y, int width, int height)
        {
            BitmapBits result = new BitmapBits(width, height);
            for (int v = 0; v < height; v++)
                Array.Copy(this.Bits, ((y + v) * this.Width) + x, result.Bits, v * width, width);
            return result;
        }

        public static BitmapBits Read1bppTile(Stream stream)
        {
            BitmapBits result = new BitmapBits(8, 8);
            BinaryReader br = new BinaryReader(stream);
            for (int y = 0; y < 8; y++)
            {
                byte b = br.ReadByte();
                result[0, y] = (byte)(b & 1);
                result[1, y] = (byte)((b >> 1) & 1);
                result[2, y] = (byte)((b >> 2) & 1);
                result[3, y] = (byte)((b >> 3) & 1);
                result[4, y] = (byte)((b >> 4) & 1);
                result[5, y] = (byte)((b >> 5) & 1);
                result[6, y] = (byte)((b >> 6) & 1);
                result[7, y] = (byte)((b >> 7) & 1);
            }
            return result;
        }

        public static BitmapBits ReadPCX(Stream stream) { Color[] palette; return ReadPCX(stream, out palette); }

        public static BitmapBits ReadPCX(Stream stream, out Color[] palette)
        {
            BinaryReader br = new BinaryReader(stream);
            stream.Seek(8, SeekOrigin.Current);
            BitmapBits pix = new BitmapBits(br.ReadUInt16() + 1, br.ReadUInt16() + 1);
            stream.Seek(0x36, SeekOrigin.Current);
            int stride = br.ReadUInt16();
            byte[] buffer = new byte[stride];
            stream.Seek(0x3C, SeekOrigin.Current);
            for (int y = 0; y < pix.Height; y++)
            {
                int i = 0;
                while (i < stride)
                {
                    int run = 1;
                    byte val = br.ReadByte();
                    if ((val & 0xC0) == 0xC0)
                    {
                        run = val & 0x3F;
                        val = br.ReadByte();
                    }
                    for (int r = 0; r < run; r++)
                        buffer[i++] = val;
                }
                for (int x = 0; x < pix.Width; x++)
                    pix[x, y] = buffer[x];
            }
            palette = new Color[256];
            if (br.ReadByte() != 0xC) return pix;
            for (int i = 0; i < 256; i++)
                palette[i] = Color.FromArgb(br.ReadByte(), br.ReadByte(), br.ReadByte());
            return pix;
        }

        public void ReplaceColor(byte old, byte @new)
        {
            for (int i = 0; i < Bits.Length; i++)
                if (Bits[i] == old)
                    Bits[i] = @new;
        }

        public static BitmapBits ReadHocusImage(Stream stream)
        {
            BinaryReader br = new BinaryReader(stream);
            int w = br.ReadUInt16() * 4;
            int h = br.ReadUInt16();
            BitmapBits result = new BitmapBits(w, h);
            for (int i = 0; i < 4; i++)
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x += 4)
                        result[x + i, y] = br.ReadByte();
            return result;
        }

        public void WriteHocusImage(Stream stream)
        {
            BinaryWriter bw = new BinaryWriter(stream);
            bw.Write((ushort)(Width / 4));
            bw.Write((ushort)Height);
            for (int i = 0; i < 4; i++)
                for (int y = 0; y < Height; y++)
                    for (int x = 0; x < Width; x += 4)
                        bw.Write(this[x + i, y]);
        }
    }
}