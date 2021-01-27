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
    public partial class SettingsWindow : Window,IDisposable
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
            if (Global.Settings != null)
            {
                textBoxBackendPath.Text = Global.Settings.MDBPath;
                textBoxGPXFolder.Text = Global.Settings.ComputerGPXFolder;
                textBoxGPXFolderDevice.Text = Global.Settings.DeviceGPXFolder;
                textBoxHoursOffsetGMT.Text = Global.Settings.HoursOffsetGMT.ToString();
                textBoxBingAPIKey.Text = Global.Settings.BingAPIKey;
                textLatestTripCount.Text = Global.Settings.LatestTripCount.ToString();
                textLatestGPXFileCount.Text = Global.Settings.LatestGPXFileCount.ToString();
                
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
            if(textBoxBackendPath.Text.Length>0 && textBoxGPXFolder.Text.Length>0 && textBoxGPXFolderDevice.Text.Length>0)
            {
                return int.TryParse(textBoxHoursOffsetGMT.Text, out int v);
            }
            return false;
        }
        public MainWindow ParentWindow { get; set; }
        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            switch(((Button)sender).Name)
            {
                case "buttonOk":
                    DialogResult =  Validate() && Global.SetSettings(
                        textBoxGPXFolder.Text, 
                        textBoxGPXFolderDevice.Text, 
                        textBoxBackendPath.Text, 
                        int.Parse(textBoxHoursOffsetGMT.Text),
                        textBoxBingAPIKey.Text,
                        int.Parse(textLatestTripCount.Text),
                        int.Parse(textLatestGPXFileCount.Text)
                        );
                    if(!Validate())
                    {
                        MessageBox.Show("All fields should be answered with the expected values", "Validation error",MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    break;
                case "buttonCancel":
                    DialogResult = false;
                    break;
                case "buttonLocate":
                    VistaFolderBrowserDialog fbd = new VistaFolderBrowserDialog();
                    fbd.UseDescriptionForTitle = true;
                    fbd.Description = "Locate GPX folder in computer";
                    if ((bool)fbd.ShowDialog() && fbd.SelectedPath.Length > 0)
                    {
                        textBoxGPXFolder.Text = fbd.SelectedPath;
                    }
                    break;
                case "buttonLocateBackend":
                    OpenFileDialog ofd = new OpenFileDialog();
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
    }
}
