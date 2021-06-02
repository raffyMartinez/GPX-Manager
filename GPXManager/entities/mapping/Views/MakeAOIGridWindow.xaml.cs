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
using Microsoft.Win32;
using System.IO;


namespace GPXManager.entities.mapping.Views
{
    /// <summary>
    /// Interaction logic for MakeAOIGridWindow.xaml
    /// </summary>
    public partial class MakeAOIGridWindow : Window
    {
        private static MakeAOIGridWindow _instance;
        public MakeAOIGridWindow()
        {
            InitializeComponent();
            Loaded += OnWindowLoaded;
            Closing += OnWindowClosing;

        }

        private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _instance = null;
        }

        public AOI AOI { get; set; }
        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            if (Global.Settings.GridSize != null)
            {
                textBoxGridSize.Text = Global.Settings.GridSize.ToString();
            }
            else
            {
                textBoxGridSize.Text = "400";
            }
            labelTitle.Content = $"Generate grid for {AOI.Name}";
        }

        public static MakeAOIGridWindow GetInstance()
        {
            if (_instance == null) _instance = new MakeAOIGridWindow();
            return _instance;
        }
        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            switch (((Button)sender).Name)
            {
                case "buttonOk":
                    SaveFileDialog sfd = null;
                    bool proceed = true;
                    AOI.GridSizeMeters = int.Parse(textBoxGridSize.Text);
                    if ((bool)checkSaveGrid.IsChecked)
                    {
                        proceed = false;
                        sfd = new SaveFileDialog();
                        sfd.DefaultExt = "*.shp";
                        sfd.Filter = "Shapefiles (*.shp)|*.shp|All files (*.*)|*.*";
                        sfd.FileName = $"{AOI.GridLayerName}";
                        if (Global.Settings.SaveFolderForGrids != null)
                        {
                            sfd.InitialDirectory = Global.Settings.SaveFolderForGrids;
                        }
                        else
                        {
                            sfd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                        }
                        if ((bool)sfd.ShowDialog() && sfd.FileName.Length > 0 && Directory.Exists(System.IO.Path.GetDirectoryName(sfd.FileName)))
                        {
                            proceed = true;

                        }
                    }

                    if (proceed)
                    {
                        gridding.Grid25.UTMZone = gridding.UTMZone.UTMZone51N;
                        if (MapWindowManager.Grid25MajorGrid == null)
                        {
                            MapWindowManager.Grid25MajorGrid = gridding.Grid25.CreateGrid25MajorGrid();

                        }

                        var grids = AOI.MajorGridIntersect();
                        AOI.GenerateMinorGrids();
                        if (!AOI.GeneratedSubGrids())
                        {
                            MessageBox.Show("Subgrid size does not fit grid", "GPX Manager", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            int h = MapWindowManager.MapLayersHandler.AddLayer(AOI.SubGrids, $"{AOI.GridLayerName}");
                            if (h >= 0)
                            {
                                var sf = (MapWinGIS.Shapefile)MapWindowManager.MapLayersHandler[h].LayerObject;
                                sf.Key = $"subgrid_{AOI.Name}";
                                sf.DefaultDrawingOptions.FillVisible = false;
                                sf.DefaultDrawingOptions.LineColor = new MapWinGIS.Utils().ColorByName(MapWinGIS.tkMapColor.DarkGray);

                                if (sfd != null)
                                {
                                    if (Global.Settings.SaveFolderForGrids == null || Global.Settings.SaveFolderForGrids.Length == 0)
                                    {
                                        Global.Settings.SaveFolderForGrids = System.IO.Path.GetDirectoryName(sfd.FileName);
                                        Global.SaveGlobalSettings();
                                    }

                                    Callback cb = new Callback();
                                    if (sf.SaveAs(sfd.FileName, cb))
                                    {
                                        AOI.GridFileName = sfd.FileName;
                                        Entities.AOIViewModel.UpdateRecordInRepo(AOI);
                                    }
                                    else
                                    {
                                        MessageBox.Show(cb.ErrorMessage, "GPX Manager", MessageBoxButton.OK, MessageBoxImage.Information);
                                    }
                                }
                                ((AOIWindow)Owner).GridIsLoaded();
                                ((Window)Owner).Focus();
                                Close();
                            }
                        }
                    }
                    break;
                case "buttonCancel":
                    Close();
                    break;
                case "buttonGridSaveLocation":
                    break;
            }
        }
    }
}
