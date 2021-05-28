using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using MapWinGIS;
using System.Xml;

namespace GPXManager.entities.mapping
{
    public class ExtractedFishingTrackViewModel
    {
        private int _counter;
        private int _idCounter;
        public delegate void TrackExtractedFromSource(ExtractedFishingTrackViewModel s, ExtractTrackEventArgs e);
        public event TrackExtractedFromSource TrackExtractedFromSourceCreated;

        private bool _editSuccess;
        public ObservableCollection<ExtractedFishingTrack> ExtractedFishingTrackCollection { get; set; }
        private ExtractedFishingTrackRepository ExtractedFishingTracks { get; set; }

        public async Task<List<ExtractedFishingTrack>> ExtractTracksFromSourcesAsync(bool save = false, bool makeShapefile = false)
        {
            return await Task.Run(() => ExtractTracksFromSources(save, makeShapefile));
        }


        public List<ExtractedFishingTrack> AllExtractedFishingTracks { get; private set; }
        private List<ExtractedFishingTrack> CreateFromSource(CTXFile ctx = null, DeviceGPX gpx = null, bool save = false)
        {
            TripAndHauls result = null;
            List<ExtractedFishingTrack> list = new List<ExtractedFishingTrack>();
            if (ctx == null && gpx == null)
                throw new ArgumentNullException("Error: Source is not specified");

            int id = 0;
            var deviceName = "";
            bool procced = false;
            ExtractedTrackSourceType sourceType = ExtractedTrackSourceType.TrackSourceTypeCTX;
            if (ctx != null)
            {
                if (ctx.XML.Length == 0)
                    return null;
                else
                {
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(ctx.XML);
                    var tracknodes = doc.SelectNodes("//T");
                    if (ctx.TrackingInterval == null)
                    {
                        ctx.TrackingInterval = Entities.CTXFileViewModel.GetGPSTimerIntervalFromCTX(ctx, true);
                    }
                    result = ShapefileFactory.TrackShapeFromCTX(tracknodes, ctx.TrackingInterval);

                    procced = result != null && result.Tracks.Where(T => T.Accept).ToList().Count > 0;
                    if (procced)
                    {
                        id = ctx.RowID;
                        deviceName = Entities.CTXFileViewModel.GetFile(id).UserName;
                    }
                }
            }
            else
            {
                sourceType = ExtractedTrackSourceType.TrackSourceTypeGPX;
                var gpxFile = Entities.GPXFileViewModel.ConvertToGPXFile(gpx);
                id = gpx.RowID;
                if (gpxFile.GPSTimerInterval == null)
                {
                    gpxFile.GPSTimerInterval = Entities.DeviceGPXViewModel.GetDeviceGPX(id).TimerInterval;
                }
                result = ShapefileFactory.TrackShapeFromGPX(gpxFile, gpxFile.GPSTimerInterval);
                procced = result != null && result.Tracks.Where(T => T.Accept).ToList().Count > 0;
                if (procced)
                {
                    deviceName = Entities.DeviceGPXViewModel.GetDeviceGPX(id).GPS.DeviceName;
                }
            }

            if (procced)
            {
                foreach (var item in result.Tracks.Where(t => t.Accept))
                {
                    ExtractedFishingTrack extractedTrack = new ExtractedFishingTrack
                    {
                        DateAdded = DateTime.Now,
                        TrackSourceType = sourceType,
                        TrackSourceID = id,
                        Start = item.ExtractedFishingTrack.Start,
                        End = item.ExtractedFishingTrack.End,
                        LengthOriginal = item.ExtractedFishingTrack.LengthOriginal,
                        LengthSimplified = item.ExtractedFishingTrack.LengthSimplified,
                        TrackPointCountOriginal = item.ExtractedFishingTrack.TrackPointCountOriginal,
                        TrackPointCountSimplified = item.ExtractedFishingTrack.TrackPointCountSimplified,
                        AverageSpeed = item.ExtractedFishingTrack.AverageSpeed,
                        TrackOriginal = item.ExtractedFishingTrack.TrackOriginal,
                        SegmentSimplified = item.ExtractedFishingTrack.SegmentSimplified,
                        SerializedTrack = item.ExtractedFishingTrack.SegmentSimplified.SerializeToString(),
                        DeviceName = deviceName
                    };
                    if (save)
                    {
                        extractedTrack.ID = NextRecordNumber;
                    }
                    else
                    {
                        extractedTrack.ID = ++_idCounter;
                    }
                    list.Add(extractedTrack);
                }
            }
            return list;
        }
        private List<ExtractedFishingTrack> CreateFromSourceEx(CTXFile ctx = null, DeviceGPX gpx = null, bool save = false)
        {
            List<ExtractedFishingTrack> list = new List<ExtractedFishingTrack>();
            if (ctx == null && gpx == null)
                throw new ArgumentNullException("Error: Source is not specified");

            int id = 0;
            var deviceName = "";
            bool procced = false;
            ExtractedTrackSourceType sourceType = ExtractedTrackSourceType.TrackSourceTypeCTX;
            if (ctx != null)
            {
                //if (ShapefileFactory.TrackShapeFromCTX(new CTXFileSummaryView(ctx)))
                var tripAndHaul = ShapefileFactory.CreateTripAndHaulsFromCTX(new CTXFileSummaryView(ctx));
                if (ShapefileFactory.TrackShapeFromCTX(ctx))
                {
                    id = ctx.RowID;
                    deviceName = Entities.CTXFileViewModel.GetFile(id).UserName;
                    procced = true;
                }
            }
            else
            {
                sourceType = ExtractedTrackSourceType.TrackSourceTypeGPX;
                var gpxFile = Entities.GPXFileViewModel.ConvertToGPXFile(gpx);
                if (ShapefileFactory.TrackShapeFromGPX(gpxFile))
                {
                    //ShapefileFactory.TrackFromGPX(gpxFile, out handles);
                    id = gpx.RowID;
                    deviceName = Entities.DeviceGPXViewModel.GetDeviceGPX(id).GPS.DeviceName;
                    procced = true;
                }
            }
            if (procced)
            {
                foreach (var eft in ShapefileFactory.ExtractedFishingTracks())
                {
                    ExtractedFishingTrack extractedTrack = new ExtractedFishingTrack
                    {
                        DateAdded = DateTime.Now,
                        TrackSourceType = sourceType,
                        TrackSourceID = id,
                        Start = eft.Start,
                        End = eft.End,
                        LengthOriginal = eft.LengthOriginal,
                        LengthSimplified = eft.LengthSimplified,
                        TrackPointCountOriginal = eft.TrackPointCountOriginal,
                        TrackPointCountSimplified = eft.TrackPointCountSimplified,
                        AverageSpeed = eft.AverageSpeed,
                        TrackOriginal = eft.TrackOriginal,
                        SegmentSimplified = eft.SegmentSimplified,
                        SerializedTrack = eft.SegmentSimplified.SerializeToString(),
                        DeviceName = deviceName
                    };
                    if (save)
                    {
                        extractedTrack.ID = NextRecordNumber;
                    }
                    else
                    {
                        extractedTrack.ID = ++_idCounter;
                    }
                    list.Add(extractedTrack);
                }
            }
            return list;
        }

