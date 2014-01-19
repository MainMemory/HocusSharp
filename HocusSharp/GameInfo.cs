using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace HocusSharp
{
    internal class GameInfo
    {
        [IniIgnore]
        public int TableOfContents { get; set; }
        [IniName("tocloc")]
        public string TableOfContentsString
        {
            get { return TableOfContents.ToString("X"); }
            set { TableOfContents = int.Parse(value, NumberStyles.HexNumber); }
        }
        [IniIgnore]
        public int IdentifierLocation { get; set; }
        [IniName("idloc")]
        public string IdentifierLocationString
        {
            get { return IdentifierLocation.ToString("X"); }
            set { IdentifierLocation = int.Parse(value, NumberStyles.HexNumber); }
        }
        [IniName("idstr")]
        public string IdentifierString { get; set; }

        public static Dictionary<string, GameInfo> Load(string file)
        {
            return IniFile.Deserialize<Dictionary<string, GameInfo>>(file);
        }

        public static void Save(Dictionary<string, GameInfo> info, string file)
        {
            IniFile.Serialize(info, file);
        }
    }

    public static class DATFile
    {
        public static Dictionary<string, byte[]> Load(string exefile)
        {
            string pathbase = Path.Combine(Environment.CurrentDirectory, Path.GetDirectoryName(exefile));
            string datfile = Path.ChangeExtension(exefile, "DAT");
            Dictionary<string, GameInfo> infodict = GameInfo.Load("Hocus Pocus versions.ini");
            KeyValuePair<string, GameInfo> info = default(KeyValuePair<string, GameInfo>);
            using (FileStream exestr = File.OpenRead(exefile))
            using (BinaryReader exereader = new BinaryReader(exestr))
            {
                foreach (KeyValuePair<string, GameInfo> item in infodict)
                    if (item.Value.IdentifierLocation + item.Value.IdentifierString.Length < exestr.Length)
                    {
                        exestr.Seek(item.Value.IdentifierLocation, SeekOrigin.Begin);
                        if (Encoding.ASCII.GetString(exereader.ReadBytes(item.Value.IdentifierString.Length)).Equals(item.Value.IdentifierString, StringComparison.Ordinal))
                        {
                            info = item;
                            break;
                        }
                    }
                exestr.Seek(info.Value.TableOfContents, SeekOrigin.Begin);
                using (FileStream datstr = File.OpenRead(datfile))
                using (BinaryReader datreader = new BinaryReader(datstr))
                {
                    Dictionary<string, byte[]> filelist = new Dictionary<string, byte[]>();
                    foreach (string item in File.ReadAllLines(info.Key + " files.lst"))
                    {
                        datstr.Seek(exereader.ReadUInt32(), SeekOrigin.Begin);
                        filelist.Add(item, datreader.ReadBytes(exereader.ReadInt32()));
                    }
                    return filelist;
                }
            }
        }

        public static void Write(string exefile, Dictionary<string, byte[]> datfile)
        {
            string pathbase = Path.Combine(Environment.CurrentDirectory, Path.GetDirectoryName(exefile));
            Dictionary<string, GameInfo> infodict = GameInfo.Load("Hocus Pocus versions.ini");
            KeyValuePair<string, GameInfo> info = default(KeyValuePair<string, GameInfo>);
            using (FileStream exestr = File.Open(exefile, FileMode.Open, FileAccess.ReadWrite))
            {
                BinaryReader exereader = new BinaryReader(exestr);
                foreach (KeyValuePair<string, GameInfo> item in infodict)
                    if (item.Value.IdentifierLocation + item.Value.IdentifierString.Length < exestr.Length)
                    {
                        exestr.Seek(item.Value.IdentifierLocation, SeekOrigin.Begin);
                        if (Encoding.ASCII.GetString(exereader.ReadBytes(item.Value.IdentifierString.Length)).Equals(item.Value.IdentifierString, StringComparison.Ordinal))
                        {
                            info = item;
                            break;
                        }
                    }
                exestr.Seek(info.Value.TableOfContents, SeekOrigin.Begin);
                using (FileStream datstr = File.Create(Path.ChangeExtension(exefile, "DAT")))
                using (BinaryWriter datwriter = new BinaryWriter(datstr))
                using (BinaryWriter exewriter = new BinaryWriter(exestr))
                    foreach (string item in File.ReadAllLines(info.Key + " files.lst"))
                    {
                        byte[] file = datfile[item];
                        exewriter.Write((int)datstr.Position);
                        exewriter.Write(file.Length);
                        datwriter.Write(file);
                    }
            }
        }
    }
}