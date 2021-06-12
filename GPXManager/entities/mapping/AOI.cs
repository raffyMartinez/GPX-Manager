using MapWinGIS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GPXManager.entities.mapping.gridding;
using System.IO;


namespace GPXManager.entities.mapping
{



    public class AOI
    {
        private List<int> _selectedMajorGridIndices;
        private Extents _extentUTM;
        private string _gridLayerName;
        private int _gridSizeMeters;
        private bool _gridIsLoaded;

        public AOI()
        {
            GridMapping = new GridMapping(this);
            Selected = false;
        }
        public GridMapping GridMapping { get; internal set; }
        public int AOIHandle { get; set; }

        public string ErrorMsg { get; internal set; }
        public List<int> MajorGridIntersect(int? forcedZoneNumber=null)
        {
            ErrorMsg = "";
            var refIndeces = new object();
            _extentUTM = Grid25.ExtentToUTM(ShapeFile.Extents, forcedZoneNumber);
            MapWindowManager.Grid25MajorGrid.SelectShapes(_extentUTM, 0, SelectMode.INTERSECTION, ref refIndeces);
            _selectedMajorGridIndices = ((int[])refIndeces).ToList();
            if (Grid25.ZonesFromConversion[0].ZoneNumber == Grid25.ZonesFromConversion[1].ZoneNumber)
            {
                return _selectedMajorGridIndices;
            }
            else
            {
                ErrorMsg = "Extent of AOI spans multiple UTM zones";
                return null;
            }
        }
        public bool GridIsLoaded { get { return _gridIsLoaded; }
            set 
            {
                _gridIsLoaded=value; 
                if(!_gridIsLoaded)
                {
                    _gridSizeMeters = 0;
                }
            } 
        }
        public int GridHandle { get; set; }
        public string GridFileName { get; set; }
        public Shapefile Grid2Km { get; private set; }
        public Shapefile SubGrids { get; private set; }

        public Dictionary<string, List<double>> GetColumnValues()
        {
            Dictionary<string, List<double>> dict = new Dictionary<string, List<double>>();
            List<double> values = new List<double>();
            int indx;
            if(EffortGridColumn.Length>0)
            {
                indx = SubGrids.FieldIndexByName[EffortGridColumn];
                
                for(int x=0;x<SubGrids.NumShapes;x++)
                {
                    values.Add(SubGrids.CellValue[indx, x]);
                }
                dict.Add(EffortGridColumn, values);

            }

            if(CPUEGridColumn.Length>0)
            {
                indx = SubGrids.FieldIndexByName[CPUEGridColumn];
                for (int x = 0; x < SubGrids.NumShapes; x++)
                {
                    values.Add(SubGrids.CellValue[indx, x]);
                }
                dict.Add(CPUEGridColumn, values);
            }

            if (BerriedGridColumn.Length > 0)
            {
                indx = SubGrids.FieldIndexByName[BerriedGridColumn];
                for (int x = 0; x < SubGrids.NumShapes; x++)
                {
                    values.Add(SubGrids.CellValue[indx, x]);
                }
                dict.Add(BerriedGridColumn, values);
            }

            if (UndersizedGridColumn.Length > 0)
            {
                indx = SubGrids.FieldIndexByName[UndersizedGridColumn];
                for (int x = 0; x < SubGrids.NumShapes; x++)
                {
                    values.Add(SubGrids.CellValue[indx, x]);
                }
                dict.Add(UndersizedGridColumn, values);
            }

            return dict;
        }
        public string EffortGridColumn { get; set; }
        public string CPUEGridColumn { get; set; }
        public string UndersizedGridColumn { get; set; }
        public string BerriedGridColumn { get; set; }
       
