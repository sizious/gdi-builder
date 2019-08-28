using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Threading;
using GDImageBuilder;

namespace GDIbuilder
{
    public partial class GDIBuilderForm : Form
    {
        private GDBuilder _builder;
        private Thread _worker;

        public GDIBuilderForm()
        {
            InitializeComponent();
            _builder = new GDBuilder();
        }

        private void btnSelectData_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                txtData.Text = fbd.SelectedPath;
            }
        }

        private void btnSelectIP_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "IP.BIN (*.bin)|*.bin";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                txtIpBin.Text = ofd.FileName;
            }
        }

        private void btnSelCdda_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Raw CDDA (*.raw)|*.raw";
            ofd.Multiselect = true;
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                foreach (string file in ofd.FileNames)
                {
                    lstCdda.Items.Add(new CddaItem { FilePath = file });
                }
            }
        }

        private void btnSelOutput_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                txtOutdir.Text = fbd.SelectedPath;
            }
        }

        private void DisableButtons()
        {
            foreach (Control ctrl in this.Controls)
            {
                if (ctrl is Button)
                {
                    ((Button)ctrl).Enabled = false;
                }
            }
            chkRawMode.Enabled = false;
        }

        private void btnMake_Click(object sender, EventArgs e)
        {
            if (txtData.Text.Length > 0 && txtIpBin.Text.Length > 0 && txtOutdir.Text.Length > 0)
            {
                List<string> cdTracks = new List<string>();
                foreach (CddaItem lvi in lstCdda.Items)
                {
                    cdTracks.Add(lvi.FilePath);
                }
                string checkMsg = _builder.CheckOutputExists(cdTracks, txtOutdir.Text);
                if (checkMsg != null)
                {
                    if(MessageBox.Show(checkMsg,"Warning",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)==DialogResult.No){
                        return;
                    }
                }
                DisableButtons();
                _builder.RawMode = chkRawMode.Checked;
                _builder.ReportProgress = UpdateProgress;
                _worker = new Thread(() => DoDiscBuild(txtData.Text, txtIpBin.Text, cdTracks, txtOutdir.Text));
                _worker.Start();
            }
            else
            {
                MessageBox.Show("Not ready to build disc. Please provide more information above.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DoDiscBuild(string dataDir, string ipBin, List<string> trackList, string outdir)
        {
            try
            {
                _builder.HighDensityArea.SourceDataDirectory = dataDir;
                _builder.HighDensityArea.BootstrapFilePath = ipBin;
                _builder.HighDensityArea.AudioTrackFileNames.AddRange(trackList);
                _builder.OutputDirectory = outdir;

                _builder.BuildHighDensityArea();

                Invoke(new Action(() =>
                {
                    string gdiPath = System.IO.Path.Combine(outdir, "disc.gdi");
                    if (System.IO.File.Exists(gdiPath))
                    {
                        _builder.WriteImageDescriptor(gdiPath, false);
                    }
                    MessageBox.Show("Done!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
//                    ResultDialog rd = new ResultDialog(_builder.GetGDIText(tracks));
//                    rd.ShowDialog();
//                    Close();
                }));
            }
            catch (Exception ex)
            {
                Invoke(new Action(()=>{
                    MessageBox.Show("Failed to build disc.\n"+ex.Message,"Error",MessageBoxButtons.OK,MessageBoxIcon.Error);
//                    Close();
                }));
            }
            _worker = null;
        }

        private void UpdateProgress(int percent)
        {
            Invoke(new Action(() => { pbProgress.Value = percent; }));
        }

        private void btnRemoveCdda_Click(object sender, EventArgs e)
        {
            if (lstCdda.SelectedIndex >= 0)
            {
                lstCdda.Items.RemoveAt(lstCdda.SelectedIndex);
            }
        }

        private void btnMoveCddaUp_Click(object sender, EventArgs e)
        {
            if (lstCdda.SelectedIndex > 0)
            {
                int idx = lstCdda.SelectedIndex;
                object item = lstCdda.Items[idx];
                lstCdda.Items.RemoveAt(idx);
                lstCdda.Items.Insert(idx - 1, item);
                lstCdda.SelectedIndex = idx - 1;
            }
        }

        private void btnMoveCddaDown_Click(object sender, EventArgs e)
        {
            if (lstCdda.SelectedIndex >= 0 && lstCdda.SelectedIndex < lstCdda.Items.Count-1)
            {
                int idx = lstCdda.SelectedIndex;
                object item = lstCdda.Items[idx];
                lstCdda.Items.RemoveAt(idx);
                lstCdda.Items.Insert(idx + 1, item);
                lstCdda.SelectedIndex = idx + 1;
            }
        }

        private void btnAdvanced_Click(object sender, EventArgs e)
        {
            AdvancedDialog adv = new AdvancedDialog();
            adv.VolumeIdentifier = _builder.PrimaryVolumeDescriptor.VolumeIdentifier;
            adv.SystemIdentifier = _builder.PrimaryVolumeDescriptor.SystemIdentifier;
            adv.VolumeSetIdentifier = _builder.PrimaryVolumeDescriptor.VolumeSetIdentifier;
            adv.PublisherIdentifier = _builder.PrimaryVolumeDescriptor.PublisherIdentifier;
            adv.DataPreparerIdentifier = _builder.PrimaryVolumeDescriptor.DataPreparerIdentifier;
            adv.ApplicationIdentifier = _builder.PrimaryVolumeDescriptor.ApplicationIdentifier;
            adv.TruncateMode = _builder.TruncateData;
            if (adv.ShowDialog() == DialogResult.OK)
            {
                _builder.PrimaryVolumeDescriptor.VolumeIdentifier = adv.VolumeIdentifier;
                _builder.PrimaryVolumeDescriptor.SystemIdentifier = adv.SystemIdentifier;
                _builder.PrimaryVolumeDescriptor.VolumeSetIdentifier = adv.VolumeSetIdentifier;
                _builder.PrimaryVolumeDescriptor.PublisherIdentifier = adv.PublisherIdentifier;
                _builder.PrimaryVolumeDescriptor.DataPreparerIdentifier = adv.DataPreparerIdentifier;
                _builder.PrimaryVolumeDescriptor.ApplicationIdentifier = adv.ApplicationIdentifier;
                _builder.TruncateData = adv.TruncateMode;
            }
        }
    }

    public class CddaItem
    {
        public string FilePath { get; set; }
        public override string ToString()
        {
            return System.IO.Path.GetFileName(FilePath);
        }
    }
}
