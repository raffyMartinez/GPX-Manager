
using MapWinGIS;
using AxMapWinGIS;
using System;
using System.Windows.Forms;

namespace GPXManager.entities.mapping
{
    /// <summary>
    /// Handles interaction with the mouse control. However, interaction is disabled and is refered to grid25MajorClass when the class is active.
    /// </summary>
    public class MapInterActionHandler : IDisposable
    {
        private tkCursorMode _mapCursorMode;
        private bool _disposed;
        private AxMap _axMap;                               //reference to the map control in the mapping form
        private MapLayersHandler _mapLayersHandler;         //reference to the map layers handler class
        private bool _selectionFromSelectBox;               //is the current selection from a selection box or from click select
        private const int CURSORWIDTH = 5;                  //expands the click select point by 5 pixels
        private MapLayer _currentMapLayer;                  //the current layer selected in the layers form
        private int[] _selectedShapeIndexes;                //an array containing the selected shapes index
        public EventHandler Selection;                      //event that shapes were selected in a shapefile
        private int _selectedShapeIndex;                    //the index of a shape that was selected

        public bool EnableMapInteraction { get; set; }      //if true, interactions with the map using the mouse is allowed
        private string _dropDownContext;
        private ContextMenuStrip _mapContextMenuStrip;

        public delegate void FishingGridMapSelectedHandler(MapInterActionHandler s, LayerEventArg e);
        public event FishingGridMapSelectedHandler GridSelected;

        public delegate void ShapesSelectedHandler(MapInterActionHandler s, LayerEventArg e);
        public event ShapesSelectedHandler ShapesSelected;

        public delegate void ExtenntCreatedHandler(MapInterActionHandler s, LayerEventArg e);
        public event ExtenntCreatedHandler ExtentCreated;

        public event EventHandler SelectionCleared;

        public tkCursorMode MapCursorMode
        {
            get { return _mapCursorMode; }
            set
            {
                _mapCursorMode = value;
            }
        }

        public AxMap MapControl { get { return _axMap; } }

        public ContextMenuStrip MapContextMenuStrip
        {
            get { return _mapContextMenuStrip; }
            set
            {
                _mapContextMenuStrip = value;
                _mapContextMenuStrip.ItemClicked += OnMapContextMenuItemClicked;
            }
        }

        //gets the map layers handler class
        public MapLayersHandler MapLayersHandler
        {
            get { return _mapLayersHandler; }
        }

        public Extents SelectionExtent { get; internal set; }

        public int[] SelectedShapeIndexes
        {
            get { return _selectedShapeIndexes; }
        }

        public int SelectedShapeIndex
        {
            get { return _selectedShapeIndex; }
            set
            {
                _selectedShapeIndex = value;
                Shapefile sf = _currentMapLayer.LayerObject as Shapefile;
                sf.SelectNone();
                sf.ShapeSelected[_selectedShapeIndex] = true;

                //set default appearance mode
                if (sf.Categories.Count > 0)
                {
                    sf.SelectionAppearance = tkSelectionAppearance.saSelectionColor;
                }
                else
                {
                    sf.SelectionAppearance = tkSelectionAppearance.saDrawingOptions;
                }

                //appearance mode depending on shapefile type
                switch (sf.ShapefileType)
                {
                    case ShpfileType.SHP_POINT:
                        if (sf.Categories.Count > 0)
                        {
                        }
                        else
                        {
                            sf.SelectionAppearance = tkSelectionAppearance.saDrawingOptions;
                            sf.SelectionDrawingOptions.PointSize = sf.DefaultDrawingOptions.PointSize;
                            sf.SelectionDrawingOptions.PointRotation = sf.DefaultDrawingOptions.PointRotation;
                            sf.SelectionDrawingOptions.PointShape = sf.DefaultDrawingOptions.PointShape;
                        }
                        break;

                    case ShpfileType.SHP_POLYGON:
                        break;

                    case ShpfileType.SHP_POLYLINE:

                        break;
                }

                _axMap.Redraw();
            }
        }

        public void DropDownContext(string context)
        {
            _dropDownContext = context;
            _axMap.SendMouseDown = context.Length > 0;
        }

