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
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using GPXManager.entities;


namespace GPXManager.views
{
    /// <summary>
    /// Interaction logic for ImportGPXByLGUFolderWindow.xaml
    /// </summary>
    public partial class ImportGPXByFolderWindow : Window
    {

        private bool _proceed;
        public ImportGPXByFolderWindow()
        {
            InitializeComponent();
        }



        public MainWindow ParentForm { get; set; }
        private async void OnButtonClick(object sender, RoutedEventArgs e)
        {
            switch (((Button)sender).Name)
            {
                case "buttonOK":
                    _proceed = false;
                    if (txtEndNumber.Text.Length > 0 &&
                        txtStartNumber.Text.Length > 0 &&
                        txtNamePart.Text.Length > 0)
                    {
                        var endNumber = txtEndNumber.Text;
                        var startNumber = txtStartNumber.Text;
                        var namePart = txtNamePart.Text;
                        if (int.TryParse(endNumber, out int val))
                        {
                            ImportGPSData.EndGPSNumbering = val;
                            if (int.TryParse(startNumber, out val))
                            {
                                ImportGPSData.StartGPSNumbering = val;
                                ImportGPSData.GPSNameStart = namePart;

                                ImportGPSData.ImportGPXEvent += OnImportGPX;


                                if (await ImportGPSData.ImportGPXAsync())
                                {
                                    _proceed = true;

                                }

                            }
                            else
                            {
                                _proceed = false;
                            }
                        }
                        else
                        {

                            _proceed = false;
                        }
                    }

                    progressBar.IsIndeterminate = false;

                    if (!_proceed)
                    {
                        MessageBox.Show("Please fill up all fields correctly", "GPX Manager", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show($"{ImportGPSData.ImportMessage}", "GPX Manager", MessageBoxButton.OK, MessageBoxImage.Information);
                        ParentForm.ShowArchive();
                        Close();
                    }
                    break;
                case "buttonCancel":
                    Close();
                    break;
            }
        }

        private void OnImportGPX(object sender, ImportGPXEventArg e)
        {
            switch (e.Intent)
            {
                case "start":
                    panelStatus.Visibility = Visibility.Visible;
                    progressBar.IsIndeterminate = true;
                    break;
                case "gpx saved":
                    statusLabel.Dispatcher.BeginInvoke
                    (
                        DispatcherPriority.Normal, new DispatcherOperationCallback(delegate
                        {
                            statusLabel.Content = $"Processed data from {e.GPS.DeviceName}\r\n" +
                                                  $"{e.ImportedCount} files saved to database";
                            //do what you need to do on UI Thread
                            return null;
                        }
                     ), null);

                    break;
            }
        }
    }
}
