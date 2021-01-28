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
    /// Interaction logic for ShowAllGPSDataInArchiveWindow.xaml
    /// </summary>
    public partial class ShowAllGPSDataInArchiveWindow : Window
    {
        public ShowAllGPSDataInArchiveWindow()
        {
            InitializeComponent();
            Loaded += OnWindowLoaded;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            int counter = 0;
            //foreach (var gps in Entities.GPSViewModel.GPSCollection.OrderBy(t => t.DeviceName))
            foreach(var gps in Entities.GPSViewModel.GetAll())
            {
                if (counter == 0)
                {
                    var chk = panelGPS.Children[0] as CheckBox;
                    chk.Content = gps.DeviceName;
                    chk.Tag = gps.DeviceID;
                }
                else
                {
                    CheckBox c = new CheckBox { Content = gps.DeviceName, Tag = gps.DeviceID, Margin = new Thickness(10, 5, 10, 0) };
                    panelGPS.Children.Add(c);
                }
                counter++;
            }
            counter = 0;
            foreach(var s in Entities.DeviceGPXViewModel.GetAllMonthYear())
            {
                if (counter == 0)
                {
                    var chk = panelMonths.Children[0] as CheckBox;
                    chk.Content = s.ToString("MMM-yyyy");
                }
                else
                {
                    CheckBox c = new CheckBox { Content = s.ToString("MMM-yyyy"), Margin = new Thickness(10, 5, 10, 0) };
                    panelMonths.Children.Add(c);
                }
                counter++;
            }
        }

        public MapWindowForm ParentForm { get; set; }

        private void ProcessChecked()
        {

            List<GPS> selectedGPS = new List<GPS>();
            List<DateTime> selectedMonth = new List<DateTime>();
            foreach (CheckBox c in panelGPS.Children)
            {
                if ((bool)c.IsChecked)
                {

                    //selectedGPS.Add()
                }
            }


            foreach (CheckBox c in panelMonths.Children)
            {
                if ((bool)c.IsChecked)
                {
                    
                }
            }

            if(selectedGPS.Count>0 && selectedMonth.Count>0)
            {

            }
            else
            {
                MessageBox.Show("At least one GPS and one date should be selected", "GPX Manager", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            switch(((Button)sender).Name)
            {
                case "buttonCancel":
                    Close();
                    break;
                case "buttonOk":
                    ProcessChecked();
                    break;
            }
        }

        private void OnCheckChecked(object sender, RoutedEventArgs e)
        {
            CheckBox chk = (CheckBox)sender;
            switch(chk.Name)
            {
                case "chkSelectAllGPS":
                    foreach(CheckBox c in panelGPS.Children)
                    {
                        c.IsChecked = chk.IsChecked;
                    }
                    break;
                case "chkSelectAllDate":
                    foreach (CheckBox c in panelMonths.Children)
                    {
                        c.IsChecked = chk.IsChecked;
                    }
                    break;
            }
        }
    }
}