        private void OnMapContextMenuItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            e.ClickedItem.Owner.Hide();
            switch (e.ClickedItem.Name)
            {
                case "itemRetrieveGrid":
                case "itemDeleteGrid":
                    var sf = ((Shapefile)_currentMapLayer.LayerObject);
                    var baseName = (string)sf.CellValue[sf.FieldIndexByName["BaseName"], _selectedShapeIndexes[0]];
                    if (GridSelected != null)
                    {
                        LayerEventArg fg = new LayerEventArg(_currentMapLayer.Handle, _currentMapLayer.Name, _currentMapLayer.Visible, _currentMapLayer.VisibleInLayersUI, _currentMapLayer.LayerType);
                        fg.SelectedIndex = _selectedShapeIndexes[0];
                        if (e.ClickedItem.Name == "itemRetrieveGrid")
                        {
                            fg.Action = "LoadGridMap";
                        }
                        else
                        {
                            fg.Action = "DeleteGridMap";
                        }
                        GridSelected(this, fg);
                    }

                    break;
            }
        }

        public void SetUpMapDropDown(string context, int mouseX, int mouseY)
        {
            MapContextMenuStrip.Items.Clear();

            switch (context)
            {
                case "FishingGridBoundary":
                    var sf = (Shapefile)_currentMapLayer.LayerObject;

                    var mapName = sf.CellValue[sf.FieldIndexByName["MapName"], _selectedShapeIndexes[0]];
                    var tsi = MapContextMenuStrip.Items.Add($"Load {mapName} grid map");
                    tsi.Name = "itemRetrieveGrid";
                    tsi.Visible = globalMapping.MappingMode == fad3MappingMode.grid25Mode;

                    tsi = MapContextMenuStrip.Items.Add($"Delete {mapName} grid map");
                    tsi.Name = "itemDeleteGrid";
                    tsi.Visible = globalMapping.MappingMode == fad3MappingMode.grid25Mode;
                    break;
            }
            MapContextMenuStrip.Show(_axMap, new System.Drawing.Point(mouseX, mouseY));
        }

        /// <summary>
        /// Constructor and sets up map control events
        /// </summary>
        /// <param name="mapControl"></param>
        /// <param name="layersHandler"></param>
        public MapInterActionHandler(AxMap mapControl, MapLayersHandler layersHandler)
        {
            _mapLayersHandler = layersHandler;
            _mapLayersHandler.CurrentLayer += OnCurrentMapLayer;
            _axMap = mapControl;
            _axMap.MouseUpEvent += OnMapMouseUp;
            _axMap.MouseDownEvent += OnMapMouseDown;
            _axMap.SelectBoxFinal += OnMapSelectBoxFinal;
            _axMap.MouseMoveEvent += OnMapMouseMove;
            _axMap.DblClick += OnMapDoubleClick;
            _axMap.SelectionChanged += OnMapSelectionChanged;
            _axMap.SendMouseDown = true;
            _axMap.SendMouseMove = true;
            _axMap.SendSelectBoxFinal = true;
            _axMap.SendMouseUp = true;
            EnableMapInteraction = true;

            _axMap.MapCursor = tkCursor.crsrArrow;
            _axMap.CursorMode = tkCursorMode.cmSelection;
        }

        private void OnMapSelectionChanged(object sender, _DMapEvents_SelectionChangedEvent e)
        {
        }

        private void OnMapDoubleClick(object sender, EventArgs e)
        {
            if (EnableMapInteraction)
            {
            }
        }

        private void OnCurrentMapLayer(MapLayersHandler s, LayerEventArg e)
        {
            _currentMapLayer = _mapLayersHandler.get_MapLayer(e.LayerHandle);
            _dropDownContext = "";
            if (_currentMapLayer.Name == "Fishing grid boundaries")
            {
                _dropDownContext = "FishingGridBoundary";
            }
        }

        private void OnMapMouseMove(object sender, _DMapEvents_MouseMoveEvent e)
        {
            if (EnableMapInteraction)
            {
                Console.WriteLine($"{_axMap.Longitude}, {_axMap.Latitude}");
            }
        }

        /// <summary>
        /// processes selection made using a mouse drag selection box
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMapSelectBoxFinal(object sender, _DMapEvents_SelectBoxFinalEvent e)
        {
            if (EnableMapInteraction && _axMap.CursorMode == tkCursorMode.cmSelection)
            {
                var extL = 0D;
                var extR = 0D;
                var extT = 0D;
                var extB = 0D;
                Extents selectionBoxExtent = new Extents();

                _axMap.PixelToProj(e.left, e.top, ref extL, ref extT);
                _axMap.PixelToProj(e.right, e.bottom, ref extR, ref extB);
                selectionBoxExtent.SetBounds(extL, extB, 0, extR, extT, 0);
                Select(selectionBoxExtent, selectionFromSelectBox: true);
                SelectionExtent = selectionBoxExtent;
                var lyArg = new LayerEventArg(selectionExtent:SelectionExtent);
                ExtentCreated?.Invoke(this, lyArg);
            }
        }

