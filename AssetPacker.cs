using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

using K4os.Compression.LZ4;

using Newtonsoft.Json;

namespace JamPacker
{
    public static class AssetPacker
    {
        public static bool PackAssets(string[] args)
        {
            string contentPath = args[1];
            string outputPath = args[2];
            string objectPath = outputPath.Replace("\\bin\\", "\\obj\\");
            string[] contentSubdirs = Directory.GetDirectories(contentPath);
            bool success = true;

            Console.WriteLine("Packing assets...");

            List<Tuple<string, string>> assetFiles = ReadManifest(objectPath + "\\Font.manifest", outputPath + "\\Fonts.jam");
            if (assetFiles.Count == 0) Console.WriteLine("Skipping fonts...");
            else
            {
                Console.WriteLine("Packing fonts...");
                if (!PackAssetsBytes(assetFiles, outputPath + "\\Fonts.jam")) success = false;
            }

            assetFiles = ReadManifest(objectPath + "\\View.manifest", outputPath + "\\Views.jam");
            if (assetFiles.Count == 0) Console.WriteLine("Skipping views...");
            else
            {
                Console.WriteLine("Packing views...");
                if (!PackAssetsAscii(assetFiles, outputPath + "\\Views.jam")) success = false;
            }

            assetFiles = ReadManifest(objectPath + "\\Sound.manifest", outputPath + "\\Sounds.jam");
            if (assetFiles.Count == 0) Console.WriteLine("Skipping sounds...");
            else
            {
                Console.WriteLine("Packing sounds...");
                if (!PackAssetsBytes(assetFiles, outputPath + "\\Sounds.jam")) success = false;
            }

            assetFiles = ReadManifest(objectPath + "\\Music.manifest", outputPath + "\\Music.jam");
            if (assetFiles.Count == 0) Console.WriteLine("Skipping music...");
            else
            {
                Console.WriteLine("Packing music...");
                if (!PackAssetsBytes(assetFiles, outputPath + "\\Music.jam")) success = false;
            }

            assetFiles = ReadManifest(objectPath + "\\Data.manifest", outputPath + "\\Data.jam");
            if (assetFiles.Count == 0) Console.WriteLine("Skipping data...");
            else
            {
                Console.WriteLine("Packing data...");
                if (!PackAssetsAscii(assetFiles, outputPath + "\\Data.jam")) success = false;
            }

            assetFiles = ReadManifest(objectPath + "\\Shader.manifest", outputPath + "\\Shaders.jam");
            if (assetFiles.Count == 0) Console.WriteLine("Skipping shaders...");
            else
            {
                Console.WriteLine("Packing shaders...");
                if (!PackAssetsShaders(assetFiles, outputPath + "\\Shaders.jam", objectPath)) success = false;
            }

            assetFiles = ReadManifest(objectPath + "\\Sprite.manifest", outputPath + "\\Sprites.jam");
            if (assetFiles.Count == 0) Console.WriteLine("Skipping sprites...");
            else
            {
                Console.WriteLine("Packing sprites...");
                if (!PackAssetsBytes(assetFiles, outputPath + "\\Sprites.jam")) success = false;
            }

            return success;
        }

        private static List<Tuple<string, string>> ReadManifest(string manifestPath, string archivePath)
        {
            JsonSerializer serializer = new JsonSerializer();
            List<Tuple<string, string>> assetFiles = null;

            try
            {
                using (JsonTextReader assetManifestReader = new JsonTextReader(new StreamReader(manifestPath)))
                {
                    assetFiles = serializer.Deserialize<List<Tuple<string, string>>>(assetManifestReader);
                    if (!File.Exists(archivePath) || Directory.GetLastWriteTime(manifestPath) > Directory.GetLastWriteTime(archivePath) ||
                        assetFiles.Any(x => Directory.GetLastWriteTime(x.Item2) >= Directory.GetLastWriteTime(archivePath)))
                    {
                        return assetFiles;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Unable to get asset manifest from {0}: {1}", manifestPath, ex.Message));

                return null;
            }

            return new List<Tuple<string, string>>();
        }

        private static bool CompressAndWriteArchive(List<Tuple<byte[], byte[]>> assets, string filePath)
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
                Console.WriteLine(string.Format("Unable to compress asset archive {0}", filePath));

                return false;
            }

            try
            {
                using (BinaryWriter binaryWriter = new BinaryWriter(File.Open(filePath, FileMode.Create)))
                {
                    binaryWriter.Write(assets.Count());
                    binaryWriter.Write(rawData.Length);
                    binaryWriter.Write(packedData, 0, packedSize);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Unable to write asset archive {0}: {1}", filePath, ex.Message));

                return false;
            }

            return true;
        }

        private static bool PackAssetsBytes(List<Tuple<string, string>> assetFiles, string archivePath)
        {
            List<Tuple<byte[], byte[]>> assets = new List<Tuple<byte[], byte[]>>();
            foreach (Tuple<string, string> assetFile in assetFiles)
            {
                byte[] nameData = Encoding.ASCII.GetBytes(assetFile.Item1);
                byte[] assetData = File.ReadAllBytes(assetFile.Item2);
                assets.Add(new Tuple<byte[], byte[]>(nameData, assetData));
            }

            return CompressAndWriteArchive(assets, archivePath);
        }

        private static bool PackAssetsAscii(List<Tuple<string, string>> assetFiles, string archivePath)
        {
            List<Tuple<byte[], byte[]>> assets = new List<Tuple<byte[], byte[]>>();
            foreach (Tuple<string, string> assetFile in assetFiles)
            {
                byte[] nameData = Encoding.ASCII.GetBytes(assetFile.Item1);
                string assetData = File.ReadAllText(assetFile.Item2);
                assets.Add(new Tuple<byte[], byte[]>(nameData, Encoding.ASCII.GetBytes(assetData)));
            }

            return CompressAndWriteArchive(assets, archivePath);
        }

        private static bool PackAssetsShaders(List<Tuple<string, string>> shaderFiles, string shaderOutputPath, string objectPath)
        {
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

            return CompressAndWriteArchive(assets, shaderOutputPath);
        }
    }
}