        public string GridSize
        {
            get
            {
                if(_gridSizeMeters==0)
                {
                    return "";
                }
                else
                {
                    return _gridSizeMeters.ToString();
                }
            }
        }
        public int GridSizeMeters
        {
            get { return _gridSizeMeters; }
            set
            {
                _gridSizeMeters = value;
                _gridLayerName = $"grid_{Name.Replace(' ', '_')}_{_gridSizeMeters}";
            }
        }
        public bool GeneratedSubGrids(int gridSize)
        {
            GridIsLoaded = false;
            var floor = Math.Floor(2000.0 / (double)gridSize);

            if (floor * gridSize == 2000)
            {
                SubGrids = new Shapefile();

                if (SubGrids.CreateNewWithShapeID("", ShpfileType.SHP_POLYGON))
                {
                    SubGrids.GeoProjection = MapWindowManager.Grid25MajorGrid.GeoProjection;
                    SubGrids.EditAddField("CellID", FieldType.INTEGER_FIELD, 1, 1);
                    SubGrids.EditAddField("CellNo", FieldType.INTEGER_FIELD, 1, 1);
                    SubGrids.EditAddField("Name", FieldType.STRING_FIELD, 1, 1);
                    SubGrids.Key = $"subgrid_{Name}";
                    var numShapes = Grid2Km.NumShapes;
                    int id = 0;
                    for (int x = 0; x < numShapes; x++)
                    {
                        var cell50km = Grid2Km.Shape[x];
                        var ext = cell50km.Extents;
                        var parentName = Grid2Km.CellValue[Grid2Km.FieldIndexByName["Name"], x];

                        var steps = 2000 / gridSize;
                        for (int r = 0; r < steps; r++)
                        {
                            var top = ext.yMax - (gridSize * r);


                            for (int c = 0; c < steps; c++)
                            {
                                var left = ext.xMin + (gridSize * c);

                                Shape cell = new Shape();
                                if (cell.Create(ShpfileType.SHP_POLYGON))
                                {
                                    cell.AddPoint(left, top);
                                    cell.AddPoint(left + (int)gridSize, top);
                                    cell.AddPoint(left + (int)gridSize, top - gridSize);
                                    cell.AddPoint(left, top - gridSize);
                                    cell.AddPoint(left, top);
                                }
                                id++;
                                int idx = SubGrids.EditAddShape(cell);
                                if (idx >= 0)
                                {
                                    int cellNo = (r * steps) + c + 1;
                                    SubGrids.EditCellValue(SubGrids.FieldIndexByName["CellID"], idx, id);
                                    SubGrids.EditCellValue(SubGrids.FieldIndexByName["CellNo"], idx, cellNo);
                                    SubGrids.EditCellValue(SubGrids.FieldIndexByName["Name"], idx, $"{parentName}-{cellNo}");
                                }
                            }
                        }
                    }

                    GridIsLoaded = true;
                }

            }
            return GridIsLoaded;
        }

        public override string ToString()
        {
            return Name;
        }
        public List<string> GridFileNames { get; set; } = new List<string>();

        public string GetGridFileNameOfGridSize(string grid_size)
        {
            var size = grid_size.Split('.')[0];
            foreach(var item in GridFileNames)
            {
                var arr = item.Split('_','.');
                if(arr[arr.Length-2]==size)
                {
                    return item;
                }
            }
            return "";
        }
        public bool Selected { get; set; }
        public string GridLayerName
        {
            get
            {
                if (_gridLayerName == null || _gridLayerName.Length == 0)
                {
                    _gridLayerName = $"grid_{Name.Replace(' ', '_')}_{GridSizeMeters}";
                }
                return _gridLayerName;
            }
            set { _gridLayerName = value; }
        }

