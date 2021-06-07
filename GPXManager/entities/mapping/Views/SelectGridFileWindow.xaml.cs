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
        public SelectGridFileWindow()
        {
            InitializeComponent();
            Loaded += OnWindowLoaded;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            panelButtons.Children.Clear();
            int counter = 0;
            foreach (var item in GridFiles)
            {
                RadioButton rb = new RadioButton { Content = Path.GetFileName(item),Tag = item };
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

        private void OnRadioButtonChecked(object sender, RoutedEventArgs e)
        {
            _selectedFile = ((RadioButton)sender).Tag.ToString();
        }

        public string SelectedFile { get; private set; }

        public List<string> GridFiles { get; set; }
        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            switch(((Button)sender).Content)
            {
                case "Ok":
                    DialogResult = true;
                    SelectedFile = _selectedFile;
                    Close();
                    break;
                case "Cancel":
                    DialogResult = false;
                    break;
            }
        }
    }
}
