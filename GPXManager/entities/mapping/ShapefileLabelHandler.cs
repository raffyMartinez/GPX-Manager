using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using MapWinGIS;
using MapWin.classes;

namespace GPXManager.entities.mapping
{
    public class ShapefileLabelHandler : IDisposable
    {
        private bool _disposed;
        private Shapefile _shapeFile;
        public LabelProperty LabelProperty { get; set; }
        private MapLayer _mapLayer;

        private void SetupLabelClass(Shapefile sf)
        {
            sf.Labels.With(l =>
            {
                LabelProperty.Expression = l.Expression;
                LabelProperty.Alignment = l.Alignment;
                LabelProperty.FontName = l.FontName;
                LabelProperty.FontSize = l.FontSize;
                LabelProperty.FontColor = l.FontColor;
                LabelProperty.FontColor2 = l.FontColor2;
                LabelProperty.FontGradientMode = l.FontGradientMode;
                LabelProperty.FontBold = l.FontBold;
                LabelProperty.FontItalic = l.FontItalic;
                LabelProperty.FontStrikeout = l.FontStrikeOut;
                LabelProperty.FontUnderline = l.FontUnderline;
                LabelProperty.FontTransparency = l.FontTransparency;
                LabelProperty.FontOutlineColor = l.FontOutlineColor;
                LabelProperty.FontOutlineWidth = l.FrameOutlineWidth;
                LabelProperty.FontOutlineVisible = l.FontOutlineVisible;

                LabelProperty.FrameBackColor = l.FrameBackColor;
                LabelProperty.FrameBackColor2 = l.FrameBackColor2;
                LabelProperty.FrameGradientMode = l.FrameGradientMode;
                LabelProperty.FrameOutlineColor = l.FrameOutlineColor;
                LabelProperty.FrameOutlineStyle = l.FrameOutlineStyle;
                LabelProperty.FrameOutlineWidth = l.FrameOutlineWidth;
                LabelProperty.FramePaddingX = l.FramePaddingX;
                LabelProperty.FramePaddingY = l.FramePaddingY;
                LabelProperty.FrameTransparency = l.FrameTransparency;
                LabelProperty.FrameType = l.FrameType;
                LabelProperty.FrameVisible = l.FrameVisible;

                LabelProperty.HaloColor = l.HaloColor;
                LabelProperty.HaloSize = l.HaloSize;
                LabelProperty.HaloVisible = l.HaloVisible;

                LabelProperty.InboxAlignment = l.InboxAlignment;
                LabelProperty.LineOrientation = l.LineOrientation;
                LabelProperty.OffsetX = l.OffsetX;
                LabelProperty.OffsetY = l.OffsetY;

                LabelProperty.ShadowColor = l.ShadowColor;
                LabelProperty.ShadowOffsetX = l.ShadowOffsetX;
                LabelProperty.ShadowOffsetY = l.ShadowOffsetY;
                LabelProperty.ShadowVisible = l.ShadowVisible;
            });
        }

        public ShapefileLabelHandler(MapLayer mapLayer)
        {
            _mapLayer = mapLayer;
            _shapeFile = (Shapefile)_mapLayer.LayerObject;
        }

        /// <summary>
        /// labels the shapefile using parameters in labelXML
        /// </summary>
        /// <param name="labelXML"></param>
        /// <returns></returns>
        public bool LabelShapefile(string labelXML)
        {
            var lbls = 0;

            //applies label properties to the current shapefile's labels
            _shapeFile.Labels.Deserialize(labelXML);

            //we convert string labelXML to an xml object
            var doc = new XmlDocument();
            doc.LoadXml(labelXML);

            //we use attributes such as SourceField and Positioning in generating labels for the shapefile
            doc.DocumentElement.With(d =>
            {
                if (d.Attributes["Expression"] != null)
                {
                    _mapLayer.Expression = d.Attributes["Expression"].Value;
                    _shapeFile.Labels.Expression = _mapLayer.Expression;
                    lbls = _shapeFile.Labels.Count;
                    _mapLayer.LabelSource = "Expression";
                }
                else if (d.Attributes["SourceField"] != null)
                {
                    _mapLayer.LabelField = int.Parse(d.Attributes["SourceField"].Value);
                    lbls = _shapeFile.GenerateLabels(_mapLayer.LabelField, (tkLabelPositioning)int.Parse(d.Attributes["Positioning"].Value), true);
                    _mapLayer.LabelSource = "SourceField";
                }

                if (d.Attributes["VisibilityExpression"] != null)
                {
                    _mapLayer.LabelsVisibilityExpression = d.Attributes["VisibilityExpression"].Value;
                }

                //AvoidCollision is not included in labelXML so we put it here
                if (d.Attributes["AvoidCollision"]?.Value == "1")
                {
                    _shapeFile.Labels.AvoidCollisions = true;
                }
                else if (d.Attributes["AvoidCollision"]?.Value == "0")
                {
                    _shapeFile.Labels.AvoidCollisions = false;
                }
            });

            return lbls >= 0;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// saves those parameters that are not included when invoking axMap.SaveMapState
        /// </summary>
        /// <param name="layerHandle"></param>
        /// <param name="AvoidCollision"></param>
        public static void SaveLabelParameters(string fileMapState, int layerHandle, bool AvoidCollision)
        {
            var doc = new XmlDocument();
            var n = 0;
            doc.Load(fileMapState);
            foreach (XmlNode ly in doc.DocumentElement.SelectNodes("//Layer"))
            {
                //if (ly.Attributes["LayerType"].Value == "Shapefile")
                if (n == layerHandle)
                {
                    foreach (XmlNode child in ly.FirstChild.ChildNodes)
                    {
                        if (child.Name == "LabelsClass")
                        {
                            var att = doc.CreateAttribute("AvoidCollision");
                            att.Value = AvoidCollision ? "1" : "0";
                            child.Attributes.Append(att);

                            doc.Save(fileMapState);
                            return;
                        }
                    }
                }
                n++;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                }
                _shapeFile = null;
                _disposed = true;
            }
        }

        /// <summary>
        /// Tests expression entered by user
        /// </summary>
        public static (bool isValid, string message) TestExpression(string expression, Shapefile shapeFile)
        {
            var result = string.Empty;
            var isValid = true;
            if (expression.Length > 0)
            {
                string expr = FixExpression(expression);
                if (expr == String.Empty)
                {
                    result = "No expression";
                }
                else
                {
                    string err = "";
                    if (!shapeFile.Table.TestExpression(expr, tkValueType.vtString, ref err))
                    {
                        result = err;
                        isValid = false;
                    }
                    else
                    {
                        result = "Expression is valid";
                    }
                }
            }

            return (isValid, result);
        }

        /// <summary>
        /// Returns the expression which complies with the ocx parser rules
        /// The new line characters should be placed in quotes
        /// </summary>
        public static string FixExpression(string s)
        {
            string res = "";
            int count = 0;
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '\"')
                {
                    count++;
                }

                // there is new line character outside any brackets
                if (s[i] == '\n' && count % 2 == 0)
                {
                    res += "\"\n\"+";
                }
                else
                {
                    res += s[i];
                }
            }
            return res;
        }
    }
}