        public bool CreateGridFromFileName(string fileName)
        {
            GridIsLoaded = false;
            string utmZone = "";
            //var prjFile = $@"{Path.GetDirectoryName(fileName)}\{Path.GetFileNameWithoutExtension(fileName)}.prj";
            var prjFile = fileName.Replace(".shp", ".prj");
            using (StreamReader sr = File.OpenText($"{prjFile}"))
            {
                string s = String.Empty;
                while ((s = sr.ReadLine()) != null)
                {
                    switch (s.Substring(8, 21))
                    {
                        case "WGS_1984_UTM_Zone_51N":
                            utmZone = "51N";
                            break;

                        case "WGS_1984_UTM_Zone_50N":
                            utmZone = "50M";
                            break;

                        default:
                            switch (s.Substring(17, 12))
                            {
                                case "UTM zone 51N":
                                    utmZone = "51N";
                                    break;

                                case "UTM zone 50N":
                                    utmZone = "50M";
                                    break;
                            }
                            break;
                    }
                }
            }
            switch (utmZone)
            {
                case "51N":
                    Grid25.UTMZone = UTMZone.UTMZone51N;
                    break;
                case "50N":
                    Grid25.UTMZone = UTMZone.UTMZone50N;
                    break;
            }
            if (utmZone.Length > 0)
            {
                MapWindowManager.MapLayersHandler.GeoProjection = MapWindowManager.Grid25MajorGrid.GeoProjection;
            }

            GridLayerName = Path.GetFileName(fileName.Replace(".shp", ""));


            var result = MapWindowManager.MapLayersHandler.FileOpenHandler(fileName, GridLayerName, true, $"subgrid_{Name}");
            if (result.success)
            {
                GridHandle = MapWindowManager.MapLayersHandler.get_MapLayer(GridLayerName).Handle;
                var sf = (Shapefile)MapWindowManager.MapLayersHandler.get_MapLayer(GridLayerName).LayerObject;
                GridSizeMeters = GetGridSizeFromShape(sf.Shape[0]);
                sf.DefaultDrawingOptions.FillVisible = false;
                sf.DefaultDrawingOptions.LineColor = new MapWinGIS.Utils().ColorByName(MapWinGIS.tkMapColor.DarkGray);
                SubGrids = sf;
                GridIsLoaded = true;
            }

            return GridIsLoaded;


        }

        public int GetGridSizeFromShape(Shape shp)
        {
            Waypoint pt1 = new Waypoint { Latitude = shp.Point[0].y, Longitude = shp.Point[0].x };
            Waypoint pt2 = new Waypoint { Latitude = shp.Point[1].y, Longitude = shp.Point[1].x };
            double elevChange;
            return (int)Waypoint.ComputeDistance(pt1, pt2, out elevChange);
        }
        public void GenerateMinorGrids()
        {
            List<double> northings = new List<double>();
            List<double> eastings = new List<double>();
            List<Shape> selectedMajorGrids = new List<Shape>();

            foreach (var idx in _selectedMajorGridIndices)
            {
                var shp = MapWindowManager.Grid25MajorGrid.Shape[idx];
                var ext = shp.Extents;
                selectedMajorGrids.Add(shp);
                northings.Add(ext.yMax);
                eastings.Add(ext.xMin);
            }
            double top = northings.Max();
            double left = eastings.Min();

            double currentRow = top;
            double topRow = 0;
            double bottomRow = 0;

            do
            {
                currentRow -= 2000;
                if (currentRow < _extentUTM.yMax && topRow == 0)
                {
                    topRow = currentRow + 2000;
                }
                bottomRow = currentRow;
            } while (currentRow > _extentUTM.yMin);


            double currentCol = left;
            double leftCol = 0;
            double righCol = 0;
            do
            {
                currentCol += 2000;
                if (currentCol > _extentUTM.xMin && leftCol == 0)
                {
                    leftCol = currentCol - 2000;
                }
                righCol = currentCol;
            } while (currentCol < _extentUTM.xMax);

            Shapefile grid2km = new Shapefile();
            if (grid2km.CreateNewWithShapeID("", ShpfileType.SHP_POLYGON))
            {
                grid2km.GeoProjection = MapWindowManager.Grid25MajorGrid.GeoProjection;
                grid2km.EditAddField("MajorGrid", FieldType.INTEGER_FIELD, 1, 1);
                grid2km.EditAddField("Col", FieldType.STRING_FIELD, 1, 1);
                grid2km.EditAddField("Row", FieldType.INTEGER_FIELD, 1, 1);
                grid2km.EditAddField("Name", FieldType.STRING_FIELD, 1, 1);
                double row = topRow;
                do
                {
                    double col = leftCol;
                    do
                    {
                        var shp = new Shape();
                        if (shp.Create(ShpfileType.SHP_POLYGON))
                        {
                            shp.AddPoint(col, row);
                            shp.AddPoint(col + 2000, row);
                            shp.AddPoint(col + 2000, row - 2000);
                            shp.AddPoint(col, row - 2000);
                            shp.AddPoint(col, row);
                        }
                        col += 2000;
                        var shpIndex = grid2km.EditAddShape(shp);
                        if (shpIndex >= 0)
                        {
                            var pt = shp.Centroid;
                            foreach (var idx in _selectedMajorGridIndices)
                            {
                                if (new Utils().PointInPolygon(MapWindowManager.Grid25MajorGrid.Shape[idx], pt))
                                {
                                    var result = GetCellAddressOfPointInMajorGrid(pt, MapWindowManager.Grid25MajorGrid.Shape[idx]);
                                    var grid_no = MapWindowManager.Grid25MajorGrid.CellValue[MapWindowManager.Grid25MajorGrid.FieldIndexByName["Grid_no"], idx];
                                    grid2km.EditCellValue(grid2km.FieldIndexByName["MajorGrid"], shpIndex, grid_no);
                                    grid2km.EditCellValue(grid2km.FieldIndexByName["Col"], shpIndex, result.col.ToString());
                                    grid2km.EditCellValue(grid2km.FieldIndexByName["Row"], shpIndex, result.row);
                                    grid2km.EditCellValue(grid2km.FieldIndexByName["Name"], shpIndex, $"{grid_no}-{result.col}{result.row}");
                                    break;
                                }
                            }
                        }

                    } while (col + 2000 <= righCol);
                    row -= 2000;
                } while (row - 2000 >= bottomRow);
                if (grid2km.NumShapes > 0)
                {
                    Grid2Km = grid2km;
                }
                else
                {
                    Grid2Km = null;
                }
            }

        }

        private (char col, int row) GetCellAddressOfPointInMajorGrid(Point pt, Shape mg)
        {
            (char col, int row) rv;
            var ext = mg.Extents;
            double row = ext.yMax;
            double col = ext.xMin;

            int rowName = 1;
            do
            {
                if (pt.y + 1000 == row)
                {
                    break;
                }
                rowName++;
                row = row - 2000;
            } while (row > ext.yMin);


            char colName = 'A';
            do
            {
                if (pt.x - 1000 == col)
                {
                    break;
                }
                colName++;
                col = col + 2000;
            } while (col < ext.xMax);

            rv.row = rowName;
            rv.col = colName;
            return rv;
        }
        public UTMExtent UTMExtent
        {
            get
            {
                LatLonUTMConverter llc = new LatLonUTMConverter("WGS 84");
                var ul = llc.convertLatLngToUtm(UpperLeftY, UpperLeftX);
                var lr = llc.convertLatLngToUtm(LowerRightY, LowerRightX);
                return new UTMExtent(new UTMPoint { Northing = ul.Northing, Easting = ul.Easting, ZoneLetter = ul.ZoneLetter, ZoneNumber = ul.ZoneNumber },
                                     new UTMPoint { Northing = lr.Northing, Easting = lr.Easting, ZoneLetter = ul.ZoneLetter, ZoneNumber = ul.ZoneNumber });

            }
        }
        public double UpperLeftX { get; set; }
        public double UpperLeftY { get; set; }
        public double LowerRightX { get; set; }
        public double LowerRightY { get; set; }
        public string Name { get; set; }

        public int MapLayerHandle { get; set; } = -1;
        public int ID { get; set; }

        private bool _visibility;
        public bool Visibility { get {return _visibility; } 
            set 
            {
                
                _visibility=value;
                if (MapWindowManager.MapLayersHandler != null)
                {
                    MapWindowManager.MapLayersHandler[AOIHandle].Visible = _visibility;
                    MapWindowManager.MapLayersHandler[GridHandle].Visible = _visibility;
                }
            }
        }

   

        public Shapefile ShapeFile
        {
            get
            {
                return ShapefileFactory.AOIShapefileFromAOI(this);
            }
        }
    }
}
