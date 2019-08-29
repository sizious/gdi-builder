using System;
using System.Collections.Generic;
using System.IO;

using GDImageBuilder;

namespace MakeGDI
{
    public class Program
    {
        static int Main(string[] args)
        {
            WriteHeader();
            GDBuilder builder = CreateBuilderInstance(args);
            if (builder != null)
            {
                Console.Write("Writing");
                builder.Execute();
                Console.WriteLine(" Done!");
                return 0;
            }
            return 1;
        }

        private static void ConsoleProgressReport(int amount)
        {
            if (amount % 10 == 0)
            {
                Console.Write('.');
            }
        }        
        
        private static GDBuilder CreateBuilderInstance(string[] args)
        {
            if (args.Length == 0)
            {
                PrintUsage();
                return null;
            }

            // Initialize the GDBuilder
            GDBuilder builder = new GDBuilder();
            builder.ReportProgress += ConsoleProgressReport;

            // Handle the output parameter
            // This one is complex so it deserve a specific method...
            string parseOutputMessage = ParseOutput(builder, args);            
            if (!string.IsNullOrEmpty(parseOutputMessage))
            {
                Console.Error.WriteLine(parseOutputMessage);
                return null;
            }

            builder.ImageDescriptorFileName = CommandLineHelper.GetSoloArgument(args, "o", "gdi");
            builder.RawMode = CommandLineHelper.HasArgument(args, "r", "raw");
//            builder.TruncateData = CommandLineHelper.HasArgument(args, "t", "truncate");            

            // Single Density Area
            builder.SingleDensityArea.BootstrapFilePath = CommandLineHelper.GetSoloArgument(args, "b", "ip-sda"); ;
            builder.SingleDensityArea.SourceDataDirectory = CommandLineHelper.GetSoloArgument(args, "i", "input-sda");
            builder.SingleDensityArea.AudioTrackFileName = CommandLineHelper.GetSoloArgument(args, "g", "gdda-sda");

            // High Density Area
            builder.HighDensityArea.BootstrapFilePath = CommandLineHelper.GetSoloArgument(args, "b", "ip");
            builder.HighDensityArea.SourceDataDirectory = CommandLineHelper.GetSoloArgument(args, "i", "input");
            builder.HighDensityArea.AudioTrackFileNames.AddRange(CommandLineHelper.GetMultiArgument(args, "g", "gdda"));

            // Volume Name
            if (CommandLineHelper.HasArgument(args, "v", "volume-name"))
            {
                builder.PrimaryVolumeDescriptor.VolumeIdentifier = CommandLineHelper.GetSoloArgument(args, "v", "volume-name");
            }

            // Additional PVD
            builder.PrimaryVolumeDescriptor.ApplicationIdentifier = "GD WORKSHOP COPYRIGHT CROSS PRODUCTS 1998";
            builder.PrimaryVolumeDescriptor.DataPreparerIdentifier = "CPL GDWORKSHOP VERSION 2_7_0F FIRM WARE VERSION 2_7_0C";

            if (!CheckArguments(builder))
            {
                return null;
            }

            return builder;
        }

        private static string ParseOutput(GDBuilder builder, string[] args)
        {
            List<string> output = CommandLineHelper.GetMultiArgument(args, "o", "output");
            
            if (output.Count == 0)
            {
                return "No output specified";
            }
            if (output.Count > 2)
            {
                return "Too many output specified.";
            }
            else if (output.Count == 2)
            {
                if (!Path.HasExtension(output[0]) || !Path.HasExtension(output[1]))
                {
                    return "Output filenames are not valid!";
                }

                // Two valid files provided: it's a splitted data track (so we should have GDDA)
                builder.HighDensityArea.DataTrackFirstFileName = output[0];
                builder.HighDensityArea.DataTrackLastFileName = output[1];
            }
            else if (output.Count == 1)
            {
                string outputPath = output[0];
                if (outputPath.EndsWith(Path.DirectorySeparatorChar.ToString()) || !Path.HasExtension(outputPath))
                {
                    // It's a directory
                    builder.OutputDirectory = outputPath;
                }
                else
                {
                    // It's a file only
                    builder.HighDensityArea.DataTrackFirstFileName = outputPath;
                }                
            }

            return string.Empty;
        }

        private static bool CheckArguments(GDBuilder builder)
        {            
            if (!Directory.Exists(builder.HighDensityArea.SourceDataDirectory))
            {
                Console.WriteLine("The specified data directory does not exist!");
                return false;
            }

            if (!File.Exists(builder.HighDensityArea.BootstrapFilePath))
            {
                Console.WriteLine("The specified IP.BIN file does not exist!");
                return false;
            }

            foreach (string track in builder.HighDensityArea.AudioTrackFileNames)
            {
                if (!File.Exists(track) && !File.Exists(Path.Combine(builder.OutputDirectory, track)))
                {
                    Console.WriteLine("The GDDA track " + track + " does not exist!");
                    return false;
                }
            }

            if (builder.HighDensityArea.AudioTrackFileNames.Count > 0 && !string.IsNullOrEmpty(builder.HighDensityArea.DataTrackLastFileName))
            {
                Console.WriteLine("Can't output a single track when CDDA is specified.");
                return false;
            }

            /*if (truncate && fileOutput)
            {
                Console.WriteLine("Can't output a single data track in truncated data mode.");
                Console.WriteLine("Please provide two different output tracks.");
                return false;
            }
            */

            return true;
        }

        private static void WriteHeader()
        {
            Console.WriteLine("BuildGDI - Command line GDIBuilder");
        }

        private static void PrintUsage()
        {            
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
