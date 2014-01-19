using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace HocusSharp
{
    public static class Extensions
    {
        /// <summary>
        /// Reads a null-terminated ASCII string from the current stream and advances the position by <paramref name="length"/> bytes.
        /// </summary>
        /// <param name="length">The maximum length of the string, in bytes.</param>
        public static string ReadString(this BinaryReader br, int length)
        {
            byte[] buffer = br.ReadBytes(length);
            for (int i = 0; i < length; i++)
                if (buffer[i] == 0)
                    return Encoding.ASCII.GetString(buffer, 0, i);
            return Encoding.ASCII.GetString(buffer);
        }

        public static void WriteString(this BinaryWriter bw, string value, int length)
        {
            if (value.Length > length)
                value = value.Substring(0, length);
            bw.Write(Encoding.ASCII.GetBytes(value));
            if (length > value.Length)
                bw.Write(new byte[length - value.Length]);
        }

        public static int CountLines(this string[] lines)
        {
            for (int i = lines.Length - 1; i >= 0; i--)
                if (!string.IsNullOrEmpty(lines[i]))
                    return i + 1;
            return 0;
        }
    }
}