using AxMapWinGIS;
using GPXManager.views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using MapWinGIS;
using System.IO;
using System.Xml;
using System.Runtime.InteropServices;
using System.Management;
using System.Security.Policy;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Threading;
using GPXManager.entities.mapping.Views;

namespace GPXManager.entities.mapping
{
    public static class MapWindowManager
    {
        private static Shapefile _coastline;
        private static List<int> _selectedShapedIDs;


        public static ShapeFileAttributesWindow ShapeFileAttributesWindow { get; set; }

        public static MapLayersWindow MapLayersWindow { get; set; }
        public static MapWindowForm MapWindowForm { get; private set; }

        public static MapLayer GPXTracksLayer { get; set; }

        public static MapLayer GPXTrackVerticesLayer { get; set; }
        public static MapLayer GPXWaypointsLayer { get; set; }
        public static MapLayer CTXTrackVerticesLayer { get; set; }
        public static MapLayer CTXTracksLayer { get; set; }

        public static MapLayer CTXWaypointsLayer { get; set; }


        public static CTXFile CTXFile { get; set; }

        public static GPXFile TrackGPXFile { get; set; }


        public static MapLayersViewModel MapLayersViewModel { get; set; }
        public static MapLayersHandler MapLayersHandler { get; private set; }
        public static MapInterActionHandler MapInterActionHandler { get; private set; }

        public static int[] SelectedTrackIndexes { get; private set; }

        public static List<Shape> SelectedTracks { get; private set; } = new List<Shape>();
        public static bool SelectTracksInAOI(AOI aoi)
        {
            if (ExtractedTracksShapefile == null)
            {
                return false;
            }
            else
            {
                SelectedTracks.Clear();
                Callback cb = new Callback();
                var selectedTracks = new object();
                var ext = ((Shapefile)MapLayersHandler[aoi.AOIHandle].LayerObject).Extents;
                ExtractedTracksShapefile.SelectShapes(ext, 0, SelectMode.INTERSECTION, ref selectedTracks);
                SelectedTrackIndexes = (int[])selectedTracks;
                for (int x = 0; x < SelectedTrackIndexes.Count(); x++)
                {
                    if (ExtractedTracksShapefile.ShapeVisible[SelectedTrackIndexes[x]])
                    {
                        SelectedTracks.Add(ExtractedTracksShapefile.Shape[SelectedTrackIndexes[x]]);
                    }
                }
            }

            return SelectedTrackIndexes.Count() > 0;

        }
        public static Shapefile Coastline
        {
            get
            {
                if (Global.Settings.CoastlineIDFieldName == null || Global.Settings.CoastlineIDFieldName == "")
                {
                    ShapefileAttributeTableManager.SetupIDColumn(_coastline, true);
                    Global.Settings.CoastlineIDFieldName = ShapefileAttributeTableManager.UnqiueIDColumnName;
                    Global.Settings.CoastlineIDFieldIndex = ShapefileAttributeTableManager.UnqiueIDColumnIndex;
                }
                return _coastline;
            }
            private set { _coastline = value; }
        }
        public static AxMap MapControl { get; private set; }
        public static Dictionary<int, string> TileProviders { get; set; } = new Dictionary<int, string>();
        public static void CleanUp(bool applicationIsClosing = false)
        {
            if (MapLayersViewModel != null)
            {
                MapLayersViewModel.CleanUp();
                MapLayersViewModel = null;
            }

            MapInterActionHandler = null;
            MapLayersHandler = null; ;
            MapControl = null;
            Coastline = null;
            MapWindowForm = null;

            if (MapLayersWindow != null)
            {
                MapLayersWindow = null;
            }

            ShapeFileAttributesWindow = null;
            GPXTracksLayer = null;
            GPXWaypointsLayer = null;

            if (applicationIsClosing)
            {
                TileProviders = null;
            }
        }

        public static void RemoveGPSDataFromMap()
        {
            RemoveLayerByKey("gpxfile_track");
            RemoveLayerByKey("gpxfile_waypoint");
            RemoveLayerByKey("trip_track");
            RemoveLayerByKey("trip_waypoints");
            RemoveLayerByKey("gpx_track_vertices");
            RemoveLayerByKey("named_points_from_gpx");
            RemoveLayerByKey("gpx_waypoints");
            RemoveLayerByKey("ctx_track");
            RemoveLayerByKey("ctxfile_waypoint");
            RemoveLayerByKey("ctx_track_vertices");
            RemoveLayerByKey("fishing_trackline");
            RemoveLayerByKey("extracted_tracks");
            GPXMappingManager.RemoveAllFromMap();
        }
        public static string CoastLineFile
        {
            get { return $@"{globalMapping.ApplicationPath}\Layers\Coastline\philippines_polygon.shp"; }
        }


        static MapWindowManager()
        {
            GlobalSettings gs = new GlobalSettings();
            if (Global.Settings?.BingAPIKey.Length > 0)
            {
                gs.BingApiKey = Global.Settings.BingAPIKey;
            }

            TileProviders.Add(0, "OpenStreetMap");
            TileProviders.Add(1, "OpenCycleMap");
            TileProviders.Add(2, "OpenTransportMap");

            if (Global.Settings?.BingAPIKey.Length > 0)
            {
                TileProviders.Add(3, "BingMaps");
                TileProviders.Add(4, "BingSatellite");
                TileProviders.Add(5, "BingHybrid");
            }
            TileProviders.Add(6, "GoogleMaps");
            //TileProviders.Add(7, "GoogleSatellite");
            //TileProviders.Add(8, "GoogleHybrid");
            TileProviders.Add(9, "GoogleTerrain");
            //TileProviders.Add(10, "HereMaps");
            //TileProviders.Add(11, "HereSatellite");
            //TileProviders.Add(12, "HereHybrid");
            //TileProviders.Add(13, "HereTerrain");
            //TileProviders.Add(21, "Rosreestr");
            TileProviders.Add(22, "OpenHumanitarianMap");
            //TileProviders.Add(23, "MapQuestAerial");

        }
        public static MapWindowForm OpenMapWindow(MainWindow ownerWindow, bool showCoastline = false)
        {

            MapWindowForm mwf = MapWindowForm.GetInstance();
            if (mwf.Visibility == Visibility.Visible)
            {
                MapWindowForm.BringIntoView();
            }
            else
            {
                MapWindowForm = mwf;
                MapWindowForm.LocationChanged += MapWindowForm_LocationChanged;
                MapWindowForm.Closing += MapWindowForm_Closing;
                MapWindowForm.Owner = ownerWindow;
                MapWindowForm.ParentWindow = ownerWindow;
                MapWindowForm.Show();

                MapLayersHandler = MapWindowForm.MapLayersHandler;
                MapInterActionHandler = MapWindowForm.MapInterActionHandler;
                ShapefileAttributeTableManager.MapInterActionHandler = MapInterActionHandler;
                AOIManager.Setup();
                MapControl = MapWindowForm.MapControl;
                if (!MapStateFileExists)
                {
                    LoadCoastline(CoastLineFile);
                    MapControl.TileProvider = tkTileProvider.ProviderNone;
                }
                else if (Coastline == null && showCoastline)
                {
                    LoadCoastline(CoastLineFile);
                }
            }


            if (MapLayersWindow != null)
            {
                MapLayersWindow.Visibility = Visibility.Visible;
                MapLayersWindow.BringIntoView();
            }

            if (ShapeFileAttributesWindow != null)
            {
                ShapeFileAttributesWindow.Visibility = Visibility.Visible;
                ShapeFileAttributesWindow.BringIntoView();
            }
            MapLayersViewModel = new MapLayersViewModel(MapLayersHandler);

            return mwf;
        }


        private static void MapWindowForm_LocationChanged(object sender, EventArgs e)
        {
            if (MapLayersWindow != null)
            {
                MapLayersWindow.Left = MapWindowForm.Left - MapLayersWindow.Width;
                MapLayersWindow.Top = MapWindowForm.Top;
            }

            if (ShapeFileAttributesWindow != null)
            {

            }
        }

        public static void RestoreMapState(MapWindowForm mwf)
        {

            string path = $"{AppDomain.CurrentDomain.BaseDirectory}/mapstate.txt";
            if (File.Exists(path))
            {
                double extentsLeft = 0;
                double extentsRight = 0;
                double extentsTop = 0;
                double extentsBottom = 0;
                bool hasCoastline = false;
                bool isCoastlineVisible = false;

                if (MapControl == null || MapLayersHandler == null || MapInterActionHandler == null)
                {
                    MapControl = mwf.MapControl;
                    MapLayersHandler = mwf.MapLayersHandler;
                    MapInterActionHandler = mwf.MapInterActionHandler;
                }


                using (XmlReader reader = XmlReader.Create(path))
                {
                    while (reader.Read())
                    {
                        if (reader.IsStartElement())
                        {
                            switch (reader.Name)
                            {
                                case "MapState":
                                    extentsLeft = double.Parse(reader.GetAttribute("ExtentsLeft"));
                                    extentsRight = double.Parse(reader.GetAttribute("ExtentsRight"));
                                    extentsTop = double.Parse(reader.GetAttribute("ExtentsTop"));
                                    extentsBottom = double.Parse(reader.GetAttribute("ExtentsBottom"));
                                    hasCoastline = reader.GetAttribute("HasCoastline") == "1";

                                    if (hasCoastline)
                                    {
                                        isCoastlineVisible = reader.GetAttribute("CoastlineVisible") == "1";
                                    }
                                    break;
                                case "Layers":
                                    break;
                                case "Tiles":
                                    if (reader.GetAttribute("Visible") == "0")
                                    {
                                        MapControl.TileProvider = tkTileProvider.ProviderNone;
                                    }
                                    else
                                    {
                                        string tileProvider = reader.GetAttribute("Provider");
                                        if (tileProvider != null && tileProvider.Length > 0)
                                        {
                                            MapControl.TileProvider = (tkTileProvider)Enum.Parse(typeof(tkTileProvider), tileProvider);
                                        }
                                    }
                                    break;
                                case "Layer":
                                    switch (reader.GetAttribute("LayerKey"))
                                    {
                                        case "coastline":
                                            if (hasCoastline)
                                            {
                                                var coastlineFile = reader.GetAttribute("Filename");
                                                if (File.Exists(coastlineFile))
                                                {
                                                    MapWindowManager.LoadCoastline(coastlineFile, isCoastlineVisible);
                                                }
                                            }
                                            break;
                                        case "aoi_boundary":
                                            var layerName = reader.GetAttribute("LayerName");
                                            if (Entities.AOIViewModel.Count > 0)
                                            {
                                                var aoi = Entities.AOIViewModel.GetAOI(layerName);
                                                if (aoi != null)
                                                {
                                                    aoi.MapLayerHandle = MapWindowManager.MapLayersHandler.AddLayer(aoi.ShapeFile, aoi.Name, uniqueLayer: true, layerKey: "aoi_boundary");
                                                    AOIManager.UpdateAOIName(aoi.MapLayerHandle, aoi.Name);
                                                }
                                            }
                                            break;
                                    }
                                    break;

                            }
                        }
                    }
                }

                var ext = new Extents();
                ext.SetBounds(extentsLeft, extentsBottom, 0, extentsRight, extentsTop, 0);
                MapControl.Extents = ext;

                // MapControl.Redraw();
            }
        }
        public static void RedrawMap()
        {
            MapControl.Redraw();
        }

        public static string LayersFolder = $@"{AppDomain.CurrentDomain.BaseDirectory}\Layers";

        public static bool AddShapefileLayer(string sf, out string feedBack)
        {
            if (File.Exists(sf))
            {

                FileInfo fi = new FileInfo(sf);
                if (fi.Extension == ".shp")
                {
                    DirectoryInfo dirInfo;
                    dirInfo = new DirectoryInfo(Path.GetDirectoryName(sf));
                    var listFiles = Directory.GetFiles(dirInfo.FullName, $"{Path.GetFileNameWithoutExtension(fi.Name)}.*");
                    var proposedPath = $@"{LayersFolder}\{Path.GetFileNameWithoutExtension(fi.Name)}";
                    if (!Directory.Exists(proposedPath))
                    {
                        dirInfo = Directory.CreateDirectory(proposedPath);
                    }
                    else
                    {
                        dirInfo = new DirectoryInfo(proposedPath);
                    }
                    var files = Directory.GetFiles(dirInfo.FullName, "*.shp");
                    if (files.Length == 0)
                    {
                        foreach (var item in listFiles)
                        {
                            File.Copy(item, $@"{dirInfo.FullName}\{Path.GetFileName(item)}");
                        }
                        files = Directory.GetFiles(dirInfo.FullName, "*.shp");
                        var result = MapLayersHandler.FileOpenHandler(files[0], $"{fi.Name}", layerkey: $"{fi.Name}");
                        feedBack = result.errMsg;
                        return result.success;
                    }
                }
            }

            feedBack = "Folder for LGU boundary not found";
            return false;
        }

        private static Shapefile _bscBoundaryShapefile;


        public static Shapefile BSCBoundaryShapefile { get; set; }
        public static Shapefile BSCBoundaryShapefile1
        {
            get
            {
                var boundaryFolder = $@"{LayersFolder}\BSCBoundaryLine";
                if (Directory.Exists(boundaryFolder))
                {
                    var boundarySF = Directory.GetFiles(boundaryFolder, "*.shp");
                    if (boundarySF.Length == 1)
                    {
                        MapLayersHandler.FileOpenHandler(boundarySF[0], "BSC boundary", layerkey: "bsc_boundary");
                        _bscBoundaryShapefile = (Shapefile)MapLayersHandler.CurrentMapLayer.LayerObject;
                        ShapefileFactory.BSCBoundaryLine = _bscBoundaryShapefile.Shape[0];
                        return _bscBoundaryShapefile;
                    }
                    else
                    {
                        return null;
                    }
                }
                return null;
            }
            set
            {
                _bscBoundaryShapefile = value;
                ShapefileFactory.BSCBoundaryLine = _bscBoundaryShapefile.Shape[0];
            }
        }

        public static Shape BSCBoundaryLine
        {
            get
            {
                if (BSCBoundaryShapefile == null)
                {
                    return null;
                }
                else
                {
                    return BSCBoundaryShapefile.Shape[0];
                }
            }
        }

        public static Shapefile Grid25MajorGrid { get; set; }
        public static bool AddExtractedTracksLayer()
        {
            if (Entities.ExtractedFishingTrackViewModel.Count() > 0)
            {
                var sf = ShapefileFactory.FishingTrackLines();
                ExtractedTracksShapefile = sf;
                sf.SelectionAppearance = tkSelectionAppearance.saDrawingOptions;
                sf.DefaultDrawingOptions.LineColor = new Utils().ColorByName(tkMapColor.Salmon);
                sf.SelectionDrawingOptions.LineWidth = 4f;
                return MapLayersHandler.AddLayer(sf, "Fishing tracks", layerKey: sf.Key, uniqueLayer: true) >= 0;

            }
            return false;
        }

        public static bool SetBoundaryShapefile()
        {
            var boundaryFolder = $@"{LayersFolder}\BSCBoundaryLine";
            if (Directory.Exists(boundaryFolder))
            {
                var boundarySF = Directory.GetFiles(boundaryFolder, "*.shp");
                if (boundarySF.Length == 1)
                {
                    MapLayersHandler.FileOpenHandler(boundarySF[0], "BSC boundary", layerkey: "bsc_boundary");
                    BSCBoundaryShapefile = (Shapefile)MapLayersHandler.CurrentMapLayer.LayerObject;
                    return true;
                }

            }
            return false;
        }
        public static bool AddBSCBoundaryLineShapefile(string inSF, out string feedBack)
        {



            var boundaryFolder = $@"{LayersFolder}\BSCBoundaryLine";
            if (File.Exists(inSF))
            {
                FileInfo fi = new FileInfo(inSF);
                if (fi.Extension == ".shp")
                {

                    if (!Directory.Exists(boundaryFolder))
                    {
                        Directory.CreateDirectory(boundaryFolder);
                    }


                    var sourceDirInfo = new DirectoryInfo(Path.GetDirectoryName(inSF));
                    var listFiles = Directory.GetFiles(sourceDirInfo.FullName, $"{Path.GetFileNameWithoutExtension(fi.Name)}.*");

                    var files = Directory.GetFiles(boundaryFolder, "*.shp");
                    if (files.Length == 0)
                    {
                        foreach (var item in listFiles)
                        {
                            File.Copy(item, $@"{boundaryFolder}\{Path.GetFileName(item)}");
                        }
                        files = Directory.GetFiles(boundaryFolder, "*.shp");
                        var result = MapLayersHandler.FileOpenHandler(files[0], "BSC boundary", layerkey: "bsc_boundary");
                        BSCBoundaryShapefile = (Shapefile)MapLayersHandler.CurrentMapLayer.LayerObject;
                        feedBack = result.errMsg;
                        return result.success;
                    }
                }
            }



            feedBack = "Folder for LGU boundary not found";
            return false;

        }
        public static bool AddLGUBoundary(out string feedBack)
        {

            if (Directory.Exists($@"{LayersFolder}\LGUBoundary"))
            {
                var files = Directory.GetFiles($@"{LayersFolder}\LGUBoundary", "*.shp");
                if (files.Length >= 0)
                {
                    var result = MapLayersHandler.FileOpenHandler(files[0], "LGU boundary", layerkey: "lgu_boundary");
                    feedBack = result.errMsg;
                    return result.success;
                }
            }
            feedBack = "Folder for LGU boundary not found";
            return false;

        }
        public static string LastError { get; set; }


        public static bool SaveMapState()
        {
            var mapState = MapControl.SerializeMapState(false, mapping.globalMapping.ApplicationPath);
            var filepath = $"{AppDomain.CurrentDomain.BaseDirectory}mapstate.txt";
            if (File.Exists(filepath))
            {
                try
                {
                    File.Delete(filepath);

                }
                catch (IOException)
                {
                    try
                    {
                        File.WriteAllText(filepath, String.Empty);
                    }
                    catch (Exception ex)
                    {
                        LastError = ex.Message;
                        return false; ;
                    }

                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                    LastError = ex.Message;
                    return false;
                }
            }

            //insert custom settings at this point
            string xml = InsertCustomSettingToMapState(mapState);

            using (StreamWriter writer = new StreamWriter(filepath, true))
            {
                writer.Write(PrettyXML.PrettyPrint(xml));
            }


            return true;
        }

        public static bool MapStateFileExists
        {
            get { return File.Exists($@"{AppDomain.CurrentDomain.BaseDirectory}mapstate.txt"); }
        }
        private static string InsertCustomSettingToMapState(string xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            var mapstate = doc.GetElementsByTagName("MapState").Item(0);

            var coastlineLayer = MapLayersHandler.get_MapLayer("Coastline");
            XmlAttribute attr = doc.CreateAttribute("HasCoastline");
            attr.Value = coastlineLayer != null ? "1" : "0";
            mapstate.Attributes.SetNamedItem(attr);

            if (coastlineLayer != null)
            {
                attr = doc.CreateAttribute("CoastlineVisible");
                attr.Value = coastlineLayer.Visible ? "1" : "0";
                mapstate.Attributes.SetNamedItem(attr);
            }
            return doc.OuterXml;

        }
        public static void ResetCursor()
        {
            if (MapControl != null)
            {
                MapControl.CursorMode = tkCursorMode.cmNone;
                MapControl.MapCursor = tkCursor.crsrArrow;
            }
        }

        public static void ZoomToShapeFileExtent(Shapefile sf)
        {
            MapControl.Extents = sf.Extents;
            MapControl.Redraw();
        }

        public static void SetLayerVisibility(int layerHandle, bool visibility)
        {

        }

        public static bool LoadCoastline(string coastlineShapeFile_FileName, bool visible = true)
        {
            bool coastlineLoaded = false;
            for (int h = 0; h < MapControl.NumLayers - 1; h++)
            {
                var sf = MapControl.get_GetObject(h) as Shapefile;
                if (sf.Key == "coastline")
                {
                    coastlineLoaded = true;
                    Coastline = sf;
                    break;
                }
            }


            if (!coastlineLoaded && coastlineShapeFile_FileName != null && coastlineShapeFile_FileName.Length > 0)
            {
                Shapefile sf = new Shapefile();
                if (sf.Open(coastlineShapeFile_FileName))
                {
                    sf.Key = "coastline";
                    var h = MapLayersHandler.AddLayer(sf, "Coastline", uniqueLayer: true, isVisible: visible, layerKey: "coastline", rejectIfExisting: true);
                    Coastline = sf;
                }
            }
            return Coastline != null;
        }
        private static void MapWindowForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MapWindowForm = null;
        }


        public static List<int> SelectedAttributeRows
        {
            get { return _selectedShapedIDs; }
            set
            {
                if (MapLayersHandler.CurrentMapLayer != null)
                {
                    _selectedShapedIDs = value;
                    var sf = (Shapefile)MapLayersHandler.CurrentMapLayer.LayerObject;
                    sf.SelectionDrawingOptions.LineWidth = 4f;
                    sf.SelectNone();
                    foreach (var h in _selectedShapedIDs)
                    {
                        sf.ShapeSelected[h] = true;
                    }
                    MapLayersHandler.MapControl.Redraw();
                }
            }
        }

        public static void RemoveLayerByKey(string key)
        {
            MapLayersHandler.RemoveLayerByKey(key);
        }


        public static int MapWaypointList(List<WaypointLocalTime> wpts, out List<int> handles)
        {
            handles = new List<int>();
            if (wpts.Count > 0)
            {
                var sf = ShapefileFactory.PointsFromWaypointList(wpts, out handles);
                MapLayersHandler.AddLayer(sf, "GPX waypoints", uniqueLayer: true, layerKey: sf.Key, rejectIfExisting: true);
            }
            return MapLayersHandler.CurrentMapLayer.Handle;
        }

        public static Shapefile ExtractedTracksShapefile { get; set; }

        public static int MapExtractedFishingTrack(FishingTripAndGearRetrievalTracks th, out List<int> handles)
        {
            handles = new List<int>();
            if (th != null && th.GearRetrievalTracks.Count > 0 && ShapefileFactory.ExtractFishingTrackLine)
            {
                var sf = ShapefileFactory.FishingTrackLine(th);
                if (sf != null)
                {
                    return MapLayersHandler.AddLayer(sf, "Extracted fishng track", uniqueLayer: true, layerKey: sf.Key);
                }
            }
            return -1;
        }
        public static int MapExtractedFishingTrack(out List<int> handles)
        {
            handles = new List<int>();
            if (ShapefileFactory.ExtractFishingTrackLine)
            {
                var sf = ShapefileFactory.FishingTrackLine();
                if (sf != null)
                {
                    //return MapLayersHandler.AddLayer(sf, "Extracted fishng track", uniqueLayer: true, layerKey: sf.Key, rejectIfExisting: true);
                    return MapLayersHandler.AddLayer(sf, "Extracted fishng track", uniqueLayer: true, layerKey: sf.Key);
                }
            }
            return -1;

        }
        public static int MapWaypointsCTX(CTXFileSummaryView ctx, out List<int> handles)
        {
            handles = new List<int>();
            if (ctx.WaypointsForSet != null && ctx.WaypointsForSet > 0 ||
                ctx.WaypointsForHaul != null && ctx.WaypointsForHaul > 0)
            {
                var sf = ShapefileFactory.WaypointsFromCTX(ctx, out handles);
                MapLayersHandler.AddLayer(sf, "CTX waypoints", uniqueLayer: true, layerKey: sf.Key, rejectIfExisting: true);
            }
            return MapLayersHandler.CurrentMapLayer.Handle;
        }


        public static FishingTripAndGearRetrievalTracks MapTrackCTX(CTXFileSummaryView ctx)
        {
            if (ctx.TrackpointsCount != null && ctx.TrackpointsCount > 0)
            {
                var result = ShapefileFactory.CreateTripAndHaulsFromCTX(ctx);
                if (result != null)
                {
                    MapLayersHandler.AddLayer(result.TripShapefile, "CTX track", uniqueLayer: true, layerKey: result.TripShapefile.Key, rejectIfExisting: true);
                }
                return result;
            }
            return null;
        }

        public static FishingTripAndGearRetrievalTracks MapTrackGPX(GPXFile gpxfile)
        {

            if (gpxfile.Tracks.Count > 0)
            {
                var result = ShapefileFactory.CreateTripAndHaulsFromGPX(gpxfile);
                if (result != null)
                {

                    MapLayersHandler.AddLayer(result.TripShapefile, "GPX track", uniqueLayer: true, layerKey: result.TripShapefile.Key, rejectIfExisting: true);
                }
                return result;
            }
            return null;
        }

        public static int MapTrip(Trip trip, out int shpIndex, out List<int> handles, bool showInMap = true)
        {
            shpIndex = -1;
            handles = new List<int>();
            var utils = new MapWinGIS.Utils();
            var shpfileName = "";
            if (showInMap)
            {
                if (trip.Track.Waypoints.Count > 0)
                {
                    Shapefile sf = null;
                    List<Trip> trips = new List<Trip>();
                    trips.Add(trip);
                    sf = ShapefileFactory.TrackFromTrip(trips, out handles);
                    shpfileName = "Trip tracks";
                    MapLayersHandler.AddLayer(sf, shpfileName, uniqueLayer: true, layerKey: sf.Key, rejectIfExisting: true);
                }


            }
            return MapLayersHandler.CurrentMapLayer.Handle;
        }

        internal static void LabelTripWaypopints()
        {

        }


        public static int MapExtractedFishingTracksShapefile(Shapefile extractedTracks)
        {
            RemoveGPSDataFromMap();
            var h=MapLayersHandler.AddLayer(extractedTracks, "Extracted tracks", uniqueLayer: true, layerKey: extractedTracks.Key, rejectIfExisting: true);

            var sf = (Shapefile)MapLayersHandler[h].LayerObject;
            sf.DefaultDrawingOptions.LineColor = new Utils().ColorByName(tkMapColor.Salmon);
            sf.SelectionAppearance = tkSelectionAppearance.saDrawingOptions;
            sf.SelectionDrawingOptions.LineWidth = 4f;
            return h;

        }

        public static int MapGPX(GPXFile gpxFile, out int shpIndex, out List<int> handles, bool showInMap = true)
        {
            shpIndex = -1;
            handles = new List<int>();
            var utils = new MapWinGIS.Utils();
            var shpfileName = "";
            if (showInMap)
            {
                if (gpxFile != null)
                {
                    Shapefile sf = null;
                    if (gpxFile.TrackCount > 0)
                    {
                        //sf = ShapefileFactory.TrackFromGPX(gpxFile,out shpIndex);
                        sf = ShapefileFactory.TracksFromGPXFiles(gpxFile, out handles);
                        //var result = ShapefileFactory.CreateTripAndHaulsFromGPX(gpxFile);
                        //sf = result.Shapefile;
                        //handles.Add(result.Handle);
                        //Console.WriteLine($"shapefile with {sf.Shape[0].numPoints} created. Handle is {result.Handle}");
                        shpfileName = "GPX tracks";
                    }
                    else if (gpxFile.WaypointCount > 0)
                    {
                        sf = ShapefileFactory.NamedPointsFromGPX(gpxFile, out handles);
                        shpfileName = "GPX waypoints";

                    }
                    //MapWindowForm.Title =$"Number of layers:{MapControl.NumLayers}";
                    MapLayersHandler.AddLayer(sf, shpfileName, uniqueLayer: true, layerKey: sf.Key, rejectIfExisting: true);

                    if (gpxFile.TrackCount > 0)
                    {
                        GPXTracksLayer = MapLayersHandler.CurrentMapLayer;
                    }
                    else if (gpxFile.WaypointCount > 0)
                    {

                        GPXWaypointsLayer = MapLayersHandler.CurrentMapLayer;

                    }

                    return MapLayersHandler.CurrentMapLayer.Handle;
                }
                else
                {
                    return -1;
                }
            }
            else
            {
                var ly = MapLayersHandler.get_MapLayer(gpxFile.FileName);
                MapLayersHandler.RemoveLayer(ly.Handle);
                return -1;
            }

        }
    }
}
