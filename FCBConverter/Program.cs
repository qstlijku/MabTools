/* 
 * FCBConverter
 * Copyright (C) 2020  Jakub Mareček (info@jakubmarecek.cz)
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with FCBConverter.  If not, see <https://www.gnu.org/licenses/>.
 */

using Gibbed.IO;
using K4os.Compression.LZ4;
using LZ4Sharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using UnluacNET;

namespace FCBConverter
{
    class Program
    {
        public static string m_Path = "";

        static readonly string listFiles = @"\FCBConverterFileNames.list";
        static readonly string listFiles_5 = @"\FCBConverterFileNames_5.list";
        public static Dictionary<ulong, string> listFilesDict = new Dictionary<ulong, string>();

        static readonly string listStrings = @"\FCBConverterStrings.list";
        public static Dictionary<uint, string> listStringsDict = new Dictionary<uint, string>();

        static readonly string settingsFile = @"\FCBConverterSettings.xml";
        static readonly string defsFile = @"\FCBConverterDefinitions.xml";

        static DefinitionsLoader definitionLoader;

        public static bool isCompressEnabled = true;
        public static bool isCombinedMoveFile = false;
        public static bool isNewDawn = false;
        public static bool isFC6 = false;
        public static bool isEntLibNamesStores = false;
        public static bool isFC2 = false;
        public static string excludeFilesFromCompress = "";
        public static string excludeFilesFromPack = "";

        public static string version = "20230112-2300";

        public static string matWarn = " - DO NOT DELETE THIS! DO NOT CHANGE LINE NUMBER!";
        public static string xmlheader = "Converted by FCBConverter v" + version + ", author ArmanIII.";
        public static string xmlheaderlua = "Converted using UnluacNET by Fireboyd78";
        public static string xmlheaderfcb = "Please remember that types are calculated and they may not be exactly the same as they are. Take care about this.";
        public static string xmlheaderthanks = "Based on Gibbed's Dunia Tools. Special thanks to: Fireboyd78 (FCBastard), Ekey (FC5 Unpacker), Gibbed, xBaebsae, id-daemon, Ganic, legendhavoc175, miru, eprilx";
        public static string xmlheaderbnk = $"Adding new WEM files is possible. DIDX will be calculated automatically, only required is WEMFile entry in DATA.{Environment.NewLine}Since not all binary data are converted into readable format, you can use Wwise to create your own SoundBank and then use FCBConverter to edit IDs inside the SoundBank.";

