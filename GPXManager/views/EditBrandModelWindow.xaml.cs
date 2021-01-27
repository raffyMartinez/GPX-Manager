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

namespace GPXManager.views
{
    /// <summary>
    /// Interaction logic for EditBrandModelWindow.xaml
    /// </summary>
    public partial class EditBrandModelWindow : Window,IDisposable
    {
        public EditBrandModelWindow()
        {
            InitializeComponent();
            Loaded += OnWindowLoaded;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            switch(ShowMode)
            {
                case ShowMode.ShowModeBrand:
                    labelTitle.Content = "Add a new brand";
                    break;
                case ShowMode.ShowModeModel:
                    labelTitle.Content = "Add a new model";
                    break;
            }
        }

        public void Dispose()
        {

        }
        public Window ParentWindow { get; set; }
        public ShowMode ShowMode { get; set; }
        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            switch(((Button)sender).Name)
            {
                case "buttonOK":
                    if (textBox.Text.Length > 0)
                    {
                       ((GPSBrandModelWindow)ParentWindow).NewBrandModel = textBox.Text;                        
                        DialogResult = true;
                        Close();
                    }
                    break;
                case "buttonCacncel":
                    DialogResult = false;
                    Close();
                    break;
            }
        }
    }
}
