using System;
using System.Collections.Generic;
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
using GPXManager.entities;
using System.IO;
namespace GPXManager.views
{
    /// <summary>
    /// Interaction logic for GPSIDWindow.xaml
    /// </summary>
    public partial class GPSIDWindow : Window
    {
        public GPSIDWindow()
        {
            InitializeComponent();
        }
        public DetectedDevice DetectedDevice { get; set; }
        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            switch(((Button)sender).Name)
            {
                case "buttonCancel":
                    DialogResult = false;
                    break;
                case "buttonOk":
                    string msg="";
                    DetectedDevice device = null;
                    if(txtGPSID.Text.Length>0 )
                    {
                        if (txtGPSID.Text.Contains("gpsid"))
                        {
                            msg = "Inputted text cannot contain 'gpsid'";
                        }
                        else
                        {
                            var gpsid = txtGPSID.Text + ".gpsid";
                            device = Entities.DetectedDeviceViewModel.GetDevice(gpsid);
                            if (device == null)
                            {
                                File.Create($@"{DetectedDevice.Disks[0].Caption}\{gpsid}").Dispose();
                                DetectedDevice.GPSID = System.IO.Path.GetFileNameWithoutExtension(gpsid);
                            }
                            else
                            {
                                msg = "GPSID already used. Try again";
                                
                            }
                        }
                        
                    }
                    else
                    {
                        msg = "GPSID cannot be empty";
                    }

                    if (msg.Length > 0)
                    {
                        MessageBox.Show(msg, "GPX Manager", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        DialogResult = true;
                    }
                    
                    break;
            }
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            txtGPSID.Focus();
        }
    }
}
