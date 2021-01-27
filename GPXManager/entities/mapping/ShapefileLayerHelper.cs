
using MapWinGIS;
using System.Collections.Generic;

namespace GPXManager.entities.mapping
{
    public static class ShapefileLayerHelper
    {
        public static int PointSizeOfMaxCategory { get; set; }
        public static int NumberOfCategories { get; set; }

        public static void CategorizeNumericPointLayer(Shapefile sf, int classificationField = 1,
                                                       tkClassificationType Method = tkClassificationType.ctNaturalBreaks)
        {
            float ptSize = 0;
            if (sf.Categories.Generate(classificationField, Method, NumberOfCategories))
            {
                for (int n = 0; n < sf.Categories.Count; n++)
                {
                    var category = sf.Categories.Item[n];
                    ptSize = PointSizeOfMaxCategory * ((float)(n + 1) / sf.Categories.Count);
                    category.DrawingOptions.PointSize = ptSize;
                    category.DrawingOptions.LineColor = new Utils().ColorByName(tkMapColor.White);
                }
            }
            var cat0 = sf.Categories.Add("zero");
            Field f = sf.Field[classificationField];
            cat0.Expression = $"[{f.Name}]=0";
            cat0.DrawingOptions.PointSize = 0;
            cat0.DrawingOptions.FillVisible = false;
            cat0.DrawingOptions.LineVisible = false;
            sf.Categories.ApplyExpression(sf.Categories.CategoryIndex[cat0]);
        }

        public static void CategorizeNumericPointLayer(Shapefile sf, List<double> breaks, int classificationField = 1, double maxValue = 0, double minValue = 0, bool ignoreZero = false)
        {
            sf.Categories.ClassificationField = classificationField;
            for (int b = 0; b < breaks.Count; b++)
            {
                var cat = sf.Categories.Add(b.ToString());
                if (b == 0 && ignoreZero)
                {
                    cat.MinValue = minValue;
                }
                else
                {
                    cat.MinValue = breaks[b];
                }
                if ((b + 1) == breaks.Count)
                {
                    cat.MaxValue = maxValue;
                }
                else
                {
                    cat.MaxValue = breaks[b + 1];
                }

                cat.DrawingOptions.LineColor = new Utils().ColorByName(tkMapColor.White);
                cat.DrawingOptions.PointSize = PointSizeOfMaxCategory * ((float)(b + 1) / breaks.Count);
            }
            var cat0 = sf.Categories.Add("zero");
            cat0.DrawingOptions.LineColor = new Utils().ColorByName(tkMapColor.White);
            cat0.DrawingOptions.PointSize = 0;
            cat0.DrawingOptions.FillVisible = false;
            cat0.DrawingOptions.LineVisible = false;
            sf.Categories.ApplyExpressions();
            for (int n = 0; n < sf.NumShapes; n++)
            {
                double v = (int)sf.CellValue[classificationField, n];
                if (v > 0)
                {
                    for (int c = 0; c < breaks.Count; c++)
                    {
                        if (c + 1 < breaks.Count)
                        {
                            if (v >= breaks[c] && v < breaks[c + 1])
                            {
                                sf.ShapeCategory[n] = c;
                                break;
                            }
                        }
                        else
                        {
                            sf.ShapeCategory[n] = breaks.Count - 1;
                        }
                    }
                }
                else
                {
                    sf.ShapeCategory3[n] = cat0;
                }
            }
        }

