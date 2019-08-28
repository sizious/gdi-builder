using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GDImageBuilder
{
    public class GDTrack
    {
        public string FileName { get; set; }

        public long FileSize { get; set; }

        public uint LBA { get; set; }

        public GDTrackType Type { get; set; } // 4 is Data, 0 is Audio

        public string ToString(int trackNumber, bool isRawMode)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(trackNumber + " " + LBA + " " + (int) Type + " ");
            if (Type == 0 || isRawMode)
            {
                sb.Append("2352 ");
            }
            else
            {
                sb.Append("2048 ");
            }
            sb.AppendLine(FileName + " 0");

            return sb.ToString();
        }
    }
}
