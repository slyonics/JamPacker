﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using K4os.Compression.LZ4;

using Newtonsoft.Json;

namespace JamPacker
{
    class Program
    {
        static int Main(string[] args)
        {
            Console.WriteLine("Starting JamPacker...");

            if (args.Length == 0)
            {
                Console.Write("Invalid parameters! Please specify either the 'Enumerate' flag before building your project to create enumerations and a manifest for all available");
                Console.WriteLine("assets, or the 'Pack' flag after building your project to pack enumerated assets into archives.");
                Console.WriteLine("Example: JamPacker Enumerate [AssetDirectory] [OutputDirectory]");
                Console.WriteLine("Example: JamPacker Pack [AssetDirectory] [OutputDirectory]");

                return -1;
            }

            if (args[0].ToLower() == "enumerate")
            {
                if (args.Length < 3)
                {
                    Console.WriteLine("Invalid parameters! Please specify an asset directory path and an output directory path when using the 'Enumerate' flag.");
                    Console.WriteLine("Example: JamPacker Enumerate [AssetDirectory] [OutputDirectory]");

                    return -1;
                }

                return EnumerateAssets(args) ? 0 : -1;
            }
            else if (args[0].ToLower() == "pack")
            {
                if (args.Length < 3)
                {
                    Console.WriteLine("Invalid parameters! Please specify an asset directory path and an output directory path when using the 'Pack' flag.");
                    Console.WriteLine("Example: JamPacker Pack [AssetDirectory] [OutputDirectory]");

                    return -1;
                }

                return PackAssets(args) ? 0 : -1;
            }
            else
            {
                Console.Write("Invalid parameters! Please specify either the 'Enumerate' flag before building your project to create enumerations and a manifest for all available");
                Console.WriteLine("assets, or the 'Pack' flag after building your project to pack enumerated assets into archives.");
                Console.WriteLine("Example: JamPacker Enumerate [AssetDirectory] [OutputDirectory]");
                Console.WriteLine("Example: JamPacker Pack [AssetDirectory] [OutputDirectory]");

                return -1;
            }
        }

        private static bool EnumerateAssets(string[] args)
        {
            string contentPath = args[1];
            string outputPath = args[2];
            string objectPath = outputPath.Replace("\\bin\\", "\\obj\\");
            string projectName = contentPath.Split('\\').TakeLast(2).First();
            string[] contentSubdirs = Directory.GetDirectories(contentPath);

            Console.WriteLine("Enumerating assets...");

            Dictionary<string, List<Tuple<string, string>>> assetDirectory = new Dictionary<string, List<Tuple<string, string>>>();
            assetDirectory.Add("Font", Enumerate(contentPath + "\\Fonts", "ttf"));
            assetDirectory.Add("View", Enumerate(new string[] { contentPath + "\\..\\Scenes", contentPath + "\\..\\SceneObjects" }, "xml"));
            assetDirectory.Add("Sound", Enumerate(contentPath + "\\Audio\\Sounds", "wav"));
            assetDirectory.Add("Music", Enumerate(contentPath + "\\Audio\\Music", new string[] { "mp3", "ogg" }));
            assetDirectory.Add("Data", Enumerate(contentPath + "\\Data", "json"));
            assetDirectory.Add("Shader", Enumerate(contentPath + "\\Shaders", "fx"));
            assetDirectory.Add("Sprite", Enumerate(contentPath + "\\Graphics", new string[] { "png", "jpg", "jpeg" }));

            Console.WriteLine("Creating C# enumerations file...");

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(string.Format("namespace {0}.Main\r\n", projectName) + "{");
            foreach (KeyValuePair<string, List<Tuple<string, string>>> assetList in assetDirectory)
            {
                if (assetList.Key == "Font") continue;

                stringBuilder.AppendLine("    public enum Game" + assetList.Key + "\r\n" + "    {");
                WriteEnumerations(assetList.Value, stringBuilder);
                stringBuilder.AppendLine("    }\r\n");
            }
            stringBuilder.AppendLine("}");
            try { File.WriteAllText(contentPath + "\\..\\Main\\AssetList.cs", stringBuilder.ToString()); }
            catch (Exception ex) { Console.WriteLine("Cannot create/overwrite C# enumerations file: " + ex.Message); return false; }

            Console.WriteLine("Determining asset archives to build...");

            JsonSerializer serializer = new JsonSerializer();
            foreach (KeyValuePair<string, List<Tuple<string, string>>> assetList in assetDirectory)
            {
                bool archiveDirty = true;
                string manifestFileName = objectPath + "\\" + assetList.Key + ".manifest";
                try
                {
                    using (JsonTextReader assetManifestReader = new JsonTextReader(new StreamReader(manifestFileName)))
                    {
                        List<Tuple<string, string>> oldAssetManifest = serializer.Deserialize<List<Tuple<string, string>>>(assetManifestReader);
                        if (oldAssetManifest.Count == assetList.Value.Count && Enumerable.SequenceEqual(oldAssetManifest, assetList.Value))
                        {
                            archiveDirty = false;
                        }
                    }
                }
                catch (Exception) { }
                if (archiveDirty)
                {
                    try
                    {
                        using (JsonTextWriter assetManifestWriter = new JsonTextWriter(new StreamWriter(manifestFileName)))
                        {
                            serializer.Serialize(assetManifestWriter, assetList.Value);
                        }
                    }
                    catch (Exception ex) { Console.WriteLine("Could not write " + assetList.Key + " manifest: " + ex.Message); return false; }
                }
            }

            return true;
        }

        private static void WriteEnumerations(List<Tuple<string, string>> assetFiles, StringBuilder stringBuilder)
        {
            foreach (Tuple<string, string> asset in assetFiles)
            {
                stringBuilder.AppendLine("        " + asset.Item1.Replace("\\", "_") + ",");
            }

            stringBuilder.AppendLine("\r\n" + "        " + "None = -1");
        }

        private static List<Tuple<string, string>> Enumerate(string[] basePaths, string[] extensions)
        {
            if (basePaths.Any(x => basePaths.Any(y => x != y && (x.Contains(y) || y.Contains(x)))))
            {
                throw new Exception();
            }

            List<Tuple<string, string>> result = new List<Tuple<string, string>>();
            foreach (string basePath in basePaths)
            {
                foreach (string extension in extensions)
                {
                    IEnumerable<string> files = Directory.EnumerateFiles(basePath, "*." + extension, SearchOption.AllDirectories);
                    foreach (string file in files)
                    {
                        result.Add(new Tuple<string, string>(file.Replace(basePath + '\\', "").Replace('.' + extension, ""), file));
                    }
                }
            }

            return result;
        }

        private static List<Tuple<string, string>> Enumerate(string[] basePaths, string extension)
        {
            return Enumerate(basePaths, new string[] { extension });
        }

        private static List<Tuple<string, string>> Enumerate(string basePath, string[] extensions)
        {
            return Enumerate(new string[] { basePath }, extensions);
        }

        private static List<Tuple<string, string>> Enumerate(string basePath, string extension)
        {
            return Enumerate(new string[] { basePath }, new string[] { extension });
        }

        private static bool PackAssets(string[] args)
        {
            string contentPath = args[1];
            string outputPath = args[2];
            string objectPath = outputPath.Replace("\\bin\\", "\\obj\\");

            string[] contentSubdirs = Directory.GetDirectories(contentPath);

            Console.WriteLine("Enumerating fonts...");
            List<Tuple<string, string>> fontFiles = Enumerate(contentPath + "\\Fonts", "ttf");
            PackFonts(fontFiles, outputPath);

            Console.WriteLine("Enumerating views...");
            List<Tuple<string, string>> viewFiles = Enumerate(new string[] { contentPath + "\\..\\Scenes", contentPath + "\\..\\SceneObjects" }, "xml");
            PackViews(viewFiles, outputPath);

            Console.WriteLine("Enumerating sounds...");
            List<Tuple<string, string>> soundFiles = Enumerate(contentPath + "\\Audio\\Sounds", "wav");
            PackSounds(soundFiles, outputPath);

            Console.WriteLine("Enumerating music...");
            List<Tuple<string, string>> musicFiles = Enumerate(contentPath + "\\Audio\\Music", new string[] { "mp3", "ogg" });
            PackMusic(musicFiles, outputPath);

            Console.WriteLine("Enumerating data...");
            List<Tuple<string, string>> dataFiles = Enumerate(contentPath + "\\Data", "json");
            PackData(dataFiles, outputPath);

            Console.WriteLine("Enumerating shaders...");
            List<Tuple<string, string>> shaderFiles = Enumerate(contentPath + "\\Shaders", "fx");
            PackShaders(shaderFiles, outputPath, objectPath);

            Console.WriteLine("Enumerating sprites...");
            List<Tuple<string, string>> spriteFiles = Enumerate(contentPath + "\\Graphics", new string[] { "png", "jpg", "jpeg" });
            PackSprites(spriteFiles, outputPath);

            return true;
        }

        private static bool Pack(List<Tuple<byte[], byte[]>> assets, string filePath)
        {
            int index = 0;
            byte[] rawData = new byte[assets.Sum(x => x.Item1.Length + x.Item2.Length + 8)];
            foreach (Tuple<byte[], byte[]> asset in assets)
            {
                Array.Copy(BitConverter.GetBytes(asset.Item1.Length), 0, rawData, index, 4);
                Array.Copy(asset.Item1, 0, rawData, index + 4, asset.Item1.Length);
                index += asset.Item1.Length + 4;

                Array.Copy(BitConverter.GetBytes(asset.Item2.Length), 0, rawData, index, 4);
                Array.Copy(asset.Item2, 0, rawData, index + 4, asset.Item2.Length);
                index += asset.Item2.Length + 4;
            }

            byte[] packedData = new byte[LZ4Codec.MaximumOutputSize(rawData.Length)];
            int packedSize = LZ4Codec.Encode(rawData, 0, rawData.Length, packedData, 0, packedData.Length);
            if (packedSize < 0)
            {
                return false;
            }

            using (BinaryWriter binaryWriter = new BinaryWriter(File.Open(filePath, FileMode.Create)))
            {
                binaryWriter.Write(assets.Count());
                binaryWriter.Write(rawData.Length);
                binaryWriter.Write(packedData, 0, packedSize);
            }

            return true;
        }
        private static void PackFonts(List<Tuple<string, string>> fontFiles, string fontOutputPath)
        {
            Console.WriteLine("Packing fonts...");

            List<Tuple<byte[], byte[]>> assets = new List<Tuple<byte[], byte[]>>();            
            foreach (Tuple<string, string> fontPath in fontFiles)
            {
                byte[] nameData = Encoding.ASCII.GetBytes(fontPath.Item1);
                byte[] fontData = File.ReadAllBytes(fontPath.Item2);
                assets.Add(new Tuple<byte[], byte[]>(nameData, fontData));
            }

            if (!Pack(assets, fontOutputPath + "\\Fonts.jam"))
            {
                throw new Exception("Unable to compress font data!");
            }
        }

        private static void PackViews(List<Tuple<string, string>> viewFiles, string viewOutputPath)
        {
            Console.WriteLine("Packing views...");

            List<Tuple<byte[], byte[]>> assets = new List<Tuple<byte[], byte[]>>();
            foreach (Tuple<string, string> viewPath in viewFiles)
            {
                byte[] nameData = Encoding.ASCII.GetBytes(viewPath.Item1);
                string viewData = File.ReadAllText(viewPath.Item2);
                assets.Add(new Tuple<byte[], byte[]>(nameData, Encoding.ASCII.GetBytes(viewData)));
            }

            if (!Pack(assets, viewOutputPath + "\\Views.jam"))
            {
                throw new Exception("Unable to compress view data!");
            }
        }

        private static void PackSounds(List<Tuple<string, string>> soundFiles, string soundOutputPath)
        {
            Console.WriteLine("Packing sounds...");

            List<Tuple<byte[], byte[]>> assets = new List<Tuple<byte[], byte[]>>();
            foreach (Tuple<string, string> soundPath in soundFiles)
            {
                byte[] nameData = Encoding.ASCII.GetBytes(soundPath.Item1);
                byte[] soundData = File.ReadAllBytes(soundPath.Item2);
                assets.Add(new Tuple<byte[], byte[]>(nameData, soundData));
            }

            if (!Pack(assets, soundOutputPath + "\\Sounds.jam"))
            {
                throw new Exception("Unable to compress sound data!");
            }
        }

        private static void PackMusic (List<Tuple<string, string>> musicFiles, string musicOutputPath)
        {
            Console.WriteLine("Packing music...");

            List<Tuple<byte[], byte[]>> assets = new List<Tuple<byte[], byte[]>>();
            foreach (Tuple<string, string> musicPath in musicFiles)
            {
                byte[] nameData = Encoding.ASCII.GetBytes(musicPath.Item1);
                byte[] musicData = File.ReadAllBytes(musicPath.Item2);
                assets.Add(new Tuple<byte[], byte[]>(nameData, musicData));
            }

            if (!Pack(assets, musicOutputPath + "\\Music.jam"))
            {
                throw new Exception("Unable to compress music data!");
            }
        }

        private static void PackData(List<Tuple<string, string>> dataFiles, string dataOutputPath)
        {
            Console.WriteLine("Packing data...");

            List<Tuple<byte[], byte[]>> assets = new List<Tuple<byte[], byte[]>>();
            foreach (Tuple<string, string> dataPath in dataFiles)
            {
                byte[] nameData = Encoding.ASCII.GetBytes(dataPath.Item1);
                string jsonData = File.ReadAllText(dataPath.Item2);
                assets.Add(new Tuple<byte[], byte[]>(nameData, Encoding.ASCII.GetBytes(jsonData)));
            }

            if (!Pack(assets, dataOutputPath + "\\Data.jam"))
            {
                throw new Exception("Unable to compress JSON data!");
            }
        }

        private static void PackShaders(List<Tuple<string, string>> shaderFiles, string shaderOutputPath, string objectPath)
        {
            Console.WriteLine("Packing shaders...");

            bool errors = false;
            List<Tuple<byte[], byte[]>> assets = new List<Tuple<byte[], byte[]>>();
            foreach (Tuple<string, string> shaderPath in shaderFiles)
            {
                string compileCommmand = string.Format("/C mgfxc {0} {1} /Profile:OpenGL", shaderPath.Item2, objectPath + "\\temp_shader");
                var process = System.Diagnostics.Process.Start("CMD.exe", compileCommmand);
                process.WaitForExit();

                if (process.ExitCode != 0) errors = true;
                else
                {
                    byte[] nameData = Encoding.ASCII.GetBytes(shaderPath.Item1);
                    byte[] shaderData = File.ReadAllBytes(objectPath + "\\temp_shader");
                    assets.Add(new Tuple<byte[], byte[]>(nameData, shaderData));
                }
            }

            try
            {
                File.Delete(objectPath + "\\temp_shader");
            }
            catch (Exception) { }

            if (errors)
            {
                throw new Exception("Unable to compile HLSL data!");
            }

            if (!Pack(assets, shaderOutputPath + "\\Shaders.jam"))
            {
                throw new Exception("Unable to compress HLSL data!");
            }
        }

        private static void PackSprites(List<Tuple<string, string>> spriteFiles, string spriteOutputPath)
        {
            Console.WriteLine("Packing sprites...");

            List<Tuple<byte[], byte[]>> assets = new List<Tuple<byte[], byte[]>>();
            foreach (Tuple<string, string> spritePath in spriteFiles)
            {
                byte[] nameData = Encoding.ASCII.GetBytes(spritePath.Item1);
                byte[] spriteData = File.ReadAllBytes(spritePath.Item2);
                assets.Add(new Tuple<byte[], byte[]>(nameData, spriteData));
            }

            if (!Pack(assets, spriteOutputPath + "\\Sprites.jam"))
            {
                throw new Exception("Unable to compress sprite data!");
            }
        }
    }
}
