using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Ookii.Dialogs.Wpf;
using GPXManager;
using GPXManager.entities;
using Microsoft.Win32;
using System.ComponentModel;

namespace GPXManager.views
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window, IDisposable
    {
        public SettingsWindow()
        {
            InitializeComponent();
            Loaded += OnWindowLoaded;
            Closing += OnWindowClosing;
        }
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            this.ApplyPlacement();
        }
        private void OnWindowClosing(object sender, CancelEventArgs e)
        {
            this.SavePlacement();
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {

            radioButtonGPX.IsChecked = true;
            if (Global.Settings != null)
            {
                textBoxBackendPath.Text = Global.Settings.MDBPath;
                textBoxGPXFolder.Text = Global.Settings.ComputerGPXFolder;
                textBoxGPXFolderDevice.Text = Global.Settings.DeviceGPXFolder;
                textBoxHoursOffsetGMT.Text = Global.Settings.HoursOffsetGMT.ToString();
                textBoxBingAPIKey.Text = Global.Settings.BingAPIKey;
                textLatestTripCount.Text = Global.Settings.LatestTripCount.ToString();
                textLatestGPXFileCount.Text = Global.Settings.LatestGPXFileCount.ToString();
                textLogImageFolder.Text = Global.Settings.LogImagesFolder.ToString();
                textBoxCTXBackupPath.Text = Global.Settings.CTXBackupFolder;
                textBoxCTXDownloadFolder.Text = Global.Settings.CTXDownloadFolder;
                textSpeedThreshold.Text = Global.Settings.SpeedThresholdForRetrieving.ToString();
                textGearRetrievalMainLength.Text = Global.Settings.GearRetrievingMinLength.ToString();
                if (Global.Settings.PathToCybertrackerExe != null)
                {
                    textBoxCybertrackerPath.Text = Global.Settings.PathToCybertrackerExe;
                }
                if(Global.Settings.GridSize!=null)
                {
                    textBoxSizeOfGrid.Text = ((int)Global.Settings.GridSize).ToString();
                }
                else
                {
                    textBoxSizeOfGrid.Text = "400";
                }
                if(Global.Settings.SaveFolderForGrids!=null)
                {
                    textBoxGridSaveFolder.Text = Global.Settings.SaveFolderForGrids;
                }

            }
            else
            {
                textBoxHoursOffsetGMT.Text = "8";
                textLatestTripCount.Text = "5";
                textLatestGPXFileCount.Text = "5";
            }
        }

        public void Dispose()
        {

        }
        public bool Validate()
        {
            if (
                textBoxBackendPath.Text.Length > 0 &&
                textBoxGPXFolder.Text.Length > 0 &&
                textBoxGPXFolderDevice.Text.Length > 0 &&
                textLogImageFolder.Text.Length > 0 &&
                textBoxCTXBackupPath.Text.Length > 0 &&
                textBoxCTXDownloadFolder.Text.Length > 0 &&
                textSpeedThreshold.Text.Length > 0 &&
                textGearRetrievalMainLength.Text.Length > 0 &&
                textBoxCybertrackerPath.Text.Length > 0 &&
                textBoxSizeOfGrid.Text.Length>0)
            {
                return int.TryParse(textBoxHoursOffsetGMT.Text, out int v);
            }
            return false;
        }
        public MainWindow ParentWindow { get; set; }
        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            switch (((Button)sender).Name)
            {
                case "buttonOk":
                    if (Validate())
                    {
                        Global.SetSettings(
                          textBoxGPXFolder.Text,
                          textBoxGPXFolderDevice.Text,
                          textBoxBackendPath.Text,
                          int.Parse(textBoxHoursOffsetGMT.Text),
                          textBoxBingAPIKey.Text,
                          int.Parse(textLatestTripCount.Text),
                          int.Parse(textLatestGPXFileCount.Text),
                          textLogImageFolder.Text,
                          textBoxCybertrackerPath.Text,
                          textBoxCTXBackupPath.Text,
                          textBoxCTXDownloadFolder.Text,
                          int.Parse(textSpeedThreshold.Text),
                          int.Parse(textGearRetrievalMainLength.Text),
                          int.Parse(textBoxSizeOfGrid.Text),
                          textBoxGridSaveFolder.Text
                          );

                        DialogResult = true;
                    }
                    else
                    {
                        MessageBox.Show("Required fields* should be answered with the expected values", "Validation error", MessageBoxButton.OK, MessageBoxImage.Information);
                    }


                    break;
                case "buttonLocateCTXDownloadFolder":
                    VistaFolderBrowserDialog fbd = new VistaFolderBrowserDialog();
                    fbd.UseDescriptionForTitle = true;
                    fbd.Description = "Locate downloaad folder for CTX files";
                    if ((bool)fbd.ShowDialog() && fbd.SelectedPath.Length > 0)
                    {
                        textBoxCTXDownloadFolder.Text = fbd.SelectedPath;
                    }
                    break;
                case "buttonLocateCTXBackup":
                    fbd = new VistaFolderBrowserDialog();
                    fbd.UseDescriptionForTitle = true;
                    fbd.Description = "Locate backup folder for CTX files";
                    if ((bool)fbd.ShowDialog() && fbd.SelectedPath.Length > 0)
                    {
                        textBoxCTXBackupPath.Text = fbd.SelectedPath;
                    }
                    break;
                case "buttonCancel":
                    DialogResult = false;
                    break;
                case "buttonLocateCybertracker":
                    OpenFileDialog ofd = new OpenFileDialog();
                    ofd.Title = "Locate Cybertracker executable file (ct3.exe)";
                    ofd.Filter = "ct3 exe file(*.exe)|*.exe|All file types (*.*)|*.*";
                    ofd.FilterIndex = 1;
                    ofd.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    if ((bool)ofd.ShowDialog() && File.Exists(ofd.FileName))
                    {
                        textBoxCybertrackerPath.Text = System.IO.Path.GetDirectoryName(ofd.FileName);
                    }
                    break;
                case "buttonLocate":
                    fbd = new VistaFolderBrowserDialog();
                    fbd.UseDescriptionForTitle = true;
                    fbd.Description = "Locate GPX folder in computer";
                    if ((bool)fbd.ShowDialog() && fbd.SelectedPath.Length > 0)
                    {
                        textBoxGPXFolder.Text = fbd.SelectedPath;
                    }
                    break;
                case "buttonLocateImageLog":
                    fbd = new VistaFolderBrowserDialog();
                    fbd.UseDescriptionForTitle = true;
                    fbd.Description = "Locate folder of logbook images in computer";
                    if ((bool)fbd.ShowDialog() && fbd.SelectedPath.Length > 0)
                    {
                        textLogImageFolder.Text = fbd.SelectedPath;
                    }
                    break;
                case "buttonLocateBackend":
                    ofd = new OpenFileDialog();
                    ofd.Title = "Locate backend database for GPS data";
                    ofd.Filter = "MDB file(*.mdb)|*.mdb|All file types (*.*)|*.*";
                    ofd.FilterIndex = 1;
                    ofd.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    if ((bool)ofd.ShowDialog() && File.Exists(ofd.FileName))
                    {
                        textBoxBackendPath.Text = ofd.FileName;
                    }
                    break;
            }
        }

        private void OnRadioChecked(object sender, RoutedEventArgs e)
        {
            panelGPX.Visibility = Visibility.Collapsed;
            panelCTX.Visibility = Visibility.Collapsed;
            panelBackend.Visibility = Visibility.Collapsed;
            panelBing.Visibility = Visibility.Collapsed;
            panelGridMaps.Visibility = Visibility.Collapsed;
            panelOtherSettings.Visibility = Visibility.Collapsed;
            switch(((RadioButton)sender).Name)
            {
                case "radioButtonGPX":
                    panelGPX.Visibility = Visibility.Visible;
                    break;
                case "radioButtonCTX":
                    panelCTX.Visibility=Visibility.Visible;
                    break;
                case "radioButtonDBBackend":
                    panelBackend.Visibility = Visibility.Visible;
                    break;
                case "radioButtonGridMaps":
                    panelGridMaps.Visibility = Visibility.Visible;
                    break;
                case "radioButtonBingMaps":
                    panelBing.Visibility = Visibility.Visible;
                    break;
                case "radioButtonOthers":
                    panelOtherSettings.Visibility = Visibility.Visible;
                    break;
            }
        }
    }
}
