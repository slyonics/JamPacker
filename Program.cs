using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using K4os.Compression.LZ4;

namespace JamPacker
{
    class Program
    {
        static int Main(string[] args)
        {
            Console.WriteLine("Starting JamPacker...");

            if (args.Length < 2)
            {
                Console.WriteLine("Please specify a content path and an output path.");

                return -1;
            }

            string contentPath = args[0];
            string outputPath = args[1];

            string[] contentSubdirs = Directory.GetDirectories(contentPath);

            Console.WriteLine("Enumerating fonts...");
            IEnumerable<string> fontFiles = Directory.EnumerateFiles(contentPath + "\\Fonts", "*.ttf", SearchOption.AllDirectories);
            PackFonts(fontFiles, outputPath);

            Console.WriteLine("Enumerating views...");
            IEnumerable<string> viewFiles = Directory.EnumerateFiles(contentPath + "\\..\\Scenes", "*.xml", SearchOption.AllDirectories);
            viewFiles = viewFiles.Concat(Directory.EnumerateFiles(contentPath + "\\..\\SceneObjects", "*.xml", SearchOption.AllDirectories));
            PackViews(viewFiles, outputPath);

            Console.WriteLine("Enumerating sounds...");
            IEnumerable<string> soundFiles = Directory.EnumerateFiles(contentPath + "\\Audio\\Sounds", "*.wav", SearchOption.AllDirectories);
            PackSounds(soundFiles, outputPath);

            Console.WriteLine("Enumerating music...");
            IEnumerable<string> musicFiles = Directory.EnumerateFiles(contentPath + "\\Audio\\Music", "*.mp3", SearchOption.AllDirectories);
            musicFiles = musicFiles.Concat(Directory.EnumerateFiles(contentPath + "\\Audio\\Music", "*.ogg", SearchOption.AllDirectories));
            PackMusic(musicFiles, outputPath);

            Console.WriteLine("Enumerating data...");
            IEnumerable<string> dataFiles = Directory.EnumerateFiles(contentPath + "\\Data", "*.json", SearchOption.AllDirectories);
            PackData(dataFiles, outputPath);

            Console.WriteLine("Enumerating shaders...");
            IEnumerable<string> shaderFiles = Directory.EnumerateFiles(contentPath + "\\Shaders", "*.fx", SearchOption.AllDirectories);
            PackShaders(shaderFiles, outputPath);

            return 0;
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
        private static void PackFonts(IEnumerable<string> fontFiles, string fontOutputPath)
        {
            Console.WriteLine("Packing fonts...");

            List<Tuple<byte[], byte[]>> assets = new List<Tuple<byte[], byte[]>>();            
            foreach (string fontPath in fontFiles)
            {
                string fontFile = Path.GetFileNameWithoutExtension(fontPath);
                byte[] nameData = Encoding.ASCII.GetBytes(fontFile);
                byte[] fontData = File.ReadAllBytes(fontPath);
                assets.Add(new Tuple<byte[], byte[]>(nameData, fontData));
            }

            if (!Pack(assets, fontOutputPath + "\\Fonts.jam"))
            {
                throw new Exception("Unable to compress font data!");
            }
        }

        private static void PackViews(IEnumerable<string> viewFiles, string viewOutputPath)
        {
            Console.WriteLine("Packing views...");

            List<Tuple<byte[], byte[]>> assets = new List<Tuple<byte[], byte[]>>();
            foreach (string viewPath in viewFiles)
            {
                string viewFile = Path.GetFileNameWithoutExtension(viewPath);
                byte[] nameData = Encoding.ASCII.GetBytes(viewFile);
                string viewData = File.ReadAllText(viewPath);
                assets.Add(new Tuple<byte[], byte[]>(nameData, Encoding.ASCII.GetBytes(viewData)));
            }

            if (!Pack(assets, viewOutputPath + "\\Views.jam"))
            {
                throw new Exception("Unable to compress view data!");
            }
        }

        private static void PackSounds(IEnumerable<string> soundFiles, string soundOutputPath)
        {
            Console.WriteLine("Packing sounds...");

            List<Tuple<byte[], byte[]>> assets = new List<Tuple<byte[], byte[]>>();
            foreach (string soundPath in soundFiles)
            {
                string soundFile = Path.GetFileNameWithoutExtension(soundPath);
                byte[] nameData = Encoding.ASCII.GetBytes(soundFile);
                byte[] soundData = File.ReadAllBytes(soundPath);
                assets.Add(new Tuple<byte[], byte[]>(nameData, soundData));
            }

            if (!Pack(assets, soundOutputPath + "\\Sounds.jam"))
            {
                throw new Exception("Unable to compress sound data!");
            }
        }

        private static void PackMusic (IEnumerable<string> musicFiles, string musicOutputPath)
        {
            Console.WriteLine("Packing music...");

            List<Tuple<byte[], byte[]>> assets = new List<Tuple<byte[], byte[]>>();
            foreach (string musicPath in musicFiles)
            {
                string musicFile = Path.GetFileNameWithoutExtension(musicPath);
                byte[] nameData = Encoding.ASCII.GetBytes(musicFile);
                byte[] musicData = File.ReadAllBytes(musicPath);
                assets.Add(new Tuple<byte[], byte[]>(nameData, musicData));
            }

            if (!Pack(assets, musicOutputPath + "\\Music.jam"))
            {
                throw new Exception("Unable to compress music data!");
            }
        }

        private static void PackData(IEnumerable<string> dataFiles, string dataOutputPath)
        {
            Console.WriteLine("Packing data...");

            List<Tuple<byte[], byte[]>> assets = new List<Tuple<byte[], byte[]>>();
            foreach (string dataPath in dataFiles)
            {
                string dataFile = Path.GetFileNameWithoutExtension(dataPath);
                byte[] nameData = Encoding.ASCII.GetBytes(dataFile);
                string jsonData = File.ReadAllText(dataPath);
                assets.Add(new Tuple<byte[], byte[]>(nameData, Encoding.ASCII.GetBytes(jsonData)));
            }

            if (!Pack(assets, dataOutputPath + "\\Data.jam"))
            {
                throw new Exception("Unable to compress JSON data!");
            }
        }

        private static void PackShaders(IEnumerable<string> shaderFiles, string shaderOutputPath)
        {
            Console.WriteLine("Packing shaders...");

            bool errors = false;
            List<Tuple<byte[], byte[]>> assets = new List<Tuple<byte[], byte[]>>();
            foreach (string shaderPath in shaderFiles)
            {
                string compileCommmand = string.Format("/C mgfxc {0} temp_shader /Profile:OpenGL", shaderPath);
                var process = System.Diagnostics.Process.Start("CMD.exe", compileCommmand);
                process.WaitForExit();

                if (process.ExitCode != 0) errors = true;
                else
                {
                    string shaderFile = Path.GetFileNameWithoutExtension(shaderPath);
                    byte[] nameData = Encoding.ASCII.GetBytes(shaderFile);
                    byte[] shaderData = File.ReadAllBytes("temp_shader");
                    assets.Add(new Tuple<byte[], byte[]>(nameData, shaderData));
                }
            }

            try
            {
                File.Delete("temp_shader");
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
    }
}
