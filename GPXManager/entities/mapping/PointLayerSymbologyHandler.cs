using MapWinGIS;
using System.Xml;

namespace GPXManager.entities.mapping
{
    public class PointLayerSymbologyHandler
    {
        private MapLayer _mapLayer;
        private Shapefile _shapeFile;

        public PointLayerSymbologyHandler(MapLayer mapLayer)
        {
            _mapLayer = mapLayer;
            _shapeFile = _mapLayer.LayerObject as Shapefile;
        }

        public void SymbolizeLayer(string symbologyXML)
        {
            _shapeFile.Deserialize(LoadSelection: false, symbologyXML);
            //we convert string labelXML to an xml object
            var doc = new XmlDocument();
            doc.LoadXml(symbologyXML);
        }
    }
}