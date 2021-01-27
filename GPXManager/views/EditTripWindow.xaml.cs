using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Policy;
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
//using WpfApp1;
using GPXManager.entities;
using Xceed.Wpf.Toolkit.PropertyGrid;

namespace GPXManager.views
{
    /// <summary>
    /// Interaction logic for EditTripWindow.xaml
    /// </summary>
    public partial class EditTripWindow : Window,IDisposable
    {
        private TripEdited _trip;
        private List<Waypoint> _waypoints;
        private string _prettyGPX;
        private string _trackXML;
        private PropertyItem _selectedProperty;
        private DateTime _oldDepartDate;
        DateTime _oldArriveDate;
        private bool _dateTimeDepartureArrivalChanged;
        private static EditTripWindow _instance;
        private DateTime? _defaultStart;
        private DateTime? _defaultEnd;
        
        public static EditTripWindow GetInstance()
        {
            if (_instance == null) _instance = new EditTripWindow();
            return _instance;
        }
        public EditTripWindow()
        {
            InitializeComponent();
            Loaded += OnWindowLoaded;
            Closing += OnWindowClosing;
        }

        public GPS GPS { get; set; }
        private void OnWindowClosing(object sender, CancelEventArgs e)
        {
            ParentWindow.NotifyEditWindowClosing();
            this.SavePlacement();
            _instance = null;
        }
        public void DefaultTripDates(DateTime start, DateTime end)
        {
            _defaultStart = start;
            _defaultEnd = end;
        }
        public void RefreshTrip(bool newTrip=false)
        {
            ShowTripDetails(newTrip);
        }
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            this.ApplyPlacement();
        }
        private void ShowTripDetails(bool newTrip=false)
        {
            if (newTrip)
            {
                TripID = Entities.TripViewModel.NextRecordNumber;
                SetNewTrip();
            }
            else
            {
                _trip = new TripEdited(Entities.TripViewModel.GetTrip(TripID));
                labelTitle.Content = $"Details of fishing trip from {_trip.DateTimeDeparture.ToString("yyyy-MMM-dd")}";
                PropertyGrid.SelectedObject = _trip;
                _defaultEnd = null;
                _defaultStart = null;
            }
        }

        private void SetNewTrip()
        {
            Title = "Add a new fishing trip";
            if (GPXFile != null)
            {
                _oldArriveDate = GPXFile.DateRangeEnd.AddMinutes(-1);
                _oldDepartDate = GPXFile.DateRangeStart.AddMinutes(1);
            }
            else
            {
                _oldArriveDate = DateTime.Now;
                _oldDepartDate = DateTime.Now;
            }

            _trip = new TripEdited(GPS);
            _trip.TripID = TripID;

            if (_defaultEnd != null && _defaultStart != null)
            {
                _trip.DateTimeDeparture = (DateTime)_defaultStart;
                _trip.DateTimeArrival = (DateTime)_defaultEnd;
            }
            else
            {
                _trip.DateTimeArrival = _oldArriveDate;
                _trip.DateTimeDeparture = _oldDepartDate;
            }

            _trip.VesselName = VesselName;
            _trip.OperatorName = OperatorName;
            if (GearCode != null && GearCode.Length > 0)
            {
                _trip.GearCode = GearCode;
            }
            labelTitle.Content = "Details of new fishing trip";
            PropertyGrid.SelectedObject = _trip;
        }
        public int TripID { get; set; }
        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            Title = "Details of fishing trip";
            if(IsNew)
            {
                SetNewTrip();
            }
            else
            {
                ShowTripDetails();
                
            }

            
            PropertyGrid.NameColumnWidth = 200;

