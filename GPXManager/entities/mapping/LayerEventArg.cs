using System;
using MapWinGIS;

namespace GPXManager.entities.mapping
{
    public class LayerEventArg : EventArgs
    {
        public int LayerHandle { get; }
        public string LayerName { get; set; }
        public bool ShowInLayerUI { get; }
        public bool LayerVisible { get; set; }
        public bool LayerRemoved { get; }
        public string LayerType { get; }
        public bool LayerSaved { get; }
        public string FileName { get; set; }
        public int SelectedIndex { get; set; }
        public string Action { get; set; }
        public string MapTitle { get; set; }
        public int[] SelectedIndexes { get; set; }
        public string VisibilityExpression { get; }
        public VisibilityExpressionTarget ExpressionTarget { get; }
        public Extents SelectedExtent { get; set; }
        public Extents SelectionExtent { get; set; }

        public LayerEventArg(int layerHandle, string layerName, bool layerVIsible, bool showInLayerUI, string layerType)
        {
            LayerHandle = layerHandle;
            LayerName = layerName;
            LayerVisible = layerVIsible;
            ShowInLayerUI = showInLayerUI;
            LayerType = layerType;
        }

        public LayerEventArg(int layerHandle, int[] selectedIndexes)
        {
            LayerHandle = layerHandle;
            SelectedIndexes = selectedIndexes;
        }

        public LayerEventArg(int layerHandle, string mapTitle)
        {
            LayerHandle = layerHandle;
            MapTitle = mapTitle;
        }

        public LayerEventArg(int layerHandle, bool layerRemoved)
        {
            LayerHandle = layerHandle;
            LayerRemoved = layerRemoved;
        }

        public LayerEventArg(int layerHandle, bool layerSaved, string fileName)
        {
            LayerHandle = layerHandle;
            LayerSaved = layerSaved;
            FileName = fileName;
        }

        public LayerEventArg(int layerHandle, VisibilityExpressionTarget target, string visibilityExpression)
        {
            LayerHandle = layerHandle;
            ExpressionTarget = target;
            VisibilityExpression = visibilityExpression;
        }

        public LayerEventArg(int layerHandle)
        {
            LayerHandle = layerHandle;
        }

        public LayerEventArg(string layerName)
        {
            LayerName = layerName;
        }

        public LayerEventArg(Extents selectionExtent)
        {
            SelectionExtent = selectionExtent;
        }


    }
}
