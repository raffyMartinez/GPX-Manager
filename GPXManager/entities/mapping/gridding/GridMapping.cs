using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MapWinGIS;

namespace GPXManager.entities.mapping.gridding
{
    public  class GridMapping
    {
        public GridMapping(AOI aoi)
        {
            AOI = aoi;
        }
        public bool HasGriddedData { get; internal set; }
        public  bool IsFishingInternsityMapped { get; internal set; }
        public  bool IsUndersizedMapped{ get; internal set; }
        public  bool IsBerriedMapped { get; internal set; }
        public  bool IsCPUEMapped { get; internal set; }

        public  Dictionary<string, int> CellsSweptByTrackDict { get; private set; } = new Dictionary<string, int>();
        public  AOI AOI { get; set; }

        public  List<Shape> SelectedTracks { get; set; }
        public  int[] SelectedTrackIndexes { get; set; }

        public  int ComputeFishingFrequency()
        {
            int counter = 0;


            //if the field exists, we delete it so that the old data is also deleted
            var fldIndex = AOI.SubGrids.FieldIndexByName["Hits"];
            if(fldIndex>=0)
            {
                AOI.SubGrids.EditDeleteField(fldIndex);
            }

            //then we add the Hits field, with emtpy data
            fldIndex = AOI.SubGrids.EditAddField("Hits", FieldType.INTEGER_FIELD, 1, 1);

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
            HasGriddedData = true;
            IsFishingInternsityMapped = counter > 0;
            return counter;
        }
    }
}
