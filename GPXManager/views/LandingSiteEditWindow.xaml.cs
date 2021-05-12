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

namespace GPXManager.views
{
    /// <summary>
    /// Interaction logic for LandingSiteEditWindow.xaml
    /// </summary>
    public partial class LandingSiteEditWindow : Window
    {
        private static LandingSiteEditWindow _instance;
        private bool _isNew;
        private LandingSite _landingSite;
        public static LandingSiteEditWindow GetInstance()
        {
            if (_instance == null)
            {
                _instance = new LandingSiteEditWindow();
            }
            return _instance;
        }

        public bool IsNew
        {
            get { return _isNew; }
            set
            {
                _isNew = value;
                if (_isNew)
                {
                    Title = "Add a landing site";
                    textID.Text = Entities.LandingSiteViewModel.NextRecordNumber().ToString();
                }
                else
                {
                    Title = "Edit landing site";
                }
            }
        }
        public LandingSiteEditWindow()
        {
            InitializeComponent();
            Loaded += OnWindowLoaded;
            Closing += OnWindowClosing;
        }

        private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {

            _instance = null;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {

        }

        public LandingSite LandingSite
        {
            get { return _landingSite; }
            set
            {
                _landingSite = value;
                _isNew = false;
                textID.Text = _landingSite.ID.ToString();
                textName.Text = _landingSite.Name;
                textMunicipality.Text = _landingSite.Municipality;
                textProvince.Text = _landingSite.Province;
                if (_landingSite.Lat != null && _landingSite.Province != null)
                {
                    textLatitude.Text = _landingSite.Lat.ToString();
                    textLongitude.Text = _landingSite.Lon.ToString();
                }
            }
        }

        private void OnButtonClicked(object sender, RoutedEventArgs e)
        {
            switch (((Button)sender).Name)
            {
                case "buttonOk":
                    if (textID.Text.Length > 0 &&
                        textName.Text.Length > 0 &&
                        textMunicipality.Text.Length > 0 &&
                        textProvince.Text.Length > 0)
                    {
                        LandingSite ls = new LandingSite
                        {
                            ID = int.Parse(textID.Text),
                            Name = textName.Text,
                            Municipality = textMunicipality.Text,
                            Province = textProvince.Text
                        };
                        if (textLatitude.Text.Length > 0 && textLongitude.Text.Length > 0)
                        {
                            if (double.TryParse(textLatitude.Text, out double lat))
                            {
                                ls.Lat = lat;
                                if (double.TryParse(textLongitude.Text, out double lon))
                                {
                                    ls.Lon = lon;
                                }
                                else
                                {
                                    ls.Lon = null;
                                    ls.Lat = null;
                                }
                            }
                            else
                            {
                                ls.Lon = null;
                                ls.Lat = null;
                            }
                        }

                        bool success = false;
                        if (_isNew)
                        {
                            if (Entities.LandingSiteViewModel.AddRecordToRepo(ls))
                            {
                                textName.Clear();
                                textMunicipality.Clear();
                                textProvince.Clear();
                                textLatitude.Clear();
                                textLongitude.Clear();
                                textID.Text = Entities.LandingSiteViewModel.NextRecordNumber().ToString();
                                success = true;
                            }
                        }
                        else
                        {
                            Entities.LandingSiteViewModel.UpdateRecordInRepo(ls);
                            success = true;
                            Close();
                        }
                        if(success)
                        {
                            ((MainWindow)Owner).RefreshLandingSiteGrid();
                        }
                    }
                    
                    break;

                case "buttonCancel":
                    Close();
                    break;
            }
        }
    }
}
