using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GDImageBuilder
{
    public class GDHighDensityArea
    {
        public GDHighDensityArea(GDBuilder owner)
        {
            Owner = owner;

            AudioTrackFileNames = new List<string>();

            SetDefaultFileNames();
            DataTrackLastFileName = string.Empty;

            BootstrapFilePath = string.Empty; // IP.BIN
        }

        internal void SetDefaultFileNames()
        {
            DataTrackFirstFileName = GDImageUtility.GetDefaultFileName(DataTrackFirstFileName, "track03", GDTrackType.Data, Owner.RawMode);
        }

        public string SourceDataDirectory { get; set; }
        public string BootstrapFilePath { get; set; }
        public string DataTrackFirstFileName { get; set; }
        public List<string> AudioTrackFileNames { get; private set; }
        public string DataTrackLastFileName { get; set; }
        public GDBuilder Owner { get; private set; }
    }
}
