using AxMapWinGIS;

using MapWinGIS;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Xml;
using GPXManager.entities;
using System.Linq;


namespace GPXManager.entities.mapping

{
    public enum VisibilityExpressionTarget
    {
        ExpressionTargetLabel,
        ExpressionTargetShape
    }

    /// <summary>
    /// Manages layers
    /// </summary>
    public class MapLayersHandler : IDisposable, IEnumerable<MapLayer>
    {
        private string _fileMapState;
        public bool _disposed;
        private AxMap _axmap;                                                                       //reference to tha map control in the mapping form
        public Dictionary<int, MapLayer> MapLayerDictionary { get; set; } = new Dictionary<int, MapLayer>();    //contains MapLayers with the layer handle as key
        private MapLayer _currentMapLayer;                                                          //the current layer selected in the map layers form
        private ShapefileLabelHandler _sfLabelHandler;
        private PointLayerSymbologyHandler _sfSymbologyHandler;

        private bool _tilesVisible = false;



        public bool TilesVisible
        {
            get { return _tilesVisible; }
            set
            {
                _tilesVisible = value;
                _axmap.Tiles.Visible = _tilesVisible;
            }
        }



        //public ColorSchemes LayerColors;

        public event EventHandler MapRedrawNeeded;

        public event EventHandler LayerRefreshNeeded;

        public event EventHandler AllSelectionsCleared;

        public delegate void LayerPositionChangedHandler(MapLayersHandler s);                 //an event that is raised when a layer positions changed
        public event LayerReadHandler LayerPositionChanged;

        public event EventHandler LayerClassificationFinished;

        public delegate void LayerReadHandler(MapLayersHandler s, LayerEventArg e);                 //an event that is raised when a layer from the mapcontrol is retrieved
        public event LayerReadHandler LayerRead;                                                    //in order for the listener is able to add the layer to the layers list

        public delegate void LayerRemovedHandler(MapLayersHandler s, LayerEventArg e);
        public event LayerRemovedHandler LayerRemoved;

        public delegate void CurrentLayerHandler(MapLayersHandler s, LayerEventArg e);              //event raised when a layer is selected from the list found in the layers form
        public event CurrentLayerHandler CurrentLayer;

        public delegate void VisibilityExpressionSet(MapLayersHandler s, LayerEventArg e);
        public event VisibilityExpressionSet OnVisibilityExpressionSet;

        public delegate void LayerNameUpdate(MapLayersHandler s, LayerEventArg e);
        public event LayerNameUpdate OnLayerNameUpdate;

        public delegate void LayerVisibilityChanged(MapLayersHandler s, LayerEventArg e);
        public event LayerNameUpdate OnLayerVisibilityChanged;

        
        public void UpdateCurrentLayerName(string layerName)
        {
            if (OnLayerNameUpdate != null)
            {
                //fill up the event argument class with the layer item
                _currentMapLayer.Name = layerName;
                _axmap.set_LayerName(_currentMapLayer.Handle, layerName);
                LayerEventArg lp = new LayerEventArg(_currentMapLayer.Handle);
                lp.LayerName = _currentMapLayer.Name;
                OnLayerNameUpdate(this, lp);
            }
        }

        /// <summary>
        /// add the MBR of a target area as a new map layer
        /// </summary>
        /// <param name="moveMapToMBRCenter"></param>

        public void MoveToTop()
        {
            _axmap.MoveLayerTop(_axmap.get_LayerPosition(_currentMapLayer.Handle));
            _axmap.Redraw();
        }

        public void MoveUp()
        {
            _axmap.MoveLayerUp(_axmap.get_LayerPosition(_currentMapLayer.Handle));
            _axmap.Redraw();
        }

        public void MoveDown()
        {
            _axmap.MoveLayerDown(_axmap.get_LayerPosition(_currentMapLayer.Handle));
            _axmap.Redraw();
        }

        public void MoveToBottom()
        {
            _axmap.MoveLayerBottom(_axmap.get_LayerPosition(_currentMapLayer.Handle));
            _axmap.Redraw();
        }

        public List<MapLayer>MapLayers
        {
            get { return MapLayerDictionary.Values.OrderBy(t => t.LayerPosition).ToList(); }
        }

        IEnumerator<MapLayer> IEnumerable<MapLayer>.GetEnumerator()
        {
            return MapLayerDictionary.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)MapLayerDictionary.Values).GetEnumerator();
        }

        public void ZoomToLayer(int layerHandle)
        {
            _axmap.ZoomToLayer(layerHandle);
        }

        public void SaveLayerSettingsToXML()
        {
            foreach (var item in MapLayerDictionary)
            {
                item.Value.SaveXMLSettings();
            }
        }



        public void RestoreLayerSettingsFromXML()
        {
            foreach (var item in MapLayerDictionary)
            {
                item.Value.RestoreSettingsFromXML();
            }
        }

        public ShapefileLabelHandler ShapeFileLableHandler
        {
            get { return _sfLabelHandler; }
        }

        public PointLayerSymbologyHandler SymbologyHandler
        {
            get { return _sfSymbologyHandler; }
        }

        public AxMap MapControl
        {
            get { return _axmap; }
        }

        /// <summary>
        /// sets the legend image in the layers form
        /// </summary>
        /// <param name="layerHandle"></param>
        /// <param name="pic"></param>
        /// <param name="layerType"></param>
        public void LayerSymbol(int layerHandle, System.Windows.Forms.PictureBox pic, string layerType, ShapeDrawingOptions drawingOptions = null)
        {
            bool isCategory = drawingOptions != null;
            if (pic.Image != null) pic.Image.Dispose();
            Rectangle rect = pic.ClientRectangle;
            int w = rect.Width / 2;
            int h = (rect.Height / 4) * 3;

            Bitmap bmp = new Bitmap(rect.Width, rect.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            Graphics g = Graphics.FromImage(bmp);

            IntPtr ptr = g.GetHdc();
            //int ptr = g.GetHdc().ToInt32();

            var ly = _axmap.get_GetObject(layerHandle);
            switch (layerType)
            {
                case "ShapefileClass":
                    ((Shapefile)ly).With(shp =>
                    {
                        ShapeDrawingOptions sdo = shp.DefaultDrawingOptions;
                        if (drawingOptions != null)
                        {
                            sdo = drawingOptions;
                        }
                        switch (shp.ShapefileType)
                        {
                            case ShpfileType.SHP_POINT:

                                if (isCategory)
                                {
                                    sdo.DrawPoint(ptr, (rect.Width / 5) * 2, rect.Height / 4, 0, 0);
                                }
                                else
                                {
                                    sdo.DrawPoint(ptr, (rect.Width / 5) * 2, rect.Height / 2, 0, 0);
                                }

                                break;

                            case ShpfileType.SHP_POLYGON:
                                sdo.DrawRectangle(ptr, rect.Width / 3, rect.Height / 4, w, h, shp.DefaultDrawingOptions.LineVisible, rect.Width, rect.Height);
                                break;

                            case ShpfileType.SHP_POLYLINE:
                                sdo.DrawLine(ptr, rect.Width / 3, rect.Height / 4, w, h, true, rect.Width, rect.Height);
                                break;
                        }

                        g.ReleaseHdc(ptr);
                        pic.Image = bmp;
                    });

                    break;

                case "ImageClass":
                    if (MapLayerDictionary[layerHandle].ImageThumbnail == null)
                    {
                        string filename = _axmap.get_Image(layerHandle).Filename;
                        bmp = new Bitmap(w, h);
                        g = Graphics.FromImage(bmp);
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        g.FillRectangle(Brushes.White, (rect.Width / 5) * 1, rect.Height / 4, w, h);
                        try
                        {
                            g.DrawImage(new Bitmap(filename), 0, 0, w, h);
                            pic.Image = bmp;
                            MapLayerDictionary[layerHandle].ImageThumbnail = bmp;
                        }
                        catch { }
                    }
                    else
                    {
                        pic.Image = MapLayerDictionary[layerHandle].ImageThumbnail;
                    }
                    break;
            }
        }

        public int CountLayersWithSelection
        {
            get
            {
                int count = 0;
                foreach (var item in MapLayerDictionary.Values)
                {
                    if (item.LayerType == "ShapefileClass")
                    {
                        var sf = item.LayerObject as Shapefile;
                        if (sf.NumSelected > 0)
                        {
                            count++;
                        }
                    }
                }
                return count;
            }
        }
        public bool MapHasSelection
        {
            get
            {
                foreach(var item in MapLayerDictionary.Values)
                {
                    if(item.LayerType=="ShapefileClass")
                    {
                        var sf = item.LayerObject as Shapefile;
                        if(sf.NumSelected>0)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }
        public void VisibilityExpression(string expression, VisibilityExpressionTarget expressiontarget)
        {
            if (expressiontarget == VisibilityExpressionTarget.ExpressionTargetLabel)
            {
                _currentMapLayer.LabelsVisibilityExpression = expression;
            }
            else
            {
                _currentMapLayer.ShapesVisibilityExpression = expression;
            }

            if (OnVisibilityExpressionSet != null)
            {
                //fill up the event argument class with the layer item
                LayerEventArg lp = new LayerEventArg(_currentMapLayer.Handle, expressiontarget, expression);
                OnVisibilityExpressionSet(this, lp);
            }
        }

        public bool Exists(string name)
        {
            foreach (MapLayer item in MapLayerDictionary.Values)
            {
                if (item.Name == name) return true;
            }
            return false;
        }

        public bool Exists(int layerHandle)
        {
            return MapLayerDictionary.ContainsKey(layerHandle);
        }

        public Dictionary<int, MapLayer> LayerDictionary
        {
            get { return MapLayerDictionary; }
        }

        public void ClearAllSelections()
        {
            foreach (var item in MapLayerDictionary)
            {
                if (item.Value.LayerType == "ShapefileClass")
                {
                    _axmap.get_Shapefile(item.Key).SelectNone();
                }
            }
            _axmap.Redraw();

            AllSelectionsCleared?.Invoke(this, EventArgs.Empty);
        }

        public MapLayer CurrentMapLayer
        {
            get { return _currentMapLayer; }
        }

        public int NumLayers
        {
            get { return MapLayerDictionary.Count; }
        }

        public MapLayer set_MapLayer(int layerHandle, bool noSelectedShapes = true, bool refreshLayerList = false)
        {
            _currentMapLayer = MapLayerDictionary[layerHandle];
            if (_currentMapLayer.LayerType == "ShapefileClass")
            {
                _sfLabelHandler = new ShapefileLabelHandler(_currentMapLayer);
                _sfSymbologyHandler = new PointLayerSymbologyHandler(_currentMapLayer);
                if (noSelectedShapes)
                {
                    ((Shapefile)_currentMapLayer.LayerObject).SelectNone();
                }
            }

            //if there are listeners to the event
            if (CurrentLayer != null)
            {
                //fill up the event argument class with the layer item
                LayerEventArg lp = new LayerEventArg(_currentMapLayer.Handle, _currentMapLayer.Name, _currentMapLayer.Visible, _currentMapLayer.VisibleInLayersUI, _currentMapLayer.LayerType);
                CurrentLayer(this, lp);
            }

            if (refreshLayerList)
            {
                RefreshLayers();
            }
            return _currentMapLayer;
        }

        public MapLayer get_MapLayer(string Name)
        {
            foreach (MapLayer item in MapLayerDictionary.Values)
            {
                if (item.Name == Name)
                    return item;
            }
            return null;
        }

        public void RefreshLayers()
        {
            LayerRefreshNeeded?.Invoke(this, EventArgs.Empty);
        }

        public void LayerFinishedClassification()
        {
            LayerClassificationFinished?.Invoke(this, EventArgs.Empty);
        }

        public void RefreshMap()
        {
            MapRedrawNeeded?.Invoke(this, EventArgs.Empty);
        }

        public MapLayer get_MapLayer(int layerHandle)
        {
            try
            {
                return MapLayerDictionary[layerHandle];
            }
            catch
            {
                return null;
            }
        }

        public int get_LayerPosition(int layerHandle)
        {
            return _axmap.get_LayerPosition(layerHandle);
        }

        public bool MoveLayerBottom(int layerHandle)
        {
            var layerMoved = false;
            if (_axmap.MoveLayerBottom(layerHandle))
            {
                try
                {
                    MapLayerDictionary[layerHandle].LayerPosition = _axmap.get_LayerPosition(layerHandle);
                    layerMoved = true;
                }
                catch
                {
                    //ignore error
                }
            }
            return layerMoved;
        }

        //private void SetLayerColorSchemes()
        //{
        //    string path= $"{AppDomain.CurrentDomain.BaseDirectory}/colorschemes.xml";
        //    LayerColors = new ColorSchemes(ColorSchemeType.Layer);
        //    XmlDocument doc = new XmlDocument();
        //    doc.Load(path);
        //    LayerColors.LoadXML(doc);
        //}

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="mapControl"></param>
        public MapLayersHandler(AxMap mapControl)
        {
            _axmap = mapControl;
            _axmap.LayerAdded += OnMapLayerAdded;
            _axmap.ProjectionMismatch += OnProjectionMismatch;
            //SetLayerColorSchemes();
        }

        /// <summary>
        /// reprojects a mismatched layer to the map's projection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnProjectionMismatch(object sender, _DMapEvents_ProjectionMismatchEvent e)
        {
            e.reproject = tkMwBoolean.blnTrue;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    foreach (var item in MapLayerDictionary)
                    {
                        item.Value.Dispose();
                    }
                    MapLayerDictionary = null;
                }
                _axmap = null;
                _disposed = true;
            }
        }

        /// <summary>
        /// gets the layer in the map and retrieves the corresponding layer item in the dictionary. Fires an event after a layer item is read
        /// </summary>
        public void ReadLayers()
        {
            for (int n = 0; n < _axmap.NumLayers; n++)
            {
                var h = _axmap.get_LayerHandle(n);
                if (MapLayerDictionary.ContainsKey(h) && MapLayerDictionary[h].VisibleInLayersUI)
                {
                    //if there is a listener to the event
                    if (LayerRead != null)
                    {
                        //get the corresponding layer item in the dictionary
                        var item = MapLayerDictionary[h];

                        //fill up the event argument class with the layer item
                        LayerEventArg lp = new LayerEventArg(item.Handle, item.Name, item.Visible, item.VisibleInLayersUI, item.LayerType);
                        LayerRead(this, lp);
                    }
                }
            }
        }

        public void ClearSelection(int handle)
        {
            try
            {
                _axmap.get_Shapefile(handle).SelectNone();
                _axmap.Redraw();
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        /// <summary>
        /// Remove a layer using layer name
        /// </summary>
        /// <param name="layerName"></param>
        public bool  RemoveLayer(string layerName)
        {
            foreach (var item in MapLayerDictionary)
            {
                if (item.Value.Name == layerName)
                {
                    RemoveLayer(item.Key);
                    return true;
                }
            }
            return false;
        }

        public void RemoveLayerByKey(string layerKey)
        {
            if (LayerDictionary.Count > 0)
            {
                List<int> layerHandles= new List<int>();
                int counter = 0;
                foreach (var item in LayerDictionary.Values)
                {
                    if (item.LayerKey == layerKey)
                    {
                        layerHandles.Add(item.Handle);
                    }
                }

                foreach (var h in layerHandles)
                {

                    MapLayerDictionary[h].Dispose();
                    MapLayerDictionary.Remove(h);
                    _axmap.RemoveLayer(h);


                    if (LayerRemoved != null)
                    {
                        LayerEventArg lp = new LayerEventArg(h, layerRemoved: true);
                        LayerRemoved(this, lp);
                    }
                    counter++;
                    
                }
                

                if (counter > 0)
                {
                    _axmap.Redraw();
                }
            }
        }
        /// <summary>
        /// Removes a layer using layer handle and raises a Layer removed event.
        /// </summary>
        /// <param name="layerHandle"></param>
        public void RemoveLayer(int layerHandle)
        {
            try
            {
                MapLayerDictionary[layerHandle].Dispose();
                MapLayerDictionary.Remove(layerHandle);

                _axmap.RemoveLayer(layerHandle);
                _axmap.Redraw();

                //fire the layer deleted event
                if (LayerRemoved != null)
                {
                    LayerEventArg lp = new LayerEventArg(layerHandle, layerRemoved: true);
                    LayerRemoved(this, lp);
                }

                //if the layer removed is the current layer, then make the current layer null
                if (CurrentMapLayer != null && layerHandle == _currentMapLayer.Handle)
                {
                    _currentMapLayer = null;
                }
            }
            catch (KeyNotFoundException)
            {
                //ignore
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public void LayersSequence(List<MapLayerSequence>layersSequnce)
        {
            foreach(MapLayerSequence mls in layersSequnce)
            {
                MapControl.MoveLayer(MapControl.get_LayerPosition(mls.MapLayer.Handle), mls.Sequence);
                MapLayerDictionary.Values.Where(t => t.Handle == mls.MapLayer.Handle).FirstOrDefault().LayerPosition = mls.Sequence;
            }
            MapControl.Redraw();
        }
        private void RemoveInMemoryLayers()
        {
            for (int n = 0; n < _axmap.NumLayers; n++)
            {
                var h = _axmap.get_LayerHandle(n);
                if (MapLayerDictionary[h].FileName.Length == 0)
                {
                    RemoveLayer(h);
                }
            }
        }

        /// <summary>
        /// handles editing of layer name and layer visibility
        /// </summary>
        /// <param name="layerHandle"></param>
        /// <param name="layerName"></param>
        /// <param name="visible"></param>
        /// <param name="isShown"></param>
        public void EditLayer(int layerHandle, string layerName, bool visible, bool isShown = true)
        {
            if (MapLayerDictionary.ContainsKey(layerHandle))
            {
                var ly = MapLayerDictionary[layerHandle];
                ly.Name = layerName;
                ly.Visible = visible;
                ly.VisibleInLayersUI = isShown;
            }

            _axmap.set_LayerName(layerHandle, layerName);
            _axmap.set_LayerVisible(layerHandle, visible);
            if (OnLayerVisibilityChanged != null)
            {
                LayerEventArg lp = new LayerEventArg(layerHandle);
                lp.LayerVisible = visible;
                lp.LayerName = layerName;
                OnLayerVisibilityChanged(this, lp);
            }
            _axmap.Redraw();
        }

        public string WorldfileExtension(string extension)
        {
            switch (extension)
            {
                case ".tif":
                    return "tifw";

                case ".jpg":
                    return "jgw";

                default:
                    var arr = extension.ToCharArray();
                    return $"{arr[1]}{arr[3]}w";
            }
        }

        /// <summary>
        /// Handles the opening of map layer files from a file open dialog
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public (bool success, string errMsg) FileOpenHandler(string fileName, string layerName = "", bool reproject = false, string layerkey = "")
        {
            var success = false;
            var errMsg = "";
            var fm = new FileManager();

            if (!fm.get_IsSupported(fileName))
            {
                errMsg = "Datasource isn't supported by MapWinGIS";
            }
            else
            {
                var obj = fm.Open(fileName, tkFileOpenStrategy.fosAutoDetect, null);
                if (fm.LastOpenIsSuccess)
                {
                    if (fm.LastOpenStrategy == tkFileOpenStrategy.fosVectorLayer)
                    {
                        var shapefile = obj as Shapefile;
                        success = shapefile != null;
                        if (success)
                        {
                            if (reproject)
                            {
                                int reprojectCount = 0;
                                var sf = shapefile.Reproject(MapControl.GeoProjection, reprojectCount);
                                if (reprojectCount > 0 || sf.NumShapes > 0)
                                {
                                    shapefile = sf;
                                }
                            }
                            if (AddLayer(shapefile, layerName, layerKey:layerkey) < 0)
                            {
                                success = false;
                                errMsg = "Failed to add layer to map";
                            }
                        }
                    }
                    else if (fm.LastOpenStrategy == tkFileOpenStrategy.fosRgbImage)
                    {
                        var folderPath = Path.GetDirectoryName(fileName);
                        var file = Path.GetFileNameWithoutExtension(fileName);
                        var ext = Path.GetExtension(fileName);
                        var prjFile = $@"{folderPath}\{file}.prj";
                        var worldFile = $@"{folderPath}\{file}.{WorldfileExtension(ext)}";

                        if (File.Exists(prjFile) || File.Exists(worldFile))
                        {
                            var image = obj as MapWinGIS.Image;
                            success = image != null;
                            if (success)
                            {
                                if (AddLayer(image,layerKey:layerkey) < 0)
                                {
                                    success = false;
                                    errMsg = "Failed to add layer to map";
                                }
                            }
                        }
                        else
                        {
                            errMsg = $"{fileName} does not have a projection or world file";
                        }
                    }
                    else if (fm.LastOpenStrategy == tkFileOpenStrategy.fosDirectGrid
                          || fm.LastOpenStrategy == tkFileOpenStrategy.fosProxyForGrid)
                    {
                        var grid = new MapWinGIS.Grid();
                        success = grid.Open(fileName, GridDataType.DoubleDataType, false, GridFileType.UseExtension, null);
                        if (success)
                        {
                            AddLayer(grid, Path.GetFileName(fileName), true, true,layerKey:layerkey);
                        }
                    }
                }
                else
                {
                    errMsg = "Failed to open datasource: " + fm.get_ErrorMsg(fm.LastErrorCode);
                }
            }
            if (success)
            {
                //save directory to the registry
                //RegistryTools.SaveSetting("MAPWINFORMS", "LastOpenedLayerDirectory", Path.GetDirectoryName(fileName));
            }
            return (success, errMsg);
        }

        /// <summary>
        /// Sets up a mapLayer object from a newly added map layer and adds it to the layers dictionary
        /// </summary>
        /// <param name="layerHandle"></param>
        /// <param name="layerName"></param>
        /// <param name="Visible"></param>
        /// <param name="ShowInLayerUI"></param>
        /// <param name="gp"></param>
        /// <param name="layerType"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private MapLayer SetMapLayer(int layerHandle, string layerName, bool Visible,
                                bool ShowInLayerUI, GeoProjection gp,
                                string layerType = "ShapefileClass", string fileName = "")
        {
            var mapLayer = new MapLayer(layerHandle, layerName, Visible, ShowInLayerUI, this);
            mapLayer.LayerType = layerType;
            mapLayer.FileName = _axmap.get_LayerFilename(layerHandle);
            if (mapLayer.FileName.Length == 0)
            {
                mapLayer.FileName = fileName;
            }

            mapLayer.GeoProjectionName = gp.Name;
            mapLayer.LayerPosition = _axmap.get_LayerPosition(layerHandle);
            mapLayer.LayerObject = _axmap.get_GetObject(layerHandle);
            mapLayer.Labels = _axmap.get_LayerLabels(layerHandle);

            MapLayerDictionary.Add(layerHandle, mapLayer);
            _axmap.Redraw();
            set_MapLayer(layerHandle);
            return mapLayer;
        }

        public MapLayer this[int index]
        {
            set { MapLayerDictionary[index] = value; }
            get { return MapLayerDictionary[index]; }
        }

        public int AddNewShapefileLayer(string layerName, ShpfileType shapefileType, bool isVisile = true, bool visibleInUI = false)
        {
            var sf = new Shapefile();
            if (sf.CreateNewWithShapeID("", shapefileType))
            {
                sf.GeoProjection = _axmap.GeoProjection;
            }
            var h = _axmap.AddLayer(sf, isVisile);
            if (h >= 0)
            {
                _axmap.set_LayerName(h, layerName);
            }
            return h;
        }

        public void SetAsPointLayerFromDatabase(MapLayer ly)
        {
            ly.IsPointDatabaseLayer = true;
            List<int> forRemove = new List<int>();
            foreach (MapLayer ml in MapLayerDictionary.Values)
            {
                if (ml.IsPointDatabaseLayer && ml.Handle != ly.Handle)
                {
                    forRemove.Add(ml.Handle);
                }
            }
            if (forRemove.Count > 0)
            {
                foreach (var item in forRemove)
                {
                    MapLayerDictionary.Remove(item);
                    _axmap.RemoveLayer(item);
                }
                RefreshLayers();
            }
        }

        /// <summary>
        /// handles a shapefile added to the map using file open dialog
        /// </summary>
        /// <param name="sf"></param>
        /// <returns></returns>
        public int AddLayer(Shapefile sf, string layerName = "", bool isVisible = true, bool uniqueLayer = false, 
            fad3MappingMode mappingMode = fad3MappingMode.defaultMode, string layerKey="", bool rejectIfExisting=false)
        {
            if(rejectIfExisting && layerName.Length>0 &&  Exists(layerName))
            {
                var handle = get_MapLayer(layerName).Handle;
                MapLayerDictionary[handle].LayerObject = sf;
                return handle;
            }
            if (uniqueLayer)
            {
                RemoveLayer(layerName);
            }
            var h = _axmap.AddLayer(sf, isVisible);
            if (h >= 0)
            {
                if (layerName.Length == 0)
                {
                    layerName = Path.GetFileName(sf.Filename);
                }
                _axmap.set_LayerName(h, layerName);
                _axmap.set_LayerKey(h, layerKey);
                _currentMapLayer = SetMapLayer(h, layerName, isVisible, true, sf.GeoProjection, "ShapefileClass", sf.Filename);
                _currentMapLayer.LayerKey = layerKey; 
                _currentMapLayer.MappingMode = mappingMode;


                if (LayerRead != null)
                {
                    LayerEventArg lp = new LayerEventArg(h, layerName, true, true, _currentMapLayer.LayerType);
                    LayerRead(this, lp);
                }
                //LineWidthFix.FixLineWidth(sf);
            }
            else
            {
                int reprojectedCount = 0;

                //if(sf.ReprojectInPlace(_axmap.GeoProjection,ref reprojectedCount))
                var sfr = sf.Reproject(_axmap.GeoProjection, reprojectedCount);
                if (reprojectedCount > 0)
                {
                    h = _axmap.AddLayer(sfr, isVisible);
                    if (h > 0)
                    {
                        if (layerName.Length == 0)
                        {
                            layerName = Path.GetFileName(sf.Filename);
                        }
                        _axmap.set_LayerName(h, layerName);
                        _currentMapLayer = SetMapLayer(h, layerName, isVisible, true, sf.GeoProjection, "ShapefileClass", sf.Filename);
                        _currentMapLayer.LayerKey = layerKey;
                        _currentMapLayer.MappingMode = mappingMode;

                        if (LayerRead != null)
                        {
                            LayerEventArg lp = new LayerEventArg(h, layerName, true, true, _currentMapLayer.LayerType);
                            LayerRead(this, lp);
                        }
                        //LineWidthFix.FixLineWidth(sf);
                    }
                }
            }
            return h;
        }

        /// <summary>
        /// handles an image added to the map using file open dialog
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public int AddLayer(MapWinGIS.Image image, string layerName = "", bool isVisible = true, string layerKey="")
        {
            var h = _axmap.AddLayer(image, isVisible);
            if (h >= 0)
            {
                if (layerName.Length == 0)
                {
                    layerName = Path.GetFileName(image.Filename);
                }

                _axmap.set_LayerName(h, layerName);
                _currentMapLayer = SetMapLayer(h, layerName, isVisible, true, image.GeoProjection, "ImageClass", image.Filename);
                _currentMapLayer.LayerKey = layerKey;

                if (LayerRead != null)
                {
                    LayerEventArg lp = new LayerEventArg(h, layerName, true, true, _currentMapLayer.LayerType);
                    LayerRead(this, lp);
                }
            }
            return h;
        }

        /// <summary>
        /// handles  shapefiles added to the map
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="layerName"></param>
        /// <param name="visible"></param>
        /// <param name="showInLayerUI"></param>
        /// <param name="layerHandle"></param>
        /// <returns></returns>
        public int AddLayer(object layer, string layerName, bool visible, bool showInLayerUI, string fileName = "", 
            fad3MappingMode mappingMode = fad3MappingMode.defaultMode, string layerKey="",bool rejectIfExisting=false)
        {

            if (rejectIfExisting && layerName.Length > 0 && Exists(layerName))
            {
                return get_MapLayer(layerName).Handle;
            }

            int h = 0;
            GeoProjection gp = new GeoProjection();

            var layerType = layer.GetType().Name;

            switch (layerType)
            {
                case "ShapefileClass":
                    h = _axmap.AddLayer((Shapefile)layer, visible);
                    gp = ((Shapefile)layer).GeoProjection;
                    //LineWidthFix.FixLineWidth((Shapefile)layer);
                    break;

                case "ImageClass":
                    h = _axmap.AddLayer((MapWinGIS.Image)layer, visible);
                    gp = ((MapWinGIS.Image)layer).GeoProjection;
                    break;

                case "GridClass":
                    h = _axmap.AddLayer((Grid)layer, visible);
                    gp = _axmap.GeoProjection;
                    break;
            }

            _axmap.set_LayerName(h, layerName);
            _currentMapLayer = SetMapLayer(h, layerName, visible, showInLayerUI, gp, layerType, fileName);
            _currentMapLayer.LayerKey = layerKey;
            _currentMapLayer.MappingMode = mappingMode;

            if (LayerRead != null)
            {
                LayerEventArg lp = new LayerEventArg(h, layerName, visible, showInLayerUI, _currentMapLayer.LayerType);
                LayerRead(this, lp);
            }

            return h;
        }

        /// <summary>
        /// save additional map options including AvoidCollision which is not saved by SaveMapState
        /// </summary>
        private void SaveOtherMapOptions()
        {
            for (int n = 0; n < _axmap.NumLayers; n++)
            {
                var h = _axmap.get_LayerHandle(n);
                if (MapLayerDictionary[h].LayerType == "ShapefileClass")
                {
                    var sf = _axmap.get_Shapefile(h);
                    if (sf.Labels.Count > 0)
                    {
                        ShapefileLabelHandler.SaveLabelParameters(_fileMapState, h, sf.Labels.AvoidCollisions);
                    }
                }
            }
        }

        /// <summary>
        /// Check for blank filenames in the mapstatefile. Blank filenames happen when a layer is reprojected
        /// and is then saved in the mapstate file.
        /// </summary>
        private void CheckFileNameInMapStateFile()
        {
            var doc = new XmlDocument();
            var n = 0;
            doc.Load(_fileMapState);
            foreach (XmlNode ly in doc.DocumentElement.SelectNodes("//Layer"))
            {
                if (ly.Attributes["Filename"].Value.Length == 0)
                {
                    ly.Attributes["Filename"].Value = MapLayerDictionary[_axmap.get_LayerHandle(n)].FileName;
                }
                n++;
            }
            doc.Save(_fileMapState);
        }

        /// <summary>
        /// saves the map state to an xml file
        /// </summary>
        public void SaveMapState()
        {
            RemoveInMemoryLayers();
            if (globalMapping.MappingMode == fad3MappingMode.defaultMode
                && _axmap.SaveMapState(_fileMapState, false, true))
            {
                SaveOtherMapOptions();
                CheckFileNameInMapStateFile();
            }
        }

        /// <summary>
        /// Load map layers from XML file generated by axmap.SaveMapState.
        ///
        /// Layers are added to the map and is followed by restoring the map extent.
        /// The first added layer automatically sets the map control's projection.
        /// </summary>
        /// <param name="restoreMapState">
        /// When restoreMapState:true, map state is restored
        /// We use restoreMapState:false to load the layers but not restore axMap extent.
        /// </param>
        public void LoadMapState(bool restoreMapState = true)
        {
            _fileMapState = $@"{globalMapping.ApplicationPath}\mapstate";
            if (File.Exists(_fileMapState))
            {
                var doc = new XmlDocument();
                var proceed = true;
                var fileName = "";
                try
                {
                    doc.Load(_fileMapState);
                }
                catch (XmlException ex)
                {
                    Logger.Log(ex.Message);
                    proceed = false;
                }
                if (proceed)
                {
                    foreach (XmlNode ly in doc.DocumentElement.SelectNodes("//Layer"))
                    {
                        fileName = ly.Attributes["Filename"].Value;
                        var isVisible = true;
                        isVisible = ly.Attributes["LayerVisible"]?.Value == "1";
                        if (ly.Attributes["LayerType"].Value == "Shapefile")
                        {
                            var sf = new Shapefile();
                            if (sf.Open(fileName))
                            {
                                var h = AddLayer(sf, ly.Attributes["LayerName"].Value, isVisible);
                                _sfSymbologyHandler.SymbolizeLayer(ly.InnerXml);
                                _currentMapLayer.Visible = ly.Attributes["LayerVisible"].Value == "1";
                                _sfLabelHandler = new ShapefileLabelHandler(_currentMapLayer);

                                if (ly.FirstChild.Name == "ShapefileClass")
                                {
                                    foreach (XmlNode child in ly.FirstChild.ChildNodes)
                                    {
                                        if (child.Name == "LabelsClass" && child.Attributes["Generated"].Value == "1")
                                        {
                                            _currentMapLayer.IsLabeled = child.Attributes["Generated"].Value == "1";
                                            _sfLabelHandler.LabelShapefile(child.OuterXml);
                                        }
                                    }
                                }
                            }
                        }
                        else if (ly.Attributes["LayerType"].Value == "Image")
                        {
                            //code when layertype is image
                            var img = new MapWinGIS.Image();
                            if (img.Open(fileName))
                            {
                                var h = AddLayer(img, ly.Attributes["LayerName"].Value, isVisible);
                            }
                        }
                    }
                    if (restoreMapState)
                    {
                        //We restore saved extent of the map but not the projection. Since layers
                        //were already added to the map, the first layer sets the map's projection.
                        foreach (XmlNode ms in doc.DocumentElement.SelectNodes("//MapState "))
                        {
                            var ext = new Extents();
                            ext.SetBounds(
                                double.Parse(ms.Attributes["ExtentsLeft"].Value),
                                double.Parse(ms.Attributes["ExtentsBottom"].Value),
                                0,
                                double.Parse(ms.Attributes["ExtentsRight"].Value),
                                double.Parse(ms.Attributes["ExtentsTop"].Value),
                                0);
                            _axmap.Extents = ext;
                            _axmap.ExtentPad = double.Parse(ms.Attributes["ExtentsPad"].Value);
                        }
                    }
                }
            }
            else
            {
                File.Create(_fileMapState);
            }
        }

        private void OnMapLayerAdded(object sender, _DMapEvents_LayerAddedEvent e)
        {
        }

        public void MakeLayerSelected(MapLayer mapLayer)
        {
            switch(mapLayer.LayerType)
            {
                case "ShapefileClass":
                    Shapefile currentShapefile = (Shapefile)mapLayer.LayerObject;
                    currentShapefile.SelectionAppearance = tkSelectionAppearance.saDrawingOptions;
                    currentShapefile.SelectionDrawingOptions.LineWidth = 3;
                    currentShapefile.SelectionDrawingOptions.PointSize = 12;
                    currentShapefile.SelectionDrawingOptions.PointShape = tkPointShapeType.ptShapeCircle;
                    currentShapefile.SelectAll();
                    
                    break;

            }
            MapControl.Redraw();
        }
    }
}