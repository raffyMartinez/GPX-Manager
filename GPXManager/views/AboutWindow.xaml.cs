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

using System.Deployment.Application;
using System.Diagnostics;
using GPXManager.entities;
using System.Reflection;

namespace GPXManager.views
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            Loaded += AboutWindow_Loaded;
        }

        private void AboutWindow_Loaded(object sender, RoutedEventArgs e)
        {

            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var buildDate = new DateTime(2000, 1, 1).AddDays(version.Build).AddSeconds(version.Revision * 2);
            var displyableVersion = $"Version: {version} ({buildDate.ToString("MMM-dd-yyyy H:mm:ss")})";

            labelAppVerion.Content = displyableVersion;

            if (Global.MapOCXInstalled)
            {
                FileVersionInfo ocxVersionInfo = FileVersionInfo.GetVersionInfo(Global.MapOCXPath);
                labelMapOCXVersion.Content = $"MapWinGIS OCX version: {ocxVersionInfo.ProductVersion}";
            }
            else
            {
                labelMapOCXVersion.Content = "MapWinGIS mapping component not installed";
            }
        }

        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
