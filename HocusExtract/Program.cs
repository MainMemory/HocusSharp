using System;
using System.Collections.Generic;
using System.IO;
using HocusSharp;

namespace HocusExtract
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
            string dir = Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file) + " Data Files")).FullName;
            foreach (KeyValuePair<string, byte[]> item in DATFile.Load(file))
                File.WriteAllBytes(Path.Combine(dir, item.Key), item.Value);
        }
    }
}