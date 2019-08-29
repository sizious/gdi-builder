using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GDImageBuilder
{
    public class GDSingleDensityArea
    {
        public GDSingleDensityArea(GDBuilder owner)
        {
            Owner = owner;
            SetDefaultFileNames();
            BootstrapFilePath = string.Empty; // IP0000.BIN
        }

        internal void SetDefaultFileNames()
        {
            DataTrackFileName = GDImageUtility.GetDefaultFileName(DataTrackFileName, "track01", GDTrackType.Data, Owner.RawMode);
            AudioTrackFileName = GDImageUtility.GetDefaultFileName(AudioTrackFileName, "track02", GDTrackType.Audio, Owner.RawMode);
        }

        public string SourceDataDirectory;

        public string BootstrapFilePath { get; set; }

        public string DataTrackFileName { get; set; }
        public string AudioTrackFileName { get; set; }

        public GDBuilder Owner { get; private set; }
    }
}
