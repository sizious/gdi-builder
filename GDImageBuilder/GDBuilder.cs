using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.IO;

using GDImageBuilder.DiscUtils;
using GDImageBuilder.DiscUtils.Iso9660;

namespace GDImageBuilder
{
    public class GDBuilder
    {
        #region Constants

        private const int TRACK_GAP_SECTOR_COUNT = 150;

        private const int DATA_SECTOR_SIZE = 2048;
        private const int RAW_SECTOR_SIZE = 2352;

        private const int SINGLE_DENSITY_AREA_LBA_START = 0;
        private const int SINGLE_DENSITY_AREA_LBA_END = 33600;

        private const int HIGH_DENSITY_AREA_LBA_START = 45000;
        private const int HIGH_DENSITY_AREA_LBA_END = 549150;

        #endregion

        #region Attributes

        private int _lastProgress;

        private readonly List<GDTrack> _singleDensityAreaTracks;
        private readonly List<GDTrack> _highDensityAreaTracks;        

        private string _singleDensityAreaDataTrackPath;
        private string _singleDensityAreaAudioTrackPath;
        private string _highDensityAreaDataTrackFirstPath;
        private readonly List<string> _gddaTrackPaths;
        private string _highDensityAreaDataTrackLastPath;

        private string _singleDensityAreaBootstrapFileName;
        private string _highDensityAreaBootstrapFileName;

        #endregion

        public GDBuilder()
        {
            _singleDensityAreaTracks = new List<GDTrack>();
            _highDensityAreaTracks = new List<GDTrack>();
            _gddaTrackPaths = new List<string>();
            PrimaryVolumeDescriptor = new GDPrimaryVolumeDescriptor();
            SingleDensityArea = new GDSingleDensityArea();
            HighDensityArea = new GDHighDensityArea();            
        }

        private CDBuilder NewCDBuilderInstance(uint startLba, uint endLba)
        {
            return new CDBuilder()
            {
                VolumeIdentifier = PrimaryVolumeDescriptor.VolumeIdentifier,
                SystemIdentifier = PrimaryVolumeDescriptor.SystemIdentifier,
                VolumeSetIdentifier = PrimaryVolumeDescriptor.VolumeSetIdentifier,
                PublisherIdentifier = PrimaryVolumeDescriptor.PublisherIdentifier,
                DataPreparerIdentifier = PrimaryVolumeDescriptor.DataPreparerIdentifier,
                ApplicationIdentifier = PrimaryVolumeDescriptor.ApplicationIdentifier,
                UseJoliet = false, // A stupid default, mkisofs won't do this by default.
                LBAoffset = startLba,
                EndSector = endLba
            };
        }

        public void BuildSingleDensityArea()
        {
            InitializeSingleDensityArea();

            CDBuilder builder = NewCDBuilderInstance(SINGLE_DENSITY_AREA_LBA_START, SINGLE_DENSITY_AREA_LBA_END);

            byte[] ip0000Data = GDImageUtility.LoadBootstrapInMemory(SingleDensityArea.BootstrapFilePath);
            _singleDensityAreaBootstrapFileName = GDImageUtility.GetBootBinaryFileName(ip0000Data);

            // Handle track02
            List<string> cdda = new List<string>
            {
                _singleDensityAreaAudioTrackPath
            };
            _singleDensityAreaTracks.AddRange(ReadCDDA(cdda));

            // Handle track01
            DirectoryInfo di = new DirectoryInfo(SingleDensityArea.SourceDataDirectory);
            PopulateFromDirectory(builder, di, di.FullName, null, SINGLE_DENSITY_AREA_LBA_END);

            using (BuiltStream isoStream = (BuiltStream)builder.Build())
            {
                _lastProgress = 0;
                WriteDataTrack(true, isoStream, ip0000Data, _singleDensityAreaTracks);
            }   
        }

