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

namespace GPXManager.views
{
    /// <summary>
    /// Interaction logic for ImportGPXByLGUFolderWindow.xaml
    /// </summary>
    public partial class ImportGPXByLGUFolderWindow : Window
    {
        private bool _proceed;
        public ImportGPXByLGUFolderWindow()
        {
            InitializeComponent();
        }

        public MainWindow ParentForm { get; set; }
        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            switch(((Button)sender).Name)
            {
                case "buttonOK":
                    _proceed = false;                    
                    if(txtEndNumber.Text.Length>0 && 
                        txtStartNumber.Text.Length>0 &&
                        txtNamePart.Text.Length>0)
                    {
                        var endNumber = txtEndNumber.Text;
                        var startNumber = txtStartNumber.Text;
                        var namePart= txtNamePart.Text;
                        if(int.TryParse(endNumber,out int val))
                        {
                            ImportGPSData.EndGPSNumbering = val;
                            if(int.TryParse(startNumber,out  val))
                            {
                                ImportGPSData.StartGPSNumbering = val;
                                ImportGPSData.GPSNameStart = namePart;
                                ParentForm.ImportGPX();
                                Close();
                                   
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
                    if(!_proceed)
                    {
                        MessageBox.Show("Please fill up all fields correctly", "GPX Manager", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        Close();
                    }
                    break;
                case "buttonCancel":
                    Close();
                    break;
            }
        }
    }
}