        static void Main(string[] args)
        {

            using var processModule = Process.GetCurrentProcess().MainModule;
            m_Path = Path.GetDirectoryName(processModule?.FileName);

            Console.Title = "FCBConverter";

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("*******************************************************************************************");
            Console.WriteLine("**** FCBConverter v" + version);
            Console.WriteLine("****   Author: ArmanIII");
            Console.WriteLine("****   " + xmlheaderthanks);
            Console.WriteLine("*******************************************************************************************");
            Console.ResetColor();
            Console.WriteLine("");

            if (args.Length < 1)
            {
                Console.WriteLine("Please include an MAB file as argument!");
            }

            Arguments arguments = new(args);

            string file = args[0];

            Console.Title = "FCBConverter - " + file;

            bool bKeep = false;

            if (LoadSetting("CompressFile") == "false" && arguments["enablecompress"] != "true")
            {
                Console.WriteLine("Compression disabled.");
                Console.WriteLine("");
                isCompressEnabled = false;
            }

            excludeFilesFromCompress = arguments["excludeFilesFromCompress"] ?? excludeFilesFromCompress;
            excludeFilesFromPack = arguments["excludeFilesFromPack"] ?? excludeFilesFromPack;

            if (arguments["disablecompress"] == "true")
            {
                Console.WriteLine("Compression disabled via param.");
                Console.WriteLine("");
                isCompressEnabled = false;
            }

            bKeep = arguments["keep"] == "true";
            isFC2 = arguments["fc2"] == "true";

            if (file.EndsWith("entitylibrarynamestoresid.fcb"))
                isEntLibNamesStores = true;

            definitionLoader = new DefinitionsLoader(m_Path + defsFile, file);

            try
            {
                string source = arguments["source"] ?? "";

                if (Directory.Exists(source) && arguments["fat"] != null && arguments["fat"].EndsWith(".fat"))
                {
                    int ver = 10;

                    if (arguments["v11"] == "true")
                        ver = 11;

                    if (arguments["v9"] == "true")
                        ver = 9;

                    if (arguments["v5"] == "true")
                        ver = 5;

                    LoadFile();
                    PackBigFile(source, arguments["fat"], ver);
                    FIN();
                }

                else if (source.EndsWith(".fat") && arguments["out"] != null && arguments["single"] != null) // excludeFromCompress is used as file name
                {
                    UnpackBigFile(source, arguments["out"], arguments["single"]);
                    FIN();
                }

                else if (file.EndsWith(".fat") && source == "") // specific - doesn't use args, for Win Open With
                {
                    UnpackBigFile(file, "");
                    FIN();
                }

                else if (source.EndsWith(".fat"))
                {
                    UnpackBigFile(source, arguments["out"] ?? "");
                    FIN();
                }

                else if (source == "") // specific - doesn't use args, for Win Open With
                {
                    Console.WriteLine("Note: Win Open With version used");
                    Processing(file, "");
                }

                else if (File.Exists(source))
                {
                    Console.WriteLine("File exists, processing...");
                    Processing(source, arguments["out"] ?? "");
                }

                else if ((Directory.Exists(source) || source == @"\") && arguments["filter"] != null)
                {
                    if (source == @"\")
                        source = Directory.GetCurrentDirectory();

                    ProcessSubFolders(source, arguments["filter"], arguments["subfolders"] == "true");
                    Console.WriteLine("Job done!");
                }

                else
                {
                    Console.WriteLine("Input file / directory doesn't exist!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.WriteLine("");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                Environment.Exit(1);
            }

            if (bKeep)
            {
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }

            return;
        }

        static void ProcessSubFolders(string folder, string filter, bool subFolders)
        {
            DirectoryInfo d = new DirectoryInfo(folder);

            string[] searchPatterns = filter.Split(',');
            foreach (string sep in searchPatterns)
            {
                FileInfo[] files = d.GetFiles(sep);

                foreach (FileInfo fileInfo in files)
                {
                    Console.WriteLine("Processing: " + fileInfo.FullName + "...");
                    Processing(fileInfo.FullName, "");
                }
            }

            if (subFolders)
            {
                DirectoryInfo[] dirs = d.GetDirectories();

                foreach (DirectoryInfo dirInfo in dirs)
                {
                    ProcessSubFolders(dirInfo.FullName, filter, true);
                }
            }
        }

        static void Processing(string file, string outputFile)
        {
            Console.Title = "FCBConverter - " + file;

            if (file.Contains("worldsector"))
            {
                isCompressEnabled = false;
            }

            // ********************************************************************************************************************************************

            if (file.EndsWith(".feu"))
            {
                byte[] bytes = File.ReadAllBytes(file);

                bytes[0] = (byte)'F';
                bytes[1] = (byte)'W';
                bytes[2] = (byte)'S';

                string newPath = file.Replace(".feu", ".swf");

                File.WriteAllBytes(newPath, bytes);

                FIN();
                return;
            }

            if (file.EndsWith(".swf"))
            {
                byte[] bytes = File.ReadAllBytes(file);

                bytes[0] = (byte)'U';
                bytes[1] = (byte)'E';
                bytes[2] = (byte)'F';

                string newPath = file.Replace(".swf", ".feu");

                File.WriteAllBytes(newPath, bytes);

                FIN();
                return;
            }

            else if (file.EndsWith(".mab"))
            {
                Console.WriteLine("MAB detected!");
                FC6MarkupExtr(file);
                FIN();
                return;
            }

            // ********************************************************************************************************************************************

            var tmpformat = File.OpenRead(file);
            ushort fmt = tmpformat.ReadValueU8();
            tmpformat.Close();

            if ((file.EndsWith(".cseq") && fmt == 0) || file.EndsWith(".gosm.xml") || file.EndsWith(".rml") || (file.EndsWith(".ndb") && fmt == 0))
            {
                string workingOriginalFile;

                if (outputFile != "")
                    workingOriginalFile = outputFile;
                else
                    workingOriginalFile = Path.GetDirectoryName(file) + "\\" + Path.GetFileName(file) + (file.EndsWith(".ndb") || file.EndsWith(".cseq") ? ".rml" : "") + ".converted.xml";

                var rez = new Gibbed.Dunia2.FileFormats.XmlResourceFile();
                using (var input = File.OpenRead(file))
                {
                    rez.Deserialize(input);
                }

                var settings = new XmlWriterSettings
                {
                    Encoding = Encoding.UTF8,
                    Indent = true,
                    OmitXmlDeclaration = true
                };

                using (var writer = XmlWriter.Create(workingOriginalFile, settings))
                {
                    writer.WriteStartDocument();
                    Gibbed.Dunia2.ConvertXml.Program.WriteNode(writer, rez.Root);
                    writer.WriteEndDocument();
                }

                FIN();
                return;
            }

            // ********************************************************************************************************************************************

            if (file.EndsWith(".lua"))
            {
                byte[] luaBytesLuaq = Array.Empty<byte>();

                FileStream luaFile = File.OpenRead(file);
                uint luaType = luaFile.ReadValueU32();

                if (luaType == 0x4341554C)
                {
                    int luaLen = luaFile.ReadValueS32();
                    luaBytesLuaq = luaFile.ReadBytes(luaLen);
                    byte[] luaBytesLuac = luaFile.ReadBytes((int)(luaFile.Length - luaLen - (sizeof(int) * 2)));

                    File.WriteAllBytes(file + ".converted.xml", luaBytesLuac);
                }

                if (luaType == 0x61754C1B)
                {
                    luaFile.Seek(0, SeekOrigin.Begin);
                    luaBytesLuaq = luaFile.ReadBytes((int)luaFile.Length);
                }

                luaFile.Close();

                MemoryStream luaMS = new(luaBytesLuaq);
                var header = new BHeader(luaMS);
                LFunction lMain = header.Function.Parse(luaMS, header);

                var d = new Decompiler(lMain);
                d.Decompile();
                var writer = new StreamWriter(file + ".converted.lua", false, new UTF8Encoding(false));
                writer.WriteLine("--" + xmlheader);
                writer.WriteLine("--" + xmlheaderlua);
                writer.WriteLine("");
                d.Print(new Output(writer));
                writer.Flush();

                FIN();
                return;
            }

            if (file.EndsWith(".lua.converted.xml"))
            {
                string newLuaFile = file.Replace(".lua.converted.xml", "_new.lua");

                string luaLuaq = File.ReadAllText(file.Replace(".lua.converted.xml", ".lua.converted.lua"));
                string luaLuac = File.ReadAllText(file);

                luaLuaq = "--" + xmlheader + Environment.NewLine + luaLuaq;

                if (File.Exists(newLuaFile))
                    File.Delete(newLuaFile);

                FileStream bin = new FileStream(newLuaFile, FileMode.Create);
                bin.Write(BitConverter.GetBytes(0x4341554c), 0, 4);
                bin.Write(BitConverter.GetBytes(luaLuaq.Length), 0, sizeof(int));
                bin.Write(Encoding.UTF8.GetBytes(luaLuaq), 0, luaLuaq.Length);
                bin.Write(Encoding.UTF8.GetBytes(luaLuac), 0, luaLuac.Length);
                bin.Close();

                FIN();
                return;
            }

            // ********************************************************************************************************************************************

            LoadString();

            // ********************************************************************************************************************************************

            if (file.EndsWith(".oasis.bin"))
            {
                string workingOriginalFile;

                if (outputFile != "")
                    workingOriginalFile = outputFile;
                else
                    workingOriginalFile = Path.GetDirectoryName(file) + "\\" + Path.GetFileName(file) + ".converted.xml";

                OasisNew.OasisDeserialize(file, workingOriginalFile);

                FIN();
                return;
            }

            if (file.EndsWith(".oasis.bin.converted.xml"))
            {
                string workingOriginalFile;

                if (outputFile != "")
                    workingOriginalFile = outputFile;
                else
                {
                    workingOriginalFile = file.Replace(".oasis.bin.converted.xml", "");
                    workingOriginalFile = Path.GetDirectoryName(file) + "\\" + Path.GetFileNameWithoutExtension(workingOriginalFile) + "_new.oasis.bin";
                }

                OasisNew.OasisSerialize(file, workingOriginalFile);

                FIN();
                return;
            }

            if (file.EndsWith("oasisstrings_compressed.bin"))
            {
                string workingOriginalFile;

                if (outputFile != "")
                    workingOriginalFile = outputFile;
                else
                    workingOriginalFile = Path.GetDirectoryName(file) + "\\" + Path.GetFileName(file) + ".converted.xml";

                OasisNew.OasisDeserialize(file, workingOriginalFile);

                FIN();
                return;
            }

            if (file.EndsWith("oasisstrings_compressed.bin.converted.xml"))
            {
                string workingOriginalFile;

                if (outputFile != "")
                    workingOriginalFile = outputFile;
                else
                {
                    workingOriginalFile = file.Replace(".bin.converted.xml", "");
                    workingOriginalFile = Path.GetDirectoryName(file) + "\\new_" + Path.GetFileNameWithoutExtension(workingOriginalFile) + ".bin";
                }

                OasisNew.OasisSerialize(file, workingOriginalFile);

                FIN();
                return;
            }

            // ********************************************************************************************************************************************

            if (file.EndsWith(".markup.bin.converted.xml"))
            {
                MarkupConvertXml(file);
                FIN();
                return;
            }
            else if (file.EndsWith(".markup.bin"))
            {
                MarkupConvertBin(file);
                FIN();
                return;
            }
            else if (file.EndsWith(".mab.converted.xml"))
            {
                FC4MarkupPack(file);
                FIN();
                return;
            }

            // ********************************************************************************************************************************************

            if (file.EndsWith(".move.bin.converted.xml"))
            {
                MoveConvertXml(file);
                FIN();
                return;
            }
            else if (file.EndsWith(".move.bin"))
            {
                MoveConvertBin(file);
                FIN();
                return;
            }

            // ********************************************************************************************************************************************

            if (file.EndsWith("combinedmovefile.bin.converted.xml"))
            {
                isCombinedMoveFile = true;
                isCompressEnabled = false;
                CombinedMoveFileConvertXml(file);
                FIN();
                return;
            }
            else if (file.EndsWith("combinedmovefile.bin"))
            {
                isCombinedMoveFile = true;
                CombinedMoveFileConvertBin(file);
                FIN();
                return;
            }

            // ********************************************************************************************************************************************

            LoadFile(isFC2 ? 5 : 10);

            // ********************************************************************************************************************************************

            if (file.EndsWith("_depload.dat.converted.xml"))
            {
                DeploadConvertXml(file);
                FIN();
                return;
            }
            else if (file.EndsWith("_depload.dat"))
            {
                DeploadConvertDat(file);
                FIN();
                return;
            }

            // ********************************************************************************************************************************************

            if (file.EndsWith(".converted.xml"))
            {
                if (file.Replace(".converted.xml", "").EndsWith(".material.bin"))
                {
                    string newFileName = file.Replace(".converted.xml", "");
                    string matFile = newFileName.Replace(".bin", ".mat");
                    newFileName = newFileName.Replace(".material.bin", "_new" + ".material.bin");

                    ConvertXML(file, newFileName);

                    List<byte> bts = new List<byte>();
                    bts.AddRange(File.ReadAllBytes(matFile));
                    bts.AddRange(File.ReadAllBytes(newFileName));

                    File.WriteAllBytes(newFileName, bts.ToArray());
                }
                else if (file.EndsWith(".part.converted.xml") && File.Exists(file.Replace(".part.converted.xml", ".pt")))
                {
                    string newFileName = file.Replace(".converted.xml", "");
                    string matFile = newFileName.Replace(".part", ".pt");
                    newFileName = newFileName.Replace(".part", "_new.part");

                    ConvertXML(file, newFileName);

                    List<byte> bts = new List<byte>();
                    bts.AddRange(File.ReadAllBytes(matFile));
                    bts.AddRange(File.ReadAllBytes(newFileName));

                    File.WriteAllBytes(newFileName, bts.ToArray());
                }
                else if (file.EndsWith(".fcb.lzo.converted.xml"))
                {
                    string workingOriginalFile;

                    if (outputFile != "")
                        workingOriginalFile = outputFile;
                    else
                    {
                        workingOriginalFile = file.Replace(".lzo.converted.xml", "");
                        string extension = Path.GetExtension(workingOriginalFile);
                        workingOriginalFile = Path.GetDirectoryName(workingOriginalFile) + "\\" + Path.GetFileNameWithoutExtension(workingOriginalFile) + "_new" + extension;
                    }

                    var bof = new Gibbed.Dunia2.FileFormats.BinaryObjectFile();

                    var basePath = Path.ChangeExtension(file, null);

                    var doc = new XPathDocument(file);
                    var nav = doc.CreateNavigator();

                    var root = nav.SelectSingleNode("/object");

                    bof.Root = Gibbed.Dunia2.ConvertBinaryObject.Importing.Import(basePath, root, definitionLoader);

                    MemoryStream ms = new MemoryStream();
                    bof.Serialize(ms);
                    ms.Seek(0, SeekOrigin.Begin);

                    byte[] uncompressedBytes = ms.ToArray();

                    byte[] compressedBytes = new byte[uncompressedBytes.Length + (uncompressedBytes.Length / 16) + 64 + 3]; // weird magic
                    int outputSize = compressedBytes.Length;

                    var result = Gibbed.Dunia2.FileFormats.LZO.Compress(uncompressedBytes,
                                                0,
                                                uncompressedBytes.Length,
                                                compressedBytes,
                                                0,
                                                ref outputSize);

                    Array.Resize(ref compressedBytes, outputSize);

                    var output = File.Create(workingOriginalFile);
                    output.WriteValueS32(uncompressedBytes.Length);
                    output.WriteBytes(compressedBytes);
                    output.Flush();
                    output.Close();
                }
                else
                {
                    string workingOriginalFile;

                    if (outputFile != "")
                        workingOriginalFile = outputFile;
                    else
                    {
                        workingOriginalFile = file.Replace(".converted.xml", "");
                        string extension = Path.GetExtension(workingOriginalFile);
                        workingOriginalFile = Path.GetDirectoryName(workingOriginalFile) + "\\" + Path.GetFileNameWithoutExtension(workingOriginalFile) + "_new" + extension;
                    }

                    if (isCompressEnabled && new FileInfo(file).Length > 20000000)
                        Console.WriteLine("Compressing big files will take some time.");

                    ConvertXML(file, workingOriginalFile);
                }

                FIN();
                return;
            }
            else if (file.EndsWith(".obj") || file.EndsWith(".lib") || file.EndsWith(".cseq") || file.EndsWith(".fcb") || file.EndsWith(".ndb") || file.EndsWith(".bin") || file.EndsWith(".bwsk") || file.EndsWith(".part") || file.EndsWith(".dsc") || file.EndsWith(".skeleton") || file.EndsWith(".animtrackcol"))
            {
                string workingOriginalFile;

                if (outputFile != "")
                    workingOriginalFile = outputFile;
                else
                    workingOriginalFile = Path.GetDirectoryName(file) + "\\" + Path.GetFileName(file) + ".converted.xml";

                tmpformat = File.OpenRead(file);
                uint nbCF = tmpformat.ReadValueU32();
                uint ver = tmpformat.ReadValueU32();
                tmpformat.Close();

                if (nbCF != 1178821230 && file.EndsWith(".fcb")) // nbCF
                {
                    var aaa = File.OpenRead(file);
                    int bbb = (int)aaa.ReadValueU32();

                    byte[] buffer = new byte[16 * 1024];
                    MemoryStream ms = new MemoryStream();
                    int read;
                    while ((read = aaa.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        ms.Write(buffer, 0, read);
                    }
                    byte[] ddd = ms.ToArray();

                    aaa.Close();

                    byte[] eee = new byte[bbb];

                    var result = Gibbed.Dunia2.FileFormats.LZO.Decompress(ddd,
                                                0,
                                                ddd.Length,
                                                eee,
                                                0,
                                                ref bbb);

                    var bof = new Gibbed.Dunia2.FileFormats.BinaryObjectFile();
                    bof.Deserialize(new MemoryStream(eee));
                    Gibbed.Dunia2.ConvertBinaryObject.Exporting.Export(workingOriginalFile.Replace(".fcb", ".fcb.lzo"), bof, definitionLoader);
                }
                else
                {
                    if (file.EndsWith(".material.bin"))
                    {
                        byte[] bytes = File.ReadAllBytes(file);

                        int pos = IndexOf(bytes, new byte[] { 0x6E, 0x62, 0x43, 0x46 }); // nbCF

                        byte[] mat = bytes.Take(pos).ToArray();
                        byte[] fcb = bytes.Skip(pos).Take(bytes.Length).ToArray();

                        string newPathMat = file.Replace(".bin", ".mat");
                        File.WriteAllBytes(newPathMat, mat);
                        File.WriteAllBytes(file + "tmp", fcb);

                        ConvertFCB(file + "tmp", workingOriginalFile);

                        File.Delete(file + "tmp");
                    }
                    else if (ver != 2 && file.EndsWith(".part"))
                    {
                        byte[] bytes = File.ReadAllBytes(file);

                        int pos = IndexOf(bytes, new byte[] { 0x6E, 0x62, 0x43, 0x46, 0x02, 0x00 }); // nbCF

                        byte[] mat = bytes.Take(pos).ToArray();
                        byte[] fcb = bytes.Skip(pos).Take(bytes.Length).ToArray();

                        string newPathMat = file.Replace(".part", ".pt");
                        File.WriteAllBytes(newPathMat, mat);
                        File.WriteAllBytes(file + "tmp", fcb);

                        ConvertFCB(file + "tmp", workingOriginalFile);

                        File.Delete(file + "tmp");
                    }
                    else
                        ConvertFCB(file, workingOriginalFile);
                }

                FIN();
                return;
            }

            // ********************************************************************************************************************************************

            FIN();
        }

        static void FIN()
        {
            //File.WriteAllLines("a.txt", aaaa);
            Console.WriteLine("FIN");
            //Environment.Exit(0);
        }

        static string LoadSetting(string settingName)
        {
            XDocument settingsXML = XDocument.Load(m_Path + settingsFile);
            XElement root = settingsXML.Element("FCBConverter");
            string selSettVal = root.Element(settingName).Value;
            return selSettVal;
        }

        public static int IndexOf(byte[] arrayToSearchThrough, byte[] patternToFind)
        {
            if (patternToFind.Length > arrayToSearchThrough.Length)
                return -1;
            for (int i = 0; i < arrayToSearchThrough.Length - patternToFind.Length; i++)
            {
                bool found = true;
                for (int j = 0; j < patternToFind.Length; j++)
                {
                    if (arrayToSearchThrough[i + j] != patternToFind[j])
                    {
                        found = false;
                        break;
                    }
                }
                if (found)
                {
                    return i;
                }
            }
            return -1;
        }

        static void LoadFile(int dwVersion = 10)
        {
            Console.WriteLine("Loading list of files...");

            if (listFilesDict.Count() > 0)
                return;

            if (!File.Exists(m_Path + listFiles))
            {
                Console.WriteLine(m_Path + listFiles + " doesn't exist!");
                return;
            }

            string[] ss;

            if (dwVersion == 5)
                ss = File.ReadAllLines(m_Path + listFiles_5);
            else
                ss = File.ReadAllLines(m_Path + listFiles);

            for (int i = 0; i < ss.Length; i++)
            {
                ulong a = dwVersion == 5 ? Gibbed.Dunia2.FileFormats.CRC32.Hash(ss[i]) : Gibbed.Dunia2.FileFormats.CRC64.Hash(ss[i]);
                if (!listFilesDict.ContainsKey(a))
                    listFilesDict.Add(a, ss[i]);
            }

            Console.WriteLine("Files loaded: " + listFilesDict.Count);
        }

        static void LoadString()
        {
            Console.WriteLine("Loading list of strings...");

            if (listStringsDict.Count() > 0)
                return;

            if (!File.Exists(m_Path + listStrings))
            {
                Console.WriteLine(m_Path + listStrings + " doesn't exist!");
                return;
            }

            string[] ss = File.ReadAllLines(m_Path + listStrings);
            for (int i = 0; i < ss.Length; i++)
            {
                uint a = Gibbed.Dunia2.FileFormats.CRC32.Hash(ss[i]);
                if (!listStringsDict.ContainsKey(a))
                    listStringsDict.Add(a, ss[i]);
            }

            Console.WriteLine("Strings loaded: " + listStringsDict.Count);
        }

        public static void ConvertFCB(string inputPath, string outputPath)
        {
            var bof = new Gibbed.Dunia2.FileFormats.BinaryObjectFile();
            var input = File.OpenRead(inputPath);
            bof.Deserialize(input);
            input.Close();

            Gibbed.Dunia2.ConvertBinaryObject.Exporting.Export(outputPath, bof, definitionLoader);
        }

        public static void ConvertXML(string inputPath, string outputPath)
        {
            var bof = new Gibbed.Dunia2.FileFormats.BinaryObjectFile();

            var basePath = Path.ChangeExtension(inputPath, null);

            var doc = new XPathDocument(inputPath);
            var nav = doc.CreateNavigator();

            var root = nav.SelectSingleNode("/object");

            bof.Root = Gibbed.Dunia2.ConvertBinaryObject.Importing.Import(basePath, root, definitionLoader);

            var output = File.Create(outputPath);
            bof.Serialize(output);
            output.Close();
        }

        public static ulong GetFileHash(string fileName, int dwVersion = 10)
        {
            if (fileName.ToLowerInvariant().Contains("__unknown"))
            {
                var partName = Path.GetFileNameWithoutExtension(fileName);

                if (dwVersion >= 9)
                {
                    if (partName.Length > 16)
                    {
                        partName = partName.Substring(0, 16);
                    }
                }
                if (dwVersion == 5)
                {
                    if (partName.Length > 8)
                    {
                        partName = partName.Substring(0, 8);
                    }
                }

                return ulong.Parse(partName, NumberStyles.AllowHexSpecifier);
            }
            else
            {
                if (dwVersion >= 9)
                {
                    return Gibbed.Dunia2.FileFormats.CRC64.Hash(fileName);
                }
                if (dwVersion == 5)
                {
                    return Gibbed.Dunia2.FileFormats.CRC32.Hash(fileName);
                }
            }

            return 0;
        }

        static void DeploadConvertDat(string file)
        {
            FileStream DeploadStream = new FileStream(file, FileMode.Open);
            BinaryReader DeploadReader = new BinaryReader(DeploadStream);

            List<Depload.DependentFile> DependentFiles = new List<Depload.DependentFile>();
            List<ulong> DependencyFiles = new List<ulong>();
            List<byte> DependencyFilesTypes = new List<byte>();
            List<string> Types = new List<string>();

            int DependentFilesCount = DeploadReader.ReadInt32();
            for (int i = 0; i < DependentFilesCount; i++)
            {
                int dependencyFilesStartIndex = DeploadReader.ReadInt32();
                int countOfDependencyFiles = DeploadReader.ReadInt32();
                ulong fileHash = DeploadReader.ReadUInt64();
                DependentFiles.Add(new Depload.DependentFile { DependencyFilesStartIndex = dependencyFilesStartIndex, CountOfDependencyFiles = countOfDependencyFiles, FileHash = fileHash });
            }

            int DependencyFilesCount = DeploadReader.ReadInt32();
            for (int i = 0; i < DependencyFilesCount; i++)
            {
                ulong fileHash = DeploadReader.ReadUInt64();
                DependencyFiles.Add(fileHash);
            }

            int DependencyFilesTypesCount = DeploadReader.ReadInt32();
            for (int i = 0; i < DependencyFilesTypesCount; i++)
            {
                byte fileTypeIndex = DeploadReader.ReadByte();
                DependencyFilesTypes.Add(fileTypeIndex);
            }

            int TypesCount = DeploadReader.ReadInt32();
            for (int i = 0; i < TypesCount; i++)
            {
                uint typeHash = DeploadReader.ReadUInt32();
                Types.Add(listStringsDict.ContainsKey(typeHash) ? listStringsDict[typeHash] : typeHash.ToString("X8"));
            }

            DeploadReader.Dispose();
            DeploadStream.Dispose();

            // ****************************************************************************************************
            // ********** Proccess
            // ****************************************************************************************************

            List<Depload.DependencyLoaderItem> dependencyLoaderItems = new List<Depload.DependencyLoaderItem>();

            for (int i = 0; i < DependentFiles.Count; i++)
            {
                Depload.DependencyLoaderItem dependencyLoaderItem = new Depload.DependencyLoaderItem();
                dependencyLoaderItem.fileName = listFilesDict.ContainsKey(DependentFiles[i].FileHash) ? listFilesDict[DependentFiles[i].FileHash] : "__Unknown\\" + DependentFiles[i].FileHash.ToString("X16");

                dependencyLoaderItem.depFiles = new List<string>();
                dependencyLoaderItem.depTypes = new List<int>();

                for (int j = 0; j < DependentFiles[i].CountOfDependencyFiles; j++)
                {
                    ulong dependencyFile = DependencyFiles[DependentFiles[i].DependencyFilesStartIndex + j];
                    dependencyLoaderItem.depFiles.Add(listFilesDict.ContainsKey(dependencyFile) ? listFilesDict[dependencyFile] : "__Unknown\\" + dependencyFile.ToString("X16"));
                }

                for (int k = 0; k < DependentFiles[i].CountOfDependencyFiles; k++)
                {
                    byte depType = DependencyFilesTypes[DependentFiles[i].DependencyFilesStartIndex + k];
                    dependencyLoaderItem.depTypes.Add(depType);
                }

                dependencyLoaderItems.Add(dependencyLoaderItem);
            }

            // ****************************************************************************************************
            // ********** Write
            // ****************************************************************************************************

            XmlDocument xmlDoc = new XmlDocument();

            XmlDeclaration xmldecl;
            xmldecl = xmlDoc.CreateXmlDeclaration("1.0", "utf-8", "yes");

            XmlNode rootNode = xmlDoc.CreateElement("root");
            xmlDoc.AppendChild(rootNode);

            xmlDoc.InsertBefore(xmldecl, rootNode);

            XmlComment comment1 = xmlDoc.CreateComment(xmlheader);
            XmlComment comment2 = xmlDoc.CreateComment(xmlheaderthanks);

            xmlDoc.InsertBefore(comment1, rootNode);
            xmlDoc.InsertBefore(comment2, rootNode);

            for (int i = 0; i < dependencyLoaderItems.Count; i++)
            {
                XmlNode FileNode = xmlDoc.CreateElement("CBinaryResourceContainer");
                rootNode.AppendChild(FileNode);

                XmlAttribute FileNameAttribute = xmlDoc.CreateAttribute("ID");
                FileNameAttribute.Value = dependencyLoaderItems[i].fileName;
                FileNode.Attributes.Append(FileNameAttribute);

                for (int j = 0; j < dependencyLoaderItems[i].depFiles.Count; j++)
                {
                    XmlNode DependencyNode = xmlDoc.CreateElement(Types[dependencyLoaderItems[i].depTypes[j]]);
                    FileNode.AppendChild(DependencyNode);

                    XmlAttribute DependencyFileNameAttribute = xmlDoc.CreateAttribute("ID");
                    DependencyFileNameAttribute.Value = dependencyLoaderItems[i].depFiles[j];
                    DependencyNode.Attributes.Append(DependencyFileNameAttribute);
                }
            }

            xmlDoc.Save(file + ".converted.xml");
        }

        static void DeploadConvertXml(string file)
        {
            SortedDictionary<ulong, Depload.DependentFile> DependentFiles = new SortedDictionary<ulong, Depload.DependentFile>();
            List<ulong> DependencyFiles = new List<ulong>();
            List<byte> DependencyFilesTypes = new List<byte>();
            List<string> Types = new List<string>();


            XDocument doc = XDocument.Load(file);
            XElement root = doc.Element("root");

            IEnumerable<XElement> DependentFilesXML = root.Elements("CBinaryResourceContainer");
            foreach (XElement DependentFileXML in DependentFilesXML)
            {
                string fileName = DependentFileXML.Attribute("ID").Value.ToLowerInvariant();
                ulong fileHash = GetFileHash(fileName);

                Depload.DependentFile dependentFile = new Depload.DependentFile();
                dependentFile.DependencyFilesStartIndex = DependencyFiles.Count;
                dependentFile.FileHash = fileHash;

                int i = 0;
                IEnumerable<XElement> dependencies = DependentFileXML.Elements();
                foreach (XElement dependency in dependencies)
                {
                    string dependFileName = dependency.Attribute("ID").Value.ToString().ToLowerInvariant();
                    string dependType = dependency.Name.ToString();

                    if (!Types.Contains(dependType))
                        Types.Add(dependType);

                    DependencyFiles.Add(GetFileHash(dependFileName));
                    DependencyFilesTypes.Add((byte)Types.FindIndex(a => a == dependType));
                    i++;
                }

                dependentFile.CountOfDependencyFiles = i;

                //DependentFiles.Add(fileHash, dependentFile);
                DependentFiles[fileHash] = dependentFile;
            }

            string newName = file.Replace("_depload.dat.converted.xml", "_new_depload.dat");

            var output = File.Create(newName);
            output.WriteValueS32(DependentFiles.Count, 0);

            foreach (ulong dependentFileHash in DependentFiles.Keys)
            {
                var dependentFile = DependentFiles[dependentFileHash];

                output.WriteValueS32(dependentFile.DependencyFilesStartIndex, 0);
                output.WriteValueS32(dependentFile.CountOfDependencyFiles, 0);
                output.WriteValueU64(dependentFile.FileHash);
            }

            output.WriteValueS32(DependencyFiles.Count, 0);
            for (int i = 0; i < DependencyFiles.Count; i++)
            {
                output.WriteValueU64(DependencyFiles[i]);
            }

            output.WriteValueS32(DependencyFilesTypes.Count, 0);
            for (int i = 0; i < DependencyFilesTypes.Count; i++)
            {
                output.WriteByte(DependencyFilesTypes[i]);
            }

            output.WriteValueS32(Types.Count, 0);
            for (int i = 0; i < Types.Count; i++)
            {
                uint type = 0;

                if (listStringsDict.ContainsValue(Types[i]))
                    type = Gibbed.Dunia2.FileFormats.CRC32.Hash(Types[i]);
                else
                    type = uint.Parse(Types[i], NumberStyles.AllowHexSpecifier);

                output.WriteValueU32(type, 0);
            }

            output.Close();
        }
        static void MarkupConvertBin(string file)
        {
            string onlyDir = Path.GetDirectoryName(file);

            XmlDocument xmlDoc = new XmlDocument();

            XmlDeclaration xmldecl;
            xmldecl = xmlDoc.CreateXmlDeclaration("1.0", "utf-8", "yes");

            XmlNode rootNode = xmlDoc.CreateElement("CMarkupResource");
            xmlDoc.AppendChild(rootNode);

            xmlDoc.InsertBefore(xmldecl, rootNode);

            XmlComment comment1 = xmlDoc.CreateComment(xmlheader);
            XmlComment comment2 = xmlDoc.CreateComment(xmlheaderthanks);

            xmlDoc.InsertBefore(comment1, rootNode);
            xmlDoc.InsertBefore(comment2, rootNode);


            FileStream MarkupStream = new FileStream(file, FileMode.Open);
            BinaryReader MarkupReader = new BinaryReader(MarkupStream);

            int ver = MarkupReader.ReadInt16();
            ushort groupCount0 = MarkupReader.ReadUInt16();
            ushort groupCount1 = MarkupReader.ReadUInt16();
            ushort groupCount2 = MarkupReader.ReadUInt16();
            ushort groupCount3 = MarkupReader.ReadUInt16();
            ushort groupCount4 = MarkupReader.ReadUInt16();

            XmlAttribute rootNodeAttributeVersion = xmlDoc.CreateAttribute("Version");
            rootNodeAttributeVersion.Value = ver.ToString();
            rootNode.Attributes.Append(rootNodeAttributeVersion);

            MarkupWriteGroup(MarkupReader, onlyDir, xmlDoc, rootNode, groupCount0, 0);
            MarkupWriteGroup(MarkupReader, onlyDir, xmlDoc, rootNode, groupCount1, 1);
            MarkupWriteGroup(MarkupReader, onlyDir, xmlDoc, rootNode, groupCount2, 2);
            MarkupWriteGroup(MarkupReader, onlyDir, xmlDoc, rootNode, groupCount3, 3);
            MarkupWriteGroup(MarkupReader, onlyDir, xmlDoc, rootNode, groupCount4, 4);

            MarkupReader.Dispose();
            MarkupStream.Dispose();

            xmlDoc.Save(file + ".converted.xml");
        }

        static void MarkupWriteGroup(BinaryReader MarkupReader, string onlyDir, XmlDocument xmlDoc, XmlNode rootNode, int count, int group)
        {
            XmlNode groupNode = xmlDoc.CreateElement("FrameGroup" + group.ToString());
            rootNode.AppendChild(groupNode);

            for (int i = 0; i < count; i++)
            {
                float unknown = MarkupReader.ReadSingle();
                uint fcbByteLength = MarkupReader.ReadUInt32();
                ulong probablyFileNameHash = MarkupReader.ReadUInt64();
                byte[] fcbData = MarkupReader.ReadBytes((int)fcbByteLength);

                string tmp = onlyDir + "\\" + probablyFileNameHash.ToString();
                File.WriteAllBytes(tmp, fcbData);
                ConvertFCB(tmp, tmp + "c");
                XmlDocument doc = new XmlDocument();
                doc.Load(tmp + "c");

                XmlNode FrameNode = xmlDoc.CreateElement("Frame");
                FrameNode.AppendChild(xmlDoc.ImportNode(doc.SelectSingleNode("object"), true));
                groupNode.AppendChild(FrameNode);

                XmlAttribute FrameNodeAttributeUnknown = xmlDoc.CreateAttribute("Time");
                FrameNodeAttributeUnknown.Value = unknown.ToString(CultureInfo.InvariantCulture);
                FrameNode.Attributes.Append(FrameNodeAttributeUnknown);

                XmlAttribute FrameNodeAttributeFileNameHash = xmlDoc.CreateAttribute("FrameCRC64");
                FrameNodeAttributeFileNameHash.Value = probablyFileNameHash.ToString();
                FrameNode.Attributes.Append(FrameNodeAttributeFileNameHash);

                File.Delete(tmp);
                File.Delete(tmp + "c");
            }
        }

        static void MarkupConvertXml(string file)
        {
            string onlyDir = Path.GetDirectoryName(file);

            string newName = file.Replace(".markup.bin.converted.xml", "_new.markup.bin");

            var output = File.Create(newName);

            XDocument doc = XDocument.Load(file);
            XElement root = doc.Element("CMarkupResource");

            output.WriteValueU16(ushort.Parse(root.Attribute("Version").Value));
            output.WriteValueU16((ushort)root.Element("FrameGroup0").Elements().Count());
            output.WriteValueU16((ushort)root.Element("FrameGroup1").Elements().Count());
            output.WriteValueU16((ushort)root.Element("FrameGroup2").Elements().Count());
            output.WriteValueU16((ushort)root.Element("FrameGroup3").Elements().Count());
            output.WriteValueU16((ushort)root.Element("FrameGroup4").Elements().Count());

            byte cnt = 0;
            IEnumerable<XElement> allFrames = root.Descendants("Frame");
            foreach (XElement allFrame in allFrames)
            {
                float unknown = float.Parse(allFrame.Attribute("Time").Value, CultureInfo.InvariantCulture);

                string tmp = file + "_" + cnt.ToString();
                XElement fcb = allFrame.Element("object");
                fcb.Save(tmp);

                ConvertXML(tmp, tmp + "c");

                byte[] fcbByte = File.ReadAllBytes(tmp + "c");

                ulong crc = Gibbed.Dunia2.FileFormats.CRC64.Hash(fcbByte, 0, fcbByte.Length);

                output.WriteValueF32(unknown, 0);
                output.WriteValueU32((uint)fcbByte.Length);
                output.WriteValueU64(crc);
                output.WriteBytes(fcbByte);

                File.Delete(tmp);
                File.Delete(tmp + "c");
                cnt++;
            }

            output.Close();
        }
        // Find exactly n zeros in a row, to search for anchor headers
        static int[] FindZeros(byte[] a, int n)
        {
            int len = a.Length;
            List<int> poses = new List<int>();
            for (int i = 1; i <= len - n; i++)
            {
                if (a[i - 1] == 0)
                    continue;
                int k = 0;
                while (k < n)
                {
                    if (a[i + k] != 0)
                        break;
                    k++;
                }
                if (k == n && a[i + k] != 0)
                {
                    // Found n zeros followed by nonzero byte, so keep track of position
                    poses.Add(i - 1);
                }
            }
            return poses.ToArray();
        }

        static int[] FindBone(byte[] a, uint bone)
        {
            int len = a.Length;
            List<int> poses = new List<int>();
            // note: b is 4 bytes
            byte[] b = new byte[4];
            b[0] = (byte)((bone & 0x000000FFu) >> 0);
            b[1] = (byte)((bone & 0x0000FF00u) >> 8);
            b[2] = (byte)((bone & 0x00FF0000u) >> 16);
            b[3] = (byte)((bone & 0xFF000000u) >> 24);
            int n = 4;
            for (int i = 0; i < len - n; i++)
            {
                int k = 0;
                while (k < n)
                {
                    if (a[i + k] != b[k])
                        break;
                    k++;
                }
                if (k == n)
                {
                    poses.Add(i);
                }
            }
            return poses.ToArray();
        }

        static void FC6MarkupExtr(string filename)
        {
            string onlyDir = Path.GetDirectoryName(filename);
            if (filename.Contains('*'))
            {
                Console.WriteLine("* detected, performing batch conversion...");
                Console.WriteLine("onlyDir: " + onlyDir);
                Console.WriteLine("Original filename: " + filename);
                string[] subdirs = Directory.GetDirectories(onlyDir);
                foreach (string subdir in subdirs)
                {
                    string[] files = Directory.GetFiles(subdir);
                    foreach (string file in files)
                    {
                        Console.WriteLine("Processing file: " + file);
                        if (Path.GetFileName(file).StartsWith("tmp"))
                        {
                            continue;
                        }
                        FC6MabConvert(file, subdir);
                    }
                }
            }
            else
            {
                FC6MabConvert(filename, "");
            }
        }

        static void FC6MabConvert(string file, string onlyDir)
        {
            string newMab;
            if (onlyDir == "")
            {
                newMab = file.Replace(".mab", "_new.mab");
            }
            else
            {
                newMab = file;
            }
            FileStream MabStream = new FileStream(file, FileMode.Open);
            uint mabVersion = MabStream.ReadValueU32();
            Console.WriteLine("Version: " + mabVersion);
            // mabVersion = B0 for FC5 MAB, = B6 for FC6 MAB
            if (mabVersion == 176)
            {
                Console.WriteLine("Detected FC5 file");
                FC5MabConvert(MabStream, file, onlyDir);
                return;
            }
            MabStream.Seek(0, SeekOrigin.Begin);
            MemoryStream ms = new MemoryStream();
            MabStream.CopyTo(ms);

            var output0 = new MemoryStream(); // for beginning of file
            var output = new MemoryStream(); // for array 9

            // beginning of file
            MabStream.Seek(0, SeekOrigin.Begin);

            mabVersion = MabStream.ReadValueU32();
            uint mabEmpty0 = MabStream.ReadValueU32();
            uint mabEmpty1 = MabStream.ReadValueU32();
            uint mabEmpty2 = MabStream.ReadValueU32();

            uint hash0 = MabStream.ReadValueU32();
            uint hash1 = MabStream.ReadValueU32();
            uint hash2 = MabStream.ReadValueU32();
            uint hash3 = MabStream.ReadValueU32();
            uint hash4 = MabStream.ReadValueU32();
            uint AnimationFileLength = MabStream.ReadValueU32();

            float ClipLength = MabStream.ReadValueF32();
            float FrameRate = MabStream.ReadValueF32();
            Console.WriteLine("Clip length: " + ClipLength);
            Console.WriteLine("Frame rate: " + FrameRate);
            ushort count0 = MabStream.ReadValueU16();

            ushort[] unks = new ushort[7];
            for (int i = 0; i < 7; i++)
                unks[i] = MabStream.ReadValueU16();

            uint[] unkArrayA = new uint[11];
            for (int i = 0; i < 11; i++)
                unkArrayA[i] = MabStream.ReadValueU32();

            uint zero0 = MabStream.ReadValueU32();
            uint frameCount = MabStream.ReadValueU32();
            uint zero1 = MabStream.ReadValueU32();

            long firstPos = MabStream.Position;
            Console.WriteLine("Current position: " + firstPos);

            // start of bone and frame arrays: TODO later
            /*
            for (i = 0; i < count0; i++)
                uint BoneArray;

            for (i = 0; i < count0; i++)
                ubyte BoneArrayValue;

            // padding
            FSeek(FTell() + (4 - (FTell() % 4)) % 4);

            ushort zero3;

            for (i = 0; i < frameCount; i++)
                ushort frameArray;
            */

            // start of array 9
            int numAnchors = 100;
            MabStream.Seek(100, SeekOrigin.Begin);
            uint pos9 = MabStream.ReadValueU32();
            Console.WriteLine("Position of array 9: " + pos9);
            List<uint> bones = new List<uint>();
            for (int i = 0; i < 2; i++)
            {
                if (pos9 == 0)
                {
                    // Array 9 does not exist, do nothing
                    MabStream.Seek(firstPos, SeekOrigin.Begin);
                    break;
                }
                MabStream.Seek(pos9, SeekOrigin.Begin);
                MabStream.Seek(20, SeekOrigin.Current); // offset of first anchor
                int anchor = 1;
                while (anchor <= numAnchors)
                {
                    uint zero = 0;
                    if (anchor == 1)
                    {
                        uint sectionSize = MabStream.ReadValueU32();
                        Console.WriteLine("sectionSize: " + sectionSize);
                        if (i == 1)
                        {
                            output.WriteValueU32(0);
                            uint offset = (uint)(4 * (numAnchors - anchor));
                            if (anchor == 1)
                            {
                                offset += 4;
                            }
                            output.WriteValueU32((uint)(sectionSize + offset));
                            Console.WriteLine("new section size: " + (sectionSize + offset));
                        }
                        zero = MabStream.ReadValueU32();
                    }
                    uint signature = MabStream.ReadValueU32();
                    uint childBone = MabStream.ReadValueU32();
                    uint parentBone = MabStream.ReadValueU32();
                    uint targetBone = MabStream.ReadValueU32();
                    byte anchorType = MabStream.ReadValueU8();
                    byte blank = MabStream.ReadValueU8();
                    ushort nameStart = MabStream.ReadValueU16();
                    // Add 4 hex bytes of padding (00) here
                    uint subsectionEnd = MabStream.ReadValueU32();
                    uint maybeZero = MabStream.ReadValueU32();
                    if (maybeZero != 0)
                    {
                        Console.WriteLine("Reached last anchor");
                        numAnchors = anchor;
                    }
                    Console.WriteLine(signature);
                    Console.WriteLine(zero);
                    Console.WriteLine(childBone);
                    Console.WriteLine(parentBone);
                    Console.WriteLine(targetBone);
                    Console.WriteLine(anchorType);
                    Console.WriteLine(blank);
                    Console.WriteLine(nameStart);
                    if (anchor < numAnchors)
                    {
                        Console.WriteLine(subsectionEnd);
                    }
                    Console.WriteLine("Subsection end above");
                    if (i == 1)
                    {
                        output.WriteValueU32(0);
                        output.WriteValueU32(signature);
                        output.WriteValueU32(childBone);
                        output.WriteValueU32(parentBone);
                        output.WriteValueU32(targetBone);
                        output.WriteValueU8(anchorType);
                        output.WriteValueU8(blank);
                        ushort offset = (ushort)(4 * (numAnchors - anchor + 1));
                        output.WriteValueU16((ushort)(nameStart + offset));
                        if (anchor < numAnchors)
                        {
                            // No additional padding or subsection end for last anchor, immediate start of string instead
                            output.WriteValueU32(0);
                            output.WriteValueU32((uint)(subsectionEnd + 4 * (numAnchors - anchor)));
                        }
                        File.WriteAllBytes("tmp" + anchor + ".mab", output.ToArray());
                    }
                    if (!bones.Contains(childBone)) {
                        bones.Add(childBone);
                    }
                    anchor++;
                }
                Console.WriteLine("Number of anchors found: " + numAnchors);
            }
            int[] poses = { };
            if (pos9 != 0)
            {
                MabStream.Seek(-8, SeekOrigin.Current);
            }
            Console.WriteLine("Original length: " + AnimationFileLength);
            Console.WriteLine("Original stream length: " + MabStream.Length);
            Console.WriteLine("Current position: " + MabStream.Position);
            FileStream newStream;
            if (onlyDir == "")
            {
                newStream = new FileStream(newMab, FileMode.Create);
            }
            else
            {
                Directory.CreateDirectory(onlyDir + "_converted\\");
                newStream = new FileStream(onlyDir + "_converted\\" + Path.GetFileName(file), FileMode.Create);
            }
            byte[] lastBytes = MabStream.ReadBytes((int)(MabStream.Length - MabStream.Position));
            MabStream.Seek(firstPos, SeekOrigin.Begin);
            byte[] firstBytes = { };
            long newLength;
            if (pos9 != 0) {
                firstBytes = MabStream.ReadBytes((int)(pos9 + 20 - firstPos));
                // new length: output0 + firstBytes + output + lastBytes
                //          = firstPos + firstBytes + output + lastBytes
                newLength = pos9 + 20 + lastBytes.Length + output.Length;
            }
            else
            {
                // new length: output0 + lastBytes = MabStream
                newLength = firstPos + lastBytes.Length;
            }
            uint addedLength = (uint)(newLength - MabStream.Length);
            Console.WriteLine("New length (calculated): " + newLength);

            output0.WriteValueU32(mabVersion - 6);
            output0.WriteValueU32(mabEmpty0);
            output0.WriteValueU32(mabEmpty1);
            output0.WriteValueU32(mabEmpty2);

            output0.WriteValueU32(hash0);
            output0.WriteValueU32(hash1);
            output0.WriteValueU32(hash2);
            output0.WriteValueU32(hash3);
            output0.WriteValueU32(hash4);
            output0.WriteValueU32(AnimationFileLength + addedLength);

            output0.WriteValueF32(ClipLength);
            output0.WriteValueF32(FrameRate);
            output0.WriteValueU16(count0);

            for (int i = 0; i < 7; i++)
            {
                output0.WriteValueU16(unks[i]);
            }

            for (int i = 0; i < 10; i++)
            {
                output0.WriteValueU32(unkArrayA[i]);
            }
            output0.WriteValueU32(unkArrayA[10] + addedLength);
            output0.WriteValueU32(zero0);
            output0.WriteValueU32(zero1);
            output0.WriteValueU32(frameCount);
            File.WriteAllBytes("tmp0.mab", output0.ToArray());

            newStream.WriteBytes(output0.ToArray());
            if (pos9 == 0)
            {
                newStream.WriteBytes(lastBytes);
                MabStream.Dispose();
                newStream.Flush();
                newStream.Dispose();
                return;
            }
            foreach (uint boneToFind in bones)
            {
                poses = FindBone(lastBytes, boneToFind);
                Console.WriteLine("Finding: " + boneToFind);
                Console.WriteLine("Num pos found: " + poses.Length);
                var temp = new MemoryStream();
                for (int i = 0; i < poses.Length; i++)
                {
                    int pos = poses[i];
                    Console.WriteLine("Pos: " + pos);
                    for (int j = 0; j < 4; j++)
                    {
                        //temp.WriteValueU8(lastBytes[pos + j]);
                        lastBytes[pos - 4 + j] = lastBytes[pos - 8 + j];
                        lastBytes[pos - 8 + j] = 0;
                    }
                }
                //File.WriteAllBytes("tmpbones.mab", temp.ToArray());
            }
            newStream.WriteBytes(firstBytes);
            newStream.WriteBytes(output.ToArray());
            Console.WriteLine("Writing final bytes...");
            File.WriteAllBytes("tmpfinal.mab", lastBytes);
            newStream.WriteBytes(lastBytes);
            MabStream.Dispose();
            newStream.Flush();
            newStream.Dispose();
        }

        static void FC5MabConvert(FileStream MabStream, string file, string onlyDir)
        {
            string newMab;
            if (onlyDir == "")
            {
                newMab = file.Replace(".mab", "_old.mab");
            }
            else
            {
                newMab = file;
            }

            MemoryStream ms = new MemoryStream();
            MabStream.CopyTo(ms);

            byte[] msArr = ms.ToArray();

            var output0 = new MemoryStream(); // for beginning of file
            var output = new MemoryStream(); // for array 9

            // beginning of file
            MabStream.Seek(0, SeekOrigin.Begin);

            uint mabVersion = MabStream.ReadValueU32();
            uint mabEmpty0 = MabStream.ReadValueU32();
            uint mabEmpty1 = MabStream.ReadValueU32();
            uint mabEmpty2 = MabStream.ReadValueU32();

            uint hash0 = MabStream.ReadValueU32();
            uint hash1 = MabStream.ReadValueU32();
            uint hash2 = MabStream.ReadValueU32();
            uint hash3 = MabStream.ReadValueU32();
            uint hash4 = MabStream.ReadValueU32();
            uint AnimationFileLength = MabStream.ReadValueU32();

            float ClipLength = MabStream.ReadValueF32();
            float FrameRate = MabStream.ReadValueF32();
            Console.WriteLine("Clip length: " + ClipLength);
            Console.WriteLine("Frame rate: " + FrameRate);
            ushort count0 = MabStream.ReadValueU16();

            ushort[] unks = new ushort[7];
            for (int i = 0; i < 7; i++)
                unks[i] = MabStream.ReadValueU16();

            uint[] unkArrayA = new uint[11];
            for (int i = 0; i < 11; i++)
                unkArrayA[i] = MabStream.ReadValueU32();

            uint zero0 = MabStream.ReadValueU32();
            uint frameCount = MabStream.ReadValueU32();
            uint zero1 = MabStream.ReadValueU32();

            long firstPos = MabStream.Position;
            Console.WriteLine("Current position: " + firstPos);

            // start of bone and frame arrays: TODO later
            /*
            for (i = 0; i < count0; i++)
                uint BoneArray;

            for (i = 0; i < count0; i++)
                ubyte BoneArrayValue;

            // padding
            FSeek(FTell() + (4 - (FTell() % 4)) % 4);

            ushort zero3;

            for (i = 0; i < frameCount; i++)
                ushort frameArray;
            */

            // start of array 9
            int numAnchors = 100;
            MabStream.Seek(100, SeekOrigin.Begin);
            uint pos9 = MabStream.ReadValueU32();
            Console.WriteLine("Position of array 9: " + pos9);
            List<uint> bones = new List<uint>();
            for (int i = 0; i < 2; i++)
            {
                if (pos9 == 0)
                {
                    // Array 9 does not exist, do nothing
                    MabStream.Seek(firstPos, SeekOrigin.Begin);
                    break;
                }
                MabStream.Seek(pos9, SeekOrigin.Begin);
                MabStream.Seek(24, SeekOrigin.Current); // offset of first anchor
                int anchor = 1;
                while (anchor <= numAnchors)
                {
                    uint zero = 0;
                    if (anchor == 1)
                    {
                        uint sectionSize = MabStream.ReadValueU32();
                        Console.WriteLine("sectionSize: " + sectionSize);
                        if (i == 1)
                        {
                            uint offset = (uint)(4 * (numAnchors - anchor));
                            if (anchor == 1)
                            {
                                offset += 4;
                            }
                            output.WriteValueU32((uint)(sectionSize - offset));
                        }
                        zero = MabStream.ReadValueU32();
                    }
                    uint signature = MabStream.ReadValueU32();
                    uint childBone = MabStream.ReadValueU32();
                    uint parentBone = MabStream.ReadValueU32();
                    uint targetBone = MabStream.ReadValueU32();
                    byte anchorType = MabStream.ReadValueU8();
                    byte blank = MabStream.ReadValueU8();
                    ushort nameStart = MabStream.ReadValueU16();
                    // Added 4 hex bytes of padding (00) here
                    if (anchor < numAnchors)
                    {
                        zero = MabStream.ReadValueU32();
                    }
                    uint subsectionEnd = MabStream.ReadValueU32();
                    uint maybeZero = MabStream.ReadValueU32();
                    if (maybeZero != 0)
                    {
                        Console.WriteLine("Reached last anchor");
                        numAnchors = anchor;
                    }
                    Console.WriteLine("signature: " + signature);
                    Console.WriteLine(zero);
                    Console.WriteLine(childBone);
                    Console.WriteLine(parentBone);
                    Console.WriteLine(targetBone);
                    Console.WriteLine("anchorType: " + anchorType);
                    Console.WriteLine("blank: " + blank);
                    Console.WriteLine(nameStart);
                    if (anchor < numAnchors)
                    {
                        Console.WriteLine(subsectionEnd);
                    }
                    Console.WriteLine("Subsection end above");
                    if (i == 1)
                    {
                        output.WriteValueU32(0);
                        output.WriteValueU32(signature);
                        output.WriteValueU32(childBone);
                        output.WriteValueU32(parentBone);
                        output.WriteValueU32(targetBone);
                        output.WriteValueU8(anchorType);
                        output.WriteValueU8(blank);
                        ushort offset = (ushort)(4 * (numAnchors - anchor + 1));
                        output.WriteValueU16((ushort)(nameStart - offset));
                        if (anchor < numAnchors)
                        {
                            // No additional padding or subsection end for last anchor, immediate start of string instead
                            output.WriteValueU32((uint)(subsectionEnd - 4 * (numAnchors - anchor)));
                        }
                        File.WriteAllBytes("tmp" + anchor + "old.mab", output.ToArray());
                    }
                    if (!bones.Contains(childBone))
                    {
                        bones.Add(childBone);
                    }
                    anchor++;
                }
                Console.WriteLine("Number of anchors found: " + numAnchors);
            }
            int[] poses = { };
            if (pos9 != 0)
            {
                MabStream.Seek(-8, SeekOrigin.Current);
            }
            Console.WriteLine("Original length: " + AnimationFileLength);
            Console.WriteLine("Original stream length: " + MabStream.Length);
            Console.WriteLine("Current position: " + MabStream.Position);
            FileStream newStream;
            if (onlyDir == "")
            {
                newStream = new FileStream(newMab, FileMode.Create);
            }
            else
            {
                Directory.CreateDirectory(onlyDir + "_converted\\");
                newStream = new FileStream(onlyDir + "_converted\\" + Path.GetFileName(file), FileMode.Create);
            }
            byte[] lastBytes = MabStream.ReadBytes((int)(MabStream.Length - MabStream.Position));
            MabStream.Seek(firstPos, SeekOrigin.Begin);
            byte[] firstBytes = { };
            long newLength;
            if (pos9 != 0)
            {
                firstBytes = MabStream.ReadBytes((int)(pos9 + 20 - firstPos));
                // new length: output0 + firstBytes + output + lastBytes
                //          = firstPos + firstBytes + output + lastBytes
                newLength = pos9 + 20 + lastBytes.Length + output.Length;
            }
            else
            {
                // new length: output0 + lastBytes = MabStream
                newLength = firstPos + lastBytes.Length;
            }
            uint addedLength = (uint)(newLength - MabStream.Length);
            Console.WriteLine("New length (calculated): " + newLength);

            output0.WriteValueU32(mabVersion + 6);
            output0.WriteValueU32(mabEmpty0);
            output0.WriteValueU32(mabEmpty1);
            output0.WriteValueU32(mabEmpty2);

            output0.WriteValueU32(hash0);
            output0.WriteValueU32(hash1);
            output0.WriteValueU32(hash2);
            output0.WriteValueU32(hash3);
            output0.WriteValueU32(hash4);
            output0.WriteValueU32(AnimationFileLength + addedLength);

            output0.WriteValueF32(ClipLength);
            output0.WriteValueF32(FrameRate);
            output0.WriteValueU16(count0);

            for (int i = 0; i < 7; i++)
            {
                output0.WriteValueU16(unks[i]);
            }

            for (int i = 0; i < 10; i++)
            {
                output0.WriteValueU32(unkArrayA[i]);
            }
            output0.WriteValueU32(unkArrayA[10] + addedLength);
            output0.WriteValueU32(zero0);
            output0.WriteValueU32(zero1);
            output0.WriteValueU32(frameCount);
            File.WriteAllBytes("tmp0.mab", output0.ToArray());

            newStream.WriteBytes(output0.ToArray());
            if (pos9 == 0)
            {
                newStream.WriteBytes(lastBytes);
                MabStream.Dispose();
                newStream.Flush();
                newStream.Dispose();
                return;
            }
            foreach (uint boneToFind in bones)
            {
                poses = FindBone(lastBytes, boneToFind);
                Console.WriteLine("Finding: " + boneToFind);
                Console.WriteLine("Num pos found: " + poses.Length);
                var temp = new MemoryStream();
                for (int i = 0; i < poses.Length; i++)
                {
                    int pos = poses[i];
                    Console.WriteLine("Pos: " + pos);
                    for (int j = 0; j < 4; j++)
                    {
                        //temp.WriteValueU8(lastBytes[pos + j]);
                        lastBytes[pos - 8 + j] = lastBytes[pos - 4 + j];
                        lastBytes[pos - 4 + j] = 0;
                    }
                }
                //File.WriteAllBytes("tmpbones.mab", temp.ToArray());
            }
            newStream.WriteBytes(firstBytes);
            newStream.WriteBytes(output.ToArray());
            Console.WriteLine("Writing final bytes...");
            File.WriteAllBytes("tmpfinal.mab", lastBytes);
            newStream.WriteBytes(lastBytes);
            MabStream.Dispose();
            newStream.Flush();
            newStream.Dispose();
        }

        static void FC4MarkupPack(string file)
        {
            string originalMab = file.Replace(".converted.xml", "");
            string lastBinaryFile = file.Replace(".converted.xml", ".converted.bin");
            string newMab = originalMab.Replace(".mab", "_new.mab");

            XDocument doc = XDocument.Load(file);
            IEnumerable<XElement> frames = doc.Element("CMarkupResource").Elements("Frame");

            var output = new MemoryStream();

            int cnt = 0;
            foreach (XElement frame in frames)
            {
                float time = float.Parse(frame.Attribute("Time").Value, CultureInfo.InvariantCulture);
                int unknown = int.Parse(frame.Attribute("Unknown").Value);

                string tmp = file + "_" + cnt.ToString();
                XElement fcb = frame.Element("object");
                fcb.Save(tmp);

                ConvertXML(tmp, tmp + "c");

                byte[] fcbByte = File.ReadAllBytes(tmp + "c");

                uint crc = Gibbed.Dunia2.FileFormats.CRC32.Hash(fcbByte, 0, fcbByte.Length);

                output.WriteValueU32(crc);
                output.WriteValueF32(time, 0);
                output.WriteValueS32(unknown);
                output.WriteValueU16((ushort)fcbByte.Length);
                output.WriteValueU16(0);
                output.WriteBytes(fcbByte);

                File.Delete(tmp);
                File.Delete(tmp + "c");
                cnt++;
            }

            output.Flush();
            output.Seek(0, SeekOrigin.Begin);

            File.Copy(originalMab, newMab, true);

            FileStream MabStream = new(newMab, FileMode.Open);

            MemoryStream ms = new();
            MabStream.CopyTo(ms);

            byte[] byteSequence = new byte[] { 0x6E, 0x62, 0x43, 0x46 }; // nbCF
            int[] poses = Helpers.SearchBytesMultiple(ms.ToArray(), byteSequence);

            int startMarkupPos = poses[0] - 16;

            MabStream.SetLength(startMarkupPos);

            MabStream.WriteBytes(output.ToArray());

            byte[] lastBytes = File.ReadAllBytes(lastBinaryFile);

            MabStream.WriteBytes(lastBytes);

            MabStream.Flush();
            MabStream.Close();
        }

        static void MoveConvertBin(string file)
        {
            string onlyDir = Path.GetDirectoryName(file);

            XmlDocument xmlDoc = new XmlDocument();

            XmlDeclaration xmldecl;
            xmldecl = xmlDoc.CreateXmlDeclaration("1.0", "utf-8", "yes");

            XmlNode rootNode = xmlDoc.CreateElement("CMoveResource");
            xmlDoc.AppendChild(rootNode);

            xmlDoc.InsertBefore(xmldecl, rootNode);

            XmlComment comment1 = xmlDoc.CreateComment(xmlheader);
            XmlComment comment2 = xmlDoc.CreateComment(xmlheaderthanks);

            xmlDoc.InsertBefore(comment1, rootNode);
            xmlDoc.InsertBefore(comment2, rootNode);


            FileStream MoveStream = new FileStream(file, FileMode.Open);
            BinaryReader MoveReader = new BinaryReader(MoveStream);

            ushort ver = MoveReader.ReadUInt16();
            ushort unk = MoveReader.ReadUInt16();
            Console.WriteLine("Version: " + ver);
            isNewDawn = ver == 65;
            isFC6 = ver == 85;
            XmlAttribute rootNodeAttributeVersion = xmlDoc.CreateAttribute("Version");
            rootNodeAttributeVersion.Value = ver.ToString();
            rootNode.Attributes.Append(rootNodeAttributeVersion);

            XmlAttribute rootNodeAttributeUnknown = xmlDoc.CreateAttribute("Unknown");
            rootNodeAttributeUnknown.Value = unk.ToString();
            rootNode.Attributes.Append(rootNodeAttributeUnknown);


            byte[] fcbData = MoveReader.ReadBytes(100000000);

            string tmp = onlyDir + "\\tmp";
            File.WriteAllBytes(tmp, fcbData);
            ConvertFCB(tmp, tmp + "c");
            XmlDocument doc = new XmlDocument();
            doc.Load(tmp + "c");

            rootNode.AppendChild(xmlDoc.ImportNode(doc.SelectSingleNode("object"), true));

            File.Delete(tmp);
            File.Delete(tmp + "c");

            MoveReader.Dispose();
            MoveStream.Dispose();

            xmlDoc.Save(file + ".converted.xml");
        }

        static void MoveConvertXml(string file)
        {
            string onlyDir = Path.GetDirectoryName(file);

            string newName = file.Replace(".move.bin.converted.xml", "_new.move.bin");

            var output = File.Create(newName);

            XDocument doc = XDocument.Load(file);
            XElement root = doc.Element("CMoveResource");

            ushort ver = ushort.Parse(root.Attribute("Version").Value);
            isNewDawn = ver == 65;
            isFC6 = ver == 85;
            output.WriteValueU16(ver);
            output.WriteValueU16(ushort.Parse(root.Attribute("Unknown").Value));


            string tmp = onlyDir + "\\tmp";
            XElement fcb = root.Element("object");
            fcb.Save(tmp);

            ConvertXML(tmp, tmp + "c");

            byte[] fcbByte = File.ReadAllBytes(tmp + "c");

            output.WriteBytes(fcbByte);

            File.Delete(tmp);
            File.Delete(tmp + "c");

            output.Close();
        }

        static void CombinedMoveFileConvertBin(string file)
        {
            string onlyDir = Path.GetDirectoryName(file);

            var settings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                Indent = true,
                CheckCharacters = false,
                OmitXmlDeclaration = false
            };
            var writer = XmlWriter.Create(file + ".converted.xml", settings);

            writer.WriteStartDocument();
            writer.WriteComment(xmlheader);
            writer.WriteComment(xmlheaderthanks);
            writer.WriteStartElement("CombinedMoveFile");


            FileStream CombinedMoveFileStream = new FileStream(file, FileMode.Open);
            BinaryReader CombinedMoveFileReader = new BinaryReader(CombinedMoveFileStream);

            uint moveCount = CombinedMoveFileReader.ReadUInt32();
            uint moveDataSize = CombinedMoveFileReader.ReadUInt32();
            uint fcbDataSize = CombinedMoveFileReader.ReadUInt32();
            Console.WriteLine("Version: " + moveCount);
            if (moveCount != 64 && moveCount != 65 && moveCount != 85)
            {
                Console.WriteLine("Unsupported version of CombinedMoveFile.bin!");
                return;
            }

            bool isNewDawn = moveCount == 65;
            bool isFC6 = moveCount == 85;
            writer.WriteAttributeString("Version", moveCount.ToString());

            byte[] moveData = CombinedMoveFileReader.ReadBytes((int)moveDataSize);
            byte[] fcbData = CombinedMoveFileReader.ReadBytes((int)fcbDataSize);

            //****

            string tmp = onlyDir + "\\tmp";
            File.WriteAllBytes(tmp, fcbData);
            ConvertFCB(tmp, tmp + "c");

            //****

            writer.WriteStartElement("PerMoveResourceInfos");

            uint currentOffset = 0;
            Stream moveDataStream = new MemoryStream(moveData);
            for (int i = 0; i < CombinedMoveFile.PerMoveResourceInfo.perMoveResourceInfos.Count(); i++)
            {
                long currentPos = moveDataStream.Position;
                byte[] resourcePathId = moveDataStream.ReadBytes(sizeof(ulong));
                moveDataStream.Seek(currentPos, SeekOrigin.Begin);

                var pmri = CombinedMoveFile.PerMoveResourceInfo.perMoveResourceInfos.Where(e => e.resourcePathId == BitConverter.ToUInt64(resourcePathId, 0)).SingleOrDefault();
                uint chunkLen = pmri.size;

                byte[] chunk = moveDataStream.ReadBytes((int)chunkLen);
                var moveBinDataChunk = new CombinedMoveFile.MoveBinDataChunk(currentOffset, true, isNewDawn, isFC6, false);
                moveBinDataChunk.Deserialize(writer, chunk, pmri.rootNodeId);
                //writer.Flush();

                currentOffset += chunkLen;
            }
            /*
            Dictionary<uint, ulong> a = new Dictionary<uint, ulong>();
            foreach (KeyValuePair<uint, ulong> aa in OffsetsHashesArray.offsetsHashesDict)
                if (!OffsetsHashesArray.offsetsHashesDict2.ContainsKey(aa.Key))
                    a.Add(aa.Key, aa.Value);*/

            writer.WriteEndElement();

            //****

            var doc = new XPathDocument(tmp + "c");
            var nav = doc.CreateNavigator();
            var root = nav.SelectSingleNode("/object");

            writer.WriteStartElement("FCBData");
            root.WriteSubtree(writer);
            writer.WriteEndElement();

            //****

            File.Delete(tmp);
            File.Delete(tmp + "c");

            CombinedMoveFileReader.Dispose();
            CombinedMoveFileStream.Dispose();

            writer.WriteEndElement();
            writer.WriteEndDocument();

            writer.Flush();
            writer.Close();
        }

        static void CombinedMoveFileConvertXml(string file)
        {
            string onlyDir = Path.GetDirectoryName(file);

            string newName = file.Replace("combinedmovefile.bin.converted.xml", "combinedmovefile_new.bin");

            var output = File.Create(newName);

            var doc = new XPathDocument(file);
            var nav = doc.CreateNavigator();

            var root = nav.SelectSingleNode("/CombinedMoveFile");

            var CMove_BlendRoot_DTRoot = nav.Select("/CombinedMoveFile/PerMoveResourceInfos/CMove_BlendRoot_DTRoot");

            uint ver = uint.Parse(root.GetAttribute("Version", ""));

            List<byte[]> perMoveResourceInfos = new List<byte[]>();
            uint currentOffset = 0;
            while (CMove_BlendRoot_DTRoot.MoveNext() == true)
            {
                var moveBinDataChunk = new CombinedMoveFile.MoveBinDataChunk(currentOffset, true, ver == 65, ver == 85, false);
                byte[] chunk = moveBinDataChunk.Serialize(CMove_BlendRoot_DTRoot.Current);

                perMoveResourceInfos.Add(chunk);

                currentOffset += (uint)chunk.Length;
            }

            byte[] perMoveResourceInfosByte = perMoveResourceInfos.SelectMany(byteArr => byteArr).ToArray();

            string tmp = onlyDir + "\\tmp";
            var fcb = nav.SelectSingleNode("CombinedMoveFile/FCBData/object");
            XmlWriter writer = XmlWriter.Create(tmp);
            fcb.WriteSubtree(writer);
            writer.Close();

            ConvertXML(tmp, tmp + "c");

            byte[] fcbByte = File.ReadAllBytes(tmp + "c");

            output.WriteValueU32(ver);
            output.WriteValueU32((uint)perMoveResourceInfosByte.Length);
            output.WriteValueU32((uint)fcbByte.Length);
            output.WriteBytes(perMoveResourceInfosByte);
            output.WriteBytes(fcbByte);

            File.Delete(tmp);
            File.Delete(tmp + "c");

            output.Close();
        }

        static void UnpackBigFile(string m_FatFile, string m_DstFolder, string oneFile = "")
        {
            if (!File.Exists(m_FatFile))
            {
                Console.WriteLine("[ERROR]: Input file does not exist!");
                return;
            }

            if (m_DstFolder == "")
            {
                m_DstFolder = Path.GetDirectoryName(m_FatFile) + @"\" + Path.GetFileNameWithoutExtension(m_FatFile) + "_unpacked";
            }

            if (!Directory.Exists(m_DstFolder))
            {
                Directory.CreateDirectory(m_DstFolder);
            }

            string m_DatName = Path.GetDirectoryName(m_FatFile) + @"\" + Path.GetFileNameWithoutExtension(m_FatFile) + ".dat";

            SortedDictionary<ulong, FatEntry> Entries = GetFatEntries(m_FatFile, out int dwVersion);

            LoadFile(dwVersion);

            if (Entries == null)
            {
                Console.WriteLine("No files in the FAT were found!");
                return;
            }

            FileStream TDATStream = new FileStream(m_DatName, FileMode.Open);
            BinaryReader TDATReader = new BinaryReader(TDATStream);

            bool oneFileFound = false;
            ulong oneFileHash = GetFileHash(oneFile);

            int cnt = 0;
            foreach (KeyValuePair<ulong, FatEntry> pair in Entries)
            {
                cnt++;

                FatEntry fatEntry = pair.Value;

                if (oneFile != "")
                {
                    if (fatEntry.NameHash != oneFileHash)
                        continue;

                    oneFileFound = true;
                }

                string m_Hash = fatEntry.NameHash.ToString(dwVersion >= 9 ? "X16" : "X8");
                string fileName;
                if (listFilesDict.ContainsKey(fatEntry.NameHash) && fatEntry.NameHash > 0)
                {
                    listFilesDict.TryGetValue(fatEntry.NameHash, out fileName);
                }
                else
                {
                    fileName = @"__Unknown\" + m_Hash;
                }

                if (oneFileFound)
                {
                    fileName = Path.GetFileName(fileName);
                }

                string m_FullPath = m_DstFolder + @"\" + fileName;

                Console.WriteLine($"[Unpacking {cnt} / {Entries.Count}]: {fileName}");

                byte[] pDstBuffer = new byte[] { };

                if (fatEntry.CompressionScheme == CompressionScheme.None)
                {
                    TDATStream.Seek(fatEntry.Offset, SeekOrigin.Begin);

                    if (dwVersion == 11 || dwVersion == 10)
                    {
                        pDstBuffer = new byte[fatEntry.UncompressedSize];
                        TDATStream.Read(pDstBuffer, 0, (int)fatEntry.UncompressedSize);
                    }
                    if (dwVersion <= 9) // because in FAT ver 9 and below there is this weird thing
                    {
                        pDstBuffer = new byte[fatEntry.CompressedSize];
                        TDATStream.Read(pDstBuffer, 0, (int)fatEntry.CompressedSize);
                    }
                }
                else if (fatEntry.CompressionScheme == CompressionScheme.LZO1x)
                {
                    TDATStream.Seek(fatEntry.Offset, SeekOrigin.Begin);

                    byte[] pSrcBuffer = new byte[fatEntry.CompressedSize];
                    pDstBuffer = new byte[fatEntry.UncompressedSize];

                    TDATStream.Read(pSrcBuffer, 0, (int)fatEntry.CompressedSize);

                    int actualUncompressedLength = (int)fatEntry.UncompressedSize;

                    var result = Gibbed.Dunia2.FileFormats.LZO.Decompress(pSrcBuffer,
                                                0,
                                                pSrcBuffer.Length,
                                                pDstBuffer,
                                                0,
                                                ref actualUncompressedLength);

                    if (result != Gibbed.Dunia2.FileFormats.LZO.ErrorCode.Success)
                    {
                        throw new FormatException(string.Format("LZO decompression failure ({0})", result));
                    }

                    if (actualUncompressedLength != fatEntry.UncompressedSize)
                    {
                        throw new FormatException("LZO decompression failure (uncompressed size mismatch)");
                    }
                }
                else if (fatEntry.CompressionScheme == CompressionScheme.LZ4)
                {
                    TDATStream.Seek(fatEntry.Offset, SeekOrigin.Begin);

                    byte[] pSrcBuffer = new byte[fatEntry.CompressedSize];
                    pDstBuffer = new byte[fatEntry.UncompressedSize];

                    TDATStream.Read(pSrcBuffer, 0, (int)fatEntry.CompressedSize);

                    if (dwVersion == 9)
                    {
                        LZ4Decompressor64 TLZ4Decompressor64 = new LZ4Decompressor64();
                        TLZ4Decompressor64.Different = true;
                        TLZ4Decompressor64.Decompress(pSrcBuffer, pDstBuffer);
                    }
                    if (dwVersion > 9)
                    {
                        LZ4Codec.Decode(pSrcBuffer, pDstBuffer);
                    }
                }
                else
                {
                    //https://www.youtube.com/watch?v=AXzEcwYs8Eo
                    throw new Exception("WHAT THE FUCK???");
                }

                if (m_FullPath.Contains(@"__Unknown"))
                {
                    uint dwID = 0;

                    if (pDstBuffer.Length > 4)
                        dwID = BitConverter.ToUInt32(pDstBuffer, 0);

                    m_FullPath = UnpackBigFileFileType(m_FullPath, dwID);
                }
                else
                {
                    if (!Directory.Exists(Path.GetDirectoryName(m_FullPath)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(m_FullPath));
                    }
                }

                FileStream TSaveStream = new FileStream(m_FullPath, FileMode.Create);
                TSaveStream.Write(pDstBuffer, 0, pDstBuffer.Length);
                TSaveStream.Close();
            }

            if (oneFile != "" && !oneFileFound)
            {
                Console.WriteLine("File " + oneFile + " was not found in " + m_FatFile);
            }

            TDATReader.Dispose();
            TDATStream.Dispose();
        }

        static string UnpackBigFileFileType(string m_UnknownFileName, uint dwID)
        {
            string m_Directory = Path.GetDirectoryName(m_UnknownFileName);
            string fileName = Path.GetFileName(m_UnknownFileName);

            if (dwID == 0x004D4154) //TAM
            {
                m_UnknownFileName = m_Directory + @"\MAT\" + fileName + ".material.bin";
            }
            else
            if (dwID == 0x474E5089) //PNG
            {
                m_UnknownFileName = m_Directory + @"\PNG\" + fileName + ".png";
            }
            else
            if (dwID == 0x42444947) //GIDB
            {
                m_UnknownFileName = m_Directory + @"\GIDB\" + fileName + ".bin";
            }
            else
            if (dwID == 0x4D4F4D41) //MOMA
            {
                m_UnknownFileName = m_Directory + @"\ANIM\" + fileName + ".bin";
            }
            else
            if (dwID == 0x4D760040) //MOVE
            {
                m_UnknownFileName = m_Directory + @"\MOVE\" + fileName + ".move.bin";
            }
            else
            if (dwID == 0x00534B4C) //SKL
            {
                m_UnknownFileName = m_Directory + @"\SKEL\" + fileName + ".skeleton";
            }
            else
            if (dwID == 0x01194170 || dwID == 0x00194170) //pA
            {
                m_UnknownFileName = m_Directory + @"\DPAX\" + fileName + ".dpax";
            }
            else
            if (dwID == 0x44484B42) //BKHD
            {
                m_UnknownFileName = m_Directory + @"\BNK\" + fileName + ".bnk";
            }
            else
            if (dwID == 0x8464555) //UEF
            {
                m_UnknownFileName = m_Directory + @"\FEU\" + fileName + ".feu";
            }
            else
            if (dwID == 0x46464952) //RIFF
            {
                m_UnknownFileName = m_Directory + @"\WEM\" + fileName + ".wem";
            }
            else
            if (dwID == 0x4D455348) //HSEM
            {
                m_UnknownFileName = m_Directory + @"\XBG\" + fileName + ".xbg";
            }
            else
            if (dwID == 0x00584254) //XBT
            {
                m_UnknownFileName = m_Directory + @"\XBT\" + fileName + ".xbt";
            }
            else
            if (dwID == 0x4643626E || dwID == 0x00000004 || dwID == 0x00000023) //nbCF
            {
                m_UnknownFileName = m_Directory + @"\FCB\" + fileName + ".fcb";
            }
            else
            if (dwID == 0x78647064) //dpdx
            {
                m_UnknownFileName = m_Directory + @"\DPDX\" + fileName + ".dpdx";
            }
            else
            if (dwID == 0x4341554C) //LUAC
            {
                m_UnknownFileName = m_Directory + @"\LUA\" + fileName + ".lua";
            }
            else
            if (dwID == 0x5161754C) //LuaQ
            {
                m_UnknownFileName = m_Directory + @"\LUA\" + fileName + ".lua";
            }
            else
            if (dwID == 0x3CBFBBEF || dwID == 0x6D783F3C || dwID == 0x003CFEFF || dwID == 0x6172673C) //XML, //<graphics
            {
                m_UnknownFileName = m_Directory + @"\XML\" + fileName + ".xml";
            }
            else
            if (dwID == 0x6E69423C) //<binary
            {
                m_UnknownFileName = m_Directory + @"\BINXML\" + fileName + ".xml";
            }
            else
            if (dwID == 0x54425043) //CPBT
            {
                m_UnknownFileName = m_Directory + @"\CPBT\" + fileName + ".cpubt";
            }
            else
            if (dwID == 0xE9001052) //SDAT
            {
                m_UnknownFileName = m_Directory + @"\SDAT\" + fileName + ".sdat";
            }
            else
            if (dwID == 0x000000B0 || dwID == 0x000000B6) //MAB
            {
                m_UnknownFileName = m_Directory + @"\MAB\" + fileName + ".mab";
            }
            else
            if (dwID == 0x01) //WSECBDL
            {
                m_UnknownFileName = m_Directory + @"\WSECBDL\" + fileName + ".wsecbdl";
            }
            else
            if (dwID == 0x694B4942 || dwID == 0x6732424B || dwID == 0x6A32424B) //BIKi //KB2g // KB2j
            {
                m_UnknownFileName = m_Directory + @"\BIK\" + fileName + ".bik";
            }
            else
            if (dwID == 0x00000032 || dwID == 0x00000036) //hkx
            {
                m_UnknownFileName = m_Directory + @"\HKX\" + fileName + ".hkx";
            }

            if (!Directory.Exists(Path.GetDirectoryName(m_UnknownFileName)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(m_UnknownFileName));
            }

            return m_UnknownFileName;
        }

        static void PackBigFile(string sourceDir, string outputFile, int dwVersion = 10)
        {
            if (sourceDir.EndsWith("\\"))
            {
                Console.WriteLine("Bad source dir name!");
                Environment.Exit(0);
            }
            if (!outputFile.EndsWith(".fat"))
            {
                Console.WriteLine("Output filename is wrong!");
                Environment.Exit(0);
            }

            if (dwVersion < 10)
            {
                isCompressEnabled = false;
                Console.WriteLine("Compression is not available for older FATs.");
            }

            List<string> notCompress = new();

            string excludeFiles = LoadSetting("CompressExcludeFiles");
            if (excludeFiles != "" && excludeFilesFromCompress == "")
                notCompress.AddRange(excludeFiles.Split(','));

            if (excludeFilesFromCompress != "")
                notCompress.AddRange(excludeFilesFromCompress.Split(','));

            notCompress = notCompress.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();

            if (isCompressEnabled)
                Console.WriteLine("Excluded extensions from compressing: " + String.Join(", ", notCompress.ToArray()));

            string fatFile = outputFile;
            string datFile = fatFile.Replace(".fat", ".dat");

            string[] allFiles = Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories);

            SortedDictionary<ulong, FatEntry> Entries = new SortedDictionary<ulong, FatEntry>();

            var outputDat = File.Open(datFile, FileMode.OpenOrCreate);
            outputDat.SetLength(0);

            int cnt = 0;
            foreach (string file in allFiles)
            {
                cnt++;

                string fatFileName = file.Replace(sourceDir + "\\", "");
                string extension = Path.GetExtension(fatFileName);

                if (excludeFilesFromPack != "" && excludeFilesFromPack.Contains(extension)) continue;

                byte[] bytes = File.ReadAllBytes(file);

                FatEntry entry = new FatEntry();

                byte[] outputBytes;

                if (isCompressEnabled && !notCompress.Contains(extension))
                {
                    byte[] tmp = new byte[LZ4Codec.MaximumOutputSize(bytes.Length)];
                    int compressedSize = LZ4Codec.Encode(bytes, tmp, LZ4Level.L00_FAST);
                    outputBytes = new byte[compressedSize];
                    Array.Copy(tmp, outputBytes, compressedSize);

                    entry.CompressionScheme = CompressionScheme.LZ4;
                    entry.UncompressedSize = (uint)bytes.Length;
                }
                else
                {
                    outputBytes = bytes;

                    entry.CompressionScheme = CompressionScheme.None;

                    if (dwVersion == 10 || dwVersion == 11)
                        entry.UncompressedSize = (uint)bytes.Length;
                    else if (dwVersion <= 9)
                        entry.UncompressedSize = 0;
                }

                entry.NameHash = GetFileHash(fatFileName, dwVersion);
                entry.CompressedSize = (uint)outputBytes.Length;
                entry.Offset = outputDat.Position;
                Entries[entry.NameHash] = entry;

                outputDat.Write(outputBytes, 0, outputBytes.Length);
                outputDat.Seek(outputDat.Position.Align(16), SeekOrigin.Begin);

                Console.WriteLine($"[Packing {cnt} / {allFiles.Length}]: {fatFileName}");
            }

            outputDat.Flush();
            outputDat.Close();

            var output = File.Create(fatFile);
            output.WriteValueU32(0x46415432, 0);
            output.WriteValueS32(dwVersion, 0);

            output.WriteByte(1);
            output.WriteByte(0);
            //output.WriteByte(3);
            output.WriteValueU16(0);

            output.WriteValueS32(0, 0); // sub FATs are hard to edit, so they aren't supported by packing process
            output.WriteValueS32(0, 0);
            output.WriteValueS32(Entries.Count, 0);

            foreach (ulong entryE in Entries.Keys)
            {
                var fatEntry = Entries[entryE];

                if (dwVersion == 11)
                {
                    uint dwHash = (uint)((fatEntry.NameHash & 0xFFFFFFFF00000000ul) >> 32);
                    uint dwHash2 = (uint)((fatEntry.NameHash & 0x00000000FFFFFFFFul) >> 0);

                    uint dwUncompressedSize = 0u;
                    dwUncompressedSize = (uint)((int)dwUncompressedSize | ((int)(fatEntry.UncompressedSize << 2) & -4));
                    dwUncompressedSize = (uint)((int)dwUncompressedSize | (int)((int)fatEntry.CompressionScheme & 3L));

                    uint dwUnresolvedOffset = (uint)(((fatEntry.Offset >> 4) & 0x7FFFFFFF8) >> 3);

                    uint dwCompressedSize = 0u;
                    dwCompressedSize = (uint)((int)dwCompressedSize | (int)((fatEntry.Offset >> 4) << 29));
                    dwCompressedSize |= (fatEntry.CompressedSize & 0x1FFFFFFF);

                    output.WriteValueU32(dwHash, 0);
                    output.WriteValueU32(dwHash2, 0);
                    output.WriteValueU32(dwUncompressedSize, 0);
                    output.WriteValueU32(dwUnresolvedOffset, 0);
                    output.WriteValueU32(dwCompressedSize, 0);
                }
                if (dwVersion == 10)
                {
                    uint value = (uint)((ulong)((long)fatEntry.NameHash & -4294967296L) >> 32);
                    uint value2 = (uint)(fatEntry.NameHash & uint.MaxValue);
                    uint num = 0u;
                    num = (uint)((int)num | ((int)(fatEntry.UncompressedSize << 2) & -4));
                    num = (uint)((int)num | (int)((long)(int)fatEntry.CompressionScheme & 3L));
                    uint value3 = (uint)((fatEntry.Offset & 0x7FFFFFFF8) >> 3);
                    uint num2 = 0u;
                    num2 = (uint)((int)num2 | (int)((fatEntry.Offset & 7) << 29));
                    num2 |= (fatEntry.CompressedSize & 0x1FFFFFFF);

                    output.WriteValueU32(value, 0);
                    output.WriteValueU32(value2, 0);
                    output.WriteValueU32(num, 0);
                    output.WriteValueU32(value3, 0);
                    output.WriteValueU32(num2, 0);
                }
                if (dwVersion == 9)
                {
                    var a = (uint)((fatEntry.NameHash & 0xFFFFFFFF00000000ul) >> 32);
                    var b = (uint)((fatEntry.NameHash & 0x00000000FFFFFFFFul) >> 0);

                    uint c = 0;
                    c |= ((fatEntry.UncompressedSize << 2) & 0xFFFFFFFCu);
                    c |= (uint)(((byte)fatEntry.CompressionScheme << 0) & 0x00000003u);

                    var d = (uint)((fatEntry.Offset & 0X00000003FFFFFFFCL) >> 2);

                    uint e = 0;
                    e |= (uint)((fatEntry.Offset & 0X0000000000000003L) << 30);
                    e |= (fatEntry.CompressedSize & 0x3FFFFFFFu) << 0;

                    output.WriteValueU32(a, 0);
                    output.WriteValueU32(b, 0);
                    output.WriteValueU32(c, 0);
                    output.WriteValueU32(d, 0);
                    output.WriteValueU32(e, 0);
                }
                if (dwVersion == 5)
                {
                    uint a = (uint)fatEntry.NameHash;
                    uint b = 0;
                    b |= ((fatEntry.UncompressedSize << 2) & 0xFFFFFFFCu);
                    b |= (uint)(((byte)fatEntry.CompressionScheme << 0) & 0x00000003u);
                    ulong c = 0;
                    c |= ((ulong)(fatEntry.Offset << 30) & 0xFFFFFFFFC0000000ul);
                    c |= (ulong)((fatEntry.CompressedSize << 0) & 0x000000003FFFFFFFul);

                    output.WriteValueU32(a, 0);
                    output.WriteValueU32(b, 0);
                    output.WriteValueU64(c, 0);
                }
            }

            output.WriteValueU32(0, 0);

            if (dwVersion >= 9)
                output.WriteValueU32(0, 0);

            output.Flush();
            output.Close();
        }

        static SortedDictionary<ulong, FatEntry> GetFatEntries(string fatFile, out int dwVersion)
        {
            SortedDictionary<ulong, FatEntry> Entries = new SortedDictionary<ulong, FatEntry>();

            FileStream TFATStream = new FileStream(fatFile, FileMode.Open);
            BinaryReader TFATReader = new BinaryReader(TFATStream);

            int dwMagic = TFATReader.ReadInt32();
            dwVersion = TFATReader.ReadInt32();
            int dwUnknown = TFATReader.ReadInt32();

            if (dwMagic != 0x46415432)
            {
                Console.WriteLine("Invalid FAT Index file!");
                TFATReader.Dispose();
                TFATStream.Dispose();
                TFATReader.Close();
                TFATStream.Close();
                return null;
            }

            // versions
            // 11 - FC6
            // 10 - FC5, FCND
            // 9 - FC3, FC3BD, FC4
            // 5 - FC2
            if (dwVersion != 11 && dwVersion != 10 && dwVersion != 9 && dwVersion != 5)
            {
                Console.WriteLine("Invalid version of FAT Index file!");
                TFATReader.Dispose();
                TFATStream.Dispose();
                TFATReader.Close();
                TFATStream.Close();
                return null;
            }

            int dwSubfatTotalEntryCount = 0;
            int dwSubfatCount = 0;

            if (dwVersion >= 9)
            {
                dwSubfatTotalEntryCount = TFATReader.ReadInt32();
                dwSubfatCount = TFATReader.ReadInt32();
            }

            int dwTotalFiles = TFATReader.ReadInt32();

            for (int i = 0; i < dwTotalFiles; i++)
            {
                FatEntry entry = GetFatEntriesDeserialize(TFATReader, dwVersion);
                Entries[entry.NameHash] = entry;
            }

            uint unknown1Count = TFATReader.ReadUInt32();
            if (unknown1Count > 0)
                throw new NotSupportedException();
            /*for (uint i = 0; i < unknown1Count; i++)
            {
                throw new NotSupportedException();
                TFATReader.ReadBytes(16);
            }*/

            if (dwVersion >= 7)
            {
                uint unknown2Count = TFATReader.ReadUInt32();
                for (uint i = 0; i < unknown2Count; i++)
                {
                    TFATReader.ReadBytes(16);
                }
            }

            // we support sub fats, but for packing it's better and easier to remove them
            for (int i = 0; i < dwSubfatCount; i++)
            {
                uint subfatEntryCount = TFATReader.ReadUInt32();
                for (uint j = 0; j < subfatEntryCount; j++)
                {
                    FatEntry entry = GetFatEntriesDeserialize(TFATReader, dwVersion);
                    Entries[entry.NameHash] = entry;
                }
            }

            TFATReader.Dispose();
            TFATStream.Dispose();
            TFATReader.Close();
            TFATStream.Close();

            return Entries;
        }

        static FatEntry GetFatEntriesDeserialize(BinaryReader TFATReader, int dwVersion)
        {
            ulong dwHash = 0;

            if (dwVersion == 11 || dwVersion == 10 || dwVersion == 9)
            {
                dwHash = TFATReader.ReadUInt64();
                dwHash = (dwHash << 32) + (dwHash >> 32);
            }
            if (dwVersion == 5)
            {
                dwHash = TFATReader.ReadUInt32();
            }

            uint dwUncompressedSize = TFATReader.ReadUInt32();
            uint dwUnresolvedOffset = TFATReader.ReadUInt32();
            uint dwCompressedSize = TFATReader.ReadUInt32();

            uint dwFlag = 0;
            ulong dwOffset = 0;

            if (dwVersion == 11)
            {
                dwFlag = dwUncompressedSize & 3;
                dwOffset = ((ulong)dwCompressedSize >> 29 | (ulong)dwUnresolvedOffset << 3) << 4; // thx to ミルクティー (miru)
                dwCompressedSize = (dwCompressedSize & 0x1FFFFFFF);
                dwUncompressedSize = (dwUncompressedSize >> 2);
            }
            if (dwVersion == 10)
            {
                dwFlag = dwUncompressedSize & 3;
                dwOffset = dwCompressedSize >> 29 | 8ul * dwUnresolvedOffset;
                dwCompressedSize = (dwCompressedSize & 0x1FFFFFFF);
                dwUncompressedSize = (dwUncompressedSize >> 2);
            }
            if (dwVersion == 9)
            {
                dwFlag = (dwUncompressedSize & 0x00000003u) >> 0;
                dwOffset = (ulong)dwUnresolvedOffset << 2;
                dwOffset |= (dwCompressedSize & 0xC0000000u) >> 30;
                dwCompressedSize = (uint)((dwCompressedSize & 0x3FFFFFFFul) >> 0);
                dwUncompressedSize = (dwUncompressedSize & 0xFFFFFFFCu) >> 2;

                /*dwUncompressedSize = ((dwUncompressedSize >> 2) & 0x3FFFFFFFu);
                dwFlag = (byte)((dwUncompressedSize >> 0) & 0x3u);
                dwOffset = ((ulong)(dwUnresolvedOffset << 2) | ((dwCompressedSize >> 30) & 0x3u));
                dwCompressedSize = (uint)((dwCompressedSize >> 0) & 0x3FFFFFFFul);*/

                /*dwFlag = dwUncompressedSize & 3;
                dwUncompressedSize = dwUncompressedSize >> 2;
                dwOffset = (ulong)(dwUnresolvedOffset * 4L) + (dwCompressedSize >> 30);
                dwCompressedSize = dwCompressedSize & 0x3FFFFFFF;*/
            }
            if (dwVersion == 5)
            {
                dwFlag = (dwUncompressedSize & 0x00000003u) >> 0;
                dwOffset = (ulong)dwCompressedSize << 2;
                dwOffset |= (dwUnresolvedOffset & 0xC0000000u) >> 30;
                dwCompressedSize = (uint)((dwUnresolvedOffset & 0x3FFFFFFFul) >> 0);
                dwUncompressedSize = (dwUncompressedSize & 0xFFFFFFFCu) >> 2;
            }

            var entry = new FatEntry();
            entry.NameHash = dwHash;
            entry.UncompressedSize = dwUncompressedSize;
            entry.Offset = (long)dwOffset;
            entry.CompressedSize = dwCompressedSize;
            entry.CompressionScheme = (CompressionScheme)dwFlag;

            return entry;
        }
    }
}
