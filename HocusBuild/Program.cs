using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using HocusSharp;

namespace HocusBuild
{
    class Program
    {
        static void Main(string[] args)
        {
            string file;
            if (args.Length > 0)
                file = args[0];
            else
            {
                Console.Write("File: ");
                file = Console.ReadLine();
            }
            string dir = Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file) + " Data Files");
            Dictionary<string, byte[]> datfile = new Dictionary<string, byte[]>();
            foreach (string item in Directory.GetFiles(dir))
                datfile.Add(Path.GetFileName(item), File.ReadAllBytes(item));
            DATFile.Write(file, datfile);
        }
    }
}