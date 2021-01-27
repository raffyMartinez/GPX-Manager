using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MapWinGIS;

namespace GPXManager.entities.mapping
{
    public static class TripMappingManager
    {
        private static MapInterActionHandler _mapInterActionHandler;
        public static MapInterActionHandler MapInteractionHandler
        {
            get { return _mapInterActionHandler; }
            set
            {
                _mapInterActionHandler = value;
                _mapInterActionHandler.ShapesSelected += _mapInterActionHandler_ShapesSelected;
            }
        }

        public static void RemoveTripLayersFromMap()
        {
            if (_mapInterActionHandler != null)
            {
                _mapInterActionHandler.MapLayersHandler.RemoveLayerByKey("trip_track");
                _mapInterActionHandler.MapLayersHandler.RemoveLayerByKey("trip_waypoints");
            }
        }

        private static void _mapInterActionHandler_ShapesSelected(MapInterActionHandler s, LayerEventArg e)
        {

        }

       
        public static bool MapTrip(List<Trip> trips)
        {

            var trackHandles = new List<int>();
            var pointHandles= new List<int>();
            var sf = ShapefileFactory.TrackFromTrip(trips, out trackHandles);
            var pointSF = ShapefileFactory.PointsFromTrips(trips, out pointHandles);

            MapInteractionHandler.MapLayersHandler.AddLayer(sf, "Track", uniqueLayer: true, layerKey: sf.Key);
            MapInteractionHandler.MapLayersHandler.AddLayer(pointSF, "Waypoints", uniqueLayer: true, layerKey: sf.Key);
            TrackShapefile = sf;
            WaypointsShapefile = pointSF;
            return sf != null;
        }

        public static Shapefile TrackShapefile { get; set; }
        public static Shapefile WaypointsShapefile { get; set; }

        public static void Cleanup()
        {
            Entities.TripViewModel.MarkAllNotShownInMap();
            _mapInterActionHandler.ShapesSelected -= _mapInterActionHandler_ShapesSelected;
            _mapInterActionHandler = null;
        }
        public static void RemoveAllFromMap()
        {
            foreach (var item in Entities.TripViewModel.TripCollection)
            {
                item.ShownInMap = false;
            }
        }
    }
}