            PropertyGrid.PropertyDefinitions.Add(new PropertyDefinition { Name = "OperatorName", DisplayName = "Name of operator", DisplayOrder = 1, Description = "Name of operator of fishing boat" });
            PropertyGrid.PropertyDefinitions.Add(new PropertyDefinition { Name = "VesselName", DisplayName = "Name of fishing vessel", DisplayOrder = 2, Description = "Name of fishing vessel" });
            PropertyGrid.PropertyDefinitions.Add(new PropertyDefinition { Name = "GearCode", DisplayName = "Gear used", DisplayOrder = 3, Description = "Name of fishing gear" });
            PropertyGrid.PropertyDefinitions.Add(new PropertyDefinition { Name = "OtherGear", DisplayName = "Other fishing gear", DisplayOrder = 4, Description = "Name of other fishing gear" });
            PropertyGrid.PropertyDefinitions.Add(new PropertyDefinition { Name = "DateTimeDeparture", DisplayName = "Date and time of departure", DisplayOrder = 5, Description = "Date and time of departure from landing site" });
            PropertyGrid.PropertyDefinitions.Add(new PropertyDefinition { Name = "DateTimeArrival", DisplayName = "Date and time of arrival", DisplayOrder = 6, Description = "Date and time of arrival at landing site" });
            PropertyGrid.PropertyDefinitions.Add(new PropertyDefinition { Name = "Notes", DisplayName = "Notes", DisplayOrder = 7, Description = "Notes" });
            PropertyGrid.PropertyDefinitions.Add(new PropertyDefinition { Name = "TripID", DisplayName = "Trip identifier", DisplayOrder = 8, Description = "Database identifier of trip" });
            PropertyGrid.PropertyDefinitions.Add(new PropertyDefinition { Name = "GPS", DisplayName = "GPS used", DisplayOrder = 9, Description = "GPS used" });
            PropertyGrid.PropertyDefinitions.Add(new PropertyDefinition { Name = "TrackSummary", DisplayName = "Track summary", DisplayOrder = 10, Description = "Summary of track" });
        }


        public GPXFile GPXFile { get; set; }
        public bool IsNew { get; set; }

        public MainWindow ParentWindow { get; set; }
        public void Dispose()
        {

        }

        private void ExtractTracks()
        {
            string trackFileName = "";
            if (_trip.DateTimeArrival > _trip.DateTimeDeparture && Entities.TrackViewModel.Tracks.Count > 0)
            {
                _waypoints = new List<Waypoint>();
                foreach (var trk in Entities.TrackViewModel.Tracks[GPS])
                {
                    foreach (var wpt in trk.Waypoints.OrderBy(t => t.Time).ToList())
                    {
                        //adjust waypoint time to local time by adding offset from GMT
                        var wptTimeAdjusted = wpt.Time.AddHours(Global.Settings.HoursOffsetGMT);

                        if (wptTimeAdjusted > _trip.DateTimeDeparture && wptTimeAdjusted < _trip.DateTimeArrival)
                        {
                            _waypoints.Add(wpt);
                        }
                    }

                    //if we have a collection of waypoints then we exit the loop to avoid reading time from other track files
                    if (_waypoints.Count > 0)
                    {
                        trackFileName = trk.FileName;
                        break;
                    }

                }
                if (_waypoints.Count > 0)
                {
                    var timeStamp = _waypoints[0].Time.AddHours(Global.Settings.HoursOffsetGMT);
                    _trip.Track.FileName = trackFileName;
                    _trip.Track.Waypoints = _waypoints;
                    _trip.Track.Name = $"{GPS.DeviceName} {timeStamp.ToString("MMM-dd-yyyy HH:mm")}";
                    _trackXML= _trip.Track.SerializeToString(GPS, timeStamp, trackFileName);
                    _trip.Track.ResetStatistics();
                    PropertyGrid.Update();
                    _dateTimeDepartureArrivalChanged = false;
                }
                else
                {
                    MessageBox.Show("No waypoints found that match date of departure and arrival", "GPX Manager", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
        public string OperatorName { get; set; }
        public string VesselName { get; set; }
        public string GearCode { get; set; }

        public string Notes { get; set; }
        private void OnButtonClicked(object sender, RoutedEventArgs e)
        {
            switch(((Button)sender).Name)
            {
                case "buttonExtractTracks":
                    ExtractTracks();
                    break;
                case "buttonOk":
                    if(_dateTimeDepartureArrivalChanged)
                    {
                        ExtractTracks();
                    }
                    if (_trip.Track.XMLString != null)
                    {
                        Trip trip = new Trip
                        {
                            VesselName = _trip.VesselName,
                            OperatorName = _trip.OperatorName,
                            DateTimeArrival = _trip.DateTimeArrival,
                            DateTimeDeparture = _trip.DateTimeDeparture,
                            GPS = _trip.GPS,
                            TripID = _trip.TripID,
                            Gear = Entities.GearViewModel.GetGear(_trip.GearCode),
                            OtherGear = _trip.OtherGear,
                            DeviceID = GPS.DeviceID,
                            Track = _trip.Track,
                            Notes = _trip.Notes,
                            GPXFileName = _trip.Track.FileName,
                            XML = _trip.Track.XMLString
                        };
                        trip.GPS = _trip.GPS;
                        var result = Entities.TripViewModel.ValidateTrip(trip, IsNew);
                        if (result.ErrorMessage.Length == 0)
                        {
                            if (IsNew)
                            {
                                Entities.TripViewModel.AddRecordToRepo(trip);
                            }
                            else
                            {
                                Entities.TripViewModel.UpdateRecordInRepo(trip);
                            }

                            DialogResult = true;
                        }

                        else
                        {
                            MessageBox.Show(result.ErrorMessage, "Validation error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Track is not defined","Validation error",MessageBoxButton.OK,MessageBoxImage.Error);
                    }
                    break;
                case "buttonCancel":
                    if (DialogResult != null)
                    {
                        DialogResult = false;
                    }
                    else
                    {
                       Close();

                    }
                    break;
            }
        }

        private void OnPropertyValueChanged(object sender, PropertyValueChangedEventArgs e)
        {
            _selectedProperty = (PropertyItem)e.OriginalSource;
            _dateTimeDepartureArrivalChanged = (_selectedProperty.PropertyName == "DateTimeDeparture" || _selectedProperty.PropertyName == "DateTimeArrival");
        }

        private void OnPropertyDblClick(object sender, MouseButtonEventArgs e)
        {
            switch(_selectedProperty.PropertyName)
            {
                case "TrackSummary":
                    if(_selectedProperty.Value.ToString().Length>0)
                    {
                        GPXFIlePropertiesWindow gpw = new GPXFIlePropertiesWindow();
                        gpw.GPXXML = _trackXML;
                        gpw.Owner = this;
                        gpw.ShowDialog();
                    }
                    break;
            }
        }



        private void OnPropertyChanged(object sender, RoutedPropertyChangedEventArgs<PropertyItemBase> e)
        {
            _selectedProperty = (PropertyItem)e.NewValue;
        }
    }
}