        public static void CategorizeNumericPointLayer1(Shapefile sf, List<double> breaks, int classificationField = 1)
        {
            for (int b = 0; b < breaks.Count; b++)
            {
                var cat = sf.Categories.Add(b.ToString());
                cat.DrawingOptions.LineColor = new Utils().ColorByName(tkMapColor.White);
                cat.DrawingOptions.PointSize = PointSizeOfMaxCategory * ((float)(b + 1) / breaks.Count);
            }
            var cat0 = sf.Categories.Add("zero");
            cat0.DrawingOptions.LineColor = new Utils().ColorByName(tkMapColor.White);
            cat0.DrawingOptions.PointSize = 0;
            cat0.DrawingOptions.FillVisible = false;
            cat0.DrawingOptions.LineVisible = false;

            for (int n = 0; n < sf.NumShapes; n++)
            {
                double v = (int)sf.CellValue[classificationField, n];
                if (v > 0)
                {
                    for (int c = 0; c < breaks.Count; c++)
                    {
                        if (c + 1 < breaks.Count)
                        {
                            if (v >= breaks[c] && v < breaks[c + 1])
                            {
                                sf.ShapeCategory[n] = c;
                                break;
                            }
                        }
                        else
                        {
                            sf.ShapeCategory[n] = breaks.Count - 1;
                        }
                    }
                }
                else
                {
                    sf.ShapeCategory3[n] = cat0;
                }
            }
        }

        static ShapefileLayerHelper()
        {
            PointSizeOfMaxCategory = 40;
            NumberOfCategories = 5;
        }

        /// <summary>
        /// Creates a new shapefile from a point shapefile where each point is located in the center of a grid25 cell. All fields in the source point shapefile are copied to the new shapefile
        /// </summary>
        /// <param name="pointShapefile"></param>
        /// <param name="utmZone"></param>
        /// <returns></returns>

        public static ShpfileType ShapefileType2D(MapWinGIS.ShpfileType shpType)
        {
            if (shpType == ShpfileType.SHP_POLYGON || shpType == ShpfileType.SHP_POLYGONM || shpType == ShpfileType.SHP_POLYGONZ)
            {
                return ShpfileType.SHP_POLYGON;
            }
            else if (shpType == ShpfileType.SHP_POLYLINE || shpType == ShpfileType.SHP_POLYLINEM || shpType == ShpfileType.SHP_POLYLINEZ)
            {
                return ShpfileType.SHP_POLYLINE;
            }
            else if (shpType == ShpfileType.SHP_POINT || shpType == ShpfileType.SHP_POINTM || shpType == ShpfileType.SHP_POINTZ ||
                     shpType == ShpfileType.SHP_MULTIPOINT || shpType == ShpfileType.SHP_MULTIPOINTM || shpType == ShpfileType.SHP_MULTIPOINTZ)
            {
                return ShpfileType.SHP_POINT;
            }
            else
            {
                return ShpfileType.SHP_NULLSHAPE;
            }
        }

        public static ExtentCompare ExtentsPosition(Extents ext1, Extents ext2)
        {
            ExtentCompare exco = ExtentCompare.excoSimilar;
            var pointInside = false;
            ext1.GetBounds(out double xMin1, out double yMin1, out double zMin1, out double xMax1, out double yMax1, out double zMax1);
            ext2.GetBounds(out double xMin2, out double yMin2, out double zMin2, out double xMax2, out double yMax2, out double zMax2);

            if (xMax1 == xMax2 && yMax1 == yMax2 && xMin1 == xMin2 && yMin1 == yMin2)
            {
                exco = ExtentCompare.excoSimilar;
            }
            else if (xMax1 > xMax2 && xMin1 < xMin2 && yMax1 > yMax2 && yMin1 < yMin2)
            {
                exco = ExtentCompare.excoInside;
            }
            else
            {
                pointInside = xMin2 > xMin1 && xMin2 < xMax1 && yMax2 < yMax1 && yMax2 > yMin1;
                if (!pointInside)
                {
                    pointInside = xMax2 > xMin1 && xMax2 < xMax1 && yMax2 < yMax1 && yMax2 > yMin1;
                }
                if (!pointInside)
                {
                    pointInside = xMin2 > xMin1 && xMin2 < xMax1 && yMin2 < yMax1 && yMin2 > yMin1;
                }
                if (!pointInside)
                {
                    pointInside = xMax2 > xMin1 && xMax2 < xMax1 && yMax2 < yMax1 && yMin2 > yMin1;
                }

                if (pointInside)
                {
                    exco = ExtentCompare.excoCrossing;
                }
                else
                {
                    exco = ExtentCompare.excoOutside;
                }
            }
            return exco;
        }
    }
}