        public void BuildHighDensityArea()
        {
            InitializeHighDensityArea();

            CDBuilder builder = NewCDBuilderInstance(HIGH_DENSITY_AREA_LBA_START, HIGH_DENSITY_AREA_LBA_END);

            byte[] ipbinData = GDImageUtility.LoadBootstrapInMemory(HighDensityArea.BootstrapFilePath);
            _highDensityAreaBootstrapFileName = GDImageUtility.GetBootBinaryFileName(ipbinData);
            
            DirectoryInfo di = new DirectoryInfo(HighDensityArea.SourceDataDirectory);
            PopulateFromDirectory(builder, di, di.FullName, _highDensityAreaBootstrapFileName, HIGH_DENSITY_AREA_LBA_END);

            // Handle GDDA
            _highDensityAreaTracks.AddRange(ReadCDDA(_gddaTrackPaths));

            // Handle Data Tracks
            using (BuiltStream isoStream = (BuiltStream)builder.Build())
            {
                _lastProgress = 0;
                WriteDataTrack(false, isoStream, ipbinData, _highDensityAreaTracks);                
            }
        }

        private void WriteDataTrack(bool isSingleDensityArea, BuiltStream isoStream, byte[] bootstrapData, List<GDTrack> tracks)
        {
            // When starting this, tracks contains only GDDA tracks (for SDA: only 1 audio track, for HDA: all GDDA if any)

            long currentBytes = 0;
            long totalBytes = isoStream.Length;
            int skip = 0;
            int currentLBA = isSingleDensityArea ? SINGLE_DENSITY_AREA_LBA_START : HIGH_DENSITY_AREA_LBA_START;

            bool isMultiDataTrack = !isSingleDensityArea && HighDensityDataTrackSplitted;

            string dataTrackFileFirstPath = isSingleDensityArea ? _singleDensityAreaDataTrackPath : _highDensityAreaDataTrackFirstPath; // for SDA/HDA
            string dataTrackFileLastPath = isSingleDensityArea ? string.Empty : _highDensityAreaDataTrackLastPath; // for HDA only

            // Retrive the real space occuped on the data track
            long lastHeaderEnd = 0;
            long firstFileStart = 0;
            foreach (BuilderExtent extent in isoStream.BuilderExtents)
            {
                if (extent is FileExtent)
                {
                    firstFileStart = extent.Start;
                    break;
                }
                else
                {
                    lastHeaderEnd = extent.Start + GDImageUtility.RoundUp(extent.Length, DATA_SECTOR_SIZE);
                }
            }
            lastHeaderEnd /= DATA_SECTOR_SIZE;
            firstFileStart /= DATA_SECTOR_SIZE;

            // HDA: Single track is filling all the available space by default, if only one data track in HDA (computed below if HDA has GDDA)            
            int trackEnd = HIGH_DENSITY_AREA_LBA_END - HIGH_DENSITY_AREA_LBA_START;
            if (isSingleDensityArea)
            {
                // SDA: Single data track (track01) is filling the available space... after taken into account the SDA audio track
                long singleDensityAreaAudioTrackFileSize = tracks[0].FileSize; // When SDA, tracks[0] = SDA Audio Track (only 1 track)

                // Computing the space filled by the SDA audio track
                int singleDensityAreaAudioTrackSectorsSize = (int)(GDImageUtility.RoundUp(singleDensityAreaAudioTrackFileSize, RAW_SECTOR_SIZE) / RAW_SECTOR_SIZE);

                // So the SDA data track will fill this space...
                trackEnd = SINGLE_DENSITY_AREA_LBA_END - singleDensityAreaAudioTrackSectorsSize - (TRACK_GAP_SECTOR_COUNT * 2);

                // Updating the SDA audio track in consequence
                tracks[0].LBA = (uint)trackEnd;
            }

            if (isMultiDataTrack)
            {
                // There is a 150 sector gap before and after the GDDA
                trackEnd = (int)(firstFileStart - TRACK_GAP_SECTOR_COUNT);
                for (int i = tracks.Count - 1; i >= 0; i--)
                {
                    trackEnd -= (int)(GDImageUtility.RoundUp(tracks[i].FileSize, RAW_SECTOR_SIZE) / RAW_SECTOR_SIZE);
                    //Track end is now the beginning of this track and the end of the previous
                    tracks[i].LBA = (uint)(trackEnd + HIGH_DENSITY_AREA_LBA_START);
                }
                trackEnd -= TRACK_GAP_SECTOR_COUNT;

                if (trackEnd < lastHeaderEnd)
                {
                    throw new Exception("Not enough room to fit all of the CDDA after we added the data.");
                }

                // trackEnd: HDA when multi tracks: computed with GDDA
            }

            // Applied for SDA data track and HDA first data track
            if (TruncateData)
            {
                trackEnd = (int)lastHeaderEnd;
            }

            // Handle data track (SDA is track01, HDA is track03)
            GDTrack firstTrack = new GDTrack
            {
                FileName = Path.GetFileName(dataTrackFileFirstPath),
                FileSize = trackEnd * DATA_SECTOR_SIZE,
                LBA = (uint)currentLBA,                                
                Type = GDTrackType.Data
            };
            tracks.Insert(0, firstTrack); // the first data track is at the beginning of the area

            // Handle last data track for HDA (if applicable)
            GDTrack lastTrack = null;
            if (isMultiDataTrack)
            {
                lastTrack = new GDTrack
                {
                    FileName = Path.GetFileName(dataTrackFileLastPath),
                    FileSize = (HIGH_DENSITY_AREA_LBA_END - HIGH_DENSITY_AREA_LBA_START - firstFileStart) * DATA_SECTOR_SIZE,
                    LBA = (uint)(HIGH_DENSITY_AREA_LBA_START + firstFileStart),
                    Type = GDTrackType.Data
                };
                tracks.Add(lastTrack);
            }

            // Update the TOC in the IP.BIN for the HDA
            if (!isSingleDensityArea)
            {
                GDImageUtility.UpdateBootstrapTableOfContents(bootstrapData, tracks);
            }

            // Initialize stream variables
            byte[] buffer = RawMode ? new byte[DATA_SECTOR_SIZE] : new byte[32 * DATA_SECTOR_SIZE];
            int numRead = 0;

            // Write first (or single) data track
            using (FileStream destStream = new FileStream(dataTrackFileFirstPath, FileMode.Create, FileAccess.Write))
            {
                // Write Bootsector data in the first sector
                long bytesWritten = GDImageWriteHelper.WriteBootSector(RawMode, isoStream, destStream, buffer, ref currentLBA, ref currentBytes, bootstrapData);

                numRead = isoStream.Read(buffer, 0, buffer.Length);
                while (numRead != 0 && bytesWritten < firstTrack.FileSize)
                {
                    GDImageWriteHelper.WriteSector(RawMode, isoStream, destStream, buffer, ref currentLBA, ref numRead);

                    numRead = isoStream.Read(buffer, 0, buffer.Length);
                    bytesWritten += numRead;
                    currentBytes += numRead;

                    skip = NotifyProgress(currentBytes, totalBytes, skip);
                }
            }

            // Write last data track (if any)
            if (isMultiDataTrack)
            {
                currentLBA = (int)lastTrack.LBA;

                using (FileStream destStream = new FileStream(dataTrackFileLastPath, FileMode.Create, FileAccess.Write))
                {
                    currentBytes = firstFileStart * DATA_SECTOR_SIZE;
                    isoStream.Seek(currentBytes, SeekOrigin.Begin);

                    numRead = isoStream.Read(buffer, 0, buffer.Length);
                    while (numRead != 0)
                    {
                        GDImageWriteHelper.WriteSector(RawMode, isoStream, destStream, buffer, ref currentLBA, ref numRead);

                        numRead = isoStream.Read(buffer, 0, buffer.Length);
                        currentBytes += numRead;

                        skip = NotifyProgress(currentBytes, totalBytes, skip);
                    }
                }
            }
        }

