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

    public enum ShowMode
    {
        ShowModeBrand,
        ShowModeModel,
        ShowModeDriveInfo
    }
    /// <summary>
    /// Interaction logic for GPSBrandModelWindow.xaml
    /// </summary>
    public partial class GPSBrandModelWindow : Window,IDisposable
    {
        public GPSBrandModelWindow()
        {
            InitializeComponent();
        }
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            this.ApplyPlacement();
        }
        public void Dispose()
        {

        }
        private void onButtonClicked(object sender, RoutedEventArgs e)
        {
            switch(((Button)sender).Name)
            {
                case "buttonOK":
                    if (textBlock.Text.Length > 0)
                    {
                        switch (ShowMode)
                        {
                            case ShowMode.ShowModeBrand:
                                foreach (var item in textBlock.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.None))
                                {
                                    Entities.GPSViewModel.AddBrand(item);
                                }
                                break;
                            case ShowMode.ShowModeModel:
                                foreach (var item in textBlock.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.None))
                                {
                                    Entities.GPSViewModel.GPSModels.Add(item);
                                }
                                break;
                        }
                        DialogResult = true;
                        Close();
                    }
                    break;
                case "buttonCancel":
                    DialogResult = false;
                    Close();
                    break;
            }
        }
        public string NewBrandModel { get; set; }
        public ShowMode ShowMode { get; set; }
        private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.SavePlacement();
        }
        public string Brand { get; set; }
        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
           
            switch(ShowMode)
            {
                case ShowMode.ShowModeBrand:
                    labelTitle.Content = "GPS brands";
                    Title = "GPS Brands";
                    if (Entities.GPSViewModel.GPSBrands!=null)
                    {
                        foreach (var item in Entities.GPSViewModel.GPSBrands)
                        {
                            textBlock.Text += $"{item}\r\n";
                        }
                    }
                    
                    break;
                case ShowMode.ShowModeModel:
                    labelTitle.Content = $"GPS models for {Brand}";
                    Title = "GPS Models";
                    if (Entities.GPSViewModel.GPSModels != null)
                    {
                        foreach (var item in Entities.GPSViewModel.GetModels(Brand))
                        {
                            textBlock.Text += $"{item}\r\n";
                        }
                    }
                    break;
            }
            textBlock.Focus();
        }
    }
}
