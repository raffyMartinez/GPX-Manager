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
        private static List<ExtractedFishingTrack> _gearHaulExtractedTracks;
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
                    sf.Key = $"{aoi.Name}_aoi_boundary";
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

        public static int MaxSelfCrossings { get; set; }

        public static Shape BSCBoundaryLine { get; set; }

        public static FishingTripAndGearRetrievalTracks GearRetrievalTrackShapeFromGPX(GPXFile gpxFile, int? interval)
        {
            ExtractFishingTrackLine = true;
            List<DetectedTrack> detectedTracks = new List<DetectedTrack>();

            if (gpxFile.TrackWaypoinsInLocalTime.Count < 2)
            {
                return null;
            }
            else
            {
                DateTime wptDateTime = DateTime.Now;
                Waypoint pt1 = null;
                Waypoint pt2 = null;
                _candidateTrack = new Shape();
                _eft = new ExtractedFishingTrack();
                _trackingInterval = interval;
                _detectedTracks = new List<DetectedTrack>();
                int counter = 0;
                foreach (var wlt in gpxFile.TrackWaypoinsInLocalTime)
                {
                    var lat = wlt.Latitude;
                    var lon = wlt.Longitude;
                    wptDateTime = wlt.Time;

                    if (counter > 0)
                    {
                        pt2 = new Waypoint { Longitude = lon, Latitude = lat, Time = wptDateTime };
                        detectedTracks = MakeSegments(counter, pt1, pt2, counter >= gpxFile.TrackWaypoinsInLocalTime.Count - 1);
                    }
                    pt1 = new Waypoint { Longitude = lon, Latitude = lat, Time = wptDateTime };
                    counter++;
                }
                return new FishingTripAndGearRetrievalTracks { TripShapefile = null, GearRetrievalTracks = detectedTracks };
            }
        }
        /// <summary>
        /// extracts the segment where gear retrieval is taking place and creates a shape from the segment
        /// </summary>
        /// <param name="tracknodes"></param>
        /// <param name="interval"></param>
        /// <returns></returns>
        public static FishingTripAndGearRetrievalTracks GearRetrievalTrackShapeFromCTX(XmlNodeList tracknodes, int? interval)
        {
            ExtractFishingTrackLine = true;
            List<DetectedTrack> detectedTracks = new List<DetectedTrack>();


            if (tracknodes.Count < 2)
            {
                return null;
            }
            else
            {

                //Shapefile sf = new Shapefile();
                DateTime wptDateTime = DateTime.Now;
                Waypoint pt1 = null;
                Waypoint pt2 = null;
                _candidateTrack = new Shape();
                _eft = new ExtractedFishingTrack();
                _trackingInterval = interval;
                _detectedTracks = new List<DetectedTrack>();
                int counter = 0;
                foreach (XmlNode node in tracknodes)
                {
                    var lat = double.Parse(node.SelectSingleNode(".//A[@N='Latitude']").Attributes["V"].Value);
                    var lon = double.Parse(node.SelectSingleNode(".//A[@N='Longitude']").Attributes["V"].Value);
                    var wptDate = node.SelectSingleNode(".//A[@N='Date']").Attributes["V"].Value;
                    var wptTime = node.SelectSingleNode(".//A[@N='Time']").Attributes["V"].Value;
                    wptDateTime = DateTime.Parse(wptDate) + DateTime.Parse(wptTime).TimeOfDay;

                    if (counter > 0)
                    {
                        pt2 = new Waypoint { Longitude = lon, Latitude = lat, Time = wptDateTime };
                        detectedTracks = MakeSegments(counter, pt1, pt2, counter >= tracknodes.Count - 1);
                    }
                    pt1 = new Waypoint { Longitude = lon, Latitude = lat, Time = wptDateTime };
                    counter++;
                }

                return new FishingTripAndGearRetrievalTracks { TripShapefile = null, GearRetrievalTracks = detectedTracks };
            }

        }

        public static FishingTripAndGearRetrievalTracks CreateTripAndHaulsFromGPX(GPXFile gpxFile)
        {
            ExtractFishingTrackLine = true;
            List<DetectedTrack> detectedTracks = new List<DetectedTrack>();
            if (gpxFile.TrackWaypoinsInLocalTime.Count == 0)
            {
                return null;
            }
            else
            {
                if (gpxFile.TrackWaypoinsInLocalTime.Count < 2)
                {
                    return null;
                }
                else
                {
                    Shapefile sf = new Shapefile();
                    DateTime wptDateTime = DateTime.Now;
                    Waypoint pt1 = null;
                    Waypoint pt2 = null;
                    _candidateTrack = new Shape();
                    _eft = new ExtractedFishingTrack();
                    if (gpxFile.GPSTimerInterval == null)
                    {
                        //ctxfile.CTXFile.TrackingInterval = Entities.CTXFileViewModel.GetGPSTimerIntervalFromCTX(ctxfile.CTXFile, true);
                    }
                    _trackingInterval = gpxFile.GPSTimerInterval;
                    _detectedTracks = new List<DetectedTrack>();
                    if (sf.CreateNewWithShapeID("", ShpfileType.SHP_POLYLINE))
                    {
                        sf.EditAddField("User", FieldType.STRING_FIELD, 1, 1);
                        sf.EditAddField("Start", FieldType.STRING_FIELD, 1, 1);
                        sf.EditAddField("Finished", FieldType.STRING_FIELD, 1, 1);
                        sf.EditAddField("Interval", FieldType.INTEGER_FIELD, 1, 1);
                        sf.Key = "gpxfile_track";
                        sf.GeoProjection = globalMapping.GeoProjection;

                        Shape shp = new Shape();
                        if (shp.Create(ShpfileType.SHP_POLYLINE))
                        {
                            int counter = 0;
                            foreach (var wlt in gpxFile.TrackWaypoinsInLocalTime)
                            {
                                var lat = wlt.Latitude;
                                var lon = wlt.Longitude;
                                wptDateTime = wlt.Time;
                                shp.AddPoint(lon, lat);
                                if (counter > 0)
                                {
                                    pt2 = new Waypoint { Longitude = lon, Latitude = lat, Time = wptDateTime };
                                    detectedTracks = MakeSegments(counter, pt1, pt2, counter >= gpxFile.TrackWaypoinsInLocalTime.Count - 1);
                                }
                                pt1 = new Waypoint { Longitude = lon, Latitude = lat, Time = wptDateTime };
                                counter++;
                            }
                            var shpIndex = sf.EditAddShape(shp);
                            sf.EditCellValue(sf.FieldIndexByName["User"], shpIndex, gpxFile.GPS.DeviceName);
                            sf.EditCellValue(sf.FieldIndexByName["Start"], shpIndex, gpxFile.DateRangeStart);
                            sf.EditCellValue(sf.FieldIndexByName["Finished"], shpIndex, gpxFile.DateRangeEnd);
                            sf.EditCellValue(sf.FieldIndexByName["Interval"], shpIndex, _trackingInterval);

                            var th = new FishingTripAndGearRetrievalTracks { TripShapefile = sf, Handle = shpIndex };
                            th.TripShapefile = sf;
                            th.GearRetrievalTracks = detectedTracks;

                            return th;
                        }
                    }
                }
            }
            return null;
        }




        /// <summary>
        /// creates a shapefile of the entire CTX data and extracts the tracks of gear retrieval
        /// </summary>
        /// <param name="ctxfile"></param>
        /// <returns></returns>
        public static FishingTripAndGearRetrievalTracks CreateTripAndHaulsFromCTX(CTXFileSummaryView ctxfile)
        {
            ExtractFishingTrackLine = true;
            List<DetectedTrack> detectedTracks = new List<DetectedTrack>();
            if (ctxfile.XML.Length == 0)
            {
                return null;
            }
            else
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(ctxfile.XML);
                var tracknodes = doc.SelectNodes("//T");
                if (tracknodes.Count < 2)
                {
                    return null;
                }
                else
                {

                    Shapefile sf = new Shapefile();
                    DateTime wptDateTime = DateTime.Now;
                    Waypoint pt1 = null;
                    Waypoint pt2 = null;
                    _candidateTrack = new Shape();
                    _eft = new ExtractedFishingTrack();
                    //int? interval = null;
                    if (ctxfile.CTXFile.TrackingInterval == null)
                    {
                        ctxfile.CTXFile.TrackingInterval = Entities.CTXFileViewModel.GetGPSTimerIntervalFromCTX(ctxfile.CTXFile, true);
                    }
                    _trackingInterval = ctxfile.CTXFile.TrackingInterval;
                    _detectedTracks = new List<DetectedTrack>();

                    if (sf.CreateNewWithShapeID("", ShpfileType.SHP_POLYLINE))
                    {
                        sf.EditAddField("User", FieldType.STRING_FIELD, 1, 1);
                        sf.EditAddField("Gear", FieldType.STRING_FIELD, 1, 1);
                        sf.EditAddField("LandingSite", FieldType.STRING_FIELD, 1, 1);
                        sf.EditAddField("Start", FieldType.STRING_FIELD, 1, 1);
                        sf.EditAddField("Finished", FieldType.STRING_FIELD, 1, 1);
                        sf.EditAddField("Interval", FieldType.INTEGER_FIELD, 1, 1);
                        sf.Key = "ctx_track";
                        sf.GeoProjection = globalMapping.GeoProjection;

                        Shape shp = new Shape();
                        if (shp.Create(ShpfileType.SHP_POLYLINE))
                        {
                            int counter = 0;
                            foreach (XmlNode node in tracknodes)
                            {
                                var lat = double.Parse(node.SelectSingleNode(".//A[@N='Latitude']").Attributes["V"].Value);
                                var lon = double.Parse(node.SelectSingleNode(".//A[@N='Longitude']").Attributes["V"].Value);
                                var wptDate = node.SelectSingleNode(".//A[@N='Date']").Attributes["V"].Value;
                                var wptTime = node.SelectSingleNode(".//A[@N='Time']").Attributes["V"].Value;
                                wptDateTime = DateTime.Parse(wptDate) + DateTime.Parse(wptTime).TimeOfDay;
                                shp.AddPoint(lon, lat);
                                if (counter > 0)
                                {
                                    pt2 = new Waypoint { Longitude = lon, Latitude = lat, Time = wptDateTime };
                                    if (ExtractFishingTrackLine)
                                    {
                                        detectedTracks = MakeSegments(counter, pt1, pt2, counter >= tracknodes.Count - 1);
                                    }
                                }
                                pt1 = new Waypoint { Longitude = lon, Latitude = lat, Time = wptDateTime };
                                counter++;
                            }
                            var shpIndex = sf.EditAddShape(shp);
                            sf.EditCellValue(sf.FieldIndexByName["User"], shpIndex, ctxfile.User);
                            sf.EditCellValue(sf.FieldIndexByName["Gear"], shpIndex, ctxfile.Gear);
                            sf.EditCellValue(sf.FieldIndexByName["LandingSite"], shpIndex, ctxfile.LandingSite);
                            sf.EditCellValue(sf.FieldIndexByName["Start"], shpIndex, ctxfile.DateStart);
                            sf.EditCellValue(sf.FieldIndexByName["Finished"], shpIndex, ctxfile.DateEnd);
                            sf.EditCellValue(sf.FieldIndexByName["Interval"], shpIndex, _trackingInterval);

                            var th = new FishingTripAndGearRetrievalTracks { TripShapefile = sf, Handle = shpIndex };
                            th.TripShapefile = sf;
                            th.GearRetrievalTracks = detectedTracks;
                            return th;
                        }
                    }
                }
            }
            return null;
        }

        private static int? _trackingInterval;
        private static ExtractedFishingTrack _eft;
        private static Shape _candidateTrack;
        private static double _accumulatedDistance;
        private static List<DetectedTrack> _detectedTracks;

        private static string SerializeShape2UTM(Shape shp)
        {
            if (shp.numPoints > 1)
            {
                string convertedString = "3;0;";
                LatLonUTMConverter converter = new LatLonUTMConverter("WGS 84");
                for (int x = 0; x < shp.numPoints; x++)
                {
                    var converted = converter.convertLatLngToUtm(shp.Point[x].y, shp.Point[x].x);
                    convertedString += $"{ converted.Easting}|{converted.Northing}|";
                }
                return convertedString;
            }

            return "";

        }
        private static List<DetectedTrack> MakeSegments(int counter, Waypoint pt1, Waypoint pt2 = null, bool? done = null)
        {
            double elevChange;
            double distance = Waypoint.ComputeDistance(pt1, pt2, out elevChange);
            TimeSpan timeElapsed = pt2.Time - pt1.Time;
            double speed = distance / timeElapsed.TotalMinutes;

            bool intervalIsOk = _trackingInterval == null || _trackingInterval == 0;
            if (!intervalIsOk)
            {
                intervalIsOk = timeElapsed.TotalSeconds < (int)_trackingInterval * 2;
            }
            if (pt1.Time < pt2.Time)
            {
                if (intervalIsOk && !(bool)done && speed < Global.Settings.SpeedThresholdForRetrieving)
                {
                    if (_candidateTrack == null)
                    {
                        _candidateTrack = new Shape();
                        _candidateTrack.Create(ShpfileType.SHP_POLYLINE);
                    }

                    if (_candidateTrack.numPoints == 0)
                    {
                        _candidateTrack.AddPoint(pt1.Longitude, pt1.Latitude);
                    }
                    _candidateTrack.AddPoint(pt2.Longitude, pt2.Latitude);
                    if (_eft.SpeedAtWaypoints.Count == 0)
                    {
                        _eft.Start = pt1.Time;
                    }
                    _eft.SpeedAtWaypoints.Add(speed);
                    _accumulatedDistance += distance;
                }
                else
                {
                    if (_candidateTrack != null)
                    {
                        if (intervalIsOk && (bool)done)
                        {
                            _candidateTrack.AddPoint(pt2.Longitude, pt2.Latitude);
                            _eft.SpeedAtWaypoints.Add(speed);
                            _accumulatedDistance += distance;
                        }

                        if (_candidateTrack.numPoints >= 6 && PolylineSelfCrossingsCount(_candidateTrack, 10, true) < 10)
                        {
                            if (BSCBoundaryLine == null || !_candidateTrack.Crosses(BSCBoundaryLine))
                            {
                                _eft.TrackPointCountOriginal = _candidateTrack.numPoints;
                                _eft.TrackOriginal = _candidateTrack;
                                _eft.SegmentSimplified = DouglasPeucker.DouglasPeucker.DouglasPeuckerReduction(_candidateTrack, 20);
                                _eft.Segment = _candidateTrack;
                                _eft.TrackPointCountSimplified = _eft.SegmentSimplified.numPoints;
                                _eft.LengthOriginal = _accumulatedDistance;
                                _eft.LengthSimplified = (double)GetPolyLineShapeLength(_eft.SegmentSimplified);
                                _eft.End = pt2.Time;
                                _eft.AverageSpeed = _eft.SpeedAtWaypoints.Average();
                                _eft.FromDatabase = false;
                                _eft.SerializedTrackUTM = SerializeShape2UTM(_eft.SegmentSimplified);
                                _detectedTracks.Add(new DetectedTrack { ExtractedFishingTrack = _eft, Accept = false, Length = _eft.LengthOriginal });
                            }
                        }
                    }
                    _accumulatedDistance = 0;
                    _candidateTrack = null;
                    _eft = new ExtractedFishingTrack();
                }
            }

            if ((bool)done)
            {
                if (_detectedTracks != null && _detectedTracks.Count > 0)
                {
                    counter = 0;
                    DetectedTrack firstSegment = null;
                    foreach (var tr in _detectedTracks)
                    {
                        if (tr.ExtractedFishingTrack.LengthOriginal > Global.Settings.GearRetrievingMinLength &&
                            tr.ExtractedFishingTrack.TrackPointCountOriginal > 6 &&
                            tr.ExtractedFishingTrack.LengthOriginal < 3500)
                        {
                            tr.Accept = true;
                        }
                        if (counter > 0)
                        {
                            Point endpt1 = firstSegment.ExtractedFishingTrack.Segment.Point[firstSegment.ExtractedFishingTrack.Segment.numPoints - 1];
                            Point endpt2 = tr.ExtractedFishingTrack.Segment.Point[0];
                            elevChange = 0;
                            distance = Waypoint.ComputeDistance(
                                new Waypoint { Latitude = endpt1.y, Longitude = endpt1.x },
                                new Waypoint { Latitude = endpt2.y, Longitude = endpt2.x },
                                out elevChange);

                            if (distance < 50 &&
                                (firstSegment.ExtractedFishingTrack.LengthOriginal +
                                tr.ExtractedFishingTrack.LengthOriginal) > Global.Settings.GearRetrievingMinLength)
                            {
                                firstSegment.Accept = true;
                                tr.Accept = true;
                            }
                        }
                        firstSegment = tr;
                        counter++;
                    }
                }

                return _detectedTracks;
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
        public static bool ExtractFishingTrackLine { get; set; }
        public static Shapefile FishingTrackLine(FishingTripAndGearRetrievalTracks th)
        {
            if (ExtractFishingTrackLine && th.GearRetrievalTracks.Count > 0)
            {
                Shapefile sf = new Shapefile();
                if (sf.CreateNewWithShapeID("", ShpfileType.SHP_POLYLINE))
                {
                    sf.EditAddField("Length", FieldType.DOUBLE_FIELD, 1, 1);
                    sf.EditAddField("DateStart", FieldType.DATE_FIELD, 1, 1);
                    sf.EditAddField("DateEnd", FieldType.DATE_FIELD, 1, 1);
                    sf.EditAddField("Duration", FieldType.STRING_FIELD, 30, 1);
                    sf.EditAddField("AvgSpeed", FieldType.DOUBLE_FIELD, 1, 1);
                    sf.EditAddField("TrackPts", FieldType.INTEGER_FIELD, 1, 1);
                    sf.EditAddField("TrackPtsSimplified", FieldType.INTEGER_FIELD, 1, 1);
                    sf.GeoProjection = globalMapping.GeoProjection;
                    sf.Key = "fishing_trackline";

                    foreach (var item in th.GearRetrievalTracks)
                    {
                        if (item.Accept)
                        {
                            int idx = sf.EditAddShape(item.ExtractedFishingTrack.SegmentSimplified);
                            sf.EditCellValue(sf.FieldIndexByName["Length"], idx, item.ExtractedFishingTrack.LengthOriginal);
                            sf.EditCellValue(sf.FieldIndexByName["DateStart"], idx, item.ExtractedFishingTrack.Start);
                            sf.EditCellValue(sf.FieldIndexByName["DateEnd"], idx, item.ExtractedFishingTrack.End);
                            sf.EditCellValue(sf.FieldIndexByName["Duration"], idx, item.ExtractedFishingTrack.Duration);
                            sf.EditCellValue(sf.FieldIndexByName["AvgSpeed"], idx, item.ExtractedFishingTrack.AverageSpeed);
                            sf.EditCellValue(sf.FieldIndexByName["TrackPts"], idx, item.ExtractedFishingTrack.TrackPointCountOriginal);
                            sf.EditCellValue(sf.FieldIndexByName["TrackPtsSimplified"], idx, item.ExtractedFishingTrack.TrackPointCountSimplified);
                        }
                    }
                }

                sf.SelectionDrawingOptions.FillTransparency = 1f;
                sf.SelectionDrawingOptions.FillVisible = false;
                sf.SelectionDrawingOptions.FillBgTransparent = true;
                sf.DefaultDrawingOptions.LineColor = _mapWinGISUtils.ColorByName(tkMapColor.Orange);
                sf.DefaultDrawingOptions.LineWidth = 3f;

                return sf;
            }
            return null;
        }

        private static Shapefile _fishingTrackLines;
        public static Shapefile FishingTrackLines()
        {
            if (Entities.ExtractedFishingTrackViewModel.Count() > 0)
            {
                if (_fishingTrackLines == null)
                {
                    _fishingTrackLines = new Shapefile();
                    if (_fishingTrackLines.CreateNewWithShapeID("", ShpfileType.SHP_POLYLINE))
                    {
                        _fishingTrackLines.GeoProjection = globalMapping.GeoProjection;
                        _fishingTrackLines.Key = "extracted_tracks";
                        _fishingTrackLines.EditAddField("ID", FieldType.INTEGER_FIELD, 1, 1);
                        _fishingTrackLines.EditAddField("DeviceName", FieldType.STRING_FIELD, 1, 1);
                        _fishingTrackLines.EditAddField("DateAdded", FieldType.DATE_FIELD, 1, 1);
                        _fishingTrackLines.EditAddField("SourceType", FieldType.STRING_FIELD, 1, 1);
                        _fishingTrackLines.EditAddField("SourceID", FieldType.INTEGER_FIELD, 1, 1);
                        _fishingTrackLines.EditAddField("Start", FieldType.DATE_FIELD, 1, 1);
                        _fishingTrackLines.EditAddField("End", FieldType.DATE_FIELD, 1, 1);
                        _fishingTrackLines.EditAddField("LenOriginal", FieldType.DOUBLE_FIELD, 1, 1);
                        _fishingTrackLines.EditAddField("LenSimplified", FieldType.DOUBLE_FIELD, 1, 1);
                        _fishingTrackLines.EditAddField("TrckPtsOriginal", FieldType.INTEGER_FIELD, 1, 1);
                        _fishingTrackLines.EditAddField("TrckPtsSimplified", FieldType.INTEGER_FIELD, 1, 1);
                        _fishingTrackLines.EditAddField("AvgSpeed", FieldType.DOUBLE_FIELD, 1, 1);
                        foreach (var item in Entities.ExtractedFishingTrackViewModel.GetAll())
                        {
                            var shp = new Shape();
                            if (item.SerializedTrack.Length > 0 && shp.Create(ShpfileType.SHP_POLYLINE))
                            {
                                shp.CreateFromString(item.SerializedTrack);
                            }
                            var idx = _fishingTrackLines.EditAddShape(shp);
                            if (idx >= 0)
                            {
                                _fishingTrackLines.EditCellValue(_fishingTrackLines.FieldIndexByName["ID"], idx, item.ID);
                                _fishingTrackLines.EditCellValue(_fishingTrackLines.FieldIndexByName["DeviceName"], idx, item.DeviceName);
                                _fishingTrackLines.EditCellValue(_fishingTrackLines.FieldIndexByName["DateAdded"], idx, item.DateAdded);
                                _fishingTrackLines.EditCellValue(_fishingTrackLines.FieldIndexByName["SourceType"], idx, item.TrackSourceTypeToString);
                                _fishingTrackLines.EditCellValue(_fishingTrackLines.FieldIndexByName["SourceID"], idx, item.TrackSourceID);
                                _fishingTrackLines.EditCellValue(_fishingTrackLines.FieldIndexByName["Start"], idx, item.Start);
                                _fishingTrackLines.EditCellValue(_fishingTrackLines.FieldIndexByName["End"], idx, item.End);
                                _fishingTrackLines.EditCellValue(_fishingTrackLines.FieldIndexByName["LenOriginal"], idx, item.LengthOriginal);
                                _fishingTrackLines.EditCellValue(_fishingTrackLines.FieldIndexByName["LenSimplified"], idx, item.LengthSimplified);
                                _fishingTrackLines.EditCellValue(_fishingTrackLines.FieldIndexByName["TrckPtsOriginal"], idx, item.TrackPointCountOriginal);
                                _fishingTrackLines.EditCellValue(_fishingTrackLines.FieldIndexByName["TrckPtsSimplified"], idx, item.TrackPointCountSimplified);
                                _fishingTrackLines.EditCellValue(_fishingTrackLines.FieldIndexByName["AvgSpeed"], idx, item.AverageSpeed);
                            }
                        }
                    }
                }
                return _fishingTrackLines;
            }
            else
            {
                return null;
            }

        }
        public static Shapefile FishingTrackLine()
        {
            if (ExtractFishingTrackLine && _gearHaulExtractedTracks.Count > 0)
            {
                Shapefile sf = new Shapefile();
                if (sf.CreateNewWithShapeID("", ShpfileType.SHP_POLYLINE))
                {
                    sf.EditAddField("Length", FieldType.DOUBLE_FIELD, 1, 1);
                    sf.EditAddField("DateStart", FieldType.DATE_FIELD, 1, 1);
                    sf.EditAddField("DateEnd", FieldType.DATE_FIELD, 1, 1);
                    sf.EditAddField("Duration", FieldType.STRING_FIELD, 30, 1);
                    sf.EditAddField("AvgSpeed", FieldType.DOUBLE_FIELD, 1, 1);
                    sf.EditAddField("TrackPts", FieldType.INTEGER_FIELD, 1, 1);
                    sf.EditAddField("TrackPtsSimplified", FieldType.INTEGER_FIELD, 1, 1);
                    sf.GeoProjection = globalMapping.GeoProjection;
                    sf.Key = "fishing_trackline";

                    foreach (var item in _gearHaulExtractedTracks)
                    {
                        int idx = sf.EditAddShape(item.SegmentSimplified);
                        sf.EditCellValue(sf.FieldIndexByName["Length"], idx, item.LengthOriginal);
                        sf.EditCellValue(sf.FieldIndexByName["DateStart"], idx, item.Start);
                        sf.EditCellValue(sf.FieldIndexByName["DateEnd"], idx, item.End);
                        sf.EditCellValue(sf.FieldIndexByName["Duration"], idx, item.Duration);
                        sf.EditCellValue(sf.FieldIndexByName["AvgSpeed"], idx, item.AverageSpeed);
                        sf.EditCellValue(sf.FieldIndexByName["TrackPts"], idx, item.TrackPointCountOriginal);
                        sf.EditCellValue(sf.FieldIndexByName["TrackPtsSimplified"], idx, item.TrackPointCountSimplified);
                    }
                }
                sf.SelectionDrawingOptions.FillTransparency = 1f;
                sf.SelectionDrawingOptions.FillVisible = false;
                sf.SelectionDrawingOptions.FillBgTransparent = true;
                sf.DefaultDrawingOptions.LineColor = _mapWinGISUtils.ColorByName(tkMapColor.Orange);
                sf.DefaultDrawingOptions.LineWidth = 3f;

                return sf;
            }
            return null;
        }
        public static Shapefile ConvexHull(List<MapWinGIS.Point> points, out List<int> handles)
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
        public static Shapefile GPXTrackVertices(GPXFile gpxfile, out List<int> shpIndexes)
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

        public static Shape PolylineNoSelfCrossing(Shape pl, out int crossingCount)
        {
            crossingCount = 0;
            int accumulatedCrossings = 0;
            var segment = new Shape();
            if (segment.Create(ShpfileType.SHP_POLYLINE))
            {
                for (int x = 0; x < pl.numPoints; x++)
                {
                    if (x > 2)
                    {
                        var line = new Shape();
                        if (line.Create(ShpfileType.SHP_POLYLINE))
                        {
                            //line.AddPoint(segment.Point[x - (1+crossingCount)].x, segment.Point[x - (1+crossingCount)].y);
                            line.AddPoint(segment.Point[segment.numPoints - 1].x, segment.Point[segment.numPoints - 1].y);
                            line.AddPoint(pl.Point[x].x, pl.Point[x].y);
                            if (line.Crosses(segment))
                            {
                                crossingCount++;
                                accumulatedCrossings++;
                                if (accumulatedCrossings > 1)
                                {
                                    segment.DeletePoint(segment.numPoints - 1);
                                    //segment.DeletePoint(segment.numPoints - 1);
                                    accumulatedCrossings = 0;
                                }

                            }
                            else
                            {
                                segment.AddPoint(x: pl.Point[x].x, y: pl.Point[x].y);
                            }
                        }
                    }
                    else
                    {
                        segment.AddPoint(x: pl.Point[x].x, y: pl.Point[x].y);
                    }
                }

            }
            return segment;
        }
        public static int PolylineSelfCrossingsCount(Shape pl, int? maxCrossingAllowed, bool stopAtMax = true)
        {
            int crossingCount = 0;
            var segment = new Shape();
            if (segment.Create(ShpfileType.SHP_POLYLINE))
            {
                for (int x = 0; x < pl.numPoints; x++)
                {
                    if (x > 2)
                    {
                        var line = new Shape();
                        if (line.Create(ShpfileType.SHP_POLYLINE))
                        {
                            line.AddPoint(segment.Point[x - 1].x, segment.Point[x - 1].y);
                            line.AddPoint(pl.Point[x].x, pl.Point[x].y);
                            if (line.Crosses(segment))
                            {
                                crossingCount++;
                                if (maxCrossingAllowed != null && maxCrossingAllowed > 0)
                                {
                                    if (stopAtMax && crossingCount > maxCrossingAllowed)
                                    {
                                        return crossingCount;
                                    }
                                }
                            }
                        }
                    }
                    segment.AddPoint(x: pl.Point[x].x, y: pl.Point[x].y);
                }
            }
            return crossingCount;
        }

        public static double? GetPolyLineShapeLength(Shape pl)
        {
            if (pl.ShapeType == ShpfileType.SHP_POLYLINE)
            {
                var wpt = new Waypoint();
                double len = 0;
                for (int x = 0; x < pl.numPoints; x++)
                {
                    if (x > 0)
                    {
                        double elevChange;
                        len += Waypoint.ComputeDistance(wpt, new Waypoint { Longitude = pl.Point[x].x, Latitude = pl.Point[x].y }, out elevChange);
                    }

                    wpt = new Waypoint { Longitude = pl.Point[x].x, Latitude = pl.Point[x].y };
                }
                return len;
            }
            return null;
        }

        public static Shapefile TracksFromGPXFiles(GPXFile gpxFile, out List<int> handles)
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
                        var ptIndex = shp.AddPoint(wlt.Longitude, wlt.Latitude);
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
                        double? lat = null;
                        double? lon = null;
                        if (nd.ParentNode.SelectSingleNode(".//A[@N='Latitude']") != null)
                        {
                            lat = double.Parse(nd.ParentNode.SelectSingleNode(".//A[@N='Latitude']").Attributes["V"].Value);
                        }
                        if (nd.ParentNode.SelectSingleNode(".//A[@N='Longitude']") != null)
                        {
                            lon = double.Parse(nd.ParentNode.SelectSingleNode(".//A[@N='Longitude']").Attributes["V"].Value);
                        }
                        if (lat != null || lon != null)
                        {
                            var pt_type = nd.ParentNode.SelectSingleNode(".//A[@N='WaypointType']").Attributes["V"].Value;
                            if (pt_type == setWaypointKey)
                            {
                                wptType = "Setting";
                            }
                            else
                            {
                                wptType = "Hauling";
                            }
                            if (shp.AddPoint((double)lon, (double)lat) >= 0)
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
                        else
                        {

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


    }
}