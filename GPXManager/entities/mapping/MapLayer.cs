
using MapWinGIS;
using System;
using System.Drawing;
using System.Collections.Generic;
using System.IO;
using MapWin.classes;

namespace GPXManager.entities.mapping
{/// <summary>
/// class for a layer that is shown on a map window
/// </summary>
    public class MapLayer : IDisposable
    {
        public override string ToString()
        {
            return $"{Name} ({LayerType})";
        }


        public string LayerKey { get; set; }
        public string Name { get; set; }
        public bool Visible { get; set; }
        public bool VisibleInLayersUI { get; set; }
        public int Handle { get; set; }
        public string FileName { get; set; }
        public string GeoProjectionName { get; set; }
        public int LayerPosition { get; set; }
        public Bitmap ImageThumbnail { get; set; }
        public string LayerType { get; set; }
        public object LayerObject { get; set; }
        private bool _disposed;
        public int[] SelectedIndexes { get; set; }
        public bool IsGraticule { get; set; }
        public bool IsFishingGrid { get; set; }
        public int? LayerWeight { get; set; }
        public bool IsMaskLayer { get; set; }
        public bool IsLabeled { get; set; }
        public int LabelField { get; set; }
        public string Expression { get; set; }
        public string LabelSource { get; set; }
        public string LabelsVisibilityExpression { get; set; }
        public string ShapesVisibilityExpression { get; set; }
        public string LabelSettingsXML { get; internal set; }
        public string SymbolSettinsXML { get; internal set; }
        public Labels Labels { get; internal set; }
        public fad3MappingMode MappingMode { get; set; }
        public bool IsFishingGridLayoutTemplate { get; set; }
        public bool KeepOnTop { get; set; }
        public bool PrintOnFront { get; set; }
        public bool PrintLabelsFront { get; set; }
        public bool PrintOnReverse { get; set; }
        public bool PrintLabelsReverse { get; set; }
        public bool IsGrid25Layer { get; set; }
        private bool _isPointDatabaseLayer;
        public bool IgnoreZeroWhenClassifying { get; set; }
        public Type ClassifiedValueDataType { get; set; }
        public List<string> ClassificationItemCaptions { get; set; }
        private ClassificationType _classificationType;
        public bool IncludeInLegend { get; set; }
        private MapLayersHandler _parent;
        public Size SymbolSize { get; set; }


        public Dictionary<string, ClassifiedItem> ClassificationItems = new Dictionary<string, ClassifiedItem>();



        public ClassificationType ClassificationType
        {
            get { return _classificationType; }
            set
            {
                var sf = LayerObject as Shapefile;
                ClassificationItems.Clear();
                _classificationType = value;
                if (_classificationType != ClassificationType.None)
                {
                    switch (sf.Field[sf.Categories.ClassificationField].Type)
                    {
                        case FieldType.BOOLEAN_FIELD:
                            ClassifiedValueDataType = typeof(bool);
                            break;

                        case FieldType.DATE_FIELD:
                            ClassifiedValueDataType = typeof(DateTime);
                            break;

                        case FieldType.DOUBLE_FIELD:
                            ClassifiedValueDataType = typeof(double);
                            break;

                        case FieldType.INTEGER_FIELD:
                            ClassifiedValueDataType = typeof(int);
                            break;

                        case FieldType.STRING_FIELD:
                            ClassifiedValueDataType = typeof(string);
                            break;
                    }
                }

                switch (_classificationType)
                {
                    case ClassificationType.EqualCount:
                        break;

                    case ClassificationType.EqualIntervals:
                        break;

                    case ClassificationType.EqualSumOfValues:
                        break;

                    case ClassificationType.NaturalBreaks:
                    case ClassificationType.JenksFisher:

                        for (int n = 0; n < sf.Categories.Count; n++)
                        {
                            double? min = null;
                            double? max = null;
                            string range = "";
                            if (sf.Categories.Item[n].MinValue != null)
                            {
                                min = (double)sf.Categories.Item[n]?.MinValue;
                                if (n == 0 && min == 0 && IgnoreZeroWhenClassifying)
                                {
                                    min = 1;
                                }
                                max = (double)sf.Categories.Item[n]?.MaxValue;
                            }

                            if (min != null)
                            {
                                range = $"{min}-{max - 1}";
                            }
                            else
                            {
                                min = (double)sf.Categories.Item[n - 1]?.MinValue;
                                max = (double)sf.Categories.Item[n - 1]?.MaxValue;
                                if (min == max)
                                {
                                    range = min.ToString();
                                }
                                else
                                {
                                    range = $"{min}-{max}";
                                }
                                ClassificationItems[(n).ToString()].Caption = range;
                                range = "";
                                break;
                            }

                            if (range.Length > 0)
                            {
                                ClassifiedItem cl = new ClassifiedItem(range);
                                cl.DrawingOptions = sf.Categories.Item[n].DrawingOptions;
                                ClassificationItems.Add((n + 1).ToString(), cl);
                            }
                        }
                        break;

                    case ClassificationType.StandardDeviation:
                        break;

                    case ClassificationType.UniqueValues:
                        break;
                }
                _parent.LayerFinishedClassification();
            }
        }


        public bool IsPointDatabaseLayer
        {
            get
            {
                return _isPointDatabaseLayer;
            }
            set
            {
                _isPointDatabaseLayer = value;
                if (_isPointDatabaseLayer)
                {
                }
            }
        }

        public void RestoreSettingsFromXML()
        {
            var sf = LayerObject as Shapefile;
            if (sf != null)
            {
                sf.Labels.Deserialize(LabelSettingsXML);
                sf.DefaultDrawingOptions.Deserialize(SymbolSettinsXML);
            }
        }

        public bool EditShapeFileField(string fieldName, int shapeIndex, object newValue)
        {
            bool successEdit = false;
            if (LayerType == "ShapefileClass")
            {
                var sf = LayerObject as Shapefile;
                successEdit = sf.EditCellValue(sf.FieldIndexByName[fieldName], shapeIndex, newValue);
            }
            return successEdit;
        }

        public void SaveXMLSettings()
        {
            if (LayerType == "ShapefileClass")
            {
                var sf = LayerObject as Shapefile;
                if (sf != null)
                {
                    LabelSettingsXML = sf.Labels.Serialize();
                    SymbolSettinsXML = sf.DefaultDrawingOptions.Serialize();
                }
            }
        }

        public MapLayer(int handle, string name, bool visible, bool visibleInLayersUI, MapLayersHandler parent)
        {
            Handle = handle;
            Name = name;
            Visible = visible;
            VisibleInLayersUI = visibleInLayersUI;
            IsFishingGrid = false;
            _parent = parent;
            //ClassificationType = ClassificationType.None;
        }

        public bool Save(string fileName)
        {
            var success = false;
            if (!fileName.EndsWith(".shp"))
            {
                fileName += ".shp";
            }
            if (LayerType == "ShapefileClass")
            {
                ((Shapefile)LayerObject).With(sf =>
                {
                    success = sf.SaveAs(fileName);                     //saves the shapefile
                    if (success)
                    {
                        string prjFile = fileName.Replace(".shp", ".prj");
                        //sf.GeoProjection.WriteToFile(Path.GetFileName(fileName) + ".prj");        //save the shapefile's projection data
                        sf.GeoProjection.WriteToFile(prjFile);
                    }
                });
            }
            else
            {
                //for image type, possibly
            }
            return success;
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
                if (ImageThumbnail != null)
                {
                    ImageThumbnail.Dispose();
                }
                ImageThumbnail = null;
                LayerObject = null;
                Labels = null;
                _disposed = true;
            }
        }
    }
}