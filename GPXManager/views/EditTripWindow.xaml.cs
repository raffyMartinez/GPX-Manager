﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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


    public enum SearchTrackResult
    {
        TrackNotSearched,
        TrackSearchedNoResult,
        TrackSearchWithResult
    }

    /// <summary>
    /// Interaction logic for EditTripWindow.xaml
    /// </summary>


    public partial class EditTripWindow : Window, IDisposable
    {
        private string _wptName;
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
        private DateTime _wayPointTime;
        private bool _dateArrivalSameAsDepartureDone;
        private bool _checkBoxWasChecked;
        private bool _formIsDirty;
        private SearchTrackResult _searchTrackResult;

        public static EditTripWindow GetInstance()
        {
            if (_instance == null) _instance = new EditTripWindow();
            return _instance;
        }

        public static EditTripWindow Instance
        {
            get
            {
                return _instance;
            }
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
        public void RefreshTrip(bool newTrip = false)
        {
            ShowTripDetails(newTrip);
        }
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            this.ApplyPlacement();
        }
        public void ShowTripDetails(bool newTrip = false)
        {
            if (newTrip)
            {
                TripID = Entities.TripViewModel.NextRecordNumber;
                SetNewTrip();
            }
            else
            {
                _trip = new TripEdited(Entities.TripViewModel.GetTrip(TripID));
                if(_trip.Track!=null)
                {
                    _searchTrackResult = SearchTrackResult.TrackSearchWithResult;
                }
                labelTitle.Content = $"Details of fishing trip from {_trip.DateTimeDeparture.ToString("yyyy-MMM-dd")}";
                PropertyGrid.SelectedObject = _trip;
                _defaultEnd = null;
                _defaultStart = null;
            }
        }

        public Trip LastTripOfGPS { get; set; }
        public void SetNewTrip()
        {
            Title = "Add a new fishing trip";
            if (GPXFile != null)
            {
                _oldArriveDate = GPXFile.DateRangeEnd.AddMinutes(-1);
                _oldDepartDate = GPXFile.DateRangeStart.AddMinutes(1);
            }
            else
            {
                if (LastTripOfGPS != null)
                {
                    _oldArriveDate = LastTripOfGPS.DateTimeArrival.Date.AddDays(1);
                    _oldDepartDate = LastTripOfGPS.DateTimeArrival.Date.AddDays(1);

                }
                else
                {
                    _oldArriveDate = DateTime.Now;
                    _oldDepartDate = DateTime.Now;
                }
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
            //_trip.OperatorID = (int)OperatorID;
            if (OperatorID != null)
            {
                _trip.OperatorID = OperatorID;
            }
            if (GearCode != null && GearCode.Length > 0)
            {
                _trip.GearCode = GearCode;
            }
            labelTitle.Content = "Details of new fishing trip";
            PropertyGrid.SelectedObject = _trip;
        }
        public int TripID { get; set; }

        public void ResetView()
        {
            rowTrip.Height = new GridLength(1, GridUnitType.Star);
            rowWaypoints.Height = new GridLength(0);
            checkEditWaypoints.IsEnabled = false;
            checkEditWaypoints.IsChecked = false;
        }
        private void ConfigureGrid()
        {
            dataGridWaypoints.AutoGenerateColumns = false;


            //create a list of waypoints for the trip
            var comboWaypoints = new ObservableCollection<string>();
            if (Entities.WaypointViewModel.Waypoints.Count == 0)
            {
                Entities.WaypointViewModel.ReadWaypointsFromRepository();
            }

            if (Entities.WaypointViewModel.Count > 0)
            {
                foreach (var item in Entities.WaypointViewModel.Waypoints[Entities.GPSViewModel.CurrentEntity])
                {
                    foreach (var wpt in item.Waypoints
                        .Where(t => t.Time.AddHours(Global.Settings.HoursOffsetGMT) > Entities.TripViewModel.CurrentEntity.DateTimeDeparture)
                        .Where(t => t.Time.AddHours(Global.Settings.HoursOffsetGMT) < Entities.TripViewModel.CurrentEntity.DateTimeArrival)
                       )
                    {

                        _waypoints.Add(wpt);
                        comboWaypoints.Add(wpt.Name);
                    }
                }
            }



            //add the columns to the datagrid
            DataGridComboBoxColumn cboWaypointName = new DataGridComboBoxColumn();
            cboWaypointName.Header = "Waypoint";
            cboWaypointName.ItemsSource = comboWaypoints;
            cboWaypointName.SelectedItemBinding = new Binding("WaypointName");
            dataGridWaypoints.Columns.Add(cboWaypointName);


            DataGridComboBoxColumn myDGCBC = new DataGridComboBoxColumn();
            myDGCBC.Header = "Type of waypoint";
            var cmbItems = new ObservableCollection<string> { "Set", "Haul" };
            myDGCBC.ItemsSource = cmbItems;
            myDGCBC.SelectedItemBinding = new Binding("WaypointType");
            dataGridWaypoints.Columns.Add(myDGCBC);


            dataGridWaypoints.Columns.Add(new DataGridTextColumn { Header = "Set #", Binding = new Binding("SetNumber") });


            var col = new DataGridTextColumn()
            {
                Binding = new Binding("TimeStampAdjusted"),
                Header = "Time stamp"
            };
            col.IsReadOnly = true;
            col.Binding.StringFormat = "MMM-dd-yyyy HH:mm:ss";
            dataGridWaypoints.Columns.Add(col);

            //dataGridWaypoints.Columns.Add(new DataGridTextColumn { Header = "Date and time", Binding = new Binding("TimeStampAdjustedDisplay"), IsReadOnly = true });
        }


        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            _waypoints = new List<Waypoint>();
            Title = "Details of fishing trip";
            ResetView();
            //ConfigureGrid();
            if (IsNew)
            {
                SetNewTrip();
            }
            else
            {
                checkEditWaypoints.IsEnabled = true;
                ShowTripDetails();

            }


            PropertyGrid.NameColumnWidth = 200;

            PropertyGrid.PropertyDefinitions.Add(new PropertyDefinition { Name = "OperatorID", DisplayName = "Name of operator", DisplayOrder = 1, Description = "Name of operator of fishing boat" });
            PropertyGrid.PropertyDefinitions.Add(new PropertyDefinition { Name = "VesselName", DisplayName = "Name of fishing vessel", DisplayOrder = 2, Description = "Name of fishing vessel" });
            PropertyGrid.PropertyDefinitions.Add(new PropertyDefinition { Name = "GearCode", DisplayName = "Gear used", DisplayOrder = 3, Description = "Name of fishing gear" });
            PropertyGrid.PropertyDefinitions.Add(new PropertyDefinition { Name = "OtherGear", DisplayName = "Other fishing gear", DisplayOrder = 4, Description = "Name of other fishing gear" });
            PropertyGrid.PropertyDefinitions.Add(new PropertyDefinition { Name = "DateTimeDeparture", DisplayName = "Date and time of departure", DisplayOrder = 5, Description = "Date and time of departure from landing site" });
            PropertyGrid.PropertyDefinitions.Add(new PropertyDefinition { Name = "DateTimeArrival", DisplayName = "Date and time of arrival", DisplayOrder = 6, Description = "Date and time of arrival at landing site" });
            PropertyGrid.PropertyDefinitions.Add(new PropertyDefinition { Name = "Notes", DisplayName = "Notes", DisplayOrder = 7, Description = "Notes" });
            PropertyGrid.PropertyDefinitions.Add(new PropertyDefinition { Name = "TripID", DisplayName = "Trip identifier", DisplayOrder = 8, Description = "Database identifier of trip" });
            PropertyGrid.PropertyDefinitions.Add(new PropertyDefinition { Name = "DeviceID", DisplayName = "GPS used", DisplayOrder = 9, Description = "GPS used" });
            PropertyGrid.PropertyDefinitions.Add(new PropertyDefinition { Name = "TrackSummary", DisplayName = "Track summary", DisplayOrder = 10, Description = "Summary of track" });
        }


        public GPXFile GPXFile { get; set; }
        public bool IsNew { get; set; }

        public MainWindow ParentWindow { get; set; }
        public void Dispose()
        {

        }

        private void ExtractTracks(bool verbose = true)
        {
            if (Entities.TripViewModel.SetTrackOfTrip(_trip))
            {
                foreach (PropertyItem prp in PropertyGrid.Properties)
                {
                    if (prp.PropertyName == "TrackSummary")
                    {
                        prp.Value = _trip.TrackSummary;
                        _searchTrackResult = SearchTrackResult.TrackSearchWithResult;
                        return;
                    }
                }
            }
            else
            {
                _searchTrackResult = SearchTrackResult.TrackSearchedNoResult;
                if (verbose)
                {
                    MessageBox.Show("No waypoints found that match date of departure and arrival", "GPX Manager", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }

        }
        public int? OperatorID { get; set; }

        public string VesselName { get; set; }
        public string GearCode { get; set; }

        public string Notes { get; set; }
        private void OnButtonClicked(object sender, RoutedEventArgs e)
        {
            switch (((Button)sender).Name)
            {
                case "buttonTripWaypointsSave":
                    MainWindow.SaveTripWaypointsFromDataGrid(dataGridWaypoints);

                    break;
                case "buttonWpts":

                    break;
                case "buttonExtractTracks":
                    ExtractTracks();
                    break;
                case "buttonOk":
                    if (!_formIsDirty && (!checkEditWaypoints.IsEnabled || !(bool)checkEditWaypoints.IsChecked))
                    {
                        Close();
                        return;
                    }

                    if (_formIsDirty && _searchTrackResult != SearchTrackResult.TrackNotSearched)
                    {
                        if (_dateTimeDepartureArrivalChanged)
                        {
                            ExtractTracks(verbose: false);
                        }

                        Trip trip = new Trip
                        {
                            VesselName = _trip.VesselName,
                            OperatorID = _trip.OperatorID,
                            DateTimeArrival = _trip.DateTimeArrival,
                            DateTimeDeparture = _trip.DateTimeDeparture,
                            GPS = _trip.GPS,
                            TripID = _trip.TripID,
                            Gear = Entities.GearViewModel.GetGear(_trip.GearCode),
                            OtherGear = _trip.OtherGear,
                            DeviceID = _trip.GPS.DeviceID,
                            Notes = _trip.Notes,
                        };

                        if (_searchTrackResult == SearchTrackResult.TrackSearchWithResult)
                        {
                            trip.Track = _trip.Track;
                            trip.GPXFileName = _trip.Track.FileName;
                            trip.XML = _trip.Track.XMLString;
                        };

                        //trip.GPS = _trip.GPS;
                        if (trip.TripID == 0)
                        {
                            trip.TripID = Entities.TripViewModel.NextRecordNumber;
                        }
                        _trip.Trip = trip;
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

                            //DialogResult = true;
                            checkEditWaypoints.IsEnabled = true;
                            _formIsDirty = false;
                            if (!checkEditWaypoints.IsEnabled || !(bool)checkEditWaypoints.IsChecked)
                            {
                                Close();
                            }
                        }

                        else
                        {
                            MessageBox.Show(result.ErrorMessage, "Validation error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }


                    }
                    else
                    {
                        if (_searchTrackResult == SearchTrackResult.TrackNotSearched)
                        {
                            MessageBox.Show("Track is not defined", "Validation error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
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
            _formIsDirty = true;
            ComboBox cbo = new ComboBox();
            cbo.Items.Clear();
            cbo.SelectionChanged += OnComboSelectionChanged;

            _selectedProperty = (PropertyItem)e.OriginalSource;
            switch (_selectedProperty.PropertyName)
            {
                case "DateTimeDeparture":
                case "DateTimeArrival":
                    _dateTimeDepartureArrivalChanged = (_selectedProperty.PropertyName == "DateTimeDeparture" || _selectedProperty.PropertyName == "DateTimeArrival");

                    if (!_dateArrivalSameAsDepartureDone && _selectedProperty.PropertyName == "DateTimeDeparture")
                    {
                        _dateArrivalSameAsDepartureDone = true;
                        foreach (PropertyItem prp in PropertyGrid.Properties)
                        {
                            if (prp.PropertyName == "DateTimeArrival")
                            {
                                prp.Value = _selectedProperty.Value;
                            }
                        }
                    }
                    break;
                case "OperatorID":
                    cbo.Tag = "vessels";

                    foreach (var item in Entities.FisherViewModel.GetFisherBoats((int)_selectedProperty.Value))
                    {
                        cbo.Items.Add(item);
                    }

                    foreach (PropertyItem prp in PropertyGrid.Properties)
                    {
                        if (prp.PropertyName == "VesselName")
                        {
                            prp.Editor = cbo;
                        }
                    }
                    break;
                case "DeviceID":
                    ComboBox c = _selectedProperty.Editor as ComboBox;
                    string key = ((Xceed.Wpf.Toolkit.PropertyGrid.Attributes.Item)c.SelectedItem).Value.ToString();
                    _trip.GPS = Entities.GPSViewModel.GetGPS(key);
                    break;
            }

        }

        private void OnComboSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cbo = (ComboBox)sender;
            switch (cbo.Tag.ToString())
            {
                case "vessels":
                    foreach (PropertyItem prp in PropertyGrid.Properties)
                    {
                        if (prp.PropertyName == "VesselName")
                        {
                            prp.Value = cbo.SelectedItem;
                            return;
                        }
                    }
                    break;
            }
        }

        private void OnPropertyDblClick(object sender, MouseButtonEventArgs e)
        {
            switch (_selectedProperty.PropertyName)
            {
                case "TrackSummary":
                    if (_selectedProperty.Value.ToString().Length > 0)
                    {
                        GPXFIlePropertiesWindow gpw = new GPXFIlePropertiesWindow();
                        gpw.GPXXML = _trackXML == null ? _trip.XML : _trackXML;
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

        private void OnCheckChanged(object sender, RoutedEventArgs e)
        {
            if (!_checkBoxWasChecked)
            {
                _checkBoxWasChecked = true;
            }

            if ((bool)checkEditWaypoints.IsChecked)
            {
                rowWaypoints.Height = new GridLength(1, GridUnitType.Star);
                rowTrip.Height = new GridLength(0);
                if (dataGridWaypoints.ItemsSource == null)
                {
                    ConfigureGrid();
                    dataGridWaypoints.ItemsSource = Entities.TripWaypointViewModel.GetAllTripWaypoints(TripID);
                }

            }
            else
            {
                rowTrip.Height = new GridLength(1, GridUnitType.Star);
                rowWaypoints.Height = new GridLength(0);
            }
        }

        private DateTime GetAdjustedTimeofWaypoint(string wpt)
        {

            return Entities.WaypointViewModel.GetWaypoint(wpt, _trip.Trip).Time.AddHours(Global.Settings.HoursOffsetGMT);

        }
        private void OnGridCellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            switch (e.Column.Header)
            {
                case "Waypoint":
                    _wptName = ((ComboBox)e.EditingElement).Text;
                    if (_wptName.Length > 0)
                    {
                        _wayPointTime = GetAdjustedTimeofWaypoint(_wptName);
                        dataGridWaypoints.GetCell(e.Row.GetIndex(), 3).Content = _wayPointTime;
                    }
                    break;
            }

        }

        private void OnGridRowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (_wptName != null && _wptName.Length > 0)
            {
                ((TripWaypoint)e.Row.Item).Waypoint = _waypoints.FirstOrDefault(t => t.Name == _wptName);
                ((TripWaypoint)e.Row.Item).TimeStampAdjusted = _wayPointTime;
                ((TripWaypoint)e.Row.Item).TimeStamp = _wayPointTime.AddHours(Global.Settings.HoursOffsetGMT * -1);
                ((TripWaypoint)e.Row.Item).Trip = _trip.Trip;
                if (GPXFile != null)
                {
                    ((TripWaypoint)e.Row.Item).WaypointGPXFileName = GPXFile.FileName;
                }
            }
        }
    }
}