        /// <summary>
        /// Receives the extent made by selection box or a click select to select shapes in a shapefile using intersection.
        /// Afterwards, a Selection event is raised
        /// </summary>
        /// <param name="selectExtents"></param>
        /// <param name="selectionFromSelectBox"></param>
        private void Select(Extents selectExtents, bool selectionFromSelectBox = false)
        {
            if (_currentMapLayer != null)
            {
                _selectedShapeIndexes = null;
                _selectionFromSelectBox = selectionFromSelectBox;
                if (_currentMapLayer.LayerType == "ShapefileClass")
                {
                    var sf = _axMap.get_Shapefile(_currentMapLayer.Handle);
                    if (sf != null)
                    {
                        _currentMapLayer.SelectedIndexes = null;
                        sf.SelectNone();
                        sf.SelectionAppearance = tkSelectionAppearance.saDrawingOptions;

                        switch (sf.ShapefileType)
                        {
                            case ShpfileType.SHP_POINT:
                                if (sf.Categories.Count > 0)
                                {
                                    sf.SelectionAppearance = tkSelectionAppearance.saSelectionColor;
                                }
                                else
                                {
                                    sf.SelectionAppearance = tkSelectionAppearance.saDrawingOptions;
                                    sf.SelectionDrawingOptions.PointSize = sf.DefaultDrawingOptions.PointSize;
                                    sf.SelectionDrawingOptions.PointRotation = sf.DefaultDrawingOptions.PointRotation;
                                    sf.SelectionDrawingOptions.PointShape = sf.DefaultDrawingOptions.PointShape;
                                }
                                break;

                            case ShpfileType.SHP_POLYGON:
                                break;

                            case ShpfileType.SHP_POLYLINE:

                                break;
                        }

                        var objSelection = new object();
                        if (sf.SelectShapes(selectExtents, 0, SelectMode.INTERSECTION, ref objSelection))
                        {
                            _selectedShapeIndexes = (int[])objSelection;
                            _currentMapLayer.SelectedIndexes = _selectedShapeIndexes;
                            for (int n = 0; n < _selectedShapeIndexes.Length; n++)
                            {
                                sf.ShapeSelected[_selectedShapeIndexes[n]] = true;
                            }
                            if (ShapesSelected != null)
                            {
                                var lyArg = new LayerEventArg(_currentMapLayer.Handle, _selectedShapeIndexes);
                                ShapesSelected(this, lyArg);
                            }

                        }
                        else
                        {
                            SelectionCleared?.Invoke(this, EventArgs.Empty);
                        }
                        _axMap.Redraw();
                        Selection?.Invoke(this, EventArgs.Empty);
                    }
                }
                else if (_currentMapLayer.LayerType == "ImageClass")
                {
                }
            }
        }

        private void OnMapMouseDown(object sender, _DMapEvents_MouseDownEvent e)
        {
            if (EnableMapInteraction)
            {
                if (e.button == 1)
                {
                    _selectionFromSelectBox = false;
                }
                else if (e.button == 2 && _selectedShapeIndexes != null && _selectedShapeIndexes.Length == 1 && _dropDownContext.Length > 0)
                {
                    if (_currentMapLayer.LayerType == "ShapefileClass")
                    {
                        SetUpMapDropDown(_dropDownContext, e.x, e.y);
                    }
                    else
                    {
                    }
                }
            }
        }

        private void OnMapMouseUp(object sender, _DMapEvents_MouseUpEvent e)
        {
            //we only proceed if a drag-select was not done
            if (EnableMapInteraction && !_selectionFromSelectBox && _axMap.CursorMode == tkCursorMode.cmSelection)
            {
                var extL = 0D;
                var extR = 0D;
                var extT = 0D;
                var extB = 0D;
                Extents ext = new Extents();

                _axMap.PixelToProj(e.x - CURSORWIDTH, e.y - CURSORWIDTH, ref extL, ref extT);
                _axMap.PixelToProj(e.x + CURSORWIDTH, e.y + CURSORWIDTH, ref extR, ref extB);
                ext.SetBounds(extL, extB, 0, extR, extT, 0);
                Select(ext, selectionFromSelectBox: false);
            }
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
                }
                _mapLayersHandler = null;
                _currentMapLayer = null;
                _axMap = null;
                _disposed = true;
            }
        }
    }
}