﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

using K4os.Compression.LZ4;

using Newtonsoft.Json;

namespace JamPacker
{
    public static class AssetEnumerator
    {
        public static bool EnumerateAssets(string[] args)
        {
            string contentPath = args[1];
            string mainPath = args[2];
            string outputPath = args[3];
            string objectPath = outputPath.Replace("\\bin\\", "\\obj\\");
            string projectName = contentPath.Split('\\').TakeLast(2).First();
            string[] contentSubdirs = Directory.GetDirectories(contentPath);

            Console.WriteLine("Enumerating assets...");

            Dictionary<string, List<Tuple<string, string>>> assetDirectory = new Dictionary<string, List<Tuple<string, string>>>();
            assetDirectory.Add("Font", Enumerate(contentPath + "\\Fonts", new string[] { "spritefont" }));
            assetDirectory.Add("View", Enumerate(new string[] { contentPath + "\\Views", contentPath + "\\..\\Scenes", contentPath + "\\..\\SceneObjects" }, new string[] { "xml", "view" }));
            assetDirectory.Add("Sound", Enumerate(new string[] { contentPath + "\\Sounds", contentPath + "\\Audio\\Sounds" }, "wav"));
            assetDirectory.Add("Music", Enumerate(new string[] { contentPath + "\\Music", contentPath + "\\Audio\\Music" }, new string[] { "mp3", "ogg" }));
            assetDirectory.Add("Data", Enumerate(contentPath + "\\Data", "json"));
            assetDirectory.Add("Shader", Enumerate(contentPath + "\\Shaders", "fx"));
            assetDirectory.Add("Sprite", Enumerate(new string[] { contentPath + "\\Sprites", contentPath + "\\Graphics" }, new string[] { "png", "jpg", "jpeg" }));
            assetDirectory.Add("Map", Enumerate(contentPath + "\\Maps", new string[] { "tmx", "tsx", "ldtk" }));

            Console.WriteLine("Creating C# enumerations file...");

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(string.Format("namespace {0}.Main\r\n", projectName) + "{");
            foreach (KeyValuePair<string, List<Tuple<string, string>>> assetList in assetDirectory)
            {
                // if (assetList.Key == "Font") continue;

                stringBuilder.AppendLine("    public enum Game" + assetList.Key + "\r\n" + "    {");
                WriteEnumerations(assetList.Value, stringBuilder);
                stringBuilder.AppendLine("    }\r\n");
            }
            stringBuilder.AppendLine("}");
            try
            {
                File.WriteAllText(mainPath + "\\AssetList.cs", stringBuilder.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine("Cannot create/overwrite C# enumerations file: " + ex.Message);
                return false;
            }

            return true;

            /*
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
                    catch (Exception ex)
                    {
                        Console.WriteLine("Could not write " + assetList.Key + " manifest: " + ex.Message);
                        return false;
                    }
                }
            }

            return true;
            */
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
                if (!Directory.Exists(basePath)) continue;

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
    }
}
