using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AxMapWinGIS;
using MapWinGIS;

namespace GPXManager.entities.mapping
{
    public static class AOIManager
    {
        private static int _editedAOI_ID;
        public static string AOIName { get; set; } = "";
        public static int _hAOI { get; set; } = -1;
        private static Shapefile _sfAOI;


        public static bool EnableMapInteraction { get; set; }
        public static void Setup()
        {
            MapWindowManager.MapInterActionHandler.ExtentCreated += OnExentCreated;
        }
        public static void AddNew ()
        {
            MapWindowManager.MapControl.MapCursor = tkCursor.crsrCross;
            MapWindowManager.MapControl.CursorMode = tkCursorMode.cmSelection;
            EnableMapInteraction = true;
        }
        public static void Edit(AOI aoi)
        {
            _hAOI = aoi.MapLayerHandle;
            AOIName = aoi.Name;
            _editedAOI_ID = aoi.ID; ;
            MapWindowManager.MapControl.MapCursor = tkCursor.crsrCross;
            MapWindowManager.MapControl.CursorMode = tkCursorMode.cmSelection;
            EnableMapInteraction = true;
        }


        public static AOI  SaveAOI(string name, bool isEdited=false)
        {
            if (_hAOI >= 0)
            {
                var aoi = new AOI
                {
                    Name = name,
                    UpperLeftX = _sfAOI.Extents.xMin,
                    UpperLeftY = _sfAOI.Extents.yMax,
                    LowerRightX = _sfAOI.Extents.xMax,
                    LowerRightY = _sfAOI.Extents.yMin,
                    Visibility = true,
                    MapLayerHandle = _hAOI
                };
                if (!isEdited)
                {
                    aoi.ID = Entities.AOIViewModel.NextRecordNumber;
                    Entities.AOIViewModel.AddRecordToRepo(aoi);
                }
                else
                {
                    aoi.ID = _editedAOI_ID;
                    Entities.AOIViewModel.UpdateRecordInRepo(aoi);
                }
                UpdateAOIName(name);
                MapWindowManager.ResetCursor();
                return aoi;
            }
            return null;
        }
        public static void UpdateAOIName(int hAOI, string aoiName)
        {
            _hAOI = hAOI;
            UpdateAOIName(aoiName);
        }
        public static void UpdateAOIName(string aoiName)
        {
            MapWindowManager.MapLayersHandler.EditLayer(_hAOI, aoiName, true);
            Shapefile sf = (Shapefile)MapWindowManager.MapLayersHandler.get_MapLayer(_hAOI).LayerObject;
            var fldName = sf.EditAddField("Name", FieldType.STRING_FIELD, 1, 1);
            sf.EditCellValue(fldName, 0, aoiName);
        }

        private static void OnExentCreated(MapInterActionHandler s, LayerEventArg e)
        {
            if (EnableMapInteraction)
            {
                if (_hAOI >= 0)
                {
                    MapWindowManager.MapLayersHandler.RemoveLayer(_hAOI);
                }
                else
                {
                    AOIName = "New AOI";
                }

                _sfAOI = new Shapefile();
                if (_sfAOI.CreateNewWithShapeID("", ShpfileType.SHP_POLYGON))
                {
                    if (_sfAOI.EditAddShape(e.SelectionExtent.ToShape()) >= 0)
                    {
                        _hAOI = MapWindowManager.MapLayersHandler.AddLayer(_sfAOI, AOIName);
                        if (_hAOI >= 0)
                        {
                            var fldName = _sfAOI.EditAddField("Name", FieldType.STRING_FIELD, 1, 1);
                            _sfAOI.Key = "aoi";
                            FormatAOI(_sfAOI);
                        }
                    }
                }
            }
        }

        public static void FormatAOI(Shapefile aoiShapeFile)
        {
            aoiShapeFile.DefaultDrawingOptions.FillTransparency = 0.25F;

            LinePattern lp = new LinePattern();
            

            MapWindowManager.MapLayersHandler.ClearAllSelections();
            MapWindowManager.RedrawMap();
        }
    }
}
