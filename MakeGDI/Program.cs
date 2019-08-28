using System;
using System.Collections.Generic;
using System.IO;

using GDImageBuilder;

namespace MakeGDI
{
    public class Program
    {
        static void Main(string[] args)
        {
            /*if (args.Length < 3)
            {
                PrintUsage();
                return;
            }*/

            string sdaData = @"C:\Temp\SDA\";
            string ip0000 = @"C:\Temp\IP0000.BIN";
            string track02 = @"C:\Temp\_out\track02.raw";

            string data = @"C:\Temp\HDA\"; // GetSoloArgument("-data", args);
            string ipBin = @"C:\Temp\IP.BIN"; // GetSoloArgument("-ip", args);
            List<string> outPath = new List<string>(); outPath.Add(@"C:\Temp\_out\"); //GetMultiArgument("-output", args);
            List<string> cdda = new List<string>(); cdda.Add(@"C:\Temp\_out\track04.raw"); // GetMultiArgument("-cdda", args);
            string gdiPath = @"C:\Temp\_out\disc.gdi"; // GetSoloArgument("-gdi", args);
            string volume = GetSoloArgument("-V", args);
            bool truncate = true; // HasArgument("-truncate", args);
            bool fileOutput = false;

            if (!CheckArguments(data, ipBin, outPath, cdda, truncate, out fileOutput))
            {
                return;
            }

            GDBuilder builder = new GDBuilder()
            {
                OutputDirectory = outPath[0],
                RawMode = false, // HasArgument("-raw", args),
                TruncateData = truncate
            };
            builder.ReportProgress += ConsoleProgressReport;

            // SDA            
            builder.SingleDensityArea.BootstrapFilePath = ip0000;
            builder.SingleDensityArea.SourceDataDirectory = sdaData;
            builder.SingleDensityArea.AudioTrackFileName = track02;

            // HDA
            builder.HighDensityArea.BootstrapFilePath = ipBin;
            builder.HighDensityArea.SourceDataDirectory = data;
            builder.HighDensityArea.AudioTrackFileNames.AddRange(cdda);

            if (volume != null)
            {
                builder.PrimaryVolumeDescriptor.VolumeIdentifier = volume;
            }

            Console.Write("Writing");
            List<GDTrack> tracks = null;

            /*if (fileOutput)
            {
                builder.SingleDensityArea.DataTrackPath = Path.GetFullPath(outPath[0]) + "01";
                builder.HighDensityArea.DataTrackFirstPath = Path.GetFullPath(outPath[0]);
                if (outPath.Count == 2 && (cdda.Count > 0 || builder.TruncateData))
                {
                    builder.HighDensityAreaDataTrackLastPath = Path.GetFullPath(outPath[1]);
                }
                tracks = builder.BuildGDROM(data, ipBin, cdda);
            }
            else
            {
                tracks = builder.BuildGDROM(data, ipBin, cdda, outPath[0]);
            }*/

            builder.BuildSingleDensityArea();
            builder.BuildHighDensityArea();

            builder.WriteImageDescriptor(gdiPath, true);

            Console.WriteLine(" Done!");            
        }

        private static void ConsoleProgressReport(int amount)
        {
            if (amount % 10 == 0)
            {
                Console.Write('.');
            }
        }

        private static bool CheckArguments(string data, string ipBin, List<string> outPath, List<string> cdda, bool truncate, out bool fileOutput)
        {
            fileOutput = false;
            if (data == null || ipBin == null || outPath.Count == 0)
            {
                Console.WriteLine("The required fields have not been provided.");
                return false;
            }
            if (!Directory.Exists(data))
            {
                Console.WriteLine("The specified data directory does not exist!");
                return false;
            }
            if (!File.Exists(ipBin))
            {
                Console.WriteLine("The specified IP.BIN file does not exist!");
                return false;
            }
            foreach (string track in cdda)
            {
                if (!File.Exists(track))
                {
                    Console.WriteLine("The CDDA track " + track + " does not exist!");
                    return false;
                }
            }
            if (outPath.Count > 2)
            {
                Console.WriteLine("Too many output paths specified.");
                return false;
            }
            else if (outPath.Count == 2)
            {
                fileOutput = true;
                if (!Path.HasExtension(outPath[0]) || !Path.HasExtension(outPath[1]))
                {
                    Console.WriteLine("Output filenames are not valid!");
                    return false;
                }
            }
            else
            {
                string path = outPath[0];
                if (path.EndsWith(Path.DirectorySeparatorChar.ToString()) || !Path.HasExtension(path))
                {
                    fileOutput = false;
                }
                else
                {
                    fileOutput = true;
                }
                if (truncate && fileOutput)
                {
                    Console.WriteLine("Can't output a single data track in truncated data mode.");
                    Console.WriteLine("Please provide two different output tracks.");
                    return false;
                }
                if (cdda.Count > 0 && fileOutput)
                {
                    Console.WriteLine("Can't output a single track when CDDA is specified.");
                    return false;
                }
            }
            return true;
        }

        private static bool HasArgument(string argument, string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals(argument, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private static string GetSoloArgument(string argument, string[] args)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i].Equals(argument, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }
            return null;
        }

        private static List<string> GetMultiArgument(string argument, string[] args)
        {
            List<string> retval = new List<string>();
            int start = -1;
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i].Equals(argument, StringComparison.OrdinalIgnoreCase))
                {
                    start = i + 1;
                    break;
                }
            }
            if (start > 0)
            {
                for (int i = start; i < args.Length; i++)
                {
                    if (args[i].StartsWith("-"))
                    {
                        break;
                    }
                    else
                    {
                        retval.Add(args[i]);
                    }
                }
            }
            return retval;
        }

        private static void PrintUsage()
        {
            Console.WriteLine("BuildGDI - Command line GDIBuilder");
            Console.WriteLine("Usage: buildgdi -data dataFolder -ip IP.BIN -cdda track04.raw track05.raw -output folder -gdi disc.gdi");
            Console.WriteLine();
            Console.WriteLine("Arguments:");
            Console.WriteLine("-data <folder> (Required) = Location of the files for the disc");
            Console.WriteLine("-ip <file> (Required) = Location of disc IP.BIN bootsector");
            Console.WriteLine("-cdda <files> (Optional) = List of RAW CDDA tracks on the disc");
            Console.WriteLine("-output <folder or file(s)> (Required) = Output location");
            Console.WriteLine("   If output is a folder, tracks with default filenames will be generated.");
            Console.WriteLine("   Otherwise, specify one filename for track03.bin on data only discs, ");
            Console.WriteLine("   or two files for discs with CDDA.");
            Console.WriteLine("-gdi <file> (Optional) = Path of the disc.gdi file for this disc.");
            Console.WriteLine("   Existing GDI files will be updated with the new tracks.");
            Console.WriteLine("   If no GDI exists, only lines for tracks 3 and above will be written.");
            Console.WriteLine("-V <volume identifier> (Optional) = The volume name (Default is DREAMCAST)");
            Console.WriteLine("-raw (Optional) = Output 2352 byte raw disc sectors instead of 2048.");
            Console.WriteLine("-truncate (Optional) = Do not pad generated data to the correct size.");
        }
    }
}
