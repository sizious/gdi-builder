using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GDImageBuilder
{
    public class GDHighDensityArea
    {
        public GDHighDensityArea()
        {
            AudioTrackFileNames = new List<string>();

            DataTrackFirstFileName = "track03.bin";
            DataTrackLastFileName = string.Empty;

            BootstrapFilePath = string.Empty; // IP.BIN
        }

        public string SourceDataDirectory { get; set; }
        public string BootstrapFilePath { get; set; }
        public string DataTrackFirstFileName { get; set; }
        public List<string> AudioTrackFileNames { get; private set; }
        public string DataTrackLastFileName { get; set; }
    }
}