        private async Task<List<ExtractedFishingTrack>> ExtractTracksFromSources(bool save = false, bool makeShapefile = false)
        {
            _counter = 0;
            var list = new List<ExtractedFishingTrack>();
            var allCTXFiles = await Entities.CTXFileViewModel.GetAllAsync(true);
            foreach (CTXFile cf in allCTXFiles)
            {
                var list1 = CombineExtractedTracks(CreateFromSource(ctx: cf, save: save));
                if (list1 != null && list1.Count > 0)
                {
                    list.AddRange(list1);
                }
                //list.AddRange(CombineExtractedTracks(CreateFromSource(ctx: cf, save: save)));
            }

            foreach (DeviceGPX gpx in Entities.DeviceGPXViewModel.GetAllDeviceWaypointGPX())
            {
                if (gpx.GPXType == "track")
                {
                    var list2 = CombineExtractedTracks(CreateFromSource(gpx: gpx, save: save));
                    if (list2 != null && list2.Count > 0)
                    {
                        list.AddRange(list2);
                    }

                }

            }

            if (save)
            {
                foreach (var item in list)
                {
                    AddRecordToRepo(item);
                }
            }

            if (makeShapefile)
            {
                ExtractedFishingTracksSF = new Shapefile();
                if (ExtractedFishingTracksSF.CreateNewWithShapeID("", ShpfileType.SHP_POLYLINE))
                {
                    ExtractedFishingTracksSF.GeoProjection = globalMapping.GeoProjection;
                    ExtractedFishingTracksSF.Key = "extracted_tracks";
                    ExtractedFishingTracksSF.EditAddField("ID", FieldType.INTEGER_FIELD, 1, 1);
                    ExtractedFishingTracksSF.EditAddField("DeviceName", FieldType.STRING_FIELD, 1, 1);
                    ExtractedFishingTracksSF.EditAddField("DateAdded", FieldType.DATE_FIELD, 1, 1);
                    ExtractedFishingTracksSF.EditAddField("SourceType", FieldType.STRING_FIELD, 1, 1);
                    ExtractedFishingTracksSF.EditAddField("SourceID", FieldType.INTEGER_FIELD, 1, 1);
                    ExtractedFishingTracksSF.EditAddField("Start", FieldType.DATE_FIELD, 1, 1);
                    ExtractedFishingTracksSF.EditAddField("End", FieldType.DATE_FIELD, 1, 1);
                    ExtractedFishingTracksSF.EditAddField("LenOriginal", FieldType.DOUBLE_FIELD, 1, 1);
                    ExtractedFishingTracksSF.EditAddField("LenSimplified", FieldType.DOUBLE_FIELD, 1, 1);
                    ExtractedFishingTracksSF.EditAddField("TrckPtsOriginal", FieldType.INTEGER_FIELD, 1, 1);
                    ExtractedFishingTracksSF.EditAddField("TrckPtsSimplified", FieldType.INTEGER_FIELD, 1, 1);
                    ExtractedFishingTracksSF.EditAddField("AvgSpeed", FieldType.DOUBLE_FIELD, 1, 1);

                    foreach (var item in list)
                    {
                        var idx = ExtractedFishingTracksSF.EditAddShape(item.SegmentSimplified);
                        if (idx >= 0)
                        {
                            ExtractedFishingTracksSF.EditCellValue(ExtractedFishingTracksSF.FieldIndexByName["ID"], idx, item.ID);
                            ExtractedFishingTracksSF.EditCellValue(ExtractedFishingTracksSF.FieldIndexByName["DateAdded"], idx, item.DateAdded);
                            if (item.TrackSourceType == ExtractedTrackSourceType.TrackSourceTypeCTX)
                            {
                                ExtractedFishingTracksSF.EditCellValue(ExtractedFishingTracksSF.FieldIndexByName["SourceType"], idx, "CTX");
                            }
                            else
                            {
                                ExtractedFishingTracksSF.EditCellValue(ExtractedFishingTracksSF.FieldIndexByName["SourceType"], idx, "GPX");
                            }
                            ExtractedFishingTracksSF.EditCellValue(ExtractedFishingTracksSF.FieldIndexByName["SourceID"], idx, item.TrackSourceID);
                            ExtractedFishingTracksSF.EditCellValue(ExtractedFishingTracksSF.FieldIndexByName["DeviceName"], idx, item.DeviceName);
                            ExtractedFishingTracksSF.EditCellValue(ExtractedFishingTracksSF.FieldIndexByName["Start"], idx, item.Start);
                            ExtractedFishingTracksSF.EditCellValue(ExtractedFishingTracksSF.FieldIndexByName["End"], idx, item.End);
                            ExtractedFishingTracksSF.EditCellValue(ExtractedFishingTracksSF.FieldIndexByName["LenOriginal"], idx, item.LengthOriginal);
                            ExtractedFishingTracksSF.EditCellValue(ExtractedFishingTracksSF.FieldIndexByName["LenSimplified"], idx, item.LengthSimplified);
                            ExtractedFishingTracksSF.EditCellValue(ExtractedFishingTracksSF.FieldIndexByName["TrckPtsOriginal"], idx, item.TrackPointCountOriginal);
                            ExtractedFishingTracksSF.EditCellValue(ExtractedFishingTracksSF.FieldIndexByName["TrckPtsSimplified"], idx, item.TrackPointCountSimplified);
                            ExtractedFishingTracksSF.EditCellValue(ExtractedFishingTracksSF.FieldIndexByName["AvgSpeed"], idx, item.AverageSpeed);
                        }
                    }
                }
            }
            AllExtractedFishingTracks = list;
            return list;
        }

        private List<ExtractedFishingTrack> CombineExtractedTracks(List<ExtractedFishingTrack> efts)
        {
            var list = new List<ExtractedFishingTrack>();
            if (efts != null)
            {
                foreach (var item in efts)
                {
                    list.Add(item);
                    if (TrackExtractedFromSourceCreated != null)
                    {
                        ExtractTrackEventArgs e = new ExtractTrackEventArgs
                        {
                            ExtractedFishingTrack = item,
                            Counter = ++_counter
                        };
                        TrackExtractedFromSourceCreated(this, e);
                    }
                }
                return list;
            }
            return null;
        }

        public Shapefile ExtractedFishingTracksSF { get; private set; }
        public ExtractedFishingTrackViewModel()
        {
            ExtractedFishingTracks = new ExtractedFishingTrackRepository();
            ExtractedFishingTrackCollection = new ObservableCollection<ExtractedFishingTrack>(ExtractedFishingTracks.ExtractedFishingTracks);
            ExtractedFishingTrackCollection.CollectionChanged += ExtractedFishingTrackCollection_CollectionChanged;
        }

        public ExtractedFishingTrack CurrentEntity { get; private set; }
        private void ExtractedFishingTrackCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            _editSuccess = false;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        int newIndex = e.NewStartingIndex;
                        ExtractedFishingTrack newItem = ExtractedFishingTrackCollection[newIndex];
                        if (ExtractedFishingTracks.Add(newItem))
                        {
                            CurrentEntity = newItem;
                            _editSuccess = true;
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    {
                        List<ExtractedFishingTrack> tempListOfRemovedItems = e.OldItems.OfType<ExtractedFishingTrack>().ToList();
                        _editSuccess = ExtractedFishingTracks.Delete(tempListOfRemovedItems[0].ID);
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    {
                        List<ExtractedFishingTrack> tempList = e.NewItems.OfType<ExtractedFishingTrack>().ToList();
                        _editSuccess = ExtractedFishingTracks.Update(tempList[0]);      // As the IDs are unique, only one row will be effected hence first index only
                    }
                    break;
            }
        }

        public int NextRecordNumber
        {
            get
            {
                if (ExtractedFishingTrackCollection.Count == 0)
                {
                    return 1;
                }
                else
                {
                    return ExtractedFishingTracks.MaxRecordNumber() + 1;
                }
            }
        }
        public bool AddRecordToRepo(ExtractedFishingTrack eft)
        {
            if (eft == null)
                throw new ArgumentNullException("Error: The argument is Null");

            ExtractedFishingTrackCollection.Add(eft);
            return _editSuccess;
        }

        public bool UpdateRecordInRepo(ExtractedFishingTrack eft)
        {
            if (eft == null)
                throw new Exception("Error: The argument is Null");

            int index = 0;
            while (index < ExtractedFishingTrackCollection.Count)
            {
                if (ExtractedFishingTrackCollection[index].ID == eft.ID)
                {
                    ExtractedFishingTrackCollection[index] = eft;
                    break;
                }
                index++;
            }
            return _editSuccess;
        }

        public bool DeleteRecordFromRepo(ExtractedFishingTrack eft)
        {
            if (eft == null)
                throw new Exception("Error: The argument is Null");

            int index = 0;
            while (index < ExtractedFishingTrackCollection.Count)
            {
                if (ExtractedFishingTrackCollection[index].ID == eft.ID)
                {
                    ExtractedFishingTrackCollection.RemoveAt(index);
                    break;
                }
                index++;
            }

            return _editSuccess;
        }
    }
}
