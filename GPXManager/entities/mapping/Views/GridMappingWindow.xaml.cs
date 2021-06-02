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

namespace GPXManager.entities.mapping.Views
{
    /// <summary>
    /// Interaction logic for GridMappingWindow.xaml
    /// </summary>
    public partial class GridMappingWindow : Window
    {
        private static GridMappingWindow _instance;
        public GridMappingWindow()
        {
            InitializeComponent();
            Loaded += OnWindowLoaded;
            Closing += OnWindowClosing;
        }
        public static GridMappingWindow GetInstance()
        {
            if (_instance == null) _instance = new GridMappingWindow();
            return _instance;
        }
        private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _instance = null;

        }


        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            if (AOI != null)
            {
                labelTitle.Content = $"Grid mapping of BSC fishery in {AOI.Name}";
                MapWindowManager.SelectTracksInAOI(AOI);
            }
        }

        public AOI AOI { get; set; }
        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            Button b = (Button)sender;
            if (b.Name == "buttonMapCancel")
            {
                Close();
            }
            else if (MapWindowManager.SelectedTrackIndexes.Count() > 0)
            {
                gridding.GridMapping.AOI = AOI;
                gridding.GridMapping.SelectedTrackIndexes = MapWindowManager.SelectedTrackIndexes;
                gridding.GridMapping.SelectedTracks = MapWindowManager.SelectedTracks;
                switch (b.Name)
                {
                    case "buttonMapUndersized":
                        break;
                    case "buttonMapBerried":
                        break;
                    case "buttonMapEffort":
                        var count = gridding.GridMapping.ComputeFishingFrequency();

                        MessageBox.Show($"{count} cells were computed for frequency");
                        break;
                    case "buttonMapCPUE":
                        break;
                }
            }

        }
    }
}
