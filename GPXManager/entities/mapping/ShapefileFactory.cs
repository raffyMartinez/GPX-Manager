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

        public static int MaxSelfCrossings { get; set; }

        public static Shape BSCBoundaryLine { get; set; }

        public static List<ExtractedFishingTrack> ExtractedFishingTracks()
        {
            return _gearHaulExtractedTracks;
        }
        public static bool TrackShapeFromGPX(GPXFile gpxFile)
        {
            Waypoint ptBefore = null;
            double accumulatedDistance = 0;
            Shape segment = null;
            ExtractedFishingTrack eft = new ExtractedFishingTrack();
            _gearHaulExtractedTracks = new List<ExtractedFishingTrack>();
            if (gpxFile.TrackWaypoinsInLocalTime.Count > 0)
            {
                int ptIndex = 0;
                foreach (var wlt in gpxFile.TrackWaypoinsInLocalTime)
                {

                    Waypoint wpt = null;
                    if (ptIndex > 0)
                    {
                        wpt = new Waypoint { Longitude = wlt.Longitude, Latitude = wlt.Latitude, Time = wlt.Time };
                        double elevChange;
                        double distance = Waypoint.ComputeDistance(ptBefore, wpt, out elevChange);
                        TimeSpan timeElapsed = wlt.Time - _timeBefore;
                        double speed = distance / timeElapsed.TotalMinutes;
                        if (speed < Global.Settings.SpeedThresholdForRetrieving)
                        {
                            if (segment == null || segment.numPoints == 0)
                            {
                                segment = new Shape();
                                segment.Create(ShpfileType.SHP_POLYLINE);

                                segment.AddPoint(ptBefore.Longitude, ptBefore.Latitude);

                            }
                            segment.AddPoint(wlt.Longitude, wlt.Latitude);
                            if (eft.SpeedAtWaypoints.Count == 0)
                            {
                                eft.Start = wlt.Time;
                            }
                            eft.SpeedAtWaypoints.Add(speed);
                            accumulatedDistance += distance;
                        }
                        else
                        {
                            if (accumulatedDistance >= Global.Settings.GearRetrievingMinLength)
                            {
                                //we have a potential haul segment
                                eft.TrackPointCountOriginal = segment.numPoints;
                                eft.TrackOriginal = segment;
                                segment = DouglasPeucker.DouglasPeucker.DouglasPeuckerReduction(segment, 30);
                                eft.SegmentSimplified = segment;
                                eft.TrackPointCountSimplified = segment.numPoints;
                                eft.LengthOriginal = accumulatedDistance;
                                eft.LengthSimplified = (double)GetPolyLineShapeLength(segment);
                                eft.End = wlt.Time;
                                eft.AverageSpeed = eft.SpeedAtWaypoints.Average();

                                if (eft.AverageSpeed > 12 &&
                                    eft.LengthSimplified >= Global.Settings.GearRetrievingMinLength &&
                                    eft.LengthOriginal < 3000)
                                {

                                    if (BSCBoundaryLine == null)
                                    {
                                        _gearHaulExtractedTracks.Add(eft);
                                    }
                                    else if (!segment.Crosses(BSCBoundaryLine))
                                    {
                                        _gearHaulExtractedTracks.Add(eft);
                                    }




                                }
                            }
                            ptIndex = 0;
                            accumulatedDistance = 0;
                            segment = new Shape();
                            segment.Create(ShpfileType.SHP_POLYLINE);
                            eft = new ExtractedFishingTrack();
                        }
                    }
                    _timeBefore = wlt.Time;
                    ptBefore = new Waypoint { Longitude = wlt.Longitude, Latitude = wlt.Latitude, Time = _timeBefore };
                    ptIndex++;
                }


            }
            return _gearHaulExtractedTracks.Count > 0;
        }

        public static bool TrackShapeFromCTX(CTXFile ctxFile)
        {
            if (ctxFile.XML.Length == 0)
                return false;
            else
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(ctxFile.XML);
                var tracknodes = doc.SelectNodes("//T");
                return TrackShapeFromCTX(tracknodes);
            }
        }
        public static TripAndHauls TrackShapeFromGPX(GPXFile gpxFile, int? interval)
        {
            List<DetectedTrack> detectedTracks = new List<DetectedTrack>();
            Waypoint ptBefore = null;
            double accumulatedDistance = 0;
            Shape segment = null;
            DateTime wptDateTime;
            ExtractedFishingTrack eft = new ExtractedFishingTrack();
            int counter = 0;
            if (gpxFile.TrackWaypoinsInLocalTime.Count >= 2)
            {
                foreach (var wlt in gpxFile.TrackWaypoinsInLocalTime)
                {
                    var lat = wlt.Latitude;
                    var lon = wlt.Longitude;
                    wptDateTime = wlt.Time;

                    if (_timeBefore < wptDateTime)
                    {

                        Waypoint wpt = null;
                        if (segment != null)
                        {
                            wpt = new Waypoint { Longitude = lon, Latitude = lat, Time = wptDateTime };
                            double elevChange;
                            double distance = Waypoint.ComputeDistance(ptBefore, wpt, out elevChange);
                            TimeSpan timeElapsed = wptDateTime - _timeBefore;
                            double speed = distance / timeElapsed.TotalMinutes;

                            bool proceed = interval == null || interval == 0;
                            if (!proceed)
                            {
                                proceed = (wptDateTime - _timeBefore).TotalSeconds < (int)interval * 2;
                            }


                            if (proceed &&
                                counter < gpxFile.TrackWaypoinsInLocalTime.Count &&
                                speed < Global.Settings.SpeedThresholdForRetrieving)
                            {
                                if (segment.numPoints == 0)
                                {
                                    segment.AddPoint(ptBefore.Longitude, ptBefore.Latitude);
                                }
                                segment.AddPoint(lon, lat);
                                if (eft.SpeedAtWaypoints.Count == 0)
                                {
                                    eft.Start = wptDateTime;
                                }
                                eft.SpeedAtWaypoints.Add(speed);
                                accumulatedDistance += distance;
                            }
                            else
                            {
                                if (counter == gpxFile.TrackWaypoinsInLocalTime.Count)
                                {
                                    segment.AddPoint(lon, lat);
                                    eft.SpeedAtWaypoints.Add(speed);
                                    accumulatedDistance += distance;
                                }
                                if (segment.numPoints >= 6 && PolylineSelfCrossingsCount(segment, 10, true) < 10)
                                {
                                    if (BSCBoundaryLine == null || !segment.Crosses(BSCBoundaryLine))
                                    {
                                        eft.TrackPointCountOriginal = segment.numPoints;
                                        eft.TrackOriginal = segment;
                                        eft.SegmentSimplified = DouglasPeucker.DouglasPeucker.DouglasPeuckerReduction(segment, 20);
                                        eft.Segment = segment;
                                        eft.TrackPointCountSimplified = eft.SegmentSimplified.numPoints;
                                        eft.LengthOriginal = accumulatedDistance;
                                        eft.LengthSimplified = (double)GetPolyLineShapeLength(eft.SegmentSimplified);
                                        eft.End = wptDateTime;
                                        eft.AverageSpeed = eft.SpeedAtWaypoints.Average();

                                        detectedTracks.Add(new DetectedTrack { ExtractedFishingTrack = eft, Accept = false, Length = eft.LengthOriginal });
                                    }
                                }
                                accumulatedDistance = 0;
                                segment = null;
                                eft = new ExtractedFishingTrack();
                            }
                        }

                    }
                    if (_timeBefore < wptDateTime || segment == null)
                    {
                        _timeBefore = wptDateTime;
                        ptBefore = new Waypoint { Longitude = lon, Latitude = lat, Time = _timeBefore };
                    }

                    if (segment == null)
                    {
                        segment = new Shape();
                        segment.Create(ShpfileType.SHP_POLYLINE);
                    }
                }

                var th = new TripAndHauls { Shapefile = null };
                if (detectedTracks.Count > 0)
                {
                    counter = 0;
                    DetectedTrack firstSegment = null;
                    foreach (var tr in detectedTracks)
                    {
                        if (tr.ExtractedFishingTrack.LengthOriginal > Global.Settings.GearRetrievingMinLength &&
                            tr.ExtractedFishingTrack.TrackPointCountOriginal > 6 &&
                            tr.ExtractedFishingTrack.LengthOriginal < 3500)
                        {
                            tr.Accept = true;
                        }
                        if (counter > 0)
                        {
                            Point pt1 = firstSegment.ExtractedFishingTrack.Segment.Point[firstSegment.ExtractedFishingTrack.Segment.numPoints - 1];
                            Point pt2 = tr.ExtractedFishingTrack.Segment.Point[0];
                            double elevChange = 0;
                            var distance = Waypoint.ComputeDistance(
                                new Waypoint { Latitude = pt1.y, Longitude = pt1.x },
                                new Waypoint { Latitude = pt2.y, Longitude = pt2.x },
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

                th.Tracks = detectedTracks;
                return th;
            }
            else
            {
                return null;
            }

        }
        public static TripAndHauls TrackShapeFromCTX(XmlNodeList tracknodes, int? interval)
        {
            List<DetectedTrack> detectedTracks = new List<DetectedTrack>();
            Waypoint ptBefore = null;
            double accumulatedDistance = 0;
            Shape segment = null;
            DateTime wptDateTime;
            ExtractedFishingTrack eft = new ExtractedFishingTrack();
            int counter = 0;
            if (tracknodes.Count >= 2)
            {
                foreach (XmlNode node in tracknodes)
                {
                    counter++;
                    var lat = double.Parse(node.SelectSingleNode(".//A[@N='Latitude']").Attributes["V"].Value);
                    var lon = double.Parse(node.SelectSingleNode(".//A[@N='Longitude']").Attributes["V"].Value);
                    var wptDate = node.SelectSingleNode(".//A[@N='Date']").Attributes["V"].Value;
                    var wptTime = node.SelectSingleNode(".//A[@N='Time']").Attributes["V"].Value;
                    wptDateTime = DateTime.Parse(wptDate) + DateTime.Parse(wptTime).TimeOfDay;


                    if (_timeBefore < wptDateTime)
                    {

                        Waypoint wpt = null;
                        if (segment != null)
                        {
                            wpt = new Waypoint { Longitude = lon, Latitude = lat, Time = wptDateTime };
                            double elevChange;
                            double distance = Waypoint.ComputeDistance(ptBefore, wpt, out elevChange);
                            TimeSpan timeElapsed = wptDateTime - _timeBefore;
                            double speed = distance / timeElapsed.TotalMinutes;

                            bool proceed = interval == null || interval == 0;
                            if (!proceed)
                            {
                                proceed = (wptDateTime - _timeBefore).TotalSeconds < (int)interval * 2;
                            }


                            if (proceed &&
                                counter < tracknodes.Count &&
                                speed < Global.Settings.SpeedThresholdForRetrieving)
                            {
                                if (segment.numPoints == 0)
                                {
                                    segment.AddPoint(ptBefore.Longitude, ptBefore.Latitude);
                                }
                                segment.AddPoint(lon, lat);
                                if (eft.SpeedAtWaypoints.Count == 0)
                                {
                                    eft.Start = wptDateTime;
                                }
                                eft.SpeedAtWaypoints.Add(speed);
                                accumulatedDistance += distance;
                            }
                            else
                            {
                                if (counter == tracknodes.Count)
                                {
                                    segment.AddPoint(lon, lat);
                                    eft.SpeedAtWaypoints.Add(speed);
                                    accumulatedDistance += distance;
                                }
                                if (segment.numPoints >= 6 && PolylineSelfCrossingsCount(segment, 10, true) < 10)
                                {
                                    if (BSCBoundaryLine == null || !segment.Crosses(BSCBoundaryLine))
                                    {
                                        eft.TrackPointCountOriginal = segment.numPoints;
                                        eft.TrackOriginal = segment;
                                        eft.SegmentSimplified = DouglasPeucker.DouglasPeucker.DouglasPeuckerReduction(segment, 20);
                                        eft.Segment = segment;
                                        eft.TrackPointCountSimplified = eft.SegmentSimplified.numPoints;
                                        eft.LengthOriginal = accumulatedDistance;
                                        eft.LengthSimplified = (double)GetPolyLineShapeLength(eft.SegmentSimplified);
                                        eft.End = wptDateTime;
                                        eft.AverageSpeed = eft.SpeedAtWaypoints.Average();

                                        detectedTracks.Add(new DetectedTrack { ExtractedFishingTrack = eft, Accept = false, Length = eft.LengthOriginal });
                                    }
                                }
                                accumulatedDistance = 0;
                                segment = null;
                                eft = new ExtractedFishingTrack();
                            }
                        }

                    }
                    if (_timeBefore < wptDateTime || segment == null)
                    {
                        _timeBefore = wptDateTime;
                        ptBefore = new Waypoint { Longitude = lon, Latitude = lat, Time = _timeBefore };
                    }

                    if (segment == null)
                    {
                        segment = new Shape();
                        segment.Create(ShpfileType.SHP_POLYLINE);
                    }


                }
                var th = new TripAndHauls { Shapefile = null };
                if (detectedTracks.Count > 0)
                {
                    counter = 0;
                    DetectedTrack firstSegment = null;
                    foreach (var tr in detectedTracks)
                    {
                        if (tr.ExtractedFishingTrack.LengthOriginal > Global.Settings.GearRetrievingMinLength &&
                            tr.ExtractedFishingTrack.TrackPointCountOriginal > 6 &&
                            tr.ExtractedFishingTrack.LengthOriginal < 3500)
                        {
                            tr.Accept = true;
                        }
                        if (counter > 0)
                        {
                            Point pt1 = firstSegment.ExtractedFishingTrack.Segment.Point[firstSegment.ExtractedFishingTrack.Segment.numPoints - 1];
                            Point pt2 = tr.ExtractedFishingTrack.Segment.Point[0];
                            double elevChange = 0;
                            var distance = Waypoint.ComputeDistance(
                                new Waypoint { Latitude = pt1.y, Longitude = pt1.x },
                                new Waypoint { Latitude = pt2.y, Longitude = pt2.x },
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

                th.Tracks = detectedTracks;
                return th;
            }
            else
            {
                return null;
            }
        }
        public static bool TrackShapeFromCTX(XmlNodeList tracknodes)
        {
            Waypoint ptBefore = null;
            double accumulatedDistance = 0;
            Shape segment = null;
            XmlDocument doc = new XmlDocument();


            //doc.LoadXml(ctxfile.XML);
            //var tracknodes = doc.SelectNodes("//T");
            ExtractedFishingTrack eft = new ExtractedFishingTrack();
            _gearHaulExtractedTracks = new List<ExtractedFishingTrack>();


            var ptIndex = 0;
            foreach (XmlNode node in tracknodes)
            {
                var lat = double.Parse(node.SelectSingleNode(".//A[@N='Latitude']").Attributes["V"].Value);
                var lon = double.Parse(node.SelectSingleNode(".//A[@N='Longitude']").Attributes["V"].Value);



                var wptDate = node.SelectSingleNode(".//A[@N='Date']").Attributes["V"].Value;
                var wptTime = node.SelectSingleNode(".//A[@N='Time']").Attributes["V"].Value;
                var wptDateTime = DateTime.Parse(wptDate) + DateTime.Parse(wptTime).TimeOfDay;
                Waypoint wpt = null;

                if (ptIndex > 0)
                {
                    wpt = new Waypoint { Longitude = lon, Latitude = lat, Time = wptDateTime };
                    double elevChange;
                    double distance = Waypoint.ComputeDistance(ptBefore, wpt, out elevChange);
                    TimeSpan timeElapsed = wptDateTime - _timeBefore;
                    double speed = distance / timeElapsed.TotalMinutes;
                    if (speed < Global.Settings.SpeedThresholdForRetrieving)
                    {
                        if (segment == null || segment.numPoints == 0)
                        {
                            segment = new Shape();
                            segment.Create(ShpfileType.SHP_POLYLINE);

                            segment.AddPoint(ptBefore.Longitude, ptBefore.Latitude);

                        }
                        segment.AddPoint(lon, lat);
                        if (eft.SpeedAtWaypoints.Count == 0)
                        {
                            eft.Start = wptDateTime;
                        }
                        eft.SpeedAtWaypoints.Add(speed);
                        accumulatedDistance += distance;
                        //segmentIndex = segment.AddPoint(lon, lat);
                    }
                    else
                    {
                        if (accumulatedDistance >= Global.Settings.GearRetrievingMinLength)
                        {
                            //we have a potential haul segment
                            eft.TrackPointCountOriginal = segment.numPoints;
                            eft.TrackOriginal = segment;
                            segment = DouglasPeucker.DouglasPeucker.DouglasPeuckerReduction(segment, 30);
                            eft.SegmentSimplified = segment;
                            eft.TrackPointCountSimplified = segment.numPoints;
                            eft.LengthOriginal = accumulatedDistance;
                            eft.LengthSimplified = (double)GetPolyLineShapeLength(segment);
                            eft.End = wptDateTime;
                            eft.AverageSpeed = eft.SpeedAtWaypoints.Average();

                            if (eft.AverageSpeed > 12 &&
                                eft.LengthSimplified >= Global.Settings.GearRetrievingMinLength &&
                                eft.LengthOriginal < 3000)
                            {
                                if (BSCBoundaryLine == null)
                                {
                                    _gearHaulExtractedTracks.Add(eft);
                                }
                                else if (!segment.Crosses(BSCBoundaryLine))
                                {
                                    _gearHaulExtractedTracks.Add(eft);
                                }
                            }
                        }

                        ptIndex = 0;
                        accumulatedDistance = 0;
                        segment = new Shape();
                        segment.Create(ShpfileType.SHP_POLYLINE);
                        eft = new ExtractedFishingTrack();
                    }
                }

                _timeBefore = wptDateTime;
                ptBefore = new Waypoint { Longitude = lon, Latitude = lat, Time = _timeBefore };
                ptIndex++;



            }

            return _gearHaulExtractedTracks.Count > 0;
        }

        public static TripAndHauls CreateTripAndHaulsFromGPX(GPXFile gpxFile)
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
                    Shape segment = null;
                    DateTime wptDateTime = DateTime.Now;
                    Waypoint ptBefore = null;
                    ExtractedFishingTrack eft = new ExtractedFishingTrack();
                    double accumulatedDistance = 0;
                    int? interval = null;
                    if (gpxFile.GPSTimerInterval == null)
                    {
                        //ctxfile.CTXFile.TrackingInterval = Entities.CTXFileViewModel.GetGPSTimerIntervalFromCTX(ctxfile.CTXFile, true);
                    }
                    interval = gpxFile.GPSTimerInterval;

                    if (sf.CreateNewWithShapeID("", ShpfileType.SHP_POLYLINE))
                    {
                        sf.EditAddField("User", FieldType.STRING_FIELD, 1, 1);
                        //sf.EditAddField("Gear", FieldType.STRING_FIELD, 1, 1);
                        //sf.EditAddField("LandingSite", FieldType.STRING_FIELD, 1, 1);
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
                                counter++;
                                var lat = wlt.Latitude;
                                var lon = wlt.Longitude;
                                wptDateTime = wlt.Time;
                                shp.AddPoint(lon, lat);
                                if (ExtractFishingTrackLine)
                                {
                                    if (_timeBefore < wptDateTime)
                                    {
                                        Waypoint wpt = null;
                                        if (segment != null)
                                        {
                                            wpt = new Waypoint { Longitude = lon, Latitude = lat, Time = wptDateTime };
                                            double elevChange;
                                            double distance = Waypoint.ComputeDistance(ptBefore, wpt, out elevChange);
                                            TimeSpan timeElapsed = wptDateTime - _timeBefore;
                                            double speed = distance / timeElapsed.TotalMinutes;


                                            bool proceed = interval == null || interval == 0;
                                            if (!proceed)
                                            {
                                                proceed = timeElapsed.TotalSeconds < (int)interval * 2;
                                            }


                                            if (proceed && counter < gpxFile.TrackWaypoinsInLocalTime.Count && speed < Global.Settings.SpeedThresholdForRetrieving)
                                            {
                                                if (segment.numPoints == 0)
                                                {
                                                    segment.AddPoint(ptBefore.Longitude, ptBefore.Latitude);
                                                }
                                                segment.AddPoint(lon, lat);
                                                if (eft.SpeedAtWaypoints.Count == 0)
                                                {
                                                    eft.Start = wptDateTime;
                                                }
                                                eft.SpeedAtWaypoints.Add(speed);
                                                accumulatedDistance += distance;
                                            }
                                            else
                                            {
                                                if (counter == gpxFile.TrackWaypoinsInLocalTime.Count)
                                                {
                                                    segment.AddPoint(lon, lat);
                                                    eft.SpeedAtWaypoints.Add(speed);
                                                    accumulatedDistance += distance;
                                                }
                                                if (segment.numPoints >= 6 && PolylineSelfCrossingsCount(segment, 10, true) < 10)
                                                {
                                                    if (BSCBoundaryLine == null || !segment.Crosses(BSCBoundaryLine))
                                                    {
                                                        eft.TrackPointCountOriginal = segment.numPoints;
                                                        eft.TrackOriginal = segment;
                                                        eft.SegmentSimplified = DouglasPeucker.DouglasPeucker.DouglasPeuckerReduction(segment, 20);
                                                        eft.Segment = segment;
                                                        eft.TrackPointCountSimplified = eft.SegmentSimplified.numPoints;
                                                        eft.LengthOriginal = accumulatedDistance;
                                                        eft.LengthSimplified = (double)GetPolyLineShapeLength(eft.SegmentSimplified);
                                                        eft.End = wptDateTime;
                                                        eft.AverageSpeed = eft.SpeedAtWaypoints.Average();

                                                        detectedTracks.Add(new DetectedTrack { ExtractedFishingTrack = eft, Accept = false, Length = eft.LengthOriginal });
                                                    }
                                                }
                                                accumulatedDistance = 0;
                                                segment = null;
                                                eft = new ExtractedFishingTrack();
                                            }
                                        }
                                    }
                                    if (_timeBefore < wptDateTime || segment == null)
                                    {
                                        _timeBefore = wptDateTime;
                                        ptBefore = new Waypoint { Longitude = lon, Latitude = lat, Time = _timeBefore };
                                    }

                                    if (segment == null)
                                    {
                                        segment = new Shape();
                                        segment.Create(ShpfileType.SHP_POLYLINE);
                                    }

                                }
                            }

                            var shpIndex = sf.EditAddShape(shp);
                            sf.EditCellValue(sf.FieldIndexByName["User"], shpIndex, gpxFile.GPS.DeviceName);
                            //sf.EditCellValue(sf.FieldIndexByName["Gear"], shpIndex, ctxfile.Gear);
                            //sf.EditCellValue(sf.FieldIndexByName["LandingSite"], shpIndex, ctxfile.LandingSite);
                            sf.EditCellValue(sf.FieldIndexByName["Start"], shpIndex, gpxFile.DateRangeStart);
                            sf.EditCellValue(sf.FieldIndexByName["Finished"], shpIndex, gpxFile.DateRangeEnd);
                            sf.EditCellValue(sf.FieldIndexByName["Interval"], shpIndex, interval);



                            var th = new TripAndHauls { Shapefile = sf, Handle = shpIndex };
                            if (detectedTracks.Count > 0)
                            {
                                counter = 0;
                                DetectedTrack firstSegment = null;
                                foreach (var tr in detectedTracks)
                                {
                                    if (tr.ExtractedFishingTrack.LengthOriginal > Global.Settings.GearRetrievingMinLength &&
                                        tr.ExtractedFishingTrack.TrackPointCountOriginal > 6 &&
                                        tr.ExtractedFishingTrack.LengthOriginal < 3500)
                                    {
                                        tr.Accept = true;
                                    }
                                    if (counter > 0)
                                    {
                                        Point pt1 = firstSegment.ExtractedFishingTrack.Segment.Point[firstSegment.ExtractedFishingTrack.Segment.numPoints - 1];
                                        Point pt2 = tr.ExtractedFishingTrack.Segment.Point[0];
                                        double elevChange = 0;
                                        var distance = Waypoint.ComputeDistance(
                                            new Waypoint { Latitude = pt1.y, Longitude = pt1.x },
                                            new Waypoint { Latitude = pt2.y, Longitude = pt2.x },
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

                            th.Tracks = detectedTracks;
                            return th;
                        }
                    }
                }

            }
            return null;
        }

        public static TripAndHauls CreateTripAndHaulsFromCTX(CTXFileSummaryView ctxfile)
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
                    Shape segment = null;
                    DateTime wptDateTime = DateTime.Now;
                    Waypoint pt1 = null;
                    Waypoint pt2 = null;
                    ExtractedFishingTrack eft = new ExtractedFishingTrack();
                    double accumulatedDistance = 0;
                    int? interval = null;
                    if (ctxfile.CTXFile.TrackingInterval == null)
                    {
                        ctxfile.CTXFile.TrackingInterval = Entities.CTXFileViewModel.GetGPSTimerIntervalFromCTX(ctxfile.CTXFile, true);
                    }
                    interval = ctxfile.CTXFile.TrackingInterval;


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
                                    MakeSegments(counter, pt1, pt2, counter >= tracknodes.Count);
                                }
                                pt1 = new Waypoint { Longitude = lon, Latitude = lat, Time = wptDateTime };
                                counter++;


                            }
                        }
                    }
                }
            }
        }

        private static List<DetectedTrack> MakeSegments(int counter, Waypoint pt1, Waypoint pt2 = null, bool? done = null)
        {
            Shape segment = null;

            double elevChange;
            double distance = Waypoint.ComputeDistance(pt1, pt2, out elevChange);
            TimeSpan timeElapsed = pt2.Time - pt1.Time;
            double speed = distance / timeElapsed.TotalMinutes;
            if (speed < Global.Settings.SpeedThresholdForRetrieving)
            {
                if (segment == null)
                {
                    segment = new Shape();
                    segment.Create(ShpfileType.SHP_POLYLINE);
                }

                if (segment.numPoints == 0)
                {
                    segment.AddPoint(pt1.Longitude, pt1.Latitude);
                }
                segment.AddPoint(pt2.Longitude, pt2.Latitude);
            }
            else
            {

            }


        }
        public static TripAndHauls CreateTripAndHaulsFromCTX1(CTXFileSummaryView ctxfile)
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
                    Shape segment = null;
                    DateTime wptDateTime = DateTime.Now;
                    Waypoint ptBefore = null;
                    ExtractedFishingTrack eft = new ExtractedFishingTrack();
                    double accumulatedDistance = 0;
                    int? interval = null;
                    if (ctxfile.CTXFile.TrackingInterval == null)
                    {
                        ctxfile.CTXFile.TrackingInterval = Entities.CTXFileViewModel.GetGPSTimerIntervalFromCTX(ctxfile.CTXFile, true);
                    }
                    interval = ctxfile.CTXFile.TrackingInterval;


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
                                counter++;
                                var lat = double.Parse(node.SelectSingleNode(".//A[@N='Latitude']").Attributes["V"].Value);
                                var lon = double.Parse(node.SelectSingleNode(".//A[@N='Longitude']").Attributes["V"].Value);
                                var wptDate = node.SelectSingleNode(".//A[@N='Date']").Attributes["V"].Value;
                                var wptTime = node.SelectSingleNode(".//A[@N='Time']").Attributes["V"].Value;
                                wptDateTime = DateTime.Parse(wptDate) + DateTime.Parse(wptTime).TimeOfDay;
                                shp.AddPoint(lon, lat);

                                if (ExtractFishingTrackLine)
                                {
                                    if (_timeBefore < wptDateTime)
                                    {

                                        Waypoint wpt = null;
                                        if (segment != null)
                                        {
                                            wpt = new Waypoint { Longitude = lon, Latitude = lat, Time = wptDateTime };
                                            double elevChange;
                                            double distance = Waypoint.ComputeDistance(ptBefore, wpt, out elevChange);
                                            TimeSpan timeElapsed = wptDateTime - _timeBefore;
                                            double speed = distance / timeElapsed.TotalMinutes;

                                            bool proceed = interval == null || interval == 0;
                                            if (!proceed)
                                            {
                                                proceed = timeElapsed.TotalSeconds < (int)interval * 2;
                                            }


                                            if (proceed && counter < tracknodes.Count && speed < Global.Settings.SpeedThresholdForRetrieving)
                                            {
                                                if (segment.numPoints == 0)
                                                {
                                                    segment.AddPoint(ptBefore.Longitude, ptBefore.Latitude);
                                                }
                                                segment.AddPoint(lon, lat);
                                                if (eft.SpeedAtWaypoints.Count == 0)
                                                {
                                                    eft.Start = wptDateTime;
                                                }
                                                eft.SpeedAtWaypoints.Add(speed);
                                                accumulatedDistance += distance;
                                            }
                                            else
                                            {
                                                if (counter == tracknodes.Count)
                                                {
                                                    segment.AddPoint(lon, lat);
                                                    eft.SpeedAtWaypoints.Add(speed);
                                                    accumulatedDistance += distance;
                                                }
                                                if (segment.numPoints >= 6 && PolylineSelfCrossingsCount(segment, 10, true) < 10)
                                                {
                                                    if (BSCBoundaryLine == null || !segment.Crosses(BSCBoundaryLine))
                                                    {
                                                        eft.TrackPointCountOriginal = segment.numPoints;
                                                        eft.TrackOriginal = segment;
                                                        eft.SegmentSimplified = DouglasPeucker.DouglasPeucker.DouglasPeuckerReduction(segment, 20);
                                                        eft.Segment = segment;
                                                        eft.TrackPointCountSimplified = eft.SegmentSimplified.numPoints;
                                                        eft.LengthOriginal = accumulatedDistance;
                                                        eft.LengthSimplified = (double)GetPolyLineShapeLength(eft.SegmentSimplified);
                                                        eft.End = wptDateTime;
                                                        eft.AverageSpeed = eft.SpeedAtWaypoints.Average();

                                                        detectedTracks.Add(new DetectedTrack { ExtractedFishingTrack = eft, Accept = false, Length = eft.LengthOriginal });
                                                    }
                                                }
                                                accumulatedDistance = 0;
                                                segment = null;
                                                eft = new ExtractedFishingTrack();
                                            }
                                        }

                                    }
                                    if (_timeBefore < wptDateTime || segment == null)
                                    {
                                        _timeBefore = wptDateTime;
                                        ptBefore = new Waypoint { Longitude = lon, Latitude = lat, Time = _timeBefore };
                                    }

                                    if (segment == null)
                                    {
                                        segment = new Shape();
                                        segment.Create(ShpfileType.SHP_POLYLINE);
                                    }

                                }
                            }

                            var shpIndex = sf.EditAddShape(shp);
                            sf.EditCellValue(sf.FieldIndexByName["User"], shpIndex, ctxfile.User);
                            sf.EditCellValue(sf.FieldIndexByName["Gear"], shpIndex, ctxfile.Gear);
                            sf.EditCellValue(sf.FieldIndexByName["LandingSite"], shpIndex, ctxfile.LandingSite);
                            sf.EditCellValue(sf.FieldIndexByName["Start"], shpIndex, ctxfile.DateStart);
                            sf.EditCellValue(sf.FieldIndexByName["Finished"], shpIndex, ctxfile.DateEnd);
                            sf.EditCellValue(sf.FieldIndexByName["Interval"], shpIndex, interval);



                            var th = new TripAndHauls { Shapefile = sf, Handle = shpIndex };
                            if (detectedTracks.Count > 0)
                            {
                                counter = 0;
                                DetectedTrack firstSegment = null;
                                foreach (var tr in detectedTracks)
                                {
                                    if (tr.ExtractedFishingTrack.LengthOriginal > Global.Settings.GearRetrievingMinLength &&
                                        tr.ExtractedFishingTrack.TrackPointCountOriginal > 6 &&
                                        tr.ExtractedFishingTrack.LengthOriginal < 3500)
                                    {
                                        tr.Accept = true;
                                    }
                                    if (counter > 0)
                                    {
                                        Point pt1 = firstSegment.ExtractedFishingTrack.Segment.Point[firstSegment.ExtractedFishingTrack.Segment.numPoints - 1];
                                        Point pt2 = tr.ExtractedFishingTrack.Segment.Point[0];
                                        double elevChange = 0;
                                        var distance = Waypoint.ComputeDistance(
                                            new Waypoint { Latitude = pt1.y, Longitude = pt1.x },
                                            new Waypoint { Latitude = pt2.y, Longitude = pt2.x },
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

                            th.Tracks = detectedTracks;
                            return th;
                        }
                    }
                }


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
        public static Shapefile FishingTrackLine(TripAndHauls th)
        {
            if (ExtractFishingTrackLine && th.Tracks.Count > 0)
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

                    foreach (var item in th.Tracks)
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

        public static Shapefile TrackFromGPX(GPXFile gpxFile, out List<int> handles)
        {
            double accumulatedDistance = 0;
            //int segmentIndex = 0;
            Waypoint ptBefore = null;
            Shape segment = null;
            ExtractFishingTrackLine = true;
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
                    ExtractedFishingTrack eft = new ExtractedFishingTrack();
                    _gearHaulExtractedTracks = new List<ExtractedFishingTrack>();
                    foreach (var wlt in gpxFile.TrackWaypoinsInLocalTime)
                    {
                        var ptIndex = shp.AddPoint(wlt.Longitude, wlt.Latitude);
                        if (ExtractFishingTrackLine)
                        {
                            Waypoint wpt = null;
                            if (ptIndex > 0)
                            {
                                wpt = new Waypoint { Longitude = wlt.Longitude, Latitude = wlt.Latitude, Time = wlt.Time };
                                double elevChange;
                                double distance = Waypoint.ComputeDistance(ptBefore, wpt, out elevChange);
                                TimeSpan timeElapsed = wlt.Time - _timeBefore;
                                double speed = distance / timeElapsed.TotalMinutes;
                                if (speed < Global.Settings.SpeedThresholdForRetrieving)
                                {
                                    if (segment == null || segment.numPoints == 0)
                                    {
                                        segment = new Shape();
                                        segment.Create(ShpfileType.SHP_POLYLINE);

                                        segment.AddPoint(ptBefore.Longitude, ptBefore.Latitude);

                                    }
                                    segment.AddPoint(wlt.Longitude, wlt.Latitude);
                                    if (eft.SpeedAtWaypoints.Count == 0)
                                    {
                                        eft.Start = wlt.Time;
                                    }
                                    eft.SpeedAtWaypoints.Add(speed);
                                    accumulatedDistance += distance;

                                    //if (eft.SpeedAtWaypoints.Count == 0)
                                    //{
                                    //    eft.Start = wlt.Time;
                                    //}
                                    //eft.SpeedAtWaypoints.Add(speed);
                                    //accumulatedDistance += distance;
                                    //segmentIndex = segment.AddPoint(wlt.Longitude, wlt.Latitude);
                                }
                                else
                                {
                                    if (accumulatedDistance >= Global.Settings.GearRetrievingMinLength)
                                    {
                                        //we have a potential haul segment
                                        //segment = PolylineNoSelfCrossing(segment, out int crossingCount);
                                        eft.TrackPointCountOriginal = segment.numPoints;
                                        eft.TrackOriginal = segment;
                                        segment = DouglasPeucker.DouglasPeucker.DouglasPeuckerReduction(segment, 20);
                                        eft.TrackPointCountSimplified = segment.numPoints;
                                        eft.SegmentSimplified = segment;
                                        eft.LengthOriginal = accumulatedDistance;
                                        eft.LengthSimplified = (double)GetPolyLineShapeLength(segment);
                                        eft.End = wlt.Time;
                                        eft.AverageSpeed = eft.SpeedAtWaypoints.Average();

                                        if (eft.AverageSpeed > 12 &&
                                            eft.LengthSimplified >= Global.Settings.GearRetrievingMinLength &&
                                            eft.LengthOriginal < 3000)
                                        {
                                            if (BSCBoundaryLine == null)
                                            {
                                                _gearHaulExtractedTracks.Add(eft);
                                            }
                                            else if (!segment.Crosses(BSCBoundaryLine))
                                            {
                                                _gearHaulExtractedTracks.Add(eft);
                                            }
                                        }

                                        //if (eft.AverageSpeed > 12 &&
                                        //    eft.LengthSimplified >= Global.Settings.GearRetrievingMinLength &&
                                        //    eft.LengthOriginal < 3000 &&
                                        //    !segment.Crosses(BSCBoundaryLine))
                                        //{
                                        //    _gearHaulExtractedTracks.Add(eft);
                                        //}
                                        // }

                                        //if (PolylineSelfCrossingsCount(segment, MaxSelfCrossings) <= MaxSelfCrossings)
                                        //{
                                        //    eft.Track = segment;
                                        //    if (segment.numPoints > 20)
                                        //    {


                                        //        eft.Length = accumulatedDistance;
                                        //        eft.End = wlt.Time;
                                        //        eft.AverageSpeed = eft.SpeedAtWaypoints.Average();
                                        //        _gearHaulExtractedTracks.Add(eft);
                                        //    }
                                        //}
                                    }
                                    else
                                    {
                                        //reset and start a new segment
                                    }



                                    ptIndex = 0;
                                    accumulatedDistance = 0;
                                    segment = new Shape();
                                    segment.Create(ShpfileType.SHP_POLYLINE);
                                    eft = new ExtractedFishingTrack();

                                    //accumulatedDistance = 0;
                                    //segment = new Shape();
                                    //segment.Create(ShpfileType.SHP_POLYLINE);
                                    //eft = new ExtractedFishingTrack();
                                }
                            }

                            _timeBefore = wlt.Time;
                            ptBefore = new Waypoint { Longitude = wlt.Longitude, Latitude = wlt.Latitude, Time = _timeBefore };
                            if (ptIndex == 0)
                            {
                                segment = new Shape();
                                segment.Create(ShpfileType.SHP_POLYLINE);
                            }
                        }
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