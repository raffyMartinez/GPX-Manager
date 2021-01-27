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
    /// Interaction logic for SelectTileProviderWindow.xaml
    /// </summary>
    public partial class SelectTileProviderWindow : Window
    {
        public SelectTileProviderWindow()
        {
            InitializeComponent();
            Loaded += OnWindowLoaded;
        }

        public int TileProviderID { get; set; }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            
            listBoxTileProviders.DisplayMemberPath = "Value";
            listBoxTileProviders.DataContext = MapWindowManager.TileProviders;
        }

        private void OnButtonClicked(object sender, RoutedEventArgs e)
        {
            switch(((Button)sender).Name)
            {
                case "buttonCancel":
                    Close();
                    break;
                case "buttonOk":
                    DialogResult = true;
                    Close();
                    break;
            }
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TileProviderID = ((KeyValuePair<int, string>)listBoxTileProviders.SelectedItem).Key;
        }
    }
}
