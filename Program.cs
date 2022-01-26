using System;
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

                return AssetEnumerator.EnumerateAssets(args) ? 0 : -1;
            }
            else if (args[0].ToLower() == "pack")
            {
                if (args.Length < 3)
                {
                    Console.WriteLine("Invalid parameters! Please specify an asset directory path and an output directory path when using the 'Pack' flag.");
                    Console.WriteLine("Example: JamPacker Pack [AssetDirectory] [OutputDirectory]");

                    return -1;
                }

                return AssetPacker.PackAssets(args) ? 0 : -1;
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

        

        
    }
}
