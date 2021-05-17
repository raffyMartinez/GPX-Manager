using GPXManager.entities;
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
using Xceed.Wpf.Toolkit.PropertyGrid;

namespace GPXManager.views
{
    /// <summary>
    /// Interaction logic for CTXFileDetailsWindow.xaml
    /// </summary>
    public partial class CTXFileDetailsWindow : Window
    {
        private static CTXFileDetailsWindow _instance;

        public static CTXFileDetailsWindow GetInstance()
        {
            if (_instance == null) _instance = new CTXFileDetailsWindow();
            return _instance;
        }
        public CTXFileDetailsWindow()
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
        public void ShowDetails()
        {
            if (CTXFIle != null)
            {
                PropertyGrid.SelectedObject = CTXFIle;
                //txtXML.Text = PrettyXML.PrettyPrint(CTXFIle.XML);
                txtXML.Text = CTXFIle.XML;
            }
        }
        public CTXFileSummaryView CTXFIle { get; set; }
        public static CTXFileDetailsWindow Instance
        {
            get
            {
                return _instance;
            }
        }
        private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _instance = null;
            this.SavePlacement();
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            PropertyGrid.AutoGenerateProperties = false;
            PropertyGrid.PropertyDefinitions.Add(new PropertyDefinition { Name = "User", DisplayName = "User name", DisplayOrder = 1, Description = "User name" });
            PropertyGrid.PropertyDefinitions.Add(new PropertyDefinition { Name = "LandingSite", DisplayName = "Landing site", DisplayOrder = 2, Description = "Landing site" });
            PropertyGrid.PropertyDefinitions.Add(new PropertyDefinition { Name = "Gear", DisplayName = "Gear", DisplayOrder = 3, Description = "Gear" });
            PropertyGrid.PropertyDefinitions.Add(new PropertyDefinition { Name = "DateStart", DisplayName = "Date start", DisplayOrder = 4, Description = "Date start of operation" });
            PropertyGrid.PropertyDefinitions.Add(new PropertyDefinition { Name = "DateEnd", DisplayName = "Date end", DisplayOrder = 5, Description = "Date end of operation" });
            PropertyGrid.PropertyDefinitions.Add(new PropertyDefinition { Name = "Duration", DisplayName = "Duration  (Hours:Minutes:Seconds)", DisplayOrder = 6, Description = "Number of waypoints for setting of gear" });
            PropertyGrid.PropertyDefinitions.Add(new PropertyDefinition { Name = "WaypointsForSet", DisplayName = "# of set waypoints", DisplayOrder = 7, Description = "Number of waypoints for setting of gear" });
            PropertyGrid.PropertyDefinitions.Add(new PropertyDefinition { Name = "WaypointsForHaul", DisplayName = "# of haul waypoints", DisplayOrder = 8, Description = "Number of waypoints for hauling the gear" });
            PropertyGrid.PropertyDefinitions.Add(new PropertyDefinition { Name = "TrackpointsCount", DisplayName = "# of track waypoints", DisplayOrder = 9, Description = "Number of waypoints in the track" });
            PropertyGrid.PropertyDefinitions.Add(new PropertyDefinition { Name = "CTXFileName", DisplayName = "File nane", DisplayOrder = 10, Description = "File name of the CTX file uploaded by the device to the server" });
            PropertyGrid.PropertyDefinitions.Add(new PropertyDefinition { Name = "DeviceID", DisplayName = "Device ID", DisplayOrder = 11, Description = "Device ID" });
            PropertyGrid.PropertyDefinitions.Add(new PropertyDefinition { Name = "Version", DisplayName = "Version", DisplayOrder = 12, Description = "Version of the BSC Tracker App" });
            ShowDetails();
        }
        
        private void OnButtonClicked(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