        private int NotifyProgress(long currentBytes, long totalBytes, int skip)
        {
            skip++;
            if (skip >= 10)
            {
                skip = 0;
                int percent = (int)((currentBytes * 100) / totalBytes);
                if (percent > _lastProgress)
                {
                    _lastProgress = percent;
                    ReportProgress?.Invoke(_lastProgress);
                }
            }
            return skip;
        }

        public string CheckOutputExists(List<string> cdda, string output)
        {
            List<string> filesToCheck = new List<string>();
            filesToCheck.Add("track03.bin");
            if (cdda != null && cdda.Count > 0)
            {
                filesToCheck.Add(GetLastTrackName(cdda.Count));
            }
            StringBuilder sb = new StringBuilder();
            int fc = 0;
            foreach (string file in filesToCheck)
            {
                if (File.Exists(Path.Combine(output, file)))
                {
                    sb.Append(file + ", ");
                    fc++;
                }
            }
            if (fc >= 2)
            {
                return "The files " + sb.ToString(0, sb.Length - 2) + " already exist. They will be overwritten. Are you sure?";
            }
            else if (fc == 1)
            {
                return "The file " + sb.ToString(0, sb.Length - 2) + " already exists. It will be overwritten. Are you sure?";
            }
            return null;
        }

        private string GenerateImageDescriptor(bool regenerateSingleDensityAreaDescriptor)
        {
            StringBuilder sb = new StringBuilder();            
            int tn = 3;

            if (regenerateSingleDensityAreaDescriptor)
            {
                tn = 1;
                foreach (GDTrack track in _singleDensityAreaTracks)
                {
                    sb.Append(track.ToString(tn, RawMode));
                    tn++;
                }
            }

            foreach (GDTrack track in _highDensityAreaTracks)
            {
                sb.Append(track.ToString(tn, RawMode));
                tn++;
            }

            return sb.ToString();
        }

        public void WriteImageDescriptor(string gdiPath, bool regenerateSingleDensityAreaDescriptor)
        {
            StringBuilder sb = new StringBuilder();

            // 2 is for SDA tracks (always 2, even if we don't have regenerated SDA)
            sb.AppendLine((2 + _highDensityAreaTracks.Count).ToString());

            // Extract SDA from existing GDI if necessary
            if (File.Exists(gdiPath))
            {
                string[] file = File.ReadAllLines(gdiPath);
                if (!regenerateSingleDensityAreaDescriptor && (file.Length > 2))
                {
                    sb.AppendLine(file[1]); // SDA: track01
                    sb.AppendLine(file[2]); // SDA: track02
                }
            }

            sb.Append(GenerateImageDescriptor(regenerateSingleDensityAreaDescriptor));
            File.WriteAllText(gdiPath, sb.ToString());
        }

        private void PopulateFromDirectory(CDBuilder builder, DirectoryInfo di, string basePath, string bootBin, uint endLba)
        {
            FileInfo bootBinFile = null;
            foreach (FileInfo file in di.GetFiles())
            {
                string filePath = file.FullName.Substring(basePath.Length);
                if (bootBin != null && file.Name.Equals(bootBin, StringComparison.OrdinalIgnoreCase))
                {
                    bootBinFile = file; // Ignore this for now, we want it last
                }
                else
                {
                    builder.AddFile(filePath, file.FullName);
                }
            }

            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                PopulateFromDirectory(builder, dir, basePath, null, endLba);
            }

            if (bootBinFile != null && bootBin != null)
            {
                builder.AddFile(bootBin, bootBinFile.FullName);
                long sectorSize = GDImageUtility.RoundUp(bootBinFile.Length, DATA_SECTOR_SIZE);
                builder.LastFileStartSector = (uint)(endLba - TRACK_GAP_SECTOR_COUNT - (sectorSize / DATA_SECTOR_SIZE));
            }
            else if (bootBin != null)
            {
                // User doesn't know what they're doing and gave us bad data.
                throw new FileNotFoundException("IP.BIN requires the boot file " + bootBin +
                    " which was not found in the data directory.");
            }
        }

        private string GetLastTrackName(int cddaTracks)
        {
            string result = "track" + (cddaTracks + 4).ToString("00") + ".bin";
            return (cddaTracks > 0) ? result : string.Empty;
        }

        private void InitializeSingleDensityArea()
        {            
            _singleDensityAreaTracks.Clear();

            // SDA data track
            _singleDensityAreaDataTrackPath = Path.Combine(OutputDirectory, SingleDensityArea.DataTrackFileName);

            // SDA audio track
            _singleDensityAreaAudioTrackPath = Path.Combine(OutputDirectory, SingleDensityArea.AudioTrackFileName);

            // SDA Boot file read from Bootstrap (usually 1ST_READ.BIN)
            _singleDensityAreaBootstrapFileName = string.Empty;
        }

        private void InitializeHighDensityArea()
        {
            _highDensityAreaTracks.Clear();

            // First HDA data track (always required)
            _highDensityAreaDataTrackFirstPath = Path.Combine(OutputDirectory, HighDensityArea.DataTrackFirstFileName);

            // Last HDA data track is only when GDDA are detected...
            string hdaLastDataTrackFileName = GetLastTrackName(HighDensityArea.AudioTrackFileNames.Count);
            if (!string.IsNullOrEmpty(HighDensityArea.DataTrackLastFileName))
            {
                hdaLastDataTrackFileName = HighDensityArea.DataTrackLastFileName;
            }
            _highDensityAreaDataTrackLastPath = !string.IsNullOrEmpty(hdaLastDataTrackFileName) ? Path.Combine(OutputDirectory, hdaLastDataTrackFileName) : string.Empty;

            // Handle HDA audio tracks
            _gddaTrackPaths.Clear();
            foreach (string gddaTrack in HighDensityArea.AudioTrackFileNames)
            {
                _gddaTrackPaths.Add(Path.Combine(OutputDirectory, gddaTrack));
            }

            // HDA Boot file read from Bootstrap (usually 1ST_READ.BIN)
            _highDensityAreaBootstrapFileName = string.Empty;
        }

        private List<GDTrack> ReadCDDA(List<string> gdda)
        {
            List<GDTrack> retval = new List<GDTrack>();
            foreach (string path in gdda)
            {
                FileInfo fi = new FileInfo(path);
                if (!fi.Exists)
                {
                    throw new FileNotFoundException("GDDA track " + fi.Name + " could not be accessed.");
                }

                GDTrack track = new GDTrack
                {
                    FileName = fi.Name,
                    Type = 0,
                    FileSize = fi.Length
                };

                retval.Add(track);
            }

            return retval;
        }

        private bool HighDensityDataTrackSplitted
        {
            get
            {
                return HighDensityArea.AudioTrackFileNames.Any() 
                    || (TruncateData && !string.IsNullOrEmpty(HighDensityArea.DataTrackLastFileName));
            }
        }

        #region Published Properties

        public GDSingleDensityArea SingleDensityArea { get; private set; }

        public GDHighDensityArea HighDensityArea { get; private set; }

        public GDPrimaryVolumeDescriptor PrimaryVolumeDescriptor { get; private set; }

        public string OutputDirectory { get; set; }

        public bool RawMode { get; set; }

        public bool TruncateData { get; set; }

        #endregion

        #region Published Events

        public delegate void OnReportProgress(int percent);
        public OnReportProgress ReportProgress { get; set; }

        #endregion
    }
}
