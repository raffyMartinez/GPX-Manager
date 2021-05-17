using MapWinGIS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace GPXManager.entities.mapping
{

    public static class ShapefileFactory
    {
        private static Dictionary<string, string> _ctxDictionary = new Dictionary<string, string>();
        private static MapWinGIS.Utils _mapWinGISUtils = new MapWinGIS.Utils();
        private static Waypoint _wptBefore;
        private static DateTime _timeBefore;
        public static List<WaypointLocalTime> WaypointsinLocalTine { get; set; }

        public static void ClearWaypoints()
        {
            WaypointsinLocalTine.Clear();
        }

        private static void FillCTXDictionary(string xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            XmlNodeList elements = doc.SelectNodes("//E");
            foreach (XmlNode n in elements)
            {
                var key = n.Attributes["I"].Value;
                var val = n.Attributes["N"].Value;
                _ctxDictionary.Add(key, val);
            }
            setWaypointKey = _ctxDictionary.FirstOrDefault(x => x.Value == "Set gear").Key;
            haulWaypointKey = _ctxDictionary.FirstOrDefault(x => x.Value == "Retrieve gear").Key;
        }

        private static string setWaypointKey { get; set; }
        private static string haulWaypointKey { get; set; }
        static ShapefileFactory()
        {
            WaypointsinLocalTine = new List<WaypointLocalTime>();
        }

        public static Shapefile AOIShapefileFromAOI(AOI aoi)
        {
            var sf = new Shapefile();
            if (sf.CreateNewWithShapeID("", ShpfileType.SHP_POLYGON))
            {
                var extent = new Extents();
                extent.SetBounds(aoi.UpperLeftX, aoi.LowerRightY, 0, aoi.LowerRightX, aoi.UpperLeftY, 0);
                if (sf.EditAddShape(extent.ToShape()) >= 0)
                {
                    sf.DefaultDrawingOptions.FillTransparency = 0.25F;
                    sf.Key = "bsc_aoi";
                    return sf;
                }
            }
            return null;
        }

        public static Shapefile PointsFromWaypointList(List<WaypointLocalTime> wpts, out List<int> handles)
        {
            handles = new List<int>();
            Shapefile sf;
            if (wpts.Count > 0)
            {
                if (TripMappingManager.WaypointsShapefile == null || TripMappingManager.WaypointsShapefile.NumFields == 0)
                {
                    sf = new Shapefile();
                    if (sf.CreateNewWithShapeID("", ShpfileType.SHP_POINT))
                    {
                        sf.EditAddField("Name", FieldType.STRING_FIELD, 1, 1);
                        sf.EditAddField("TimeStamp", FieldType.DATE_FIELD, 1, 1);
                        sf.Key = "gpx_waypoints";
                        sf.GeoProjection = globalMapping.GeoProjection;
                        TripMappingManager.WaypointsShapefile = sf;
                    }
                }
                else
                {
                    sf = TripMappingManager.WaypointsShapefile;
                }

                foreach (var pt in wpts)
                {
                    var shp = new Shape();
                    if (shp.Create(ShpfileType.SHP_POINT))
                    {
                        if (shp.AddPoint(pt.Longitude, pt.Latitude) >= 0)
                        {
                            var shpIndex = sf.EditAddShape(shp);
                            if (shpIndex >= 0)
                            {
                                sf.EditCellValue(sf.FieldIndexByName["Name"], shpIndex, pt.Name);
                                sf.EditCellValue(sf.FieldIndexByName["TimeStamp"], shpIndex, pt.Time);
                                handles.Add(shpIndex);
                            }
                        }
                    }
                }
                sf.DefaultDrawingOptions.PointShape = tkPointShapeType.ptShapeCircle;
                sf.DefaultDrawingOptions.PointSize = 12;
                sf.DefaultDrawingOptions.FillColor = _mapWinGISUtils.ColorByName(tkMapColor.Red);
                return sf;

            }
            else
            {
                return null;
            }
        }

        public static Shapefile PointsFromTrips(List<Trip> trips, out List<int> handles)
        {
            int counter = 0;
            handles = new List<int>();
            Shapefile sf = null;
            foreach (var trip in trips)
            {
                if (counter == 0)
                {
                    if (TripMappingManager.WaypointsShapefile == null || TripMappingManager.WaypointsShapefile.NumFields == 0)
                    {
                        sf = new Shapefile();
                        if (sf.CreateNewWithShapeID("", ShpfileType.SHP_POINT))
                        {
                            sf.EditAddField("Name", FieldType.STRING_FIELD, 1, 1);
                            sf.EditAddField("Type", FieldType.STRING_FIELD, 1, 1);
                            sf.EditAddField("Set number", FieldType.INTEGER_FIELD, 3, 1);
                            sf.EditAddField("TimeStamp", FieldType.DATE_FIELD, 1, 1);
                            sf.EditAddField("GPS", FieldType.STRING_FIELD, 1, 1);
                            sf.EditAddField("Filename", FieldType.STRING_FIELD, 1, 1);
                            sf.Key = "trip_waypoints";
                            sf.GeoProjection = globalMapping.GeoProjection;
                            TripMappingManager.WaypointsShapefile = sf;
                        }
                    }
                    else
                    {
                        sf = TripMappingManager.WaypointsShapefile;
                        sf.EditClear();

                    }

                    foreach (var pt in trip.Waypoints)
                    {
                        var shp = new Shape();
                        if (shp.Create(ShpfileType.SHP_POINT))
                        {
                            if (shp.AddPoint(pt.Waypoint.Longitude, pt.Waypoint.Latitude) >= 0)
                            {
                                var shpIndex = sf.EditAddShape(shp);
                                if (shpIndex >= 0)
                                {
                                    sf.EditCellValue(sf.FieldIndexByName["Name"], shpIndex, pt.WaypointName);
                                    sf.EditCellValue(sf.FieldIndexByName["TimeStamp"], shpIndex, pt.TimeStampAdjusted);
                                    sf.EditCellValue(sf.FieldIndexByName["Type"], shpIndex, pt.WaypointType);
                                    sf.EditCellValue(sf.FieldIndexByName["Set number"], shpIndex, pt.SetNumber);
                                    sf.EditCellValue(sf.FieldIndexByName["GPS"], shpIndex, trip.GPS.DeviceName);
                                    sf.EditCellValue(sf.FieldIndexByName["Filename"], shpIndex, trip.GPXFileName);
                                    handles.Add(shpIndex);
                                }
                            }
                        }
                    }
                }
                counter++;
            }

            sf.DefaultDrawingOptions.PointShape = tkPointShapeType.ptShapeCircle;
            sf.DefaultDrawingOptions.PointSize = 12;
            sf.DefaultDrawingOptions.FillColor = _mapWinGISUtils.ColorByName(tkMapColor.Red);

            return sf;
        }
        //public static Shapefile PointsFromWayPointList(List<TripWaypoint>wpts, out List<int>handles, string gpsName, string fileName)
        //{
        //    handles = new List<int>();
        //    Shapefile sf;
        //    if (wpts.Count > 0)
        //    {
        //        if (TripMappingManager.WaypointsShapefile == null || TripMappingManager.WaypointsShapefile.NumFields == 0)
        //        {
        //            sf = new Shapefile();
        //            if (sf.CreateNewWithShapeID("", ShpfileType.SHP_POINT))
        //            {
        //                sf.EditAddField("Name", FieldType.STRING_FIELD, 1, 1);
        //                sf.EditAddField("Type",FieldType.STRING_FIELD,1,1);
        //                sf.EditAddField("Set number",FieldType.INTEGER_FIELD,3,1);
        //                sf.EditAddField("TimeStamp", FieldType.DATE_FIELD, 1, 1);
        //                sf.EditAddField("GPS", FieldType.STRING_FIELD, 1, 1);
        //                sf.EditAddField("Filename", FieldType.STRING_FIELD, 1, 1);
        //                sf.Key = "trip_waypoints";
        //                sf.GeoProjection = globalMapping.GeoProjection;
        //                TripMappingManager.WaypointsShapefile = sf;
        //            }
        //        }
        //        else
        //        {
        //            sf = TripMappingManager.WaypointsShapefile;
        //        }

        //        foreach (var pt in wpts)
        //        {
        //            var shp = new Shape();
        //            if (shp.Create(ShpfileType.SHP_POINT))
        //            {
        //                if (shp.AddPoint(pt.Waypoint.Longitude, pt.Waypoint.Latitude) >= 0)
        //                {
        //                    var shpIndex = sf.EditAddShape(shp);
        //                    if (shpIndex >= 0)
        //                    {
        //                        sf.EditCellValue(sf.FieldIndexByName["Name"], shpIndex, pt.WaypointName);
        //                        sf.EditCellValue(sf.FieldIndexByName["TimeStamp"], shpIndex, pt.TimeStampAdjusted);
        //                        sf.EditCellValue(sf.FieldIndexByName["Type"], shpIndex, pt.WaypointType);
        //                        sf.EditCellValue(sf.FieldIndexByName["Set number"], shpIndex, pt.SetNumber);
        //                        sf.EditCellValue(sf.FieldIndexByName["GPS"], shpIndex, gpsName);
        //                        sf.EditCellValue(sf.FieldIndexByName["Filename"], shpIndex, fileName);
        //                        handles.Add(shpIndex);
        //                    }
        //                }
        //            }
        //        }
        //        sf.DefaultDrawingOptions.PointShape = tkPointShapeType.ptShapeCircle;
        //        sf.DefaultDrawingOptions.PointSize = 12;
        //        sf.DefaultDrawingOptions.FillColor = _mapWinGISUtils.ColorByName(tkMapColor.Red);
        //        return sf;

        //    }
        //    else
        //    {
        //        return null;
        //    }
        //}

        private static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        public static Shapefile TrackFromCTX(CTXFileSummaryView ctxfile, out List<int> handles)
        {
            handles = new List<int>();
            Shapefile sf = null;
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(ctxfile.XML);
            var tracknodes = doc.SelectNodes("//T");
            sf = new Shapefile();
            if (tracknodes.Count > 0 && sf.CreateNewWithShapeID("", ShpfileType.SHP_POLYLINE))
            {
                sf.EditAddField("User", FieldType.STRING_FIELD, 1, 1);
                sf.EditAddField("Gear", FieldType.STRING_FIELD, 1, 1);
                sf.EditAddField("LandingSite", FieldType.STRING_FIELD, 1, 1);
                sf.EditAddField("Start", FieldType.STRING_FIELD, 1, 1);
                sf.EditAddField("Finished", FieldType.STRING_FIELD, 1, 1);



                var shpIndex = -1;

                sf.Key = "ctx_track";
                sf.GeoProjection = globalMapping.GeoProjection;

                var shp = new Shape();
                if (shp.Create(ShpfileType.SHP_POLYLINE))
                {
                    foreach (XmlNode node in tracknodes)
                    {
                        var lat = double.Parse(node.SelectSingleNode(".//A[@N='Latitude']").Attributes["V"].Value);
                        var lon = double.Parse(node.SelectSingleNode(".//A[@N='Longitude']").Attributes["V"].Value);
                        shp.AddPoint(lon, lat);
                    }
                }
                shpIndex = sf.EditAddShape(shp);
                handles.Add(shpIndex);
                sf.EditCellValue(sf.FieldIndexByName["User"], shpIndex, ctxfile.User);
                sf.EditCellValue(sf.FieldIndexByName["Gear"], shpIndex, ctxfile.Gear);
                sf.EditCellValue(sf.FieldIndexByName["LandingSite"], shpIndex, ctxfile.LandingSite);
                sf.EditCellValue(sf.FieldIndexByName["Start"], shpIndex, ctxfile.DateStart);
                sf.EditCellValue(sf.FieldIndexByName["Finished"], shpIndex, ctxfile.DateEnd);

                return sf;
            }
            return null;
        }

        public static string GearPathXML { get; private set; }
        public static Shapefile TrackFromTrip(List<Trip> trips, out List<int> handles)
        {
            handles = new List<int>();
            Shapefile sf = null;
            var shpIndex = -1;
            int counter = 0;
            foreach (var trip in trips)
            {
                if (counter == 0)
                {
                    if (TripMappingManager.TrackShapefile == null || TripMappingManager.TrackShapefile.NumFields == 0)
                    {
                        sf = new Shapefile();
                        if (sf.CreateNewWithShapeID("", ShpfileType.SHP_POLYLINE))
                        {
                            sf.EditAddField("GPS", FieldType.STRING_FIELD, 1, 1);
                            sf.EditAddField("Fisher", FieldType.STRING_FIELD, 1, 1);
                            sf.EditAddField("Vessel", FieldType.STRING_FIELD, 1, 1);
                            sf.EditAddField("Gear", FieldType.STRING_FIELD, 1, 1);
                            sf.EditAddField("Departed", FieldType.DATE_FIELD, 1, 1);
                            sf.EditAddField("Arrived", FieldType.DATE_FIELD, 1, 1);
                            sf.EditAddField("Filename", FieldType.STRING_FIELD, 1, 1);
                            sf.EditAddField("Length", FieldType.DOUBLE_FIELD, 1, 1);

                            sf.Key = "trip_track";
                            sf.GeoProjection = globalMapping.GeoProjection;
                            TripMappingManager.TrackShapefile = sf;
                        }
                    }
                    else
                    {
                        sf = TripMappingManager.TrackShapefile;
                        sf.EditClear();
                    }
                }

                var shp = new Shape();
                if (shp.Create(ShpfileType.SHP_POLYLINE))
                {
                    foreach (var wpt in trip.Track.Waypoints)
                    {
                        shp.AddPoint(wpt.Longitude, wpt.Latitude);
                    }
                }
                shpIndex = sf.EditAddShape(shp);
                handles.Add(shpIndex);
                sf.EditCellValue(sf.FieldIndexByName["GPS"], shpIndex, trip.GPS.DeviceName);
                sf.EditCellValue(sf.FieldIndexByName["Fisher"], shpIndex, trip.Operator.Name);
                sf.EditCellValue(sf.FieldIndexByName["Vessel"], shpIndex, trip.VesselName);
                sf.EditCellValue(sf.FieldIndexByName["Departed"], shpIndex, trip.DateTimeDeparture);
                sf.EditCellValue(sf.FieldIndexByName["Arrived"], shpIndex, trip.DateTimeArrival);
                sf.EditCellValue(sf.FieldIndexByName["Gear"], shpIndex, trip.Gear);
                sf.EditCellValue(sf.FieldIndexByName["FileName"], shpIndex, trip.GPXFileName);
                if (trip.Track.Statistics != null)
                {
                    sf.EditCellValue(sf.FieldIndexByName["Length"], shpIndex, trip.Track.Statistics.Length);
                }
                counter++;
            }
            return sf;
        }
        //public static Shapefile TrackFromTrip1( Trip trip, out List<int>handles)
        //{
        //    handles = new List<int>();
        //    var shpIndex = -1;
        //    Shapefile sf;
        //    if(trip.Track.Waypoints.Count>0)
        //    {
        //        if (TripMappingManager.TrackShapefile == null || TripMappingManager.TrackShapefile.NumFields == 0)
        //        {
        //            sf = new Shapefile();
        //            if (sf.CreateNewWithShapeID("", ShpfileType.SHP_POLYLINE))
        //            {
        //                sf.EditAddField("GPS", FieldType.STRING_FIELD, 1, 1);
        //                sf.EditAddField("Filename", FieldType.STRING_FIELD, 1, 1);
        //                sf.EditAddField("Length", FieldType.DOUBLE_FIELD, 1, 1);
        //                sf.Key = "trip_track";
        //                sf.GeoProjection = globalMapping.GeoProjection;
        //                TripMappingManager.TrackShapefile = sf;
        //            }
        //        }
        //        else
        //        {
        //            sf = TripMappingManager.TrackShapefile;
        //        }

        //        var shp = new Shape();
        //        if (shp.Create(ShpfileType.SHP_POLYLINE))
        //        {
        //            foreach (var wpt in trip.Track.Waypoints)
        //            {
        //                shp.AddPoint(wpt.Longitude, wpt.Latitude);
        //            }
        //        }
        //        shpIndex = sf.EditAddShape(shp);
        //        handles.Add(shpIndex);
        //        sf.EditCellValue(sf.FieldIndexByName["GPS"], shpIndex, trip.GPS.DeviceName);
        //        sf.EditCellValue(sf.FieldIndexByName["FileName"], shpIndex, trip.GPXFileName);
        //        sf.EditCellValue(sf.FieldIndexByName["Length"], shpIndex, trip.Track.Statistics.Length);

        //        return sf;
        //    }
        //    else
        //    {
        //        return null;
        //    }
        //}

        public static Shapefile CTXTrackVertices(CTXFile ctxFile, out List<int> shpIndexes, bool extractGearPath = false)
        {
            shpIndexes = new List<int>();
            Shapefile sf;
            if (ctxFile.TrackPtCount != null && ctxFile.TrackPtCount > 0)
            {
                sf = new Shapefile();
                if (sf.CreateNewWithShapeID("", ShpfileType.SHP_POINT))
                {
                    sf.EditAddField("Name", FieldType.INTEGER_FIELD, 1, 1);
                    sf.EditAddField("Time", FieldType.DATE_FIELD, 1, 1);
                    sf.EditAddField("Distance", FieldType.DOUBLE_FIELD, 1, 1);
                    sf.EditAddField("Speed", FieldType.DOUBLE_FIELD, 1, 1);
                    sf.Key = "ctx_track_vertices";
                    sf.GeoProjection = globalMapping.GeoProjection;
                    GPXMappingManager.TrackVerticesShapefile = sf;
                }
            }
            else
            {
                sf = GPXMappingManager.TrackVerticesShapefile;
            }

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(ctxFile.XML);
            var tracknodes = doc.SelectNodes("//T");

            double lat = 0;
            double lon = 0;
            foreach (XmlNode node in tracknodes)
            {
                var shp = new Shape();
                if (shp.Create(ShpfileType.SHP_POINT))
                {
                    lat = double.Parse(node.SelectSingleNode(".//A[@N='Latitude']").Attributes["V"].Value);
                    lon = double.Parse(node.SelectSingleNode(".//A[@N='Longitude']").Attributes["V"].Value);
                    if (shp.AddPoint(lon, lat) >= 0)
                    {
                        var shpIndex = sf.EditAddShape(shp);
                        if (shpIndex >= 0)
                        {
                            sf.EditCellValue(sf.FieldIndexByName["Name"], shpIndex, shpIndex + 1);
                            var wptDate = node.SelectSingleNode(".//A[@N='Date']").Attributes["V"].Value;
                            var wptTime = node.SelectSingleNode(".//A[@N='Time']").Attributes["V"].Value;
                            var wptDateTime = DateTime.Parse(wptDate) + DateTime.Parse(wptTime).TimeOfDay;
                            sf.EditCellValue(sf.FieldIndexByName["Time"], shpIndex, wptDateTime);

                            shpIndexes.Add(shpIndex);

                            if (shpIndex > 0)
                            {
                                var wptNow = new Waypoint { Longitude = lon, Latitude = lat, Elevation = 0, Time = wptDateTime };
                                double elevChange;
                                double distance = Waypoint.ComputeDistance(_wptBefore, wptNow, out elevChange);
                                TimeSpan timeElapsed = wptDateTime - _timeBefore;
                                double speed = distance / timeElapsed.TotalMinutes;
                                sf.EditCellValue(sf.FieldIndexByName["Distance"], shpIndex, distance);
                                sf.EditCellValue(sf.FieldIndexByName["Speed"], shpIndex, speed);
                            }
                            else
                            {
                                sf.EditCellValue(sf.FieldIndexByName["Distance"], shpIndex, 0);
                                sf.EditCellValue(sf.FieldIndexByName["Speed"], shpIndex, 0);
                            }
                            _wptBefore = new Waypoint { Longitude = lon, Latitude = lat, Elevation = 0, Time = wptDateTime };
                            _timeBefore = wptDateTime;
                        }
                    }
                }
            }
            sf.DefaultDrawingOptions.PointShape = tkPointShapeType.ptShapeRegular;
            sf.DefaultDrawingOptions.PointSize = 10;
            sf.DefaultDrawingOptions.PointSidesCount = 4;
            sf.DefaultDrawingOptions.FillColor = _mapWinGISUtils.ColorByName(tkMapColor.Orange);
            sf.DefaultDrawingOptions.LineColor = _mapWinGISUtils.ColorByName(tkMapColor.Black);
            sf.DefaultDrawingOptions.LineWidth = 1.5f;
            return sf;
        }

        public static Shapefile ConvexHull(List<Point> points, out List<int> handles)
        {
            handles = new List<int>();
            var sf = new MapWinGIS.Shapefile();
            int shpIndex;
            if (sf.CreateNewWithShapeID("", ShpfileType.SHP_POLYLINE))
            {
                sf.GeoProjection = globalMapping.GeoProjection;
                sf.Key = "convex_hull";
                var shp = new Shape();
                if (shp.Create(ShpfileType.SHP_POLYLINE))
                {
                    foreach (var pt in points)
                    {
                        shp.AddPoint(pt.x, pt.y);
                    }
                }
                shpIndex = sf.EditAddShape(shp);
                handles.Add(shpIndex);
            }
            return sf;
        }
        public static Shapefile ConvexHull(Shape shp, out List<int> handles)
        {
            handles = new List<int>();
            int shpIndex;
            Shapefile sf = new Shapefile();
            if (sf.CreateNewWithShapeID("", ShpfileType.SHP_POLYGON))
            {
                sf.GeoProjection = globalMapping.GeoProjection;
                sf.Key = "convex_hull";
                shpIndex = sf.EditAddShape(shp.ConvexHull());
                handles.Add(shpIndex);
            }
            return sf;
        }
        public static Shapefile GPXTrackVertices(GPXFile gpxfile, out List<int> shpIndexes, bool extractGearPath = false)
        {
            shpIndexes = new List<int>();
            Shapefile sf;
            if (gpxfile.GPXFileType == GPXFileType.Track && gpxfile.TrackWaypoinsInLocalTime.Count > 0)
            {
                sf = new Shapefile();
                if (sf.CreateNewWithShapeID("", ShpfileType.SHP_POINT))
                {
                    sf.EditAddField("Name", FieldType.INTEGER_FIELD, 1, 1);
                    sf.EditAddField("Time", FieldType.DATE_FIELD, 1, 1);
                    sf.EditAddField("Distance", FieldType.DOUBLE_FIELD, 1, 1);
                    sf.EditAddField("Speed", FieldType.DOUBLE_FIELD, 1, 1);
                    sf.Key = "gpx_track_vertices";
                    sf.GeoProjection = globalMapping.GeoProjection;
                    GPXMappingManager.TrackVerticesShapefile = sf;
                }
            }
            else
            {
                sf = GPXMappingManager.TrackVerticesShapefile;
            }

            foreach (var wlt in gpxfile.TrackWaypoinsInLocalTime)
            {
                var shp = new Shape();
                if (shp.Create(ShpfileType.SHP_POINT))
                {
                    if (shp.AddPoint(wlt.Longitude, wlt.Latitude) >= 0)
                    {
                        var shpIndex = sf.EditAddShape(shp);
                        if (shpIndex >= 0)
                        {
                            sf.EditCellValue(sf.FieldIndexByName["Name"], shpIndex, shpIndex + 1);
                            sf.EditCellValue(sf.FieldIndexByName["Time"], shpIndex, wlt.Time);
                            shpIndexes.Add(shpIndex);

                            if (shpIndex > 0)
                            {
                                var wptNow = new Waypoint { Longitude = wlt.Longitude, Latitude = wlt.Latitude, Elevation = 0, Time = wlt.Time };
                                double elevChange;
                                double distance = Waypoint.ComputeDistance(_wptBefore, wptNow, out elevChange);
                                TimeSpan timeElapsed = wlt.Time - _timeBefore;
                                double speed = distance / timeElapsed.TotalMinutes;
                                sf.EditCellValue(sf.FieldIndexByName["Distance"], shpIndex, distance);
                                sf.EditCellValue(sf.FieldIndexByName["Speed"], shpIndex, speed);
                                if (extractGearPath)
                                {

                                }
                            }
                            else
                            {
                                sf.EditCellValue(sf.FieldIndexByName["Distance"], shpIndex, 0);
                                sf.EditCellValue(sf.FieldIndexByName["Speed"], shpIndex, 0);
                            }
                            _wptBefore = new Waypoint { Longitude = wlt.Longitude, Latitude = wlt.Latitude, Elevation = 0, Time = wlt.Time };
                            _timeBefore = wlt.Time;
                        }
                    }
                }
            }
            sf.DefaultDrawingOptions.PointShape = tkPointShapeType.ptShapeRegular;
            sf.DefaultDrawingOptions.PointSize = 10;
            sf.DefaultDrawingOptions.PointSidesCount = 4;
            sf.DefaultDrawingOptions.FillColor = _mapWinGISUtils.ColorByName(tkMapColor.Orange);
            sf.DefaultDrawingOptions.LineColor = _mapWinGISUtils.ColorByName(tkMapColor.Black);
            sf.DefaultDrawingOptions.LineWidth = 1.5f;
            return sf;
        }
        public static Shapefile TrackFromGPX(GPXFile gpxFile, out List<int> handles)
        {
            handles = new List<int>();
            var shpIndex = -1;
            Shapefile sf;
            if (gpxFile.TrackWaypoinsInLocalTime.Count > 0)
            {
                if (GPXMappingManager.TrackShapefile == null || GPXMappingManager.TrackShapefile.NumFields == 0)
                {
                    sf = new Shapefile();
                    if (sf.CreateNewWithShapeID("", ShpfileType.SHP_POLYLINE))
                    {
                        sf.EditAddField("GPS", FieldType.STRING_FIELD, 1, 1);
                        sf.EditAddField("Filename", FieldType.STRING_FIELD, 1, 1);
                        sf.EditAddField("Length", FieldType.DOUBLE_FIELD, 1, 1);
                        sf.EditAddField("DateStart", FieldType.DATE_FIELD, 1, 1);
                        sf.EditAddField("DateEnd", FieldType.DATE_FIELD, 1, 1);
                        sf.Key = "gpxfile_track";
                        sf.GeoProjection = GPXManager.entities.mapping.globalMapping.GeoProjection;
                        GPXMappingManager.TrackShapefile = sf;
                    }
                }
                else
                {
                    sf = GPXMappingManager.TrackShapefile;
                }

                var shp = new Shape();
                if (shp.Create(ShpfileType.SHP_POLYLINE))
                {
                    foreach (var wlt in gpxFile.TrackWaypoinsInLocalTime)
                    {
                        shp.AddPoint(wlt.Longitude, wlt.Latitude);
                    }
                }
                shpIndex = sf.EditAddShape(shp);
                handles.Add(shpIndex);
                sf.EditCellValue(sf.FieldIndexByName["GPS"], shpIndex, gpxFile.GPS.DeviceName);
                sf.EditCellValue(sf.FieldIndexByName["FileName"], shpIndex, gpxFile.FileName);
                sf.EditCellValue(sf.FieldIndexByName["Length"], shpIndex, gpxFile.TrackLength);
                sf.EditCellValue(sf.FieldIndexByName["DateStart"], shpIndex, gpxFile.DateRangeStart);
                sf.EditCellValue(sf.FieldIndexByName["DateEnd"], shpIndex, gpxFile.DateRangeEnd);

                return sf;
            }
            else
            {
                return null;
            }
        }

        public static Shapefile WaypointsFromCTX(CTXFileSummaryView ctxfile, out List<int> shpIndexes)
        {
            shpIndexes = new List<int>();
            Shapefile sf;
            if (ctxfile.WaypointsForSet != null && ((int)ctxfile.WaypointsForSet) > 0 ||
                ctxfile.WaypointsForHaul != null && ((int)ctxfile.WaypointsForHaul) > 0)
            {
                if (GPXMappingManager.WaypointsShapefile == null || GPXMappingManager.WaypointsShapefile.NumFields == 0)
                {
                    sf = new Shapefile();

                    if (sf.CreateNewWithShapeID("", ShpfileType.SHP_POINT))
                    {
                        sf.GeoProjection = globalMapping.GeoProjection;
                        sf.Key = "ctxfile_waypoint";
                        sf.EditAddField("Name", FieldType.STRING_FIELD, 1, 1);
                        sf.EditAddField("TimeStamp", FieldType.DATE_FIELD, 1, 1);
                        sf.EditAddField("User", FieldType.STRING_FIELD, 1, 1);
                        sf.EditAddField("Type", FieldType.STRING_FIELD, 1, 1);
                        GPXMappingManager.WaypointsShapefile = sf;
                    }

                }
                else
                {
                    sf = GPXMappingManager.WaypointsShapefile;
                }

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(ctxfile.XML);
                var wptNodes = doc.SelectNodes("//A[@N='Waypoint name']");
                if (_ctxDictionary.Count == 0)
                {
                    FillCTXDictionary(ctxfile.XML);
                }



                foreach (XmlNode nd in wptNodes)
                {
                    var shp = new Shape();
                    if (shp.Create(ShpfileType.SHP_POINT))
                    {
                        string wptType = "";
                        double lat = double.Parse(nd.ParentNode.SelectSingleNode(".//A[@N='Latitude']").Attributes["V"].Value);
                        double lon = double.Parse(nd.ParentNode.SelectSingleNode(".//A[@N='Longitude']").Attributes["V"].Value);
                        var pt_type = nd.ParentNode.SelectSingleNode(".//A[@N='WaypointType']").Attributes["V"].Value;
                        if (pt_type == setWaypointKey)
                        {
                            wptType = "Setting";
                        }
                        else
                        {
                            wptType = "Hauling";
                        }
                        if (shp.AddPoint(lon, lat) >= 0)
                        {
                            var shpIndex = sf.EditAddShape(shp);
                            if (shpIndex >= 0)
                            {
                                sf.EditCellValue(sf.FieldIndexByName["Name"], shpIndex, nd.ParentNode.SelectSingleNode(".//A[@N='Waypoint name']").Attributes["V"].Value);
                                var wptDate = nd.ParentNode.SelectSingleNode(".//A[@N='Date']").Attributes["V"].Value;
                                var wptTime = nd.ParentNode.SelectSingleNode(".//A[@N='Time']").Attributes["V"].Value;
                                sf.EditCellValue(sf.FieldIndexByName["TimeStamp"], shpIndex, DateTime.Parse(wptDate) + DateTime.Parse(wptTime).TimeOfDay);
                                sf.EditCellValue(sf.FieldIndexByName["User"], shpIndex, ctxfile.User);
                                sf.EditCellValue(sf.FieldIndexByName["Type"], shpIndex, wptType);
                                shpIndexes.Add(shpIndex);
                            }
                        }
                    }
                }

                sf.DefaultDrawingOptions.PointShape = tkPointShapeType.ptShapeCircle;
                sf.DefaultDrawingOptions.PointSize = 12;
                sf.DefaultDrawingOptions.FillColor = _mapWinGISUtils.ColorByName(tkMapColor.Red);
                return sf;


            }
            return null;

        }
        public static Shapefile NamedPointsFromGPX(GPXFile gpxFile, out List<int> shpIndexes)
        {
            shpIndexes = new List<int>();
            Shapefile sf;
            if (gpxFile.NamedWaypointsInLocalTime.Count > 0)
            {
                if (GPXMappingManager.WaypointsShapefile == null || GPXMappingManager.WaypointsShapefile.NumFields == 0)
                {
                    sf = new Shapefile();

                    if (sf.CreateNewWithShapeID("", ShpfileType.SHP_POINT))
                    {
                        sf.GeoProjection = globalMapping.GeoProjection;
                        sf.Key = "gpxfile_waypoint";
                        sf.EditAddField("Name", FieldType.STRING_FIELD, 1, 1);
                        sf.EditAddField("TimeStamp", FieldType.DATE_FIELD, 1, 1);
                        sf.EditAddField("GPS", FieldType.STRING_FIELD, 1, 1);
                        sf.EditAddField("Filename", FieldType.STRING_FIELD, 1, 1);
                        sf.Key = "named_points_from_gpx";
                        GPXMappingManager.WaypointsShapefile = sf;
                    }

                }
                else
                {
                    sf = GPXMappingManager.WaypointsShapefile;
                }

                foreach (var wlt in gpxFile.NamedWaypointsInLocalTime)
                {
                    var shp = new Shape();
                    if (shp.Create(ShpfileType.SHP_POINT))
                    {
                        if (shp.AddPoint(wlt.Longitude, wlt.Latitude) >= 0)
                        {
                            var shpIndex = sf.EditAddShape(shp);
                            if (shpIndex >= 0)
                            {
                                sf.EditCellValue(sf.FieldIndexByName["Name"], shpIndex, wlt.Name);
                                sf.EditCellValue(sf.FieldIndexByName["TimeStamp"], shpIndex, wlt.Time);
                                sf.EditCellValue(sf.FieldIndexByName["GPS"], shpIndex, gpxFile.GPS.DeviceName);
                                sf.EditCellValue(sf.FieldIndexByName["Filename"], shpIndex, gpxFile.FileName);
                                shpIndexes.Add(shpIndex);
                            }
                        }
                    }
                }

                sf.DefaultDrawingOptions.PointShape = tkPointShapeType.ptShapeCircle;
                sf.DefaultDrawingOptions.PointSize = 12;
                sf.DefaultDrawingOptions.FillColor = _mapWinGISUtils.ColorByName(tkMapColor.Red);
                return sf;
            }
            else
            {
                return null;
            }
        }

        public static Shapefile TrackFromWaypoints()
        {
            if (WaypointsinLocalTine.Count > 0)
            {
                Shapefile shpFile = new Shapefile();
                if (shpFile.CreateNewWithShapeID("", ShpfileType.SHP_POLYLINE))
                {
                    shpFile.GeoProjection = globalMapping.GeoProjection;
                    var shp = new Shape();
                    if (shp.Create(ShpfileType.SHP_POLYLINE))
                    {
                        foreach (var wlt in WaypointsinLocalTine)
                        {
                            var shpIndex = shp.AddPoint(wlt.Longitude, wlt.Latitude);
                        }
                    }
                    shpFile.EditAddShape(shp);
                }

                return shpFile;
            }
            else
            {
                throw new ArgumentException("Waypoint source cannot be null or cannot have zero elements");
            }
        }
    }
}