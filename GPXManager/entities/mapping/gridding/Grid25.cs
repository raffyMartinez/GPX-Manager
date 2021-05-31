using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MapWinGIS;
namespace GPXManager.entities.mapping.gridding
{
    public struct UTMPoint
    {
        public double Northing { get; set; }
        public double Easting { get; set; }

        public string ZoneLetter { get; set; }
        public int ZoneNumber { get; set; }
    }
    public class UTMExtent
    {
        public UTMExtent(UTMPoint upperLeft, UTMPoint lowerRight)
        {
            UpperLeft = upperLeft;
            LowerRight = lowerRight;
        }
        public UTMPoint UpperLeft { get; private set; }
        public UTMPoint LowerRight { get; private set; }
    }
    public enum UTMZone
    {
        UTMZone50N,
        UTMZone51N
    }
    public static class Grid25
    {
        static Grid25()
        {
            UTMZone = UTMZone.UTMZone50N;
        }

        private static int GRIDSIZE = 50000;
        private static UTMZone _utmZone;
        public static UTMZone UTMZone
        {
            get { return _utmZone; }
            set 
            {
                _utmZone = value; 
                switch(_utmZone)
                {
                    case UTMZone.UTMZone50N:
                        GeoProjecction = tkWgs84Projection.Wgs84_UTM_zone_50N;
                        break;
                    case UTMZone.UTMZone51N:
                        GeoProjecction = tkWgs84Projection.Wgs84_UTM_zone_51N;
                        break;
                }
            }
        }

        public static Extents ExtentToUTM(Extents ext)
        {
            LatLonUTMConverter llc = new LatLonUTMConverter("WGS 84");
            var ul = llc.convertLatLngToUtm(ext.yMax, ext.xMin);
            var lr = llc.convertLatLngToUtm(ext.yMin, ext.xMax);
            ext = new Extents();
            ext.SetBounds(ul.Easting, lr.Northing, 0, lr.Easting, ul.Northing, 0);
            return ext;
        }
        public static Shapefile CreateGrid25MajorGrid()
        {
            var sf = new Shapefile();
            Shape shp;
            Point pt;
            var xOrigin = 0;
            var yOrigin = 0;
            var cols = 0;
            var rows = 0;
            var gridNumber = 0;
            var iFld = 0;
            var offsetColumns = 0;
            var iShp = 0;

            if (sf.CreateNewWithShapeID("", ShpfileType.SHP_POLYGON))
            {
                iFld = sf.EditAddField("Grid_no", FieldType.INTEGER_FIELD, 1, 4);
                sf.EditAddField("toGrid", FieldType.STRING_FIELD, 1, 1);
                sf.GeoProjection.SetWgs84Projection(GeoProjecction);

                //set the origin, rows and columns
                //yOrigin = FishingGrid.Grid25.MajorGridYOrigin;
                //xOrigin = FishingGrid.Grid25.MajorGridXOrigin;
                switch (GeoProjecction)
                {
                    case tkWgs84Projection.Wgs84_UTM_zone_50N:
                        xOrigin = 300000;
                        //yOrigin = 800000;
                        yOrigin = 400000;
                        cols = 15;
                        rows = 21;
                        offsetColumns = 0;
                        gridNumber = 1;
                        break;

                    case tkWgs84Projection.Wgs84_UTM_zone_51N:
                        xOrigin = 0;
                        yOrigin = 350000;
                        cols = 20;
                        rows = 41;
                        offsetColumns = 10;
                        gridNumber = 11;
                        break;
                }

                //build the major grids
                for (int row = 0; row < rows; row++)
                {
                    for (int col = 0; col < cols; col++)
                    {
                        shp = new Shape();
                        if (shp.Create(ShpfileType.SHP_POLYGON))
                        {
                            for (int n = 0; n < 5; n++)
                            {
                                pt = new Point();
                                switch (n)
                                {
                                    case 0:
                                        pt.x = xOrigin + (col * GRIDSIZE);
                                        pt.y = yOrigin + (row * GRIDSIZE);
                                        break;

                                    case 1:
                                        pt.x = xOrigin + (col * GRIDSIZE);
                                        pt.y = yOrigin + GRIDSIZE + (row * GRIDSIZE);
                                        break;

                                    case 2:
                                        pt.x = xOrigin + GRIDSIZE + (col * GRIDSIZE);
                                        pt.y = yOrigin + GRIDSIZE + (row * GRIDSIZE);
                                        break;

                                    case 3:
                                        pt.x = xOrigin + GRIDSIZE + (col * GRIDSIZE);
                                        pt.y = yOrigin + (row * GRIDSIZE);
                                        break;

                                    case 4:
                                        pt.x = xOrigin + (col * GRIDSIZE);
                                        pt.y = yOrigin + (row * GRIDSIZE);
                                        break;
                                }
                                shp.AddPoint(pt.x, pt.y);
                            }
                            iShp = sf.EditAddShape(shp);

                            if (iShp >= 0)
                                sf.EditCellValue(iFld, iShp, gridNumber);

                            gridNumber++;
                        }
                    }
                    gridNumber += offsetColumns;
                }
            }

            if (sf.NumShapes > 0)
            {
                MajorGrid = sf;
                ConfigureGridAppearance();
            }
            return sf;
        }

        private static void ConfigureGridAppearance()
        {
            MajorGrid.With(sf =>
            {
                //appearance for unselected grids
                sf.DefaultDrawingOptions.FillVisible = false;
                sf.DefaultDrawingOptions.LineWidth = 2;
                sf.DefaultDrawingOptions.LineColor = new Utils().ColorByName(tkMapColor.Red);
                var fldIndex = sf.FieldIndexByName["grid_no"];
                sf.GenerateLabels(fldIndex, tkLabelPositioning.lpCenter);
                sf.Labels.FontSize = 12;
                sf.Labels.FontBold = true;
                sf.Labels.FrameVisible = false;

                //create a category which will set the appearance of selected grids
                if (sf.StartEditingTable(null))
                {
                    var category = new ShapefileCategory
                    {
                        Name = "Selected grid",
                        Expression = @"[toGrid] =""T"""
                    };
                    category.DrawingOptions.FillColor = new Utils().ColorByName(tkMapColor.Red);
                    category.DrawingOptions.FillTransparency = 25;
                    category.DrawingOptions.LineWidth = 2;
                    category.DrawingOptions.LineColor = new Utils().ColorByName(tkMapColor.Red);
                    sf.Categories.Add2(category);
                }
            });
        }
        public static Shapefile MajorGrid { get; private set; }
        public static tkWgs84Projection GeoProjecction { get; internal set; }
    }
}
