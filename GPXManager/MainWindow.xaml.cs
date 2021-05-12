using GPXManager.entities;
using GPXManager.entities.mapping;
using GPXManager.views;
using MapWinGIS;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using xceedPropertyGrid = Xceed.Wpf.Toolkit.PropertyGrid;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Collections.ObjectModel;
using System.Windows.Threading;
//using Xceed.Wpf.Toolkit.PropertyGrid;

namespace GPXManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<GPXFile> _mappedGPXFiles = new List<GPXFile>();
        private bool _inArchive;
        private string _deviceIdentifier;
        private string _selectedProperty;
        private DetectedDevice _detectedDevice;
        private ComboBox _cboBrand;
        private ComboBox _cboModel;
        private GPS _gps;
        private GPXFile _gpxFile;
        private bool _isTrackGPX;
        private bool _isNew;
        private List<Trip> _trips;
        private Trip _selectedTrip;
        private TripWaypoint _selectedTripWaypoint;
        private bool _gpsPropertyChanged;
        private string _changedPropertyName;
        private TreeViewItem _gpsTreeViewItem;
        private DateTime _archiveMonthYear;
        private DateTime _tripMonthYear;
        private bool _usbGPSPresent;
        private bool _inDeviceNode;
        private List<TripWaypoint> _tripWaypoints;
        private string _oldGPSName;
        private string _oldGPSCode;
        private EditTripWindow _editTripWindow;
        private string _gpsid;
        private bool _archiveTreeExpanded;
        private Dictionary<string, string> _deviceDetectError = new Dictionary<string, string>();
        private bool _tripWaypointsDirty;

        private List<FileInfo> _rawImageFiles;
        private BitmapImage _src;
        private FileInfo _fileSelectedLogBookImage;
        private LogbookImage _logbookImage;
        private TreeViewItem _selectedTreeViewItem;
        private bool _startUp = false;
        public DataGrid CurrentDataGrid { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            Loaded += OnWindowLoaded;
            Closing += OnWindowClosing;
            _usbGPSPresent = false;
            treeDevices.MouseRightButtonDown += Tree_MouseRightButtonDown;
            treeArchive.MouseRightButtonDown += Tree_MouseRightButtonDown;
            treeCalendar.MouseRightButtonDown += Tree_MouseRightButtonDown;
        }




        /// <summary>
        /// Used for initiating right click menus on the tree
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Tree_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            ContextMenu cm = new ContextMenu();
            MenuItem m = null;
            switch (((TreeView)sender).Name)
            {
                case "treeDevices":

                    if (_inDeviceNode)
                    {
                        m = new MenuItem { Header = "Eject device", Name = "menuEjectDevice" };
                        m.Click += OnMenuClick;
                        cm.Items.Add(m);
                    }
                    else
                    {
                        return;
                    }
                    break;
                case "treeCalendar":
                    if (((TreeViewItem)treeCalendar.SelectedItem).Tag.GetType().Name == "GPS")
                    {
                        _gps = (GPS)((TreeViewItem)treeCalendar.SelectedItem).Tag;
                        m = new MenuItem { Header = "Add trip using selected GPS", Name = "menuAddTripUsingGPS" };
                        m.Click += OnMenuClick;
                        cm.Items.Add(m);
                    }
                    else
                    {
                        return;
                    }
                    break;
                case "treeArchive":
                    string tag = ((TreeViewItem)treeArchive.SelectedItem).Tag.ToString();
                    if (DateTime.TryParse(tag, out DateTime v))
                    {
                        _archiveMonthYear = v;
                        if (MapWindowForm.Instance != null)
                        {
                            m = new MenuItem { Header = "Show GPS month data in map", Name = "menuMapGPSMonthData" };
                            m.Click += OnMenuClick;
                            cm.Items.Add(m);

                            m = new MenuItem { Header = "Show GPS monthly track data in map", Name = "menuMapGPSMonthTrackData" };
                            m.Click += OnMenuClick;
                            cm.Items.Add(m);

                            m = new MenuItem { Header = "Show GPS monthly waypoint data in map", Name = "menuMapGPSMonthWaypopintData" };
                            m.Click += OnMenuClick;
                            cm.Items.Add(m);
                        }
                        else
                        {
                            return;
                        }
                    }
                    else if (tag == "root")
                    {
                        m = new MenuItem { Header = "Import GPS", Name = "menuImportGPS" };
                        m.Click += OnMenuClick;
                        cm.Items.Add(m);

                        //m = new MenuItem { Header = "Import GPX", Name = "menuImportGPX" };
                        //m.Click += OnMenuClick;
                        //cm.Items.Add(m);

                        m = new MenuItem { Header = "Import GPX", Name = "menuImportGPXByFolder" };
                        m.Click += OnMenuClick;
                        cm.Items.Add(m);

                        cm.Items.Add(new Separator());

                        m = new MenuItem { Header = "Open backup location", Name = "menuOpenBackupFolder" };
                        m.Click += OnMenuClick;
                        cm.Items.Add(m);

                        m = new MenuItem { Header = "Backup GPX to drive", Name = "menuBackupGPX" };
                        m.Click += OnMenuClick;
                        cm.Items.Add(m);

                        cm.Items.Add(new Separator());

                        m = new MenuItem { Header = "Expand all", Name = "menuExpandAll" };
                        m.Click += OnMenuClick;
                        cm.Items.Add(m);

                        m = new MenuItem { Header = "Collapse all", Name = "menuCollapseAll" };
                        m.Click += OnMenuClick;
                        cm.Items.Add(m);

                        if (MapWindowForm.Instance != null)

                        {
                            cm.Items.Add(new Separator());

                            m = new MenuItem { Header = "Show track and waypoint on map", Name = "menuShowAllGPXOnMap" };
                            m.Click += OnMenuClick;
                            cm.Items.Add(m);
                        }
                    }
                    else
                    {
                        return;
                    }
                    break;
            }

            cm.IsOpen = true;


        }

        public void ResetDataGrids()
        {
            dataGridGPXFiles.Items.Refresh();
            dataGridTrips.Items.Refresh();
            dataGridGPSSummary.Items.Refresh();
        }

        private void Cleanup()
        {
            _detectedDevice = null;
            _gps = null;
            _gpxFile = null;
            _selectedTrip = null;
            _selectedTripWaypoint = null;
            MapWindowManager.CleanUp(true);
        }

        private void OnWindowClosing(object sender, CancelEventArgs e)
        {
            if (_usbGPSPresent)
            {
                Entities.DeviceGPXViewModel.SaveDeviceGPXToRepository();
            }
            Cleanup();
        }


        private void SetupEntities()
        {
            SplashWindow sw = new SplashWindow();
            sw.ShowDialog();
            //Entities.GPSViewModel = new GPSViewModel();
            //Entities.DetectedDeviceViewModel = new DetectedDeviceViewModel();
            //Entities.GPXFileViewModel = new GPXFileViewModel();
            //Entities.GearViewModel = new GearViewModel();
            //Entities.TripViewModel = new TripViewModel();
            //Entities.WaypointViewModel = new WaypointViewModel();
            //Entities.TrackViewModel = new TrackViewModel();
            //Entities.TripWaypointViewModel = new TripWaypointViewModel();
            //Entities.DeviceGPXViewModel = new DeviceGPXViewModel();
            //Entities.AOIViewModel = new AOIViewModel();
            //Entities.LogbookImageViewModel = new LogbookImageViewModel();
            //Entities.LandingSiteViewModel = new LandingSiteViewModel();
            //Entities.FisherViewModel = new FisherViewModel();

            Entities.FisherViewModel.EntitiesChanged += OnEntitiesChanged;


            _cboBrand = new ComboBox();
            _cboModel = new ComboBox();

            _cboModel.Name = "cboModel";
            _cboBrand.Name = "cboBrand";

            Entities.DetectedDeviceViewModel.DeviceDetected += OnDeviceDetected;

            _cboBrand.SelectionChanged += OnComboSelectionChanged;
            _cboModel.SelectionChanged += OnComboSelectionChanged;

            _cboBrand.ItemsSource = Entities.GPSViewModel.GPSBrands;
            _cboModel.ItemsSource = Entities.GPSViewModel.GPSModels;

            ConfigureGrids();

            statusLabel.Content = Global.MDBPath;
        }

        private void OnEntitiesChanged(object sender, EventArgs e)
        {
            switch (sender.GetType().Name)
            {
                case "FisherViewModel":
                    dataGridFishers.ItemsSource = Entities.FisherViewModel.GetAll();
                    RefreshComboFisher();
                    break;
            }
        }



        private void OnDeviceDetected(DetectedDeviceViewModel s, DetectDeviceEventArg e)
        {
            if (e.HasDetectError)
            {
                //MessageBox.Show(e.Message, "USB device detection", MessageBoxButton.OK, MessageBoxImage.Information);
                _deviceDetectError.Add(e.DriveName, e.Message);
            }
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            statusLabel.Content = "";
            _archiveTreeExpanded = true;
            ResetView();

            if (Global.AppProceed)
            {
                if (File.Exists(Global.MDBPath))
                {
                    SetupEntities();
                    SetupLogBookImageControls();
                    _startUp = true;
                    ShowArchive();
                    ((TreeViewItem)treeViewCybertracker.Items[0]).IsSelected = true;
                }
                else
                {
                    statusLabel.Content = "Path to backend database not found";
                }
            }
            else
            {
                if (Global.MDBPath == null)
                {
                    statusLabel.Content = "Application need to be setup first";
                }
                else if (Global.MDBPath.Length > 0 && File.Exists(Global.MDBPath))
                {
                    statusLabel.Content = "Application need to be setup first";
                }
                else
                {
                    statusLabel.Content = "Path to backend database not found";
                }
            }

            if (Debugger.IsAttached)
            {
                menuClearTables.Visibility = Visibility.Visible;
            }

            SetMapButtonsEnabled();
        }

        private void SetupLogBookImageControls()
        {
            comboGPS.DisplayMemberPath = "Value";
            comboGPS.SelectedValuePath = "Key";

            comboGear.DisplayMemberPath = "Value";
            comboGear.SelectedValuePath = "Key";

            comboFisher.DisplayMemberPath = "Value";
            comboFisher.SelectedValuePath = "Key";

            foreach (var item in Entities.GPSViewModel.GetAll())
            {

                KeyValuePair<string, string> kv = new KeyValuePair<string, string>(item.DeviceID, item.DeviceName);
                comboGPS.Items.Add(kv);

            }

            foreach (var gear in Entities.GearViewModel.GetAllGears())
            {
                KeyValuePair<string, string> kv = new KeyValuePair<string, string>(gear.Code, gear.Name);
                comboGear.Items.Add(kv);
            }

            RefreshComboFisher();


            labelSelectedImageItem.Content = "";

        }

        private void RefreshComboFisher()
        {
            comboFisher.Items.Clear();
            foreach (var fisher in Entities.FisherViewModel.GetAll())
            {
                KeyValuePair<int, string> kv = new KeyValuePair<int, string>(fisher.FisherID, fisher.Name);
                comboFisher.Items.Add(kv);
            }
        }

        private void ShowImageList()
        {
            foreach (TreeViewItem item in treeImages.Items)
            {
                item.Items.Clear();
            }
            if (_rawImageFiles.Count > 0)
            {
                foreach (var rawImage in _rawImageFiles)
                {

                    var logbookImage = Entities.LogbookImageViewModel.GetImage(rawImage.FullName);
                    if (logbookImage != null)
                    {
                        if (logbookImage.Ignore)
                        {
                            tvItemIgnored.Items.Add(new TreeViewItem { Header = rawImage.Name, Tag = logbookImage, FontWeight = FontWeights.Normal });
                        }
                        else
                        {
                            AddSavedImageToTree(logbookImage);
                        }
                    }
                    else
                    {
                        tvItemNotRegistered.Items.Add(new TreeViewItem { Header = rawImage.Name, Tag = rawImage, FontWeight = FontWeights.Normal });
                    }
                }
            }
        }
        private void OnComboSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var cbo = (ComboBox)sender;
            if (cbo.SelectedItem != null && cbo.SelectedItem.ToString().Length > 0)
            {
                switch (cbo.Name)
                {
                    case "comboFisher":
                        comboVessel.Items.Clear();
                        foreach (var item in Entities.FisherViewModel.GetFisherBoats(((KeyValuePair<int, string>)cbo.SelectedItem).Key))
                        {
                            comboVessel.Items.Add(item);

                        }
                        break;
                    case "cboModel":

                        foreach (xceedPropertyGrid.PropertyItem prp in gpsPropertiesGrid.Properties)
                        {
                            if (prp.DisplayName == "Model")
                            {
                                prp.Value = cbo.SelectedItem;
                            }
                        }

                        break;

                    case "cboBrand":
                        if (cbo.SelectedItem != null)
                        {
                            _cboModel.ItemsSource = Entities.GPSViewModel.GetModels(cbo.SelectedItem.ToString());
                            _cboModel.SelectedItem = null;
                        }
                        foreach (xceedPropertyGrid.PropertyItem prp in gpsPropertiesGrid.Properties)
                        {
                            if (prp.DisplayName == "Brand")
                            {
                                prp.Value = cbo.SelectedItem;
                            }
                        }
                        break;
                }
            }
        }

        private bool ReadUSBDrives()
        {
            if (Entities.DetectedDeviceViewModel == null)
            {
                if (!Global.AppProceed)
                {
                    MessageBox.Show("Application need to be setup first", "GPX Manager", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                return false;
            }
            else if (Entities.DetectedDeviceViewModel.ScanUSBDevices() == 0)
            {
                MessageBox.Show("No USB storage devices detected", "GPX Manager", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            else
            {
                return true;
            }
        }

        private void ShowEditTripWindow(bool isNew, int tripID, int? operatorID = null,
            string vesselName = "", string gearCode = "", bool showWaypoints = false, bool fromImage = false,
            Trip lastTripOfGPS = null)
        {

            if (operatorID != null)
            {
                Entities.FisherViewModel.SelectedTripFisherID((int)operatorID);
            }
            if (isNew)
            {
                using (EditTripWindow etw = new EditTripWindow
                {
                    ParentWindow = this,
                    IsNew = isNew,
                    TripID = tripID,
                    GPS = _gps,
                    VesselName = vesselName,
                    GearCode = gearCode,
                    GPXFile = _gpxFile,
                    OperatorID = operatorID,
                    LastTripOfGPS = lastTripOfGPS
                })
                {
                    etw.Owner = this;
                    if ((bool)etw.ShowDialog())
                    {
                        dataGridGPXFiles.Items.Refresh();
                        var currentTreeItem = (TreeViewItem)treeCalendar.SelectedItem;

                        TreeViewItem tv = new TreeViewItem { Header = Entities.TripViewModel.GetLastTripOfDevice(_gps.DeviceID).DateTimeDeparture.ToString("MMM-yyyy"), Tag = "month_archive" };
                        TreeViewItem gpsNode = null;
                        switch (currentTreeItem.Tag.GetType().Name)
                        {
                            case "GPS":
                                gpsNode = currentTreeItem;
                                break;
                            case "String":
                                gpsNode = currentTreeItem.Parent as TreeViewItem;
                                break;
                        }

                        bool exists = false;
                        for (int x = 0; x < gpsNode.Items.Count; x++)
                        {
                            if (((TreeViewItem)gpsNode.Items[x]).Header.ToString() == tv.Header.ToString())
                            {
                                exists = true;
                                break;
                            }
                        }

                        if (!exists)
                        {
                            gpsNode.Items.Add(tv);
                        }

                        dataGridGPSSummary.ItemsSource = Entities.TripViewModel.LatestTripsUsingGPS(_gps);

                    }
                }
            }
            else
            {
                if (operatorID == null)
                {
                    operatorID = Entities.TripViewModel.GetFisherOfTrip(tripID).FisherID;
                }


                if (fromImage)
                {
                    if (vesselName.Length == 0)
                    {
                        vesselName = Entities.TripViewModel.VesselOfTrip(tripID);
                    }

                    EditTripWindow ew = EditTripWindow.GetInstance();
                    ew.ParentWindow = this;
                    ew.IsNew = isNew;
                    ew.TripID = tripID;
                    ew.GPS = Entities.GPSViewModel.CurrentEntity;
                    ew.OperatorID = (int)operatorID;
                    ew.VesselName = vesselName;
                    ew.GearCode = gearCode;
                    ew.GPXFile = _gpxFile;
                    if (ew.Visibility == Visibility.Visible)
                    {
                        ew.ShowTripDetails();
                        ew.BringIntoView();
                    }
                    else
                    {
                        ew.Owner = this;
                        ew.Show();
                    }

                }
                else
                {

                    using (EditTripWindow etw = new EditTripWindow
                    {
                        ParentWindow = this,
                        IsNew = isNew,
                        TripID = tripID,
                        GPS = _gps,
                        OperatorID = (int)operatorID,
                        VesselName = vesselName,
                        GearCode = gearCode,
                        GPXFile = _gpxFile
                    })
                    {

                        etw.TripID = tripID;
                        etw.VesselName = Entities.TripViewModel.VesselOfTrip((tripID));
                        etw.Owner = this;
                        if ((bool)etw.ShowDialog())
                        {
                            dataGridGPXFiles.Items.Refresh();
                        }
                    }
                }
            }

            ConfigureTripWaypointsGrid();
            ShowTripWaypoints(fromGPSSummary: true);

        }



        private void AddTrip()
        {

            var lastTripOfGPS = Entities.TripViewModel.GetLastTripOfDevice(_gpsid == null ? _gps.DeviceID : _gpsid);
            if (lastTripOfGPS != null)
            {
                ShowEditTripWindow(isNew: true, Entities.TripViewModel.NextRecordNumber, lastTripOfGPS.OperatorID, lastTripOfGPS.VesselName, lastTripOfGPS.Gear.Code, lastTripOfGPS: lastTripOfGPS);
            }
            else
            {
                ShowEditTripWindow(isNew: true, Entities.TripViewModel.NextRecordNumber);
            }
        }
        private void ArchiveGPSData()
        {
            if (Entities.DeviceGPXViewModel.SaveDeviceGPXToRepository(_detectedDevice))
            {
                buttonArchiveGPX.Visibility = Visibility.Collapsed;
                dataGridGPXFiles.Items.Refresh();
                MessageBox.Show("GPX data successfully archived", "GPX Manager", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ShowImageMetadata()
        {
            if (_fileSelectedLogBookImage != null || _logbookImage != null)
            {
                panelMetadata.Visibility = Visibility.Visible;
                panelImageFields.Visibility = Visibility.Collapsed;

                var fileName = _fileSelectedLogBookImage != null ? _fileSelectedLogBookImage.FullName : _logbookImage.FileName;
                textMetadata.Text = Entities.LogbookImageViewModel.MetadataFlatText(fileName);
            }
            else
            {
                MessageBox.Show("No image selected", "GPXManager", MessageBoxButton.OK, MessageBoxImage.Information);
            }


        }

        private void AddSavedImageToTree(LogbookImage li, bool isRegistered = true)
        {
            int index;
            int newIndex;
            TreeViewItem parentGPS = null;
            imagePreview.Source = null;
            if (isRegistered)
            {
                bool gpsNameFoundInTree = false;
                index = tvItemNotRegistered.Items.IndexOf(_selectedTreeViewItem);
                if (li.TripWithTrack)
                {

                    foreach (TreeViewItem item in tvItemRegistered.Items)
                    {
                        if (item.Header.ToString() == li.GPS.DeviceName)
                        {
                            parentGPS = item;
                            //newIndex=  parentGPS.Items.Add(new TreeViewItem { Header = Path.GetFileName(li.FileName), Tag = li, FontWeight = FontWeights.Normal });
                            gpsNameFoundInTree = true;
                            break;
                        }
                    }
                    if (!gpsNameFoundInTree)
                    {
                        parentGPS = new TreeViewItem { Header = li.GPS.DeviceName, Tag = li.GPS, Foreground = Brushes.Gray };
                        //parentGPS = tvi;
                        tvItemRegistered.Items.Add(parentGPS);
                        //newIndex= tvi.Items.Add(new TreeViewItem { Header = Path.GetFileName(li.FileName), Tag = li,FontWeight = FontWeights.Normal });
                    }

                    //newIndex=  parentGPS.Items.Add(new TreeViewItem { Header = Path.GetFileName(li.FileName), Tag = li, FontWeight = FontWeights.Normal });
                }
                else
                {
                    foreach (TreeViewItem item in tvItemRegisteredNoTrack.Items)
                    {
                        if (item.Header.ToString() == li.GPS.DeviceName)
                        {
                            parentGPS = item;
                            //newIndex= item.Items.Add(new TreeViewItem { Header = Path.GetFileName(li.FileName), Tag = li });
                            gpsNameFoundInTree = true;
                            break;
                        }
                    }
                    if (!gpsNameFoundInTree)
                    {
                        parentGPS = new TreeViewItem { Header = li.GPS.DeviceName, Tag = li.GPS };
                        //parentGPS = tvi;
                        tvItemRegisteredNoTrack.Items.Add(parentGPS);
                        //newIndex= tvi.Items.Add(new TreeViewItem { Header = Path.GetFileName(li.FileName), Tag = li });
                    }

                }
                newIndex = parentGPS.Items.Add(new TreeViewItem { Header = $"{((DateTime)li.Start).ToString("dd-MMM-yyyy")} {Path.GetFileName(li.FileName)}", Tag = li, FontWeight = FontWeights.Normal });
            }
            else
            {
                var ignoredImage = Entities.LogbookImageViewModel.CurrentEntity;
                newIndex = tvItemIgnored.Items.Add(new TreeViewItem { Header = Path.GetFileName(ignoredImage.FileName), Tag = ignoredImage });
                index = tvItemNotRegistered.Items.IndexOf(_selectedTreeViewItem);
            }
            if (index >= 0)
            {

                if (_selectedTreeViewItem != null)
                {
                    tvItemNotRegistered.Items.Remove(_selectedTreeViewItem);


                    if ((bool)checkViewCreatedTrip.IsChecked)
                    {
                        ((TreeViewItem)parentGPS.Items[newIndex]).IsSelected = true;
                    }
                    else
                    {
                        _selectedTreeViewItem = ((TreeViewItem)tvItemNotRegistered.Items[index]);
                        _selectedTreeViewItem.IsSelected = true;
                    }
                }
            }
        }
        private bool RegisterLogBookImage()
        {
            bool success = false;
            bool isNew = true;

            if (_fileSelectedLogBookImage == null)
            {
                isNew = false;
            }

            string fileName = _fileSelectedLogBookImage != null ? _fileSelectedLogBookImage.FullName : _logbookImage.FileName;

            //if (_fileSelectedLogBookImage != null &&
            if (fileName != null &&
                  comboFisher.SelectedIndex >= 0 &&
                  comboVessel.SelectedIndex >= 0 &&
                  comboGear.SelectedIndex >= 0 &&
                  comboGPS.SelectedIndex >= 0 &&
                  dtPickerEnd.Value != null &&
                  dtPickerStart.Value != null)
            {

                //show appropriate track and waypoint if map is open
                if (MapWindowForm.Instance != null)
                {

                }

                LogbookImage li = new LogbookImage
                {
                    FisherID = ((KeyValuePair<int, string>)comboFisher.SelectedItem).Key,
                    FileName = fileName,
                    Boat = comboVessel.SelectedItem.ToString(),
                    Gear = Entities.GearViewModel.GetGear(((KeyValuePair<string, string>)comboGear.SelectedItem).Key),
                    GPS = Entities.GPSViewModel.GetGPS(((KeyValuePair<string, string>)comboGPS.SelectedItem).Key),
                    Start = dtPickerStart.Value,
                    End = dtPickerEnd.Value,
                    Notes = textImageNotes.Text
                };

                var validationReult = Entities.LogbookImageViewModel.EntityValidated(li, true);

                if (validationReult.ErrorMessage.Length == 0)
                {
                    TripEdited t = null;

                    if (isNew)
                    {
                        t = new TripEdited
                        {
                            DateTimeDeparture = (DateTime)li.Start,
                            DateTimeArrival = (DateTime)li.End,
                            GPS = li.GPS,
                            OperatorID = li.Fisher.FisherID,
                            VesselName = li.Boat,
                            TripID = Entities.TripViewModel.NextRecordNumber,
                            GearCode = li.Gear.Code,
                            Notes = li.Notes,
                        };
                    }
                    else
                    {
                        t = new TripEdited(_logbookImage.Trip);
                        t.DateTimeDeparture = (DateTime)li.Start;
                        t.DateTimeArrival = (DateTime)li.End;
                        t.GPS = li.GPS;
                        t.OperatorID = li.Fisher.FisherID;
                        t.VesselName = li.Boat;
                        //t.TripID = Entities.TripViewModel.NextRecordNumber;
                        t.GearCode = li.Gear.Code;
                        t.Notes = li.Notes;
                    }

                    bool tripHasMatchingTrack = Entities.TripViewModel.SetTrackOfTrip(t);
                    //if (Entities.TripViewModel.SetTrackOfTrip(t))
                    //{
                    Trip trip = new Trip
                    {
                        VesselName = t.VesselName,
                        OperatorID = t.OperatorID,
                        DateTimeArrival = t.DateTimeArrival,
                        DateTimeDeparture = t.DateTimeDeparture,
                        GPS = t.GPS,
                        TripID = t.TripID,
                        Gear = Entities.GearViewModel.GetGear(t.GearCode),
                        OtherGear = t.OtherGear,
                        DeviceID = t.GPS.DeviceID,
                        //Track = t.Track,
                        Notes = t.Notes
                        //GPXFileName = t.Track.FileName,
                        //XML = t.Track.XMLString
                    };

                    if (tripHasMatchingTrack)
                    {
                        trip.Track = t.Track;
                        trip.GPXFileName = t.Track.FileName;
                        trip.XML = t.Track.XMLString;
                    }


                    var result = Entities.TripViewModel.ValidateTrip(trip, true);
                    if (result.ErrorMessage.Length == 0)
                    {
                        if (isNew)
                        {
                            if (Entities.TripViewModel.AddRecordToRepo(trip))
                            {
                                _selectedTrip = trip;
                                li.Trip = trip;
                                if (Entities.LogbookImageViewModel.AddRecordToRepo(li))
                                {

                                    AddSavedImageToTree(li);
                                    //dtPickerStart.Value = null;
                                    dtPickerEnd.Value = null;
                                    textImageNotes.Text = "";

                                }
                                success = true;
                            }
                        }
                        else
                        {
                            if (Entities.TripViewModel.UpdateRecordInRepo(trip))
                            {
                                li.Trip = trip;
                                if (Entities.LogbookImageViewModel.UpdateRecordInRepo(li))
                                {
                                    success = true;
                                }
                            }
                        }

                    }
                    else
                    {
                        MessageBox.Show(result.ErrorMessage, "GPX Manager", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    //}
                }
                else
                {
                    MessageBox.Show(validationReult.ErrorMessage, "GPX Manager", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("Please prvovide required items", "GPX Manager", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            return success;
        }
        private void ImageRotate(int rotationDegrees)
        {
            if (_src != null)
            {
                imagePreview.Source = Entities.LogbookImageViewModel.ImageRotate(_src, rotationDegrees);
            }
        }



        public static bool ValidateTripWaypointsFromDataGrid(DataGrid dg)
        {
            Dictionary<int, string> rowErrors = new Dictionary<int, string>();
            foreach (TripWaypoint trip in dg.Items)
            {
                string rowError = "";
                if (trip.WaypointName.Length == 0)
                {
                    rowError += "Waypoint name cannot be empty\r\n";
                }
                if (trip.WaypointType.Length == 0)
                {
                    rowError += "Waypoint type cannot be empty\r\n";
                }
                if (trip.SetNumber == 0)
                {
                    rowError += "Set number cannot be zero\r\n";
                }
            }


            return rowErrors.Count == 0;
        }

        public static bool SaveTripWaypointsFromDataGrid(DataGrid dg)
        {
            int saveCount = 0;

            try
            {
                //validate all trips in the datagrid and only proceed if there are no error messages
                string errorMessage = "";
                try
                {
                    foreach (TripWaypoint item in dg.Items)
                    {
                        if (item.WaypointName.Length > 0 && item.WaypointType.Length > 0)
                        {
                            var result = Entities.TripWaypointViewModel.ValidateTripWaypoint(item, item.RowID == 0);
                            errorMessage += result.ErrorMessage;
                        }
                    }
                }
                catch (InvalidCastException)
                {
                    //ignore
                }

                if (errorMessage.Length == 0)
                {
                    try
                    {
                        foreach (TripWaypoint item in dg.Items)
                        {

                            if (item.WaypointName != null && item.WaypointName.Length > 0 && item.WaypointType.Length > 0)
                            {
                                if (Entities.TripWaypointViewModel.GetTripWaypoint(item.WaypointName, item.Trip.TripID) == null)
                                {
                                    item.RowID = Entities.TripWaypointViewModel.NextRecordNumber;
                                    if (Entities.TripWaypointViewModel.AddRecordToRepo(item))
                                    {
                                        saveCount++;
                                    }
                                }
                                else
                                {
                                    if (Entities.TripWaypointViewModel.UpdateRecordInRepo(item, fromGridEidtor: true))
                                    {
                                        saveCount++;
                                    }
                                }
                            }

                        }
                    }
                    catch (InvalidCastException)
                    {
                        //ignore
                    }
                }
                else
                {
                    MessageBox.Show(errorMessage, "GPX Manager", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }

            if (saveCount > 0)
            {
                MessageBox.Show($"Saved {saveCount} trip waypoints", "GPX Manager", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            return saveCount > 0;
        }

        public void ChildFormClosed()
        {
            this.Focus();
        }
        private async void OnButtonClick(object sender, RoutedEventArgs e)
        {
            switch (((Button)sender).Name)
            {
                case "buttonBackupCTXFiles":
                    Entities.CTXFileViewModel.CopyCTXToBackupFolder();
                    break;

                case "buttonLandingSiteDelete":
                    break;

                case "buttonLandingSiteEdit":
                    ShowSelectedLandingSite();
                    break;

                case "buttonLandingSiteAdd":
                    var lsew = LandingSiteEditWindow.GetInstance();
                    lsew.IsNew = true;
                    lsew.Owner = this;
                    if (lsew.Visibility == Visibility.Visible)
                    {
                        lsew.BringIntoView();
                    }
                    else
                    {
                        lsew.Show();
                    }
                    break;
                case "buttonDownloadSelected":

                    var forDownloading = new List<CTXFIle>();
                    var downloadedCount = 0;
                    foreach (var item in cybertrackerGridAvailableFiles.Items)
                    {
                        if (!((CTXFIle)item).IsDownloaded && ((CTXFIle)item).DownloadFile)
                        {
                            forDownloading.Add((CTXFIle)item);
                        }
                        else if (((CTXFIle)item).IsDownloaded)
                        {
                            downloadedCount++;
                        }
                    }
                    if (forDownloading.Count > 0)
                    {
                        VistaFolderBrowserDialog fbd = new VistaFolderBrowserDialog();
                        fbd.UseDescriptionForTitle = true;
                        fbd.SelectedPath = Global.Settings.CTXDownloadFolder;
                        fbd.Description = "Locate download folder for cybertracker data files";
                        if ((bool)fbd.ShowDialog() && fbd.SelectedPath.Length > 0)
                        {
                            buttonDownloadSelected.IsEnabled = false;
                            Entities.CTXFileViewModel.XMLFileFromCTXCreated += CTXFileViewModel_XMLFileFromCTXCreated;
                            statusProgress.Visibility = Visibility.Visible;
                            statusProgress.Maximum = forDownloading.Count;
                            statusProgress.Value = 0;
                            await Entities.CTXFileViewModel.DownloadFromServerAsync(forDownloading, fbd.SelectedPath);
                            cybertrackerGridAvailableFiles.Items.Refresh();
                            Entities.CTXFileViewModel.XMLFileFromCTXCreated -= CTXFileViewModel_XMLFileFromCTXCreated;
                            statusProgress.Visibility = Visibility.Collapsed;
                            MessageBox.Show($"Finished downloading {forDownloading.Count} files", "GPX Manager", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    else
                    {
                        if (downloadedCount == cybertrackerGridAvailableFiles.Items.Count)
                        {
                            MessageBox.Show("All files are already downloaded", "GPX Manager", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show("Select at least one file for downloading", "GPX Manager", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    buttonDownloadSelected.IsEnabled = true;
                    break;
                case "buttonCheckAll":
                    foreach (var item in cybertrackerGridAvailableFiles.Items)
                    {
                        ((CTXFIle)item).DownloadFile = true;
                    }
                    cybertrackerGridAvailableFiles.Items.Refresh();
                    break;
                case "buttonConnectFTP":
                    buttonConnectFTP.IsEnabled = false;
                    if (await Entities.CTXFileViewModel.GetFileListInServerAsync(
                         cybertrackerTextBoxFolderName.Text,
                         cybertrackerTextBoxUserName.Text,
                         cybertrackerTextBoxPassword.Password))
                    {

                        if (Entities.CTXFileViewModel.FilesInServer != null && Entities.CTXFileViewModel.FilesInServer.Count > 0)
                        {
                            cybertrackerGridAvailableFiles.DataContext = Entities.CTXFileViewModel.FilesInServer;
                            cybertrackerPanelGridFilesAvailable.Visibility = Visibility.Visible;
                        }
                        else if (Entities.CTXFileViewModel.LastRepositoryError().Length > 0)
                        {
                            MessageBox.Show(Entities.CTXFileViewModel.LastRepositoryError(), "GPX Manager", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    else
                    {
                        if(Entities.CTXFileViewModel.LastRepositoryError().Length>0)
                        {
                            MessageBox.Show(Entities.CTXFileViewModel.LastRepositoryError(),"GPX Manager",MessageBoxButton.OK,MessageBoxImage.Information);
                        }
                    }
                    buttonConnectFTP.IsEnabled = true;
                    break;

                case "buttonSaveWaypointTripEdits":
                    if (SaveTripWaypointsFromDataGrid(dataGridTripWaypoints))
                    {
                        _tripWaypointsDirty = false;
                        checkEditTripWaypoints.IsChecked = false;
                        buttonSaveWaypointTripEdits.IsEnabled = false;
                        dataGridGPSSummary.Items.Refresh();
                    }
                    break;
                case "buttonTripOfImage":
                    ShowEditTripWindow(false, _selectedTrip.TripID, _selectedTrip.OperatorID);
                    break;
                case "buttonCloseMetadata":
                    panelMetadata.Visibility = Visibility.Collapsed;
                    panelImageFields.Visibility = Visibility.Visible;
                    break;
                case "buttonIgnore":
                    if (_fileSelectedLogBookImage != null)
                    {
                        var li = new LogbookImage { Ignore = true, FileName = _fileSelectedLogBookImage.FullName };
                        if (Entities.LogbookImageViewModel.IgnoreImage(li))
                        {
                            AddSavedImageToTree(li, false);
                        }
                    }
                    break;
                case "buttonMetadata":
                    ShowImageMetadata();
                    break;
                case "buttonRegister":
                    if (RegisterLogBookImage())
                    {
                        MessageBox.Show("Image successfuly registered", "GPX Manager", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    break;
                case "buttonSelectFolder":
                    _rawImageFiles = await Entities.LogbookImageViewModel.GetImagesFromFolder();
                    ShowImageList();
                    imagePreview.Source = null;
                    tvItemNotRegistered.IsExpanded = true;
                    break;
                case "buttonFlipVertical":
                    ImageRotate(180);
                    break;
                case "buttonRotateLeft":
                    ImageRotate(270);
                    break;
                case "buttonRotateRight":
                    ImageRotate(90);
                    break;
                case "buttonFishersAdd":
                    EditFisherWindow efw = EditFisherWindow.GetInstance();
                    efw.Owner = this;
                    if (efw.Visibility == Visibility.Visible)
                    {
                        efw.BringIntoView();
                    }
                    else
                    {
                        efw.IsNew = true;
                        efw.Show();
                    }
                    break;
                case "buttonArchiveGPX":
                    ArchiveGPSData();
                    break;

                case "buttonEjectDevice":
                    EjectDevice();
                    break;

                case "buttonGPXDetails":
                    ShowGPXFileDetails();
                    break;


                case "buttonDeleteWaypoint":
                    if (Entities.TripWaypointViewModel.DeleteRecordFromRepo(_selectedTripWaypoint.RowID))
                    {
                        dataGridTripWaypoints.ItemsSource = Entities.TripWaypointViewModel.GetAllTripWaypoints(_selectedTrip.TripID);
                        _selectedTripWaypoint = null;
                    }
                    break;
                case "buttonAddTrip1":
                case "buttonAddTrip":
                    _gpxFile = null;
                    AddTrip();
                    break;
                case "buttonEditTrip1":
                case "buttonEditTrip":
                    ShowEditTripWindow(false, _selectedTrip.TripID, _selectedTrip.OperatorID);
                    break;

                case "buttonDeleteTrip":
                    if (Entities.TripViewModel.DeleteRecordFromRepo(_selectedTrip.TripID))
                    {
                        dataGridTrips.ItemsSource = Entities.TripViewModel.GetAllTrips(_deviceIdentifier);
                        stackPanelTripWaypoints.Visibility = Visibility.Collapsed;
                        buttonDeleteTrip.IsEnabled = false;
                        buttonEditTrip.IsEnabled = false;
                        _selectedTrip = null;
                    }
                    break;

                case "buttonOk":

                    break;

                case "buttonCancel":
                    break;

                case "buttonMakeGPSID":
                    GPSIDWindow gw = new GPSIDWindow();
                    gw.DetectedDevice = _detectedDevice;
                    if ((bool)gw.ShowDialog())
                    {
                        buttonMakeGPSID.Visibility = Visibility.Collapsed;
                        _gpsid = _detectedDevice.GPSID;
                        ShowGPSDetail();
                    }
                    break;

                case "buttonSave":
                    _gps.Device = _detectedDevice;
                    var result = Entities.GPSViewModel.ValidateGPS(_gps, _isNew, _oldGPSName, _oldGPSCode, fromArchive: _inArchive);
                    if (result.ErrorMessage.Length == 0)
                    {
                        Entities.GPSViewModel.AddRecordToRepo(_gps);
                        TreeViewItem selectedItem = (TreeViewItem)treeDevices.SelectedItem;
                        selectedItem.Header = _gps.DeviceName;
                        ((TreeViewItem)selectedItem.Items[0]).Header = $"{_detectedDevice.Disks[0].Caption}\\{_gps.Folder}";
                        ((TreeViewItem)selectedItem.Items[0]).Tag = "gpx_folder";
                        _detectedDevice.GPS = _gps;
                        Entities.GPXFileViewModel.GetFilesFromDevice(_detectedDevice);
                        //Entities.DeviceGPXViewModel.RefreshArchivedGPXCollection(_gps);
                        AddTripNode(selectedItem);
                        ShowGPXMonthNodes((TreeViewItem)selectedItem.Items[0], _gps);
                        buttonSave.Visibility = Visibility.Collapsed;
                        buttonEjectDevice.Visibility = Visibility.Visible;
                        selectedItem.IsExpanded = true;
                        _usbGPSPresent = true;
                        ShowGPSDetail();
                    }
                    else
                    {
                        MessageBox.Show(result.ErrorMessage, "Validation error", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    break;
            }
        }

        private void CTXFileViewModel_XMLFileFromCTXCreated(CTXFileViewModel s, WinSCP.TransferEventArgs e)
        {

            statusProgress.Dispatcher.BeginInvoke
                (DispatcherPriority.Normal, new DispatcherOperationCallback(delegate
                    {
                        statusProgress.Value++;
                        return null;
                    }), null);
        }

        private void SetGPXFileMenuMapVisibility(bool hideMapVisibilityMenu, bool refreshGrid = true)
        {
            if (hideMapVisibilityMenu)
            {
                menuGPXMap.Visibility = Visibility.Collapsed;
                menuGPXRemoveFromMap.Visibility = Visibility.Visible;
                menuGPXRemoveAllFromMap.Visibility = Visibility.Visible;
                menuGPXCenterInMap.Visibility = Visibility.Visible;
            }
            else
            {
                menuGPXMap.Visibility = Visibility.Visible;
                menuGPXRemoveFromMap.Visibility = Visibility.Collapsed;
                menuGPXRemoveAllFromMap.Visibility = Visibility.Collapsed;
                menuGPXCenterInMap.Visibility = Visibility.Collapsed;
            }

            if (refreshGrid)
            {
                dataGridGPXFiles.Items.Refresh();
            }
        }

        private void ShowTripMap(bool showInMap = true)
        {
            MapWindowManager.MapLayersWindow?.DisableGrid();
            int h = -1;
            List<int> handles = new List<int>();
            if (MapWindowForm.Instance == null)
            {
                MapWindowManager.OpenMapWindow(this, true);
            }
            if (MapWindowManager.Coastline == null)
            {
                MapWindowManager.LoadCoastline(MapWindowManager.CoastLineFile);
            }

            var datagrid = (DataGrid)LayerSelector;
            if (datagrid.SelectedItems.Count == 1)
            {
                List<Trip> trip = new List<Trip>();
                trip.Add((Trip)datagrid.SelectedItem);
                ((Trip)datagrid.SelectedItem).ShownInMap = TripMappingManager.MapTrip(trip);
            }

            SetGPXFileMenuMapVisibility(h > 0);
            datagrid.SelectedItems.Clear();
            datagrid.Items.Refresh();
            MapWindowManager.MapControl.Redraw();
            MapWindowManager.MapLayersWindow?.RefreshCurrentLayer();
            MapWindowManager.MapLayersWindow?.DisableGrid(false);
        }

        private void SetMapButtonsEnabled()
        {
            bool itemEnabled = Global.MapOCXInstalled;
            double buttonOpacity = .20d;
            buttonMap.IsEnabled = itemEnabled;
            menuMapper.IsEnabled = itemEnabled;
            menuCalendaredTripMap.IsEnabled = itemEnabled;
            menuGPXMap.IsEnabled = itemEnabled;
            menuGPXRemoveAllFromMap.IsEnabled = itemEnabled;
            menuGPXRemoveFromMap.IsEnabled = itemEnabled;
            menuTripMap.IsEnabled = itemEnabled;

            if (!itemEnabled)
            {
                buttonMap.Opacity = buttonOpacity;
                menuMapper.Opacity = buttonOpacity;
                menuCalendaredTripMap.Opacity = buttonOpacity;
                menuGPXMap.Opacity = buttonOpacity;
                menuGPXRemoveAllFromMap.Opacity = buttonOpacity;
                menuGPXRemoveFromMap.Opacity = buttonOpacity;
                menuTripMap.Opacity = buttonOpacity;
            }
        }

        private void ShowSelectedMonthGPXDataOnMap(string whatToShow)
        {
            var gpxFiles = new List<GPXFile>();
            switch (whatToShow)
            {
                case "track_wpt":
                    ShowSelectedMonthGPXDataOnMap(whatToShow: "track");
                    ShowSelectedMonthGPXDataOnMap(whatToShow: "wpt");
                    break;
                case "track":
                    gpxFiles = Entities.DeviceGPXViewModel.GetDeviceGPX(_gps, GPXFileType.Track, _archiveMonthYear);
                    break;
                case "wpt":
                    gpxFiles = Entities.DeviceGPXViewModel.GetDeviceGPX(_gps, GPXFileType.Waypoint, _archiveMonthYear);
                    break;
            }

            int h = -1;
            List<int> handles = new List<int>();
            foreach (var item in gpxFiles)
            {
                MapWindowManager.MapGPX(item, out h, out handles);
                item.ShapeIndexes = handles;
                item.ShownInMap = true;
            }
            if (h >= 0)
            {
                MapWindowManager.MapControl.Redraw();
            }
        }

        private void ShowGPXOnMap(bool showInMap = true)
        {
            int h = -1;
            List<int> handles = new List<int>();
            string coastLineFile = $@"{globalMapping.ApplicationPath}\Layers\Coastline\philippines_polygon.shp";
            MapWindowManager.OpenMapWindow(this, true);
            if (MapWindowManager.Coastline == null)
            {
                MapWindowManager.LoadCoastline(coastLineFile);
            }

            if (dataGridGPXFiles.SelectedItems.Count > 0)
            {
                foreach (var item in dataGridGPXFiles.SelectedItems)
                {
                    _gpxFile = (GPXFile)item;
                    MapWindowManager.MapGPX(_gpxFile, out h, out handles);
                    _gpxFile.ShapeIndexes = handles;
                    _gpxFile.ShownInMap = showInMap;
                }
            }
            SetGPXFileMenuMapVisibility(h > 0);
            dataGridGPXFiles.SelectedItems.Clear();
            MapWindowManager.MapControl.Redraw();
        }

        private void ShowTripGPXFileDetails(Trip trip, bool showAsXML = false)
        {
            if (_inArchive || Entities.WaypointViewModel.Waypoints.ContainsKey(_gps))
            {
                //var gpsWaypointSet = Entities.WaypointViewModel.Waypoints[_gps].Where(t => t.FullFileName == _gpxFile.FileInfo.FullName).FirstOrDefault();

                using (GPXFIlePropertiesWindow gpw = new GPXFIlePropertiesWindow
                {
                    ParentWindow = this,
                    Owner = this,
                    ShowAsXML = showAsXML
                    //GPSWaypointSet = gpsWaypointSet
                })
                {
                    GPXFile gpxFile = new GPXFile(trip.GPXFileName);
                    gpxFile.XML = trip.XML;
                    gpw.GPXFile = new GPXFile(trip.GPXFileName) { XML = trip.XML };
                    gpw.ShowDialog();
                }
            }
        }

        private void ShowGPXFileDetails(bool showAsXML = false)
        {
            if (_inArchive || Entities.WaypointViewModel.Waypoints.ContainsKey(_gps))
            {
                //var gpsWaypointSet = Entities.WaypointViewModel.Waypoints[_gps].Where(t => t.FullFileName == _gpxFile.FileInfo.FullName).FirstOrDefault();

                using (GPXFIlePropertiesWindow gpw = new GPXFIlePropertiesWindow
                {
                    ParentWindow = this,
                    GPXFile = _gpxFile,
                    Owner = this,
                    ShowAsXML = showAsXML
                    //GPSWaypointSet = gpsWaypointSet
                })
                {
                    gpw.ShowDialog();
                }
            }
        }

        private TreeViewItem AddTripNode(TreeViewItem parent)
        {
            TreeViewItem tripData = new TreeViewItem { Header = "Trip log", Tag = "trip_data" };
            parent.Items.Add(tripData);
            return tripData;
        }

        private void SelectBrandModel(ShowMode showMode, string brand = "")
        {
            using (var selectWindow = new GPSBrandModelWindow())
            {
                selectWindow.Owner = this;
                selectWindow.ShowMode = showMode;
                selectWindow.Brand = brand;
                if ((bool)selectWindow.ShowDialog())
                {
                    switch (showMode)
                    {
                        case ShowMode.ShowModeBrand:
                            _cboBrand.ItemsSource = Entities.GPSViewModel.GPSBrands;
                            break;

                        case ShowMode.ShowModeModel:
                            _cboModel.ItemsSource = Entities.GPSViewModel.GPSModels;
                            break;
                    }
                }
            }
        }

        private void ShowCalendarTree()
        {
            labelTitle.Content = "Calendar of tracked fishing operations by GPS";
            treeCalendar.Visibility = Visibility.Visible;
            var root = (TreeViewItem)treeCalendar.Items[0];
            var gps_root = (TreeViewItem)treeCalendar.Items[1];

            root.Items.Clear();
            gps_root.Items.Clear();

            foreach (var month in Entities.TripViewModel.GetMonthYears().OrderBy(t => t.Date))
            {
                root.Items.Add(new TreeViewItem { Header = month.ToString("MMM-yyyy"), Tag = month });
            }
            root.IsExpanded = true;


            var deviceGPSList = Entities.DeviceGPXViewModel.GetAllGPS().OrderBy(t => t.DeviceName).ToList();
            foreach (var gps in deviceGPSList)
            {
                Entities.GPSViewModel.AddToCollection(gps);
            }


            foreach (var gps in Entities.GPSViewModel.GPSCollection.OrderBy(t => t.DeviceName))
            {
                gps_root.Items.Add(new TreeViewItem { Header = gps.DeviceName, Tag = gps });
            }


            if (root.Items.Count > 0)
            {
                ((TreeViewItem)root.Items[0]).IsSelected = true;
            }

            if (gps_root.Items.Count > 0)
            {
                foreach (TreeViewItem gpsItem in gps_root.Items)
                {
                    var gps = deviceGPSList.FirstOrDefault(t => t.DeviceID == gpsItem.Tag.ToString());
                    if (gps != null)
                    {
                        foreach (var month in Entities.TripViewModel.TripArchivesByMonth(gps).Keys)
                        {
                            string monthName = month.ToString("MMM-yyyy");
                            TreeViewItem tvi = new TreeViewItem { Header = monthName, Tag = "month_archive" };
                            gpsItem.Items.Add(tvi);
                        }
                        gpsItem.IsExpanded = true;
                    }
                }
            }

            gps_root.IsExpanded = true;

            if (gps_root.Items.Count == 0 && root.Items.Count == 0)
            {
                labelNoData.Visibility = Visibility.Visible;

                labelNoData.Content = "There are no trips saved in the database";

                labelTitle.Visibility = Visibility.Hidden;
                treeCalendar.Visibility = Visibility.Collapsed;
            }
        }

        private void HideTrees()
        {
            treeCalendar.Visibility = Visibility.Collapsed;
            treeDevices.Visibility = Visibility.Collapsed;
            treeArchive.Visibility = Visibility.Collapsed;
        }

        private void ShowMap()
        {
            if (Global.AppProceed)
            {
                MapWindowManager.OpenMapWindow(this);
            }
            else
            {
                MessageBox.Show("Application need to be setp first", "GPX Manager", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void EjectDevice()
        {
            string outMessage;
            if (Entities.DetectedDeviceViewModel.EjectDrive(_detectedDevice, out outMessage))
            {
                //if (Entities.GPSViewModel.RemoveByEject(_detectedDevice.GPS))
                //{
                TreeViewItem tvi = treeDevices.SelectedItem as TreeViewItem;
                tvi.IsSelected = false;
                tvi.Items.Clear();
                ((TreeViewItem)treeDevices.Items[0]).Items.Remove(tvi);
                //}
            }
            MessageBox.Show(outMessage, "GPX Manager", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LocateMatchingTrackFromWaypoints()
        {
            foreach (var wpt in _gpxFile.NamedWaypointsInLocalTime)
            {
                foreach (GPXFile item in dataGridGPXFiles.Items)
                {
                    if (item.TrackCount > 0 && item.DateRangeStart < wpt.Time && item.DateRangeEnd > wpt.Time)
                    {
                        dataGridGPXFiles.SelectedItem = item;
                        //break;
                    }
                }
            }
        }

        //public void ImportGPX()
        //{
        //    string msg = "Import successful";
        //    if (ImportGPSData.ImportGPX())
        //    {
        //        ShowArchive();
        //        msg += $"\r\n{ImportGPSData.ImportMessage}";
        //    }
        //    else
        //    {
        //        msg = ImportGPSData.ImportMessage;
        //    }

        //    if(msg==null || msg.Length==0)
        //    {
        //        msg = "No GPX data was imported";
        //    }

        //     MessageBox.Show(msg, "GPX Manager", MessageBoxButton.OK, MessageBoxImage.Information);

        //}

        private void ImportGPS()
        {
            string msg = "Import successful";
            if (ImportGPSData.ImportGPS())
            {
                ShowArchive();
                msg += $"\r\n{ImportGPSData.ImportMessage}";
            }
            else
            {
                if (ImportGPSData.ImportCount == 0)
                {
                    msg = "No GPS data was imported";
                }
                else
                {
                    msg = ImportGPSData.ImportMessage;
                }
            }
            if (msg != null && msg.Length > 0)
            {
                MessageBox.Show(msg, "GPX Manager", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ShowAboutWindow()
        {
            AboutWindow aw = new AboutWindow();
            aw.Owner = this;
            aw.ShowDialog();
        }

        private void OnMenuClick(object sender, RoutedEventArgs e)
        {
            string menuName = ((MenuItem)sender).Name;
            switch (menuName)
            {
                case "menuAddTripUsingGPS":

                    //EditTripWindow etw = EditTripWindow.GetInstance();
                    //etw.IsNew = true;
                    //etw.GPS = _gps;
                    //etw.ParentWindow = this;
                    //if (etw.Visibility == Visibility.Visible)
                    //{
                    //    etw.BringIntoView();
                    //}
                    //else
                    //{
                    //    etw.Show();
                    //}
                    AddTrip();
                    break;
                case "menuGetTimeFromTrack":

                    break;
                case "menuShowAllGPXOnMap":
                    var showAll = new ShowAllGPSDataInArchiveWindow();
                    showAll.ParentForm = MapWindowForm.Instance;
                    showAll.Owner = MapWindowForm.Instance;
                    showAll.Show();
                    break;
                case "menuMapGPSMonthWaypopintData":
                    ShowSelectedMonthGPXDataOnMap(whatToShow: "wpt");
                    break;
                case "menuMapGPSMonthTrackData":
                    ShowSelectedMonthGPXDataOnMap(whatToShow: "track");
                    break;
                case "menuMapGPSMonthData":
                    ShowSelectedMonthGPXDataOnMap(whatToShow: "track_wpt");
                    break;
                case "menuImportGPXByFolder":
                    ImportGPXByFolderWindow iw = new ImportGPXByFolderWindow();
                    iw.ParentForm = this;
                    iw.ShowDialog();
                    break;
                case "menuGPXCenterInMap":
                    break;

                case "menuOpenBackupFolder":
                    Entities.DeviceGPXViewModel.CheckGPXBackupFolder();
                    Process.Start($@"{Global.Settings.ComputerGPXFolder}\{Entities.DeviceGPXViewModel.GPXBackupFolder}");
                    break;

                case "menuBackupGPX":
                    var backupCount = Entities.DeviceGPXViewModel.BackupGPXToDrive();
                    var msg = "No files were backed up;";
                    if (backupCount > 0)
                    {
                        msg = $"{backupCount} {(backupCount == 1 ? "file" : "files")} saved to backup folder in your comnputer";
                    }
                    MessageBox.Show(msg, "GPX Manager", MessageBoxButton.OK, MessageBoxImage.Information);
                    break;

                case "menuFileImportGPX":
                case "menuImportGPX":
                    //ImportGPX();
                    break;

                case "menuFileImportGPS":
                case "menuImportGPS":
                    ImportGPS();
                    break;

                case "menuGPXFileLocateTrack":
                    LocateMatchingTrackFromWaypoints();
                    break;

                case "menuCalendaredTripViewGPXDetails":
                    ShowTripGPXFileDetails(_selectedTrip);
                    break;

                case "menuCalendaredTripViewGPX":
                    ShowTripGPXFileDetails(_selectedTrip, true);

                    break;

                case "menuViewTripGPX":
                    break;

                case "menuGPXFileView":
                    ShowGPXFileDetails(true);
                    break;

                case "menuHelpAbout":
                    ShowAboutWindow();
                    break;

                case "menuCalendaredTripMap":
                case "menuTripMap":
                    ShowTripMap();
                    break;

                case "menuArchive":
                    ShowArchive();
                    break;

                case "menuEjectDevice":
                    EjectDevice();
                    break;

                case "menuGPXRemoveAllFromMap":
                    if (dataGridGPXFiles.SelectedItems.Count > 0)
                    {
                        MapWindowManager.RemoveGPSDataFromMap();
                        dataGridGPXFiles.Items.Refresh();
                    }
                    break;

                case "menuGPXRemoveFromMap":
                    _gpxFile.ShownInMap = false;
                    MapWindowManager.MapControl.Redraw();
                    SetGPXFileMenuMapVisibility(false);
                    break;

                case "menuGPXMap":
                    ShowGPXOnMap();
                    break;

                case "menuMapper":
                    ShowMap();
                    break;

                case "menuGPXFileDetails":
                    ShowGPXFileDetails();
                    break;

                case "menuClearTables":
                    string result;
                    if (Entities.ClearTables())
                    {
                        result = "All tables cleared";
                    }
                    else
                    {
                        result = "Not all tables cleared";
                    }
                    MessageBox.Show(result, "GPX Manager", MessageBoxButton.OK, MessageBoxImage.Information);
                    _usbGPSPresent = false;
                    break;

                case "menuAddTripFromTRack":
                    AddTrip();
                    break;

                case "menuTripCalendar":
                    ShowTripCalendar();

                    break;

                case "menuSaveGPS":
                case "menuSaveTrips":
                    string dialogTitle = "";
                    switch (menuName)
                    {
                        case "menuSaveGPS":
                            dialogTitle = "Save GPS devices to an XML file";
                            break;

                        case "menuSaveTrips":
                            dialogTitle = "Save trips to an XML file";
                            break;
                    }

                    SaveFileDialog sfd = new SaveFileDialog();
                    sfd.Title = dialogTitle;
                    sfd.DefaultExt = ".xml";
                    sfd.Filter = "XML|*.xml|Text|*.txt";
                    sfd.FilterIndex = 1;

                    if ((bool)sfd.ShowDialog() && sfd.FileName.Length > 0)
                    {
                        switch (menuName)
                        {
                            case "menuSaveGPS":
                                Entities.GPSViewModel.Serialize(sfd.FileName);
                                break;

                            case "menuSaveTrips":
                                Entities.TripViewModel.Serialize(sfd.FileName);
                                break;
                        }
                    }
                    break;

                case "menuOptions":
                    ShowSettingsWindow();
                    break;

                case "menuGPSBrands":
                    SelectBrandModel(ShowMode.ShowModeBrand);
                    break;

                case "menuGPSModels":
                    SelectBrandModel(ShowMode.ShowModeModel);
                    break;

                case "menuCloseApp":
                    Close();
                    break;

                case "menuScanDevices":
                    HideTrees();
                    treeDevices.Visibility = Visibility.Visible;
                    ScanUSBDevices();
                    break;

                case "menuGPXFolder":
                    LocateGPXFolder();
                    break;
            }
        }

        private void ShowTripCalendar()
        {
            if (Global.AppProceed)
            {
                _inArchive = true;
                HideTrees();
                ResetView();
                treeRowCalendar.Height = new GridLength(1, GridUnitType.Star);
                ShowCalendarTree();
            }
            else
            {
                MessageBox.Show("Application need to be setup first", "GPX Manager", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ShowSettingsWindow()
        {
            using (var settingsWindow = new SettingsWindow())
            {
                settingsWindow.Owner = this;
                settingsWindow.ParentWindow = this;
                if ((bool)settingsWindow.ShowDialog() && Global.AppProceed)
                {
                    SetupEntities();
                }
            }
        }

        private void LocateGPXFolder()
        {
            if (_detectedDevice != null)
            {
                VistaFolderBrowserDialog fbd = new VistaFolderBrowserDialog();
                fbd.UseDescriptionForTitle = true;
                fbd.Description = "Locate GPX folder in selected device";
                if (Directory.Exists($"{_detectedDevice.Disks[0].Caption}\\{Global.Settings.DeviceGPXFolder}"))
                {
                    fbd.SelectedPath = $"{_detectedDevice.Disks[0].Caption}\\{Global.Settings.DeviceGPXFolder}";
                }
                if ((bool)fbd.ShowDialog() && fbd.SelectedPath.Length > 0)
                {
                    foreach (xceedPropertyGrid.PropertyItem prp in gpsPropertiesGrid.Properties)
                    {
                        if (prp.DisplayName == "Folder")
                        {
                            var path = fbd.SelectedPath.Substring(3);
                            prp.Value = path;
                            if (path != Global.Settings.DeviceGPXFolder)
                            {
                                Global.Settings.DeviceGPXFolder = path;
                                Global.Settings.Save(Global._DefaultSettingspath);
                            }
                        }
                    }
                }
            }
        }

        private void ScanUSBDevices()
        {
            _inArchive = false;
            ((TreeViewItem)treeDevices.Items[0]).Items.Clear();
            ResetView();
            treeRowUSB.Height = new GridLength(1, GridUnitType.Star);
            ConfigureGPXGrid();
            if (ReadUSBDrives())
            {
                foreach (var device in Entities.DetectedDeviceViewModel.DetectedDeviceCollection)
                {
                    var tvi = new TreeViewItem
                    {
                        Header = $"{device.Caption} ({device.SerialNumber})"
                    };

                    if (device.GPSID == null || device.GPSID.Length == 0)
                    {
                        tvi.Tag = device.PNPDeviceID;
                    }
                    else
                    {
                        tvi.Tag = device.GPSID;
                    }

                    if (device.GPSID != null && device.GPSID.Length > 0 && device.GPS != null)
                    {
                        var gpsItem = new TreeViewItem
                        {
                            Header = $"{device.Disks[0].Caption}\\{device.GPS.Folder}",
                            Tag = "gpx_folder",
                        };
                        tvi.Header = device.GPS.DeviceName;
                        tvi.Tag = device.GPSID;
                        tvi.Items.Add(gpsItem);
                        AddTripNode(tvi);
                        GetGPXFiles(device);
                        ShowGPXMonthNodes(gpsItem, device.GPS);
                        _usbGPSPresent = true;
                    }
                    else
                    {
                        foreach (var disk in device.Disks)
                        {
                            var subItem = new TreeViewItem
                            {
                                Header = $"Drive {disk.Caption}",
                                Tag = "disk"
                            };
                            tvi.Items.Add(subItem);
                        }
                    }
                    tvi.IsExpanded = true;
                    TreeViewItem root = (TreeViewItem)treeDevices.Items[0];
                    if (root.Items.Count == 0)
                    {
                        root.Items.Add(tvi);
                    }
                    else
                    {
                        if (!TreeItemExists(root, tvi))
                        {
                            root.Items.Add(tvi);
                        }
                    }
                }
                ((TreeViewItem)treeDevices.Items[0]).IsExpanded = true;
            }
        }

        private void ShowGPXMonthNodes(TreeViewItem parent, GPS gps)
        {
            foreach (var item in Entities.GPXFileViewModel.FilesByMonth(gps).Keys)
            {
                int h = parent.Items.Add(new TreeViewItem { Header = item.ToString("MMM-yyyy") });
                TreeViewItem monthNode = parent.Items[h] as TreeViewItem;
                monthNode.Tag = "month_node";
            }
            parent.IsExpanded = true;
        }

        private bool TreeItemExists(TreeViewItem parent, TreeViewItem testItem)
        {
            foreach (TreeViewItem item in parent.Items)
            {
                var device = Entities.DetectedDeviceViewModel.GetDevice(testItem.Tag.ToString());
                if (item.Tag.ToString() == device.PNPDeviceID)
                {
                    return true;
                }
            }
            return false;
        }

        private void SetupDevicePropertyGrid()
        {
            gpsPropertiesGrid.PropertyDefinitions.Add(new xceedPropertyGrid.PropertyDefinition { Name = "DeviceName", DisplayName = "Device name", DisplayOrder = 1, Description = "Name assigned to the device" });

            var definition = new xceedPropertyGrid.PropertyDefinition { Name = "Code", DisplayName = "Device code", DisplayOrder = 2, Description = "Code assigned to the device" };
            gpsPropertiesGrid.PropertyDefinitions.Add(definition);

            gpsPropertiesGrid.PropertyDefinitions.Add(new xceedPropertyGrid.PropertyDefinition { Name = "Brand", DisplayName = "Brand", DisplayOrder = 3, Description = "Brand of device" });
            gpsPropertiesGrid.PropertyDefinitions.Add(new xceedPropertyGrid.PropertyDefinition { Name = "Model", DisplayName = "Model", DisplayOrder = 4, Description = "Model of device" });
            gpsPropertiesGrid.PropertyDefinitions.Add(new xceedPropertyGrid.PropertyDefinition { Name = "Folder", DisplayName = "Folder", DisplayOrder = 5, Description = "Folder where GPX files are saved" });
            gpsPropertiesGrid.PropertyDefinitions.Add(new xceedPropertyGrid.PropertyDefinition { Name = "DeviceID", DisplayName = "Device ID", DisplayOrder = 6, Description = "Identifier of device" });

            foreach (xceedPropertyGrid.PropertyItem prp in gpsPropertiesGrid.Properties)
            {
                switch (prp.DisplayName)
                {
                    case "Brand":
                        prp.Editor = _cboBrand;
                        break;

                    case "Model":
                        prp.Editor = _cboModel;
                        break;
                }
            }
        }

        private void NewGPS()
        {
            _isNew = true;
            _cboBrand.SelectedItem = null;
            _cboModel.ItemsSource = null;

            labelTitle.Content = "Add this USB storage as a GPS to the database";

            if (_gpsid != null && _gpsid.Length > 0)
            {
                _gps = new GPS
                {
                    DeviceID = _gpsid
                };

                gridRowGPS.Height = new GridLength(1, GridUnitType.Star);
                gpsPanel.Visibility = Visibility.Visible;
                buttonEjectDevice.Visibility = Visibility.Collapsed;
                gpsPropertiesGrid.SelectedObject = _gps;
                SetupDevicePropertyGrid();
                buttonSave.Visibility = Visibility.Visible;
                buttonSave.IsEnabled = true;
            }
            else
            {
                labelTitle.Content = "A required gpsid file is missing";
                buttonMakeGPSID.Visibility = Visibility.Visible;
                gridRowHeader.Height = new GridLength(1, GridUnitType.Star);
            }
        }

        private void ConfigureGPXGrid(bool fromDevice = true)
        {
            DataGridTextColumn col;
            dataGridGPXFiles.Columns.Clear();
            dataGridGPXFiles.AutoGenerateColumns = false;

            dataGridGPXFiles.Columns.Add(new DataGridTextColumn { Header = "File name", Binding = new Binding("FileName") });
            dataGridGPXFiles.Columns.Add(new DataGridTextColumn { Header = "Date range", Binding = new Binding("DateRange") });

            dataGridGPXFiles.Columns.Add(new DataGridTextColumn { Header = "Waypoints", Binding = new Binding("WaypointCount") });
            dataGridGPXFiles.Columns.Add(new DataGridTextColumn { Header = "Tracks", Binding = new Binding("TrackCount") });
            dataGridGPXFiles.Columns.Add(new DataGridTextColumn { Header = "Track points", Binding = new Binding("TrackPointsCount") });
            dataGridGPXFiles.Columns.Add(new DataGridTextColumn { Header = "Time span", Binding = new Binding("TimeSpanHourMinute") });

            col = new DataGridTextColumn()
            {
                Binding = new Binding("TrackLength"),
                Header = "Length (km)"
            };
            col.Binding.StringFormat = "N3";
            dataGridGPXFiles.Columns.Add(col);

            dataGridGPXFiles.Columns.Add(new DataGridTextColumn { Header = "Trips", Binding = new Binding("TripCount") });
            dataGridGPXFiles.Columns.Add(new DataGridCheckBoxColumn { Header = "Mapped", Binding = new Binding("ShownInMap") });

            if (fromDevice)
            {
                dataGridGPXFiles.Columns.Add(new DataGridCheckBoxColumn { Header = "Archived", Binding = new Binding("IsArchived") });
                dataGridGPXFiles.Columns.Add(new DataGridTextColumn { Header = "Size", Binding = new Binding("SizeFormatted") });

                col = new DataGridTextColumn()
                {
                    Binding = new Binding("TimeStampUTC"),
                    Header = "Date created"
                };
                col.Binding.StringFormat = "MMM-dd-yyyy HH:mm";
                dataGridGPXFiles.Columns.Add(col);

                col = new DataGridTextColumn()
                {
                    Binding = new Binding("DateModifiedUTC"),
                    Header = "Date modified"
                };
                col.Binding.StringFormat = "MMM-dd-yyyy HH:mm";
                dataGridGPXFiles.Columns.Add(col);
            }
        }

        private void ConfigureGrids()
        {
            DataGridTextColumn col;

            //setup trip data grid
            dataGridTrips.AutoGenerateColumns = false;
            dataGridTrips.Columns.Add(new DataGridTextColumn { Header = "Trip ID", Binding = new Binding("TripID") });
            dataGridTrips.Columns.Add(new DataGridTextColumn { Header = "Operator", Binding = new Binding("OperatorName") });
            dataGridTrips.Columns.Add(new DataGridTextColumn { Header = "Fishing vessel", Binding = new Binding("VesselName") });
            dataGridTrips.Columns.Add(new DataGridTextColumn { Header = "Gear", Binding = new Binding("Gear.Name") });
            dataGridTrips.Columns.Add(new DataGridTextColumn { Header = "Other gear", Binding = new Binding("OtherGear") });

            col = new DataGridTextColumn()
            {
                Binding = new Binding("DateTimeDeparture"),
                Header = "Departure"
            };
            col.Binding.StringFormat = "MMM-dd-yyyy HH:mm";
            dataGridTrips.Columns.Add(col);

            col = new DataGridTextColumn()
            {
                Binding = new Binding("DateTimeArrival"),
                Header = "Arrival"
            };
            col.Binding.StringFormat = "MMM-dd-yyyy HH:mm";
            dataGridTrips.Columns.Add(col);

            dataGridTrips.Columns.Add(new DataGridTextColumn { Header = "Track source GPX", Binding = new Binding("Track.FileName") });
            dataGridTrips.Columns.Add(new DataGridTextColumn { Header = "Waypoints", Binding = new Binding("WaypointCount") });
            dataGridTrips.Columns.Add(new DataGridTextColumn { Header = "Summary (Length:km Duration Hours:Minutes)", Binding = new Binding("TrackSummary") });


            dataGridGPSSummary.AutoGenerateColumns = false;
            dataGridGPSSummary.Columns.Add(new DataGridTextColumn { Header = "Trip ID", Binding = new Binding("TripID") });
            dataGridGPSSummary.Columns.Add(new DataGridTextColumn { Header = "GPS", Binding = new Binding("GPS.DeviceName") });
            dataGridGPSSummary.Columns.Add(new DataGridTextColumn { Header = "Name of operator", Binding = new Binding("Operator.Name") });
            dataGridGPSSummary.Columns.Add(new DataGridTextColumn { Header = "Name of fishing vessel", Binding = new Binding("VesselName") });
            dataGridGPSSummary.Columns.Add(new DataGridTextColumn { Header = "Gear", Binding = new Binding("Gear.Name") });
            dataGridGPSSummary.Columns.Add(new DataGridTextColumn { Header = "Other gear", Binding = new Binding("OtherGear") });

            col = new DataGridTextColumn()
            {
                Binding = new Binding("DateTimeDeparture"),
                Header = "Date and time departed"
            };
            col.Binding.StringFormat = "MMM-dd-yyyy HH:mm";
            dataGridGPSSummary.Columns.Add(col);

            col = new DataGridTextColumn()
            {
                Binding = new Binding("DateTimeArrival"),
                Header = "Date and time arrived"
            };
            col.Binding.StringFormat = "MMM-dd-yyyy HH:mm";
            dataGridGPSSummary.Columns.Add(col);

            dataGridGPSSummary.Columns.Add(new DataGridTextColumn { Header = "Waypoints", Binding = new Binding("WaypointCount") });
            dataGridGPSSummary.Columns.Add(new DataGridTextColumn { Header = "Summary (Length:km Duration Hours:Minutes)", Binding = new Binding("TrackSummary") });
            dataGridGPSSummary.Columns.Add(new DataGridCheckBoxColumn { Header = "Mapped", Binding = new Binding("ShownInMap") });


            dataGridFishers.AutoGenerateColumns = false;
            dataGridFishers.Columns.Add(new DataGridTextColumn { Header = "Fisher ID", Binding = new Binding("FisherID") });
            dataGridFishers.Columns.Add(new DataGridTextColumn { Header = "Nane", Binding = new Binding("Name") });
            dataGridFishers.Columns.Add(new DataGridTextColumn { Header = "Landing site", Binding = new Binding("LandingSite") });
            dataGridFishers.Columns.Add(new DataGridTextColumn { Header = "Vessels", Binding = new Binding("VesselListCSV") });
            dataGridFishers.Columns.Add(new DataGridTextColumn { Header = "Gears", Binding = new Binding("GearList") });
            dataGridFishers.Columns.Add(new DataGridTextColumn { Header = "Type of device", Binding = new Binding("DeviceTypeString") });
            dataGridFishers.Columns.Add(new DataGridTextColumn { Header = "Identifier", Binding = new Binding("DeviceIdentifier") });


            dataGridGPXSummary.AutoGenerateColumns = false;
            dataGridGPXSummary.Columns.Add(new DataGridTextColumn { Header = "GPS", Binding = new Binding("GPS.DeviceName") });
            dataGridGPXSummary.Columns.Add(new DataGridTextColumn { Header = "# of tracks", Binding = new Binding("NumberOfSavedTracks") });
            dataGridGPXSummary.Columns.Add(new DataGridTextColumn { Header = "# of waypoints", Binding = new Binding("NumberOfSavedWaypoints") });
            dataGridGPXSummary.Columns.Add(new DataGridTextColumn { Header = "# of tracks 500m +", Binding = new Binding("NumberTrackLength500m") });
            dataGridGPXSummary.Columns.Add(new DataGridTextColumn { Header = "# of tracks 500m -", Binding = new Binding("NumberTrackLengthLess500m") });

            cybertrackerGridAvailableFiles.AutoGenerateColumns = false;
            cybertrackerGridAvailableFiles.CanUserAddRows = false;
            cybertrackerGridAvailableFiles.Columns.Add(new DataGridTextColumn { Header = "Name", Binding = new Binding("RemoteFileInfo.Name"), IsReadOnly = true });

            col = new DataGridTextColumn()
            {
                Binding = new Binding("RemoteFileInfo.LastWriteTime"),
                Header = "Date modified",
                IsReadOnly = true
            };
            col.Binding.StringFormat = "MMM-dd-yyyy HH:mm";
            cybertrackerGridAvailableFiles.Columns.Add(col);

            cybertrackerGridAvailableFiles.Columns.Add(new DataGridTextColumn { Header = "Size", Binding = new Binding("RemoteFileInfo.Length32"), IsReadOnly = true });
            cybertrackerGridAvailableFiles.Columns.Add(new DataGridCheckBoxColumn { Header = "Already downloaded", Binding = new Binding("IsDownloaded"), IsReadOnly = true });
            cybertrackerGridAvailableFiles.Columns.Add(new DataGridCheckBoxColumn { Header = "Download file", Binding = new Binding("DownloadFile") });


            dataGridLandingSites.AutoGenerateColumns = false;
            dataGridLandingSites.CanUserAddRows = false;
            dataGridLandingSites.Columns.Add(new DataGridTextColumn { Header = "ID", Binding = new Binding("ID") });
            dataGridLandingSites.Columns.Add(new DataGridTextColumn { Header = "Name", Binding = new Binding("Name") });
            dataGridLandingSites.Columns.Add(new DataGridTextColumn { Header = "Municipality", Binding = new Binding("Municipality") });
            dataGridLandingSites.Columns.Add(new DataGridTextColumn { Header = "Province", Binding = new Binding("Province") });
            dataGridLandingSites.Columns.Add(new DataGridTextColumn { Header = "Latitude", Binding = new Binding("Lat") });
            dataGridLandingSites.Columns.Add(new DataGridTextColumn { Header = "Longitude", Binding = new Binding("Lon") });

        }

        public GPS GPS { get; set; }

        private void GetGPXFiles(DetectedDevice device)
        {
            Entities.GPXFileViewModel.GetFilesFromDevice(device);
        }

        private void ShowGPXFolderLatest(GPS gps)
        {
            gridRowGPXFiles.Height = new GridLength(1, GridUnitType.Star);
            gpxPanel.Visibility = Visibility.Visible;
            _gpxFile = null;
            var tracks = Entities.GPXFileViewModel.LatestTrackFileUsingGPS(gps, (int)Global.Settings.LatestGPXFileCount);
            var wpts = Entities.GPXFileViewModel.LatestWaypointFileUsingGPS(gps, (int)Global.Settings.LatestGPXFileCount);

            PopulateGPXDataGrid(tracks.Union(wpts).ToList());
        }

        private void PopulateGPXDataGrid(List<GPXFile> gpxFiles)
        {
            dataGridGPXFiles.ItemsSource = gpxFiles;
            if (gpxFiles.Count(t => t.IsArchived == false) > 0)
            {
                buttonArchiveGPX.Visibility = Visibility.Visible;
            }
            CurrentDataGrid = dataGridGPXFiles;
        }

        private void ShowGPXFolder(DetectedDevice device, string month_year = "")
        {
            gridRowGPXFiles.Height = new GridLength(1, GridUnitType.Star);
            labelTitle.Visibility = Visibility.Visible;
            labelTitle.Content = $"GPX files saved in GPS for {DateTime.Parse(month_year).ToString("MMMM, yyyy")}";
            gpxPanel.Visibility = Visibility.Visible;
            _gpxFile = null;
            if (month_year.Length > 0)
            {
                PopulateGPXDataGrid(Entities.GPXFileViewModel.GetFiles(device.GPSID, DateTime.Parse(month_year)));
            }
            else
            {
                PopulateGPXDataGrid(Entities.GPXFileViewModel.GetFiles(device.SerialNumber));
            }

        }

        private void ShowGPS(bool fromArchive = false)
        {
            gridRowGPS.Height = new GridLength(1, GridUnitType.Star);
            _isNew = false;
            gpsPanel.Visibility = Visibility.Visible;
            gpsPanel.Visibility = Visibility.Visible;
            GPSEdited gpsEdited = new GPSEdited(_gps);
            gpsPropertiesGrid.SelectedObject = gpsEdited;

            SetupDevicePropertyGrid();
            _cboBrand.SelectedItem = gpsEdited.Brand;
            _cboModel.SelectedItem = gpsEdited.Model;

            if (fromArchive)
            {
                buttonEjectDevice.Visibility = Visibility.Collapsed;
                buttonSave.IsEnabled = false;
                _oldGPSName = gpsEdited.DeviceName;
                _oldGPSCode = gpsEdited.Code;
            }
        }

        private void ShowLogbookManager()
        {
            ResetView();
            parentGridRowClient.Height = new GridLength(0);
            parentGridRowClientLogbookImages.Height = new GridLength(1, GridUnitType.Star);

        }
        private void ShowFishers()
        {
            ResetView();
            parentGridRowClient.Height = new GridLength(0);
            parentGridRowClientFishers.Height = new GridLength(1, GridUnitType.Star);

            dataGridFishers.DataContext = Entities.FisherViewModel.FisherCollection
                .OrderBy(t => t.Name).ToList();
        }
        private void ResetView()
        {
            gpsPanel.Visibility = Visibility.Collapsed;
            gpxPanel.Visibility = Visibility.Collapsed;
            menuGPXFolder.Visibility = Visibility.Collapsed;
            buttonSave.Visibility = Visibility.Collapsed;
            buttonMakeGPSID.Visibility = Visibility.Collapsed;
            menuGPSBrands.Visibility = Visibility.Collapsed;
            menuGPSModels.Visibility = Visibility.Collapsed;
            tripPanel.Visibility = Visibility.Collapsed;
            stackPanelTripWaypoints.Visibility = Visibility.Collapsed;
            dataGridCalendar.Visibility = Visibility.Collapsed;
            dataGridGPSSummary.Visibility = Visibility.Collapsed;
            labelNoData.Visibility = Visibility.Collapsed;
            labelTitle.Content = "";
            labelDeviceName.Visibility = Visibility.Collapsed;
            textBlock.Visibility = Visibility.Collapsed;
            buttonEjectDevice.Visibility = Visibility.Visible;
            labelCalendarMonth.Visibility = Visibility.Collapsed;

            parentGridRowClient.Height = new GridLength(1, GridUnitType.Star);
            parentGridRowClientFishers.Height = new GridLength(0);
            parentGridRowClientLogbookImages.Height = new GridLength(0);
            parentGridRowFTPFolder.Height = new GridLength(0);
            parentGridRowLandingSite.Height = new GridLength(0);

            treeRowArchive.Height = new GridLength(0);
            treeRowUSB.Height = new GridLength(0);
            treeRowCalendar.Height = new GridLength(0);

            gridRowHeader.Height = new GridLength(30);
            gridRowGPS.Height = new GridLength(0);
            gridRowCalendar.Height = new GridLength(0);
            gridRowGPSSummary.Height = new GridLength(0);
            gridRowGPXFiles.Height = new GridLength(0);
            gridRowTrips.Height = new GridLength(0);
            gridRowTripWaypoints.Height = new GridLength(0);
            gridRowGPXSummaryGrid.Height = new GridLength(0);

            panelMetadata.Visibility = Visibility.Collapsed;
            panelImageFields.Visibility = Visibility.Visible;
        }

        private void ShowTripData()
        {
            dataGridTrips.ItemsSource = Entities.TripViewModel.GetAllTrips(_deviceIdentifier);
            dataGridTrips.IsReadOnly = true;
            gridRowTrips.Height = new GridLength(1, GridUnitType.Star);
            tripPanel.Visibility = Visibility.Visible;
            stackPanelTripWaypoints.Visibility = Visibility.Collapsed;
            buttonDeleteTrip.IsEnabled = false;
            buttonEditTrip.IsEnabled = false;
            _selectedTrip = null;
            CurrentDataGrid = dataGridTrips;
        }


        public void ShowTripWaypoints(List<TripWaypoint> tripWaypoints)
        {

        }
        private void ShowTripWaypoints(bool fromGPSSummary = false)
        {
            if (fromGPSSummary)
            {
                gridRowGPSSummary.Height = new GridLength(1, GridUnitType.Star);

            }
            else
            {
                gridRowTrips.Height = new GridLength(1, GridUnitType.Star);
            }
            gridRowTripWaypoints.Height = new GridLength(1, GridUnitType.Star);
            gridRowHeader.Height = new GridLength(30);


            stackPanelTripWaypoints.Visibility = Visibility.Visible;
            _selectedTripWaypoint = null;

            if (_selectedTrip != null)
            {
                _tripWaypoints = Entities.TripWaypointViewModel.GetAllTripWaypoints(_selectedTrip.TripID);
                if (_tripWaypoints.Count > 0)
                {
                    dataGridTripWaypoints.ItemsSource = _tripWaypoints;
                }
                else
                {
                    dataGridTripWaypoints.ItemsSource = null;
                }
            }
        }

        private void SaveChangesToGPS()
        {
            if (_gpsPropertyChanged)
            {
                var editedGPS = (GPSEdited)gpsPropertiesGrid.SelectedObject;
                bool gpxFolderExists = Directory.Exists($"{_detectedDevice.Disks[0].Caption}\\{editedGPS.Folder}");

                if (gpxFolderExists)
                {
                    _gps = new GPS
                    {
                        Brand = editedGPS.Brand,
                        Model = editedGPS.Model,
                        DeviceID = editedGPS.DeviceID,
                        DeviceName = editedGPS.DeviceName,
                        Folder = editedGPS.Folder,
                        Code = editedGPS.Code,
                    };
                    Entities.GPSViewModel.UpdateRecordInRepo(_gps);

                    switch (_changedPropertyName)
                    {
                        case "Folder":
                            var updatedGPXFolder = $"{_detectedDevice.Disks[0].Caption}\\{_gps.Folder}";

                            var folderNode = (TreeViewItem)_gpsTreeViewItem.Items[0];

                            if (folderNode.Header.ToString() != updatedGPXFolder)
                            {
                                folderNode.Header = updatedGPXFolder;

                                _detectedDevice.GPS = _gps;
                                Entities.GPXFileViewModel.GetFilesFromDevice(_detectedDevice);
                            }

                            break;

                        case "DeviceName":
                            _gpsTreeViewItem.Header = _gps.DeviceName; ;
                            break;
                    }
                }
                else
                {
                    MessageBox.Show("GPX folder is not found", "Validation error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                _gpsPropertyChanged = false;
            }
        }

        private void ResetGrids()
        {
            dataGridTripWaypoints.SelectedItems.Clear();
            dataGridGPXFiles.SelectedItems.Clear();
            dataGridTrips.SelectedItems.Clear();
        }

        private void RefreshDetectedDevice()
        {
            Entities.DetectedDeviceViewModel.ScanUSBDevices();
            _detectedDevice = Entities.DetectedDeviceViewModel.GetDevice(_deviceIdentifier);
        }

        private void OnTreeViewSelectedItemChange(object sender, RoutedPropertyChangedEventArgs<object> e)
        {

            _inDeviceNode = false;
            _gps = null;
            _gpsid = null;
            ResetView();
            ResetGrids();

            gridRowHeader.Height = new GridLength(30);
            labelTitle.Visibility = Visibility.Visible;

            switch (((TreeView)sender).Name)
            {
                case "treeArchive":
                    treeRowArchive.Height = new GridLength(1, GridUnitType.Star);
                    treeArchive.Visibility = Visibility.Visible;
                    var selectedNode = (TreeViewItem)treeArchive.SelectedItem;
                    if (selectedNode != null)
                    {
                        DateTime month_year;
                        switch (selectedNode.Tag.GetType().Name)
                        {
                            case "String":
                                if (selectedNode.Tag.ToString() == "root")
                                {
                                    gridRowGPXSummaryGrid.Height = new GridLength(1, GridUnitType.Star);
                                    dataGridGPXSummary.Visibility = Visibility.Visible;
                                    dataGridGPXSummary.DataContext = Entities.DeviceGPXViewModel.GetGPSDataSummaries();
                                }
                                break;

                            case "GPS":
                                _gps = (GPS)selectedNode.Tag;
                                _gpsid = _gps.DeviceID;
                                ShowGPS(_inArchive);
                                break;

                            case "DateTime":

                                _gps = (GPS)((TreeViewItem)selectedNode.Parent).Tag;
                                _gpsid = _gps.DeviceID;
                                month_year = (DateTime)selectedNode.Tag;

                                if (!Entities.DeviceGPXViewModel.ArchivedGPXFiles.Keys.Contains(_gps))
                                {
                                    Entities.DeviceGPXViewModel.RefreshArchivedGPXCollection(_gps);
                                }
                                if (Entities.DeviceGPXViewModel.DeviceGPXCollection.Count(t => t.GPS.DeviceID == _gps.DeviceID) != Entities.DeviceGPXViewModel.ArchivedGPXFiles[_gps].Count)
                                {
                                    Entities.DeviceGPXViewModel.RefreshArchivedGPXCollection(_gps);
                                }

                                var archivedGPX = Entities.DeviceGPXViewModel.ArchivedFilesByGPSByMonth(_gps, month_year);

                                gpxPanel.Visibility = Visibility.Visible;
                                dataGridGPXFiles.ItemsSource = archivedGPX;
                                labelTitle.Content = $"Archived content of GPS for {month_year.ToString("MMMM, yyyy")}";
                                labelTitle.Visibility = Visibility.Visible;
                                gridRowGPXFiles.Height = new GridLength(1, GridUnitType.Star);
                                break;
                        }
                    }
                    break;

                case "treeCalendar":

                    treeRowCalendar.Height = new GridLength(1, GridUnitType.Star);
                    var treeNode = (TreeViewItem)e.NewValue;
                    if (treeNode.Tag.ToString() == "month_archive")
                    {
                        _gps = Entities.GPSViewModel.GetGPSEx(((TreeViewItem)treeNode.Parent).Tag.ToString());
                        DateTime month = DateTime.Parse(treeNode.Header.ToString());
                        labelTitle.Content = $"Details of trips tracked by GPS for {month.ToString("MMMM, yyyy")}";

                        gridRowGPSSummary.Height = new GridLength(1, GridUnitType.Star);
                        dataGridGPSSummary.Visibility = Visibility.Visible;
                        dataGridGPSSummary.ItemsSource = Entities.TripViewModel.TripsUsingGPSByMonth(_gps, month);
                        buttonAddTrip1.IsEnabled = false;
                        buttonEditTrip1.IsEnabled = false;
                        buttonDeleteTrip1.IsEnabled = false;
                    }
                    else if (treeNode.Tag.ToString() != "root")
                    {
                        switch (((TreeViewItem)treeNode.Parent).Header)
                        {
                            case "Trip calendar":

                                labelTitle.Content = "Calendar of tracked fishing operations by GPS";
                                _tripMonthYear = (DateTime)(treeNode).Tag;
                                var tripCalendarVM = new TripCalendarViewModel(_tripMonthYear);
                                gridRowHeader.Height = new GridLength(60);
                                gridRowCalendar.Height = new GridLength(1, GridUnitType.Star);
                                dataGridCalendar.Visibility = Visibility.Visible;
                                dataGridCalendar.DataContext = tripCalendarVM.DataTable;
                                labelCalendarMonth.Visibility = Visibility.Visible;
                                labelCalendarMonth.Content = _tripMonthYear.ToString("MMMM, yyyy");
                                break;

                            case "Trips by GPS":
                                _gps = Entities.GPSViewModel.GetGPSEx(((GPS)treeNode.Tag).DeviceID);
                                labelTitle.Content = $"Details of {Global.Settings.LatestTripCount} latest trips tracked by GPS";
                                gridRowGPSSummary.Height = new GridLength(1, GridUnitType.Star);
                                dataGridGPSSummary.Visibility = Visibility.Visible;
                                dataGridGPSSummary.ItemsSource = Entities.TripViewModel.LatestTripsUsingGPS(_gps, (int)Global.Settings.LatestTripCount);
                                buttonEditTrip1.IsEnabled = false;
                                buttonDeleteTrip1.IsEnabled = false;
                                buttonAddTrip1.IsEnabled = true;
                                break;
                            default:
                                ShowGPS();
                                break;
                        }
                    }

                    break;

                case "treeDevices":
                    treeRowUSB.Height = new GridLength(1, GridUnitType.Star);
                    if (e.NewValue != null)
                    {
                        var tag = ((TreeViewItem)e.NewValue).Tag.ToString();

                        labelTitle.Visibility = Visibility.Visible;
                        labelTitle.Content = "Add this device as GPS";

                        SaveChangesToGPS();
                        _gpsTreeViewItem = null;

                        if (tag != "root")
                        {
                            if (tag == "month_node")
                            {
                                _deviceIdentifier = ((TreeViewItem)((TreeViewItem)((TreeViewItem)e.NewValue).Parent).Parent).Tag.ToString();
                            }
                            else
                            {
                                _deviceIdentifier = ((TreeViewItem)((TreeViewItem)e.NewValue).Parent).Tag.ToString();
                            }
                        }

                        switch (tag)
                        {
                            case "root":
                                labelTitle.Visibility = Visibility.Collapsed;
                                if (treeDevices.Items.Count == 1)
                                {
                                    ScanUSBDevices();
                                }
                                return;

                            case "gpx_folder":
                                labelTitle.Content = "Latest GPX files in GPX folder";
                                _detectedDevice = Entities.DetectedDeviceViewModel.GetDevice(_deviceIdentifier);
                                if (_detectedDevice == null)
                                {
                                    RefreshDetectedDevice();
                                }
                                ShowGPXFolderLatest(_detectedDevice.GPS);
                                break;

                            case "disk":
                                labelTitle.Visibility = Visibility.Collapsed;
                                labelDeviceName.Visibility = Visibility.Visible;
                                labelDeviceName.Content = ((TreeViewItem)((TreeViewItem)treeDevices.SelectedItem).Parent).Header;

                                _detectedDevice = Entities.DetectedDeviceViewModel.GetDevice(_deviceIdentifier);
                                textBlock.Text = _detectedDevice.DriveSummary;
                                textBlock.Visibility = Visibility.Visible;
                                return;
                            //break;
                            case "trip_data":
                                labelTitle.Content = "Trip log";
                                ShowTripData();
                                break;

                            case "month_node":
                                //detectedDevice = Entities.DetectedDeviceViewModel.GetDevice(_deviceSerialNumber);
                                _detectedDevice = Entities.DetectedDeviceViewModel.GetDevice(_deviceIdentifier);
                                if (_detectedDevice == null)
                                {
                                    RefreshDetectedDevice();
                                }
                                ShowGPXFolder(_detectedDevice, ((TreeViewItem)e.NewValue).Header.ToString());
                                break;

                            default:
                                _gpsTreeViewItem = (TreeViewItem)treeDevices.SelectedItem;
                                _inDeviceNode = true;
                                _deviceIdentifier = tag;
                                menuGPXFolder.Visibility = Visibility.Visible;
                                _detectedDevice = Entities.DetectedDeviceViewModel.GetDevice(_deviceIdentifier);

                                if (_detectedDevice != null && _detectedDevice.GPSID != null)
                                {
                                    _gpsid = _detectedDevice.GPSID;
                                }
                                if (_detectedDevice == null)
                                {
                                    RefreshDetectedDevice();
                                }
                                labelDeviceName.Visibility = Visibility.Visible;

                                if (_detectedDevice.GPS == null)
                                {
                                    labelDeviceName.Content = ((TreeViewItem)treeDevices.SelectedItem).Header;
                                }

                                break;
                        }
                        ShowGPSDetail();
                    }
                    break;
            }
        }

        private void ShowGPSDetail()
        {
            if (Entities.GPSViewModel.Count > 0)
            {
                _gps = Entities.GPSViewModel.GetGPSEx(_deviceIdentifier);
                if (_gpsid != null && _gpsid.Length > 0 && _gps != null && _inDeviceNode)
                {
                    labelTitle.Content = "Details of GPS";
                    labelDeviceName.Content = $"{_detectedDevice.GPS.DeviceName} ({_detectedDevice.GPS.Brand} {_detectedDevice.GPS.Model})";
                    ShowGPS();
                    buttonMakeGPSID.Visibility = Visibility.Collapsed;
                }
                else if (_gps == null)
                {
                    NewGPS();
                }
            }
            else
            {
                NewGPS();
            }
        }

        private void OnPropertyMouseDblClick(object sender, MouseButtonEventArgs e)
        {
            switch (_selectedProperty)
            {
                case "Brand":
                    SelectBrandModel(ShowMode.ShowModeBrand);
                    break;

                case "Model":
                    if (_cboBrand.SelectedItem != null)
                    {
                        SelectBrandModel(ShowMode.ShowModeModel, _cboBrand.SelectedItem.ToString());
                    }
                    else
                    {
                        MessageBox.Show("Select a GPS brand", "GPX Manager", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    break;

                case "Folder":
                    LocateGPXFolder();
                    break;
            }
        }

        private void OnPropertyChanged(object sender, RoutedPropertyChangedEventArgs<xceedPropertyGrid.PropertyItemBase> e)
        {
            if (e.NewValue != null)
            {
                _selectedProperty = ((xceedPropertyGrid.PropertyItem)e.NewValue).PropertyName;

                menuGPSBrands.Visibility = Visibility.Collapsed;
                menuGPSModels.Visibility = Visibility.Collapsed;

                if (_selectedProperty == "Brand")
                {
                    menuGPSBrands.Visibility = Visibility.Visible;
                }
                else if (_selectedProperty == "Model")
                {
                    menuGPSModels.Visibility = Visibility.Visible;
                }
            }
        }

        private void OnPropertyValueChanged(object sender, xceedPropertyGrid.PropertyValueChangedEventArgs e)
        {
            _changedPropertyName = ((xceedPropertyGrid.PropertyItem)e.OriginalSource).PropertyName;
            if (_detectedDevice != null && _detectedDevice.GPS != null)
            {
                //_gpsPropertyChanged = true;
                //_changedPropertyName = ((xceedPropertyGrid.PropertyItem)e.OriginalSource).PropertyName;
                _gpsPropertyChanged = _changedPropertyName == "Folder" || _changedPropertyName == "DeviceName";
            }
            else if (_inArchive && buttonSave.Visibility == Visibility.Visible)
            {
                buttonSave.IsEnabled = true;
            }
        }

        public Control LayerSelector { get; set; }

        private void OnDataGridSelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            LayerSelector = (DataGrid)sender;
            MapWindowManager.TrackGPXFile = null;
            if (MapWindowForm.Instance != null)
            {
                MapWindowManager.MapWindowForm.LayerSelector = LayerSelector;
            }
            switch (((DataGrid)sender).Name)
            {
                case "dataGridLandingSites":
                    buttonLandingSiteDelete.IsEnabled = true;
                    buttonLandingSiteEdit.IsEnabled = true;
                    break;
                case "dataGridFishers":
                    buttonFishersDelete.IsEnabled = true;
                    buttonFishersEdit.IsEnabled = true;

                    break;
                case "dataGridGPSSummary":
                    if (dataGridGPSSummary.Items.Count > 0 && dataGridGPSSummary.SelectedItems.Count > 0)
                    {
                        _selectedTrip = (Trip)dataGridGPSSummary.SelectedItem;
                        _tripWaypoints = Entities.TripWaypointViewModel.GetAllTripWaypoints(_selectedTrip.TripID);
                        if (dataGridGPSSummary.SelectedItems.Count == 1 && MapWindowForm.Instance != null)
                        {
                            MapWindowManager.MapLayersHandler.ClearAllSelections();
                            //TripMappingManager.RemoveTripLayersFromMap();
                            MapWindowManager.RemoveGPSDataFromMap();
                            Entities.TripViewModel.MarkAllNotShownInMap();
                            ShowTripMap();
                        }
                        else if (_selectedTrip != null)
                        {
                            Entities.TripViewModel.GetTrip(_selectedTrip.TripID);
                            checkEditTripWaypoints.IsChecked = false;
                            ConfigureTripWaypointsGrid();
                            ShowTripWaypoints(fromGPSSummary: true);
                            buttonDeleteTrip1.IsEnabled = true;
                            buttonEditTrip1.IsEnabled = true;

                        }
                    }
                    break;

                case "dataGridTrips":
                    buttonEditTrip.IsEnabled = true;
                    buttonDeleteTrip.IsEnabled = true;
                    _selectedTrip = (Trip)dataGridTrips.SelectedItem;
                    if (dataGridTrips.SelectedItems.Count == 1 && MapWindowForm.Instance != null)
                    {
                        MapWindowManager.MapLayersHandler.ClearAllSelections();
                        //TripMappingManager.RemoveTripLayersFromMap();
                        MapWindowManager.RemoveGPSDataFromMap();
                    }
                    else if (_selectedTrip != null)
                    {
                        _selectedTrip.GPS = _gps;

                        Entities.TripViewModel.GetTrip(_selectedTrip.TripID);

                        ShowTripWaypoints();
                    }
                    break;

                case "dataGridTripWaypoints":
                    //buttonEditWaypoint.IsEnabled = true;
                    //buttonDeleteWaypoint.IsEnabled = true;
                    //_selectedTripWaypoint = (TripWaypoint)dataGridTripWaypoints.SelectedItem;
                    break;

                case "dataGridGPXFiles":
                    dataGridGPXFiles.Visibility = Visibility.Hidden;
                    _isTrackGPX = false;
                    menuGPXViewTrip.Visibility = Visibility.Collapsed;
                    _gpxFile = (GPXFile)dataGridGPXFiles.SelectedItem;
                    _isTrackGPX = _gpxFile?.TrackCount > 0;

                    if (_inArchive && _isTrackGPX)
                    {
                        _trips = Entities.TripViewModel.GetTrips(_gpxFile.GPS, _gpxFile.FileName);
                        menuGPXViewTrip.Visibility = Visibility.Visible;
                        if (_editTripWindow != null)
                        {
                            if (_trips.Count == 1)
                            {
                                _editTripWindow.TripID = _trips[0].TripID;
                            }
                            else if (_trips.Count == 0)
                            {
                                _editTripWindow.DefaultTripDates(_gpxFile.DateRangeStart.AddMinutes(1), _gpxFile.DateRangeEnd.AddMinutes(-1));
                            }
                            _editTripWindow.RefreshTrip(_trips.Count == 0);
                        }
                    }

                    if (_gpxFile != null)
                    {
                        if (_inArchive)
                        {
                            Entities.DeviceGPXViewModel.MarkAllNotShownInMap();
                        }
                        else
                        {
                            Entities.GPXFileViewModel.MarkAllNotShownInMap();
                        }

                        menuGPXFileLocateTrack.IsEnabled = !_isTrackGPX;

                        SetGPXFileMenuMapVisibility(_gpxFile.ShownInMap, false);

                        MapWindowManager.MapLayersHandler?.ClearAllSelections();

                        if (MapWindowManager.MapWindowForm != null && dataGridGPXFiles.SelectedItems.Count == 1)
                        {
                            GPXMappingManager.RemoveGPXLayersFromMap();
                            MapWindowManager.RemoveGPSDataFromMap();
                            foreach (var item in _mappedGPXFiles)
                            {
                                item.ShownInMap = false;
                            }
                            _mappedGPXFiles = new List<GPXFile>();

                            List<int> ptHandles = new List<int>();
                            if (_isTrackGPX)
                            {
                                MapWindowManager.TrackGPXFile = _gpxFile;
                                List<int> handles = new List<int>();
                                if (MapWindowManager.MapTrackGPX(_gpxFile, out handles) >= 0)
                                {
                                    List<GPXFile> gpxFiles;
                                    List<WaypointLocalTime> waypoints = null;

                                    if (_inArchive)
                                    {
                                        waypoints = Entities.DeviceGPXViewModel.GetWaypointsMatch(_gpxFile, out gpxFiles);
                                    }
                                    else
                                    {
                                        waypoints = Entities.GPXFileViewModel.GetWaypointsMatch(_gpxFile, out gpxFiles);
                                    }

                                    _gpxFile.ShownInMap = true;
                                    _mappedGPXFiles.Add(_gpxFile);

                                    if (waypoints.Count > 0)
                                    {
                                        MapWindowManager.MapWaypointList(waypoints, out ptHandles);

                                        foreach (var item in gpxFiles)
                                        {
                                            item.ShownInMap = true;
                                            _mappedGPXFiles.Add(item);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                var waypoints = Entities.DeviceGPXViewModel.GetWaypoints(_gpxFile);
                                if (waypoints.Count > 0)
                                {
                                    MapWindowManager.MapWaypointList(waypoints, out ptHandles);
                                }
                                _gpxFile.ShownInMap = true;
                            }

                            MapWindowManager.MapControl.Redraw();
                            MapWindowManager.MapLayersWindow?.RefreshCurrentLayer();
                        }
                        else
                        {
                            if (_gpxFile.ShownInMap)
                            {
                                if (_isTrackGPX)
                                {
                                    MapWindowManager.MapLayersHandler.set_MapLayer(MapWindowManager.GPXTracksLayer.Handle);
                                }
                                else
                                {
                                    MapWindowManager.MapLayersHandler.set_MapLayer(MapWindowManager.GPXWaypointsLayer.Handle);
                                }

                                foreach (int handle in _gpxFile.ShapeIndexes)
                                {
                                    ((Shapefile)MapWindowManager.MapLayersHandler.CurrentMapLayer.LayerObject).ShapeSelected[handle] = true;
                                }
                            }
                        }
                    }
                    dataGridGPXFiles.Items.Refresh();
                    dataGridGPXFiles.Visibility = Visibility.Visible;
                    buttonGPXDetails.IsEnabled = dataGridGPXFiles.SelectedItems.Count > 0;
                    dataGridGPXFiles.Focus();
                    break;
            }
        }

        public void RefreshLandingSiteGrid()
        {
            dataGridLandingSites.DataContext = Entities.LandingSiteViewModel.LandingSiteCollection.ToList();
            buttonLandingSiteEdit.IsEnabled = false;
            buttonLandingSiteDelete.IsEnabled = false;
        }
        private void ShowSelectedLandingSite()
        {
            if (dataGridLandingSites.SelectedItems.Count == 1)
            {
                var ls = (LandingSite)dataGridLandingSites.SelectedItem;
                var elsw = LandingSiteEditWindow.GetInstance();
                elsw.LandingSite = ls;
                elsw.Owner = this;
                if (elsw.Visibility == Visibility.Visible)
                {
                    elsw.BringIntoView();
                }
                else
                {
                    elsw.Show();
                }
            }
        }
        private void ShowSelectedFisher()
        {
            if (dataGridFishers.SelectedItems.Count == 1)
            {
                var fisher = (Fisher)dataGridFishers.SelectedItem;
                var efw = EditFisherWindow.GetInstance();
                efw.Fisher = fisher;
                efw.Owner = this;
                if (efw.Visibility == Visibility.Visible)
                {
                    efw.BringIntoView();
                }
                else
                {
                    efw.Show();
                }
            }
        }
        private void OnGridDoubleClick(object sender, MouseButtonEventArgs e)
        {
            switch (((DataGrid)sender).Name)
            {
                case "dataGridLandingSites":
                    ShowSelectedLandingSite();
                    break;
                case "dataGridGPSSummary":
                    ShowEditTripWindow(false, _selectedTrip.TripID, _selectedTrip.OperatorID);
                    break;
                case "dataGridFishers":
                    ShowSelectedFisher();
                    break;
                case "dataGridGPXFiles":
                    ShowGPXFileDetails();
                    break;

                case "dataGridCalendar":
                    if (dataGridCalendar.SelectedCells.Count == 1)
                    {
                        DataGridCellInfo cell = dataGridCalendar.SelectedCells[0];
                        var gridRow = dataGridCalendar.Items.IndexOf(cell.Item);
                        var gridCol = cell.Column.DisplayIndex;
                        var item = dataGridCalendar.Items[gridRow] as DataRowView;
                        var gps = Entities.GPSViewModel.GetGPSEx((string)item.Row.ItemArray[1]);
                        var trips = new List<Trip>();
                        if (((string)item.Row.ItemArray[gridCol]) == "x")
                        {
                            DateTime tripDate = new DateTime(_tripMonthYear.Year, _tripMonthYear.Month, gridCol - 1);
                            trips = Entities.TripViewModel.TripsUsingGPSByDate(gps, tripDate);
                        }
                        else if (((string)item.Row.ItemArray[gridCol]).Length > 0)
                        {
                            trips = Entities.TripViewModel.TripsUsingGPSByMonth(gps, _tripMonthYear);
                        }

                        if (trips.Count == 1)
                        {
                            _selectedTrip = trips[0];
                            ShowEditTripWindow(false, _selectedTrip.TripID, _selectedTrip.OperatorID);
                            //ShowEditTripWindow(isNew: false, tripID: trips[0].TripID,  showWaypoints: true);
                        }
                        else if (trips.Count > 1)
                        {
                        }
                    }
                    break;


                case "dataGridTrips":
                    ShowEditTripWindow(false, _selectedTrip.TripID);
                    break;
            }
        }


        private void OnDataGridMouseUp(object sender, MouseButtonEventArgs e)
        {
            //switch(((DataGrid)sender).Name)
            //{
            //    case "dataGridGPXFiles":
            //         if(!_isTrackGPX)
            //        {
            //        }
            //        break;
            //}
        }

        private void OnGridAutogeneratedColumns(object sender, EventArgs e)
        {
            if (dataGridCalendar.Columns.Count > 0)
            {
                dataGridCalendar.Columns[1].Visibility = Visibility.Collapsed;
            }
        }



        private void OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (!_isTrackGPX)
            {
                menuAddTripFromTRack.Visibility = Visibility.Collapsed;
            }
            else
            {
                menuAddTripFromTRack.Visibility = Visibility.Visible;
            }
        }

        private void LoadGPSToArchiveTree(TreeViewItem root)
        {
            ((TreeViewItem)treeArchive.Items[0]).Items.Clear();
            List<GPS> listGPS = Entities.GPSViewModel.GPSCollection.OrderBy(t => t.DeviceName).ToList();
            if (listGPS.Count == 0)
            {
                listGPS = Entities.DeviceGPXViewModel.ArchivedGPXFiles.Keys.ToList();
            }

            foreach (var gps in listGPS)
            {
                var nd = root.Items.Add(new TreeViewItem { Header = gps.DeviceName, Tag = gps });
                var gpsNode = root.Items[nd] as TreeViewItem;
                foreach (var month in Entities.DeviceGPXViewModel.GetMonthsInArchive(gps))
                {
                    gpsNode.Items.Add(new TreeViewItem { Header = month.ToString("MMM, yyyy"), Tag = new DateTime(month.Year, month.Month, 1) });
                }
                gpsNode.IsExpanded = _archiveTreeExpanded;
            }
        }
        public void ShowArchive()
        {
            if (Global.AppProceed)
            {
                _inArchive = true;
                ResetView();
                treeRowArchive.Height = new GridLength(1, GridUnitType.Star);
                HideTrees();
                ConfigureGPXGrid(false);
                treeArchive.Visibility = Visibility.Visible;

                labelTitle.Content = "Archived GPX files";
                //var nd = treeArchive.Items.Count - 1;
                var root = (TreeViewItem)treeArchive.Items[0];
                root.Tag = "root";


                if (_startUp)
                {
                    LoadGPSToArchiveTree(root);
                    root.IsExpanded = true;
                    if (root.Items.Count == 0)
                    {
                        labelNoData.Visibility = Visibility.Visible;
                        labelNoData.Content = "There are no archived GPX files in the database";
                        labelTitle.Visibility = Visibility.Visible;

                        //treeArchive.Visibility = Visibility.Collapsed;
                    }
                    ((TreeViewItem)treeArchive.Items[0]).IsSelected = true;
                    _startUp = false;
                }



                var previousSelectedItem = ClearTreeOfSelection(treeArchive);
                if (previousSelectedItem != null)
                {
                    previousSelectedItem.IsSelected = true;
                }



                buttonGPXDetails.IsEnabled = false;
            }
            else
            {
                MessageBox.Show("Application need to be setup first", "GPX Manager", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private TreeViewItem ClearTreeOfSelection(TreeView tv)
        {
            //foreach (TreeViewItem item in tv.Items)
            //{
            //    if(item.IsSelected)
            //    {
            //        selectedItem = item;
            //    }
            //    item.IsSelected = false;
            //}
            TreeViewItem selectedItem = (TreeViewItem)tv.SelectedItem;
            ((TreeViewItem)tv.SelectedItem).IsSelected = false;
            return selectedItem;
        }

        private void ToBeImplemented(string usage)
        {
            MessageBox.Show($"The {usage} functionality is not yet implemented", "Placeholder and not yet working", MessageBoxButton.OK, MessageBoxImage.Information); ;
        }

        private void ShowCybertracker()
        {
            ResetView();
            parentGridRowClient.Height = new GridLength(0);
            parentGridRowFTPFolder.Height = new GridLength(1, GridUnitType.Star);



            //dataGridFishers.DataContext = Entities.FisherViewModel.FisherCollection
            //.OrderBy(t => t.Name).ToList();
        }

        private void ShowLandingSites()
        {
            ResetView();
            parentGridRowClient.Height = new GridLength(0);
            parentGridRowLandingSite.Height = new GridLength(1, GridUnitType.Star);
            dataGridLandingSites.DataContext = Entities.LandingSiteViewModel.LandingSiteCollection
                .OrderBy(t => t.Name).ToList();
        }
        private void OnToolbarButtonClick(object sender, RoutedEventArgs e)
        {

            switch (((Button)sender).Name)
            {
                case "buttonLandingSite":
                    ShowLandingSites();
                    break;
                case "buttonCybertracker":
                    if (Global.Settings.PathToCybertrackerExe != null &&
                        Global.Settings.PathToCybertrackerExe.Length > 0 &&
                        Global.Settings.CTXBackupFolder != null &&
                        Global.Settings.CTXBackupFolder.Length > 0 &&
                        Global.Settings.CTXDownloadFolder != null &&
                        Global.Settings.CTXDownloadFolder.Length > 0)
                    {
                        ShowCybertracker();
                    }
                    else
                    {
                        MessageBox.Show("Settings must include path to cybertracker, \r\n" +
                                         "folder name for CTX download, and\r\n" +
                                         "folder name for CTX backup", "GPX Manager",
                                         MessageBoxButton.OK,
                                         MessageBoxImage.Information);
                    }
                    break;
                case "buttonPerson":
                    ShowFishers();
                    break;
                case "buttonImage":
                    //ImageManagerWindow imw = new ImageManagerWindow();
                    //imw.Owner = this;
                    //imw.Show();
                    ShowLogbookManager();
                    break;
                case "buttonAbout":
                    ShowAboutWindow();
                    break;

                case "buttonArchive":
                    ShowArchive();
                    break;

                case "buttonUploadCloud":
                    ToBeImplemented("upload to cloud");
                    break;

                case "buttonCalendar":
                    ShowTripCalendar();
                    break;

                case "buttonSettings":
                    ShowSettingsWindow();
                    break;

                case "buttonExit":
                    Close();
                    break;

                case "buttonUSB":
                    HideTrees();
                    treeDevices.Visibility = Visibility.Visible;
                    ScanUSBDevices();
                    break;

                case "buttonMap":
                    ShowMap();
                    break;
            }
        }

        private void OnStatusLabelDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Process.Start($"{System.IO.Path.GetDirectoryName(((System.Windows.Controls.Label)sender).Content.ToString())}");
        }

        private void OnDatagGridSelectedCellChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            switch (((DataGrid)sender).Name)
            {
                case "dataGridCalendar":
                    if (dataGridCalendar.SelectedCells.Count == 1)
                    {
                        DataGridCellInfo cell = dataGridCalendar.SelectedCells[0];
                        var gridRow = dataGridCalendar.Items.IndexOf(cell.Item);
                        var gridCol = cell.Column.DisplayIndex;
                        var item = dataGridCalendar.Items[gridRow] as DataRowView;
                        var gps = Entities.GPSViewModel.GetGPSByName((string)item.Row.ItemArray[0]);
                        var cellContent = (string)item.Row.ItemArray[gridCol];
                        TripMappingManager.RemoveTripLayersFromMap();
                        if (cellContent == "x" && MapWindowManager.MapWindowForm != null)
                        {
                            var fishngDate = DateTime.Parse(((TreeViewItem)treeCalendar.SelectedItem).Header.ToString()).AddDays(gridCol - 2);
                            var trips = Entities.TripViewModel.GetTrips(gps, fishngDate);
                            TripMappingManager.MapTrip(trips);
                        }
                    }
                    break;
            }
        }

        public void NotifyEditWindowClosing()
        {
            menuGPXViewTrip.IsChecked = false;
        }

        private void OnMenuChecked(object sender, RoutedEventArgs e)
        {
            MenuItem item = (MenuItem)sender;
            switch (item.Name)
            {
                case "menuGPXViewTrip":
                    bool isNew = _trips.Count == 0;
                    if (item.IsChecked)
                    {
                        _editTripWindow = EditTripWindow.GetInstance();
                        _editTripWindow.IsNew = isNew;
                        if (!isNew && _trips.Count == 1)
                        {
                            _editTripWindow.TripID = _trips[0].TripID;
                        }
                        _editTripWindow.Owner = this;
                        if (_editTripWindow.Visibility == Visibility.Visible)
                        {
                            _editTripWindow.BringIntoView();
                        }
                        else
                        {
                            _editTripWindow.ParentWindow = this;
                            _editTripWindow.Show();
                        }
                    }
                    else
                    {
                        if (_editTripWindow != null)
                        {
                            try
                            {
                                _editTripWindow.Close();
                            }
                            catch (InvalidOperationException)
                            {
                                //ignore
                            }
                            catch (Exception ex)
                            {
                                Logger.Log(ex);
                            }
                            finally
                            {
                                _editTripWindow = null;
                            }
                        }
                    }
                    break;
            }
        }

        private void OnImageTreeSelectionChnaged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _selectedTreeViewItem = e.NewValue as TreeViewItem;
            _fileSelectedLogBookImage = null;
            _logbookImage = null;
            _src = null;


            dtPickerStart.Value = null;
            dtPickerEnd.Value = null;

            if (_selectedTreeViewItem.Tag.ToString() != "main")
            {
                buttonIgnore.IsEnabled = true;
                buttonRegister.IsEnabled = true;
                buttonTripOfImage.Visibility = Visibility.Collapsed;

                switch (_selectedTreeViewItem.Tag.GetType().Name)
                {
                    case "FileInfo":
                        _fileSelectedLogBookImage = _selectedTreeViewItem.Tag as FileInfo;
                        Entities.LogbookImageViewModel.AddImageCommentToMetadata(_fileSelectedLogBookImage.FullName);

                        _src = new BitmapImage(uriSource: new Uri(_fileSelectedLogBookImage.FullName));
                        labelSelectedImageItem.Content = _fileSelectedLogBookImage.FullName;

                        break;
                    case "LogbookImage":

                        _logbookImage = _selectedTreeViewItem.Tag as LogbookImage;
                        _src = new BitmapImage(uriSource: new Uri(_logbookImage.FileName));

                        var parentHeader = ((TreeViewItem)_selectedTreeViewItem.Parent).Tag;
                        switch (parentHeader.GetType().Name)
                        {
                            case "GPS":
                                buttonTripOfImage.Visibility = Visibility.Visible;
                                buttonRegister.IsEnabled = true;
                                dtPickerStart.Value = (DateTime)_logbookImage.Start;
                                dtPickerEnd.Value = (DateTime)_logbookImage.End;
                                comboFisher.Text = _logbookImage.Fisher.Name;
                                comboVessel.Text = _logbookImage.Boat;
                                comboGear.Text = _logbookImage.Gear.Name;
                                comboGPS.Text = _logbookImage.GPS.DeviceName;
                                Entities.GPSViewModel.CurrentEntity = _logbookImage.GPS;

                                if (EditTripWindow.Instance != null)
                                {
                                    ShowEditTripWindow(false, _logbookImage.Trip.TripID, fromImage: true);
                                }

                                break;
                            case "String":
                                if (((TreeViewItem)_selectedTreeViewItem.Parent).Header.ToString() == "Ignored")
                                {
                                    comboFisher.Text = "";
                                    comboVessel.Text = "";
                                    comboGear.Text = "";
                                    comboGPS.Text = "";
                                    buttonIgnore.IsEnabled = false;
                                }
                                break;
                        }
                        labelSelectedImageItem.Content = _logbookImage.Title;
                        break;
                }
                imagePreview.Source = _src;
                if (panelMetadata.Visibility == Visibility.Visible && _fileSelectedLogBookImage != null || _logbookImage != null)
                {
                    var fileName = _fileSelectedLogBookImage != null ? _fileSelectedLogBookImage.FullName : _logbookImage.FileName;
                    textMetadata.Text = Entities.LogbookImageViewModel.MetadataFlatText(fileName);
                }
                else
                {
                    textMetadata.Text = "No image selected";
                }
            }
        }

        private void OnDTPickerValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (dtPickerEnd.Value == null)
            {
                dtPickerEnd.Value = ((Xceed.Wpf.Toolkit.DateTimePicker)sender).Value;
            }
        }

        private void OnDTPickerRightButtonDown(object sender, MouseButtonEventArgs e)
        {

            ContextMenu cm = new ContextMenu();
            MenuItem m = null;

            m = new MenuItem { Header = "Get time from track", Name = "menuGetTimeFromTrack" };
            m.Click += OnMenuClick;
            cm.Items.Add(m);

            cm.IsOpen = true;
        }


        private List<Waypoint> _waypoints;
        private List<TripWaypoint> _tripwaypointsCopy = new List<TripWaypoint>();
        //private string _wptName;
        //private DateTime _wayPointTime;
        private bool ConfigureTripWaypointsGrid(bool forEdit = false)
        {
            bool configured = true;
            buttonSaveWaypointTripEdits.IsEnabled = false;
            _tripWaypointsDirty = false;
            _tripwaypointsCopy.Clear();
            dataGridTripWaypoints.Columns.Clear();
            if (forEdit)
            {
                //buttonSaveWaypointTripEdits.IsEnabled = true;
                _waypoints = new List<Waypoint>();
                dataGridTripWaypoints.AutoGenerateColumns = false;


                //create a list of waypoints for the trip
                var comboWaypoints = new ObservableCollection<string>();
                if (Entities.WaypointViewModel.Waypoints.Count == 0)
                {
                    Entities.WaypointViewModel.ReadWaypointsFromRepository();
                }

                if (Entities.WaypointViewModel.Count > 0)
                {
                    try
                    {
                        var waypoints = Entities.TripViewModel.GetWaypointSelectionForATrip(Entities.TripViewModel.CurrentEntity);
                        if (waypoints != null)
                        {
                            foreach (var wpt in waypoints)
                            {
                                _waypoints.Add(wpt);
                                comboWaypoints.Add(wpt.Name);
                            }
                        }
                        else
                        {
                            configured = false;
                        }
                    }
                    catch (NullReferenceException)
                    {
                        //ignore
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex);
                    }

                }




                //add the columns to the datagrid
                DataGridComboBoxColumn cboWaypointName = new DataGridComboBoxColumn();
                cboWaypointName.Header = "Waypoint";
                cboWaypointName.ItemsSource = comboWaypoints;
                cboWaypointName.SelectedItemBinding = new Binding("WaypointName");
                dataGridTripWaypoints.Columns.Add(cboWaypointName);


                DataGridComboBoxColumn myDGCBC = new DataGridComboBoxColumn();
                myDGCBC.Header = "Type of waypoint";
                var cmbItems = new ObservableCollection<string> { "Set", "Haul" };
                myDGCBC.ItemsSource = cmbItems;
                myDGCBC.SelectedItemBinding = new Binding("WaypointType");
                dataGridTripWaypoints.Columns.Add(myDGCBC);


                dataGridTripWaypoints.Columns.Add(new DataGridTextColumn { Header = "Set #", Binding = new Binding("SetNumber") });


                var col = new DataGridTextColumn()
                {
                    Binding = new Binding("TimeStampAdjusted"),
                    Header = "Time stamp"
                };
                col.IsReadOnly = true;
                col.Binding.StringFormat = "MMM-dd-yyyy HH:mm:ss";
                dataGridTripWaypoints.Columns.Add(col);
            }
            else
            {
                dataGridTripWaypoints.Columns.Add(new DataGridTextColumn { Header = "Waypoint", Binding = new Binding("WaypointName") });
                dataGridTripWaypoints.Columns.Add(new DataGridTextColumn { Header = "Type of waypoint", Binding = new Binding("WaypointType") });
                dataGridTripWaypoints.Columns.Add(new DataGridTextColumn { Header = "Set #", Binding = new Binding("SetNumber") });

                var col = new DataGridTextColumn()
                {
                    Binding = new Binding("TimeStampAdjusted"),
                    Header = "Waypoint timestamp"
                };
                col.Binding.StringFormat = "MMM-dd-yyyy HH:mm";
                dataGridTripWaypoints.Columns.Add(col);

                //dataGridTripWaypoints.Columns.Add(new DataGridTextColumn { Header = "Waypoint source GPX", Binding = new Binding("WaypointGPXFileName") });


            }
            return configured;

            //dataGridWaypoints.Columns.Add(new DataGridTextColumn { Header = "Date and time", Binding = new Binding("TimeStampAdjustedDisplay"), IsReadOnly = true });
        }
        private void onCheckBoxCheckChange(object sender, RoutedEventArgs e)
        {
            var chk = (CheckBox)sender;

            switch (chk.Name)
            {
                case "checkEditTripWaypoints":

                    try
                    {
                        dataGridTripWaypoints.CanUserAddRows = false;
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex);
                    }
                    dataGridTripWaypoints.IsReadOnly = true;
                    buttonSaveWaypointTripEdits.IsEnabled = false;
                    if ((bool)chk.IsChecked)
                    {
                        if (ConfigureTripWaypointsGrid(true))
                        {
                            var tripWaypoints = Entities.TripWaypointViewModel.GetAllTripWaypoints(_selectedTrip.TripID);


                            foreach (var item in tripWaypoints)
                            {
                                _tripwaypointsCopy.Add(new TripWaypoint
                                {
                                    Trip = _selectedTrip,
                                    RowID = item.RowID,
                                    Waypoint = item.Waypoint,
                                    TimeStamp = item.TimeStamp,
                                    TimeStampAdjusted = item.TimeStampAdjusted,
                                    SetNumber = item.SetNumber,
                                    WaypointType = item.WaypointType,
                                    WaypointName = item.WaypointName
                                });
                            }

                            dataGridTripWaypoints.ItemsSource = tripWaypoints;
                            //dataGridGPSSummary.IsReadOnly = false;
                            dataGridTripWaypoints.CanUserAddRows = true;
                            dataGridTripWaypoints.IsReadOnly = false;
                        }
                        else
                        {
                            checkEditTripWaypoints.IsChecked = false;
                            MessageBox.Show("There are no waypoints for the current trip", "GPX-Manager", MessageBoxButton.OK, MessageBoxImage.Information);

                        }
                    }
                    else
                    {
                        if (_tripWaypointsDirty)
                        {
                            dataGridTripWaypoints.ItemsSource = null;
                            dataGridTripWaypoints.Items.Clear();
                            dataGridTripWaypoints.ItemsSource = _tripwaypointsCopy;
                        }
                    }

                    break;
            }
        }

        private void OnDataGridCellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            string wptName;
            DateTime wayPointTime;
            switch (e.Column.Header)
            {
                case "Waypoint":
                    wptName = ((ComboBox)e.EditingElement).Text;
                    if (wptName.Length > 0)
                    {
                        wayPointTime = GetAdjustedTimeofWaypoint(wptName);
                        dataGridTripWaypoints.GetCell(e.Row.GetIndex(), 3).Content = wayPointTime;
                    }
                    break;
            }
        }
        private DateTime GetAdjustedTimeofWaypoint(string wpt)
        {

            return Entities.WaypointViewModel.GetWaypoint(wpt, _selectedTrip).Time.AddHours(Global.Settings.HoursOffsetGMT);

        }

        private DateTime GetTimeofWaypoint(string wpt)
        {

            return Entities.WaypointViewModel.GetWaypoint(wpt, _selectedTrip).Time;

        }
        private void OnDataGridRowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            _tripWaypointsDirty = true;
            buttonSaveWaypointTripEdits.IsEnabled = true;
            TripWaypoint tw = e.Row.Item as TripWaypoint;
            tw.Trip = _selectedTrip;
            var cbo = ((ComboBox)dataGridTripWaypoints.GetCell(e.Row.GetIndex(), 0).Content);
            tw.WaypointName = cbo.SelectedItem?.ToString() == null ? "" : cbo.SelectedItem.ToString();
            cbo = ((ComboBox)dataGridTripWaypoints.GetCell(e.Row.GetIndex(), 1).Content);
            tw.WaypointType = cbo.SelectedItem?.ToString() == null ? "" : cbo.SelectedItem.ToString();
            var setNumber = ((TextBlock)dataGridTripWaypoints.GetCell(e.Row.GetIndex(), 2).Content).Text;
            if (int.TryParse(setNumber, out int v))
            {
                tw.SetNumber = v;
            }
            if (tw.WaypointName.Length > 0)
            {
                tw.TimeStamp = GetTimeofWaypoint(tw.WaypointName);
            }

            var result = Entities.TripWaypointViewModel.ValidateTripWaypoint(tw, tw.RowID == 0);
            if (result.ErrorMessage.Length == 0)
            {
                tw.Waypoint = _waypoints.FirstOrDefault(t => t.Name == tw.WaypointName);
            }
            else
            {
                e.Cancel = true;
                MessageBox.Show(result.ErrorMessage, "GPX Manager", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnCybertrackerTreeItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            cybertrackerPanelGridFilesAvailable.Visibility = Visibility.Collapsed;
            switch (((TreeViewItem)e.NewValue).Header.ToString())
            {
                case "Connect to server":
                    cybertrackerPanelGrid.Visibility = Visibility.Collapsed;
                    cybertrackerPanelTools.Visibility = Visibility.Collapsed;
                    cybertrackerPanelFTP.Visibility = Visibility.Visible;
                    break;
                case "Users":
                    cybertrackerPanelGrid.Visibility = Visibility.Visible;
                    cybertrackerPanelFTP.Visibility = Visibility.Collapsed;
                    cybertrackerPanelTools.Visibility = Visibility.Collapsed;
                    break;
                case "Tools":
                    cybertrackerPanelGrid.Visibility = Visibility.Collapsed;
                    cybertrackerPanelFTP.Visibility = Visibility.Collapsed;
                    cybertrackerPanelTools.Visibility = Visibility.Visible;
                    break;
            }

        }
    }
}