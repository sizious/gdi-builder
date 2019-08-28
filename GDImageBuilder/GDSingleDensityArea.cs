using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GDImageBuilder
{
    public class GDSingleDensityArea
    {
        public GDSingleDensityArea()
        {
            DataTrackFileName = "track01.bin";
            AudioTrackFileName = "track02.raw";
            BootstrapFilePath = string.Empty; // IP0000.BIN
        }

        public string SourceDataDirectory;

        public string BootstrapFilePath { get; set; }

        public string DataTrackFileName { get; set; }
        public string AudioTrackFileName { get; set; }
    }
}
