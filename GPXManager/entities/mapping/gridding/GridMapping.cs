using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MapWinGIS;

namespace GPXManager.entities.mapping.gridding
{
    public static class GridMapping
    {
        public static Dictionary<string, int> CellsSweptByTrackDict { get; private set; } = new Dictionary<string, int>();
        public static AOI AOI { get; set; }

        public static List<Shape> SelectedTracks { get; set; }
        public static int[] SelectedTrackIndexes { get; set; }

        public static int ComputeFishingFrequency()
        {
            int counter = 0;
            var fldIndex = AOI.SubGrids.EditAddField("Hits", FieldType.INTEGER_FIELD, 1, 1);
            if (fldIndex >= 0)
            {
                foreach (var shp in SelectedTracks)
                {
                    var sf = new Shapefile();
                    if (sf.CreateNew("", ShpfileType.SHP_POLYLINE))
                    {
                        sf.GeoProjection = MapWindowManager.ExtractedTracksShapefile.GeoProjection;
                        var idx = sf.EditAddShape(shp);
                        if (idx >= 0)
                        {
                            var selected = new object();
                            AOI.SubGrids.SelectByShapefile(sf, tkSpatialRelation.srIntersects, false, ref selected);
                            var selected2 = (int[])selected;
                            if (selected2.Count() > 0)
                            {
                                for (int x = 0; x < selected2.Count(); x++)
                                {
                                    var cellHit = AOI.SubGrids.CellValue[fldIndex, selected2[x]];
                                    if (cellHit == null)
                                    {
                                        AOI.SubGrids.EditCellValue(fldIndex, selected2[x], 1);
                                    }
                                    else
                                    {
                                        AOI.SubGrids.EditCellValue(fldIndex, selected2[x], (int)cellHit + 1);
                                    }
                                    counter++;
                                }
                            }

                        }

                    }
                }
            }
            return counter;
        }
    }
}
