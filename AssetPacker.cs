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

            Console.WriteLine("Enumerating fonts...");
            JsonSerializer serializer = new JsonSerializer();
            List<Tuple<string, string>> fontFiles = null;
            try
            {
                using (JsonTextReader assetManifestReader = new JsonTextReader(new StreamReader(objectPath + "\\Font.manifest")))
                {
                    fontFiles = serializer.Deserialize<List<Tuple<string, string>>>(assetManifestReader);
                }
            }
            catch (Exception ex) { }
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
        private static bool PackFonts(List<Tuple<string, string>> fontFiles, string fontOutputPath)
        {
            Console.WriteLine("Packing fonts...");

            List<Tuple<byte[], byte[]>> assets = new List<Tuple<byte[], byte[]>>();
            foreach (Tuple<string, string> fontPath in fontFiles)
            {
                byte[] nameData = Encoding.ASCII.GetBytes(fontPath.Item1);
                byte[] fontData = File.ReadAllBytes(fontPath.Item2);
                assets.Add(new Tuple<byte[], byte[]>(nameData, fontData));
            }

            return Pack(assets, fontOutputPath + "\\Fonts.jam");
        }

        private static bool PackViews(List<Tuple<string, string>> viewFiles, string viewOutputPath)
        {
            Console.WriteLine("Packing views...");

            List<Tuple<byte[], byte[]>> assets = new List<Tuple<byte[], byte[]>>();
            foreach (Tuple<string, string> viewPath in viewFiles)
            {
                byte[] nameData = Encoding.ASCII.GetBytes(viewPath.Item1);
                string viewData = File.ReadAllText(viewPath.Item2);
                assets.Add(new Tuple<byte[], byte[]>(nameData, Encoding.ASCII.GetBytes(viewData)));
            }

            return Pack(assets, viewOutputPath + "\\Views.jam");
        }

        private static bool PackSounds(List<Tuple<string, string>> soundFiles, string soundOutputPath)
        {
            Console.WriteLine("Packing sounds...");

            List<Tuple<byte[], byte[]>> assets = new List<Tuple<byte[], byte[]>>();
            foreach (Tuple<string, string> soundPath in soundFiles)
            {
                byte[] nameData = Encoding.ASCII.GetBytes(soundPath.Item1);
                byte[] soundData = File.ReadAllBytes(soundPath.Item2);
                assets.Add(new Tuple<byte[], byte[]>(nameData, soundData));
            }

            return Pack(assets, soundOutputPath + "\\Sounds.jam");
        }

        private static bool PackMusic(List<Tuple<string, string>> musicFiles, string musicOutputPath)
        {
            Console.WriteLine("Packing music...");

            List<Tuple<byte[], byte[]>> assets = new List<Tuple<byte[], byte[]>>();
            foreach (Tuple<string, string> musicPath in musicFiles)
            {
                byte[] nameData = Encoding.ASCII.GetBytes(musicPath.Item1);
                byte[] musicData = File.ReadAllBytes(musicPath.Item2);
                assets.Add(new Tuple<byte[], byte[]>(nameData, musicData));
            }

            return Pack(assets, musicOutputPath + "\\Music.jam");
        }

        private static bool PackData(List<Tuple<string, string>> dataFiles, string dataOutputPath)
        {
            List<Tuple<byte[], byte[]>> assets = new List<Tuple<byte[], byte[]>>();
            foreach (Tuple<string, string> dataPath in dataFiles)
            {
                byte[] nameData = Encoding.ASCII.GetBytes(dataPath.Item1);
                string jsonData = File.ReadAllText(dataPath.Item2);
                assets.Add(new Tuple<byte[], byte[]>(nameData, Encoding.ASCII.GetBytes(jsonData)));
            }

            return Pack(assets, dataOutputPath + "\\Data.jam");
        }

        private static bool PackShaders(List<Tuple<string, string>> shaderFiles, string shaderOutputPath, string objectPath)
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

            return Pack(assets, shaderOutputPath + "\\Shaders.jam");
        }

        private static bool PackSprites(List<Tuple<string, string>> spriteFiles, string spriteOutputPath)
        {
            Console.WriteLine("Packing sprites...");

            List<Tuple<byte[], byte[]>> assets = new List<Tuple<byte[], byte[]>>();
            foreach (Tuple<string, string> spritePath in spriteFiles)
            {
                byte[] nameData = Encoding.ASCII.GetBytes(spritePath.Item1);
                byte[] spriteData = File.ReadAllBytes(spritePath.Item2);
                assets.Add(new Tuple<byte[], byte[]>(nameData, spriteData));
            }

            return Pack(assets, spriteOutputPath + "\\Sprites.jam");
        }
    }
}
