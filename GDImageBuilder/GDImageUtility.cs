using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GDImageBuilder
{
    internal class GDImageUtility
    {
        private const uint BOOTSTRAP_SIZE = 0x8000; // 32 KB

        public static long RoundUp(long value, long unit)
        {
            return ((value + (unit - 1)) / unit) * unit;
        }
      
        public static byte[] LoadBootstrapInMemory(string bootstrapFilePath)
        {
            byte[] bootstrapData = new byte[BOOTSTRAP_SIZE];

            using (FileStream ipfs = new FileStream(bootstrapFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (ipfs.Length != bootstrapData.Length)
                {
                    throw new Exception("Wrong Bootstrap size. Possibly the wrong file? Cannot continue.");
                }
                ipfs.Read(bootstrapData, 0, bootstrapData.Length);
            }

            return bootstrapData;
        }

        public static string GetBootBinaryFileName(byte[] bootstrapData)
        {
            return Encoding.ASCII.GetString(bootstrapData, 0x60, 0x0F).Trim();
        }

        public static void UpdateBootstrapTableOfContents(byte[] bootstrapData, List<GDTrack> tracks)
        {
            //Tracks 03 to 99, 1 and 2 were in the low density area
            for (int t = 0; t < 97; t++)
            {
                uint dcLBA = 0xFFFFFF;
                byte dcType = 0xFF;
                if (t < tracks.Count)
                {
                    GDTrack track = tracks[t];
                    dcLBA = track.LBA + 150;
                    dcType = (byte)(((uint)track.Type << 4) | 0x1);
                }
                int offset = 0x104 + (t * 4);
                bootstrapData[offset++] = (byte)(dcLBA & 0xFF);
                bootstrapData[offset++] = (byte)((dcLBA >> 8) & 0xFF);
                bootstrapData[offset++] = (byte)((dcLBA >> 16) & 0xFF);
                bootstrapData[offset] = dcType;
            }
        }
    }
}
