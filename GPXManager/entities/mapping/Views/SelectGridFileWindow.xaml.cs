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
using System.IO;

namespace GPXManager.entities.mapping.Views
{
    /// <summary>
    /// Interaction logic for SelectGridFileWindow.xaml
    /// </summary>
    public partial class SelectGridFileWindow : Window
    {
        private string _selectedFile;
        private int? _meters;
        public SelectGridFileWindow()
        {
            InitializeComponent();
            Loaded += OnWindowLoaded;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            _selectedFile = "";
            panelButtons.Children.Clear();
            int counter = 0;

            if (GridFiles != null && GridFiles.Count > 0)
            {
                foreach (var item in GridFiles)
                {
                    RadioButton rb = new RadioButton { Content = Path.GetFileName(item), Tag = item };
                    if (counter == 0)
                    {
                        rb.Margin = new Thickness(10, 5, 0, 5);
                    }
                    else
                    {
                        rb.Margin = new Thickness(10, 0, 0, 5);
                    }
                    rb.Checked += OnRadioButtonChecked;
                    panelButtons.Children.Add(rb);
                    counter++;
                }
            }
            else if (CommonGridSizes != null && CommonGridSizes.Count > 0)
            {
                labelTitle.Content = "Select grid size for mapping";
                Title = labelTitle.Content.ToString();
                foreach (var grid_size in CommonGridSizes)
                {
                    var arr = grid_size.Split('.');
                    int meters = int.Parse(grid_size.Split('.')[0]);
                    RadioButton rb = new RadioButton { Content = $"{meters} meters", Tag = meters};
                    if (counter == 0)
                    {
                        rb.Margin = new Thickness(10, 5, 0, 5);
                    }
                    else
                    {
                        rb.Margin = new Thickness(10, 0, 0, 5);
                    }
                    rb.Checked += OnRadioButtonChecked;
                    panelButtons.Children.Add(rb);
                    counter++;
                }
            }
        }

        private void OnRadioButtonChecked(object sender, RoutedEventArgs e)
        {
            if (GridFiles != null)
            {
                _selectedFile = ((RadioButton)sender).Tag.ToString();
            }
            else
            {
                _meters = int.Parse(((RadioButton)sender).Tag.ToString());
            }
        }

        public string SelectedFile { get; private set; }

        public int? SelectedGridSize { get; private set; }
        public List<string> CommonGridSizes { get; set; }
        public List<string> GridFiles { get; set; }
        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            switch(((Button)sender).Content)
            {
                case "Ok":
                    DialogResult = true;
                    SelectedFile = _selectedFile;
                    SelectedGridSize = _meters;
                    Close();
                    break;
                case "Cancel":
                    DialogResult = false;
                    break;
            }
        }
    }
}
