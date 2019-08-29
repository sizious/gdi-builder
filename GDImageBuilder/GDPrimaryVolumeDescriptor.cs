using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GDImageBuilder
{
    public class GDPrimaryVolumeDescriptor
    {
        public GDPrimaryVolumeDescriptor(GDBuilder owner)
        {
            Owner = owner;
            VolumeIdentifier = "DREAMCAST";
            SystemIdentifier = string.Empty;
            VolumeSetIdentifier = string.Empty;
            PublisherIdentifier = string.Empty;
            DataPreparerIdentifier = string.Empty;
            ApplicationIdentifier = string.Empty;
        }

        public string VolumeIdentifier { get; set; }
        public string SystemIdentifier { get; set; }
        public string VolumeSetIdentifier { get; set; }
        public string PublisherIdentifier { get; set; }
        public string DataPreparerIdentifier { get; set; }
        public string ApplicationIdentifier { get; set; }
        public GDBuilder Owner { get; private set; }
    }
}
