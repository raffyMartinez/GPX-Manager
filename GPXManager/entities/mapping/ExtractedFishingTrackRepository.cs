using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.OleDb;

namespace GPXManager.entities.mapping
{
    public class ExtractedFishingTrackRepository
    {
        public List<ExtractedFishingTrack> ExtractedFishingTracks { get; set; }
        public ExtractedFishingTrackRepository()
        {
            ExtractedFishingTracks = GetTracks(removeDuplicate:true);
        }


        private List<ExtractedFishingTrack> GetTracks(bool removeDuplicate = false)
        {
            var list = new List<ExtractedFishingTrack>();
            var dt = new DataTable();
            using (var conection = new OleDbConnection(Global.ConnectionString))
            {
                try
                {
                    conection.Open();
                    string query = "Select * from extractedFishingTracks";
                    if(removeDuplicate)
                    {
                        //query = @"SELECT 
                        //            First(extractedFishingTracks.ID) AS ID,
                        //            extractedFishingTracks.DeviceName,
                        //            First(extractedFishingTracks.DateAdded) AS DateAdded,
                        //            First(extractedFishingTracks.DateStart) AS DateStart,
                        //            extractedFishingTracks.DateEnd,
                        //            Count(extractedFishingTracks.ID) AS DuplicateCount,
                        //            extractedFishingTracks.SourceType,
                        //            First(extractedFishingTracks.SourceID) AS SourceID,
                        //            First(extractedFishingTracks.SerializedTrack) AS SerializedTrack,
                        //            First(extractedFishingTracks.SerializedTrackUTM) AS SerializedTrackUTM,
                        //            First(extractedFishingTracks.LengthOriginal) AS LengthOriginal,
                        //            First(extractedFishingTracks.LenghtSimplified) AS LenghtSimplified,
                        //            First(extractedFishingTracks.AverageSpeed) AS AverageSpeed,
                        //            First(extractedFishingTracks.PointCountOriginal) AS PointCountOriginal,
                        //            First(extractedFishingTracks.PointCountSimplified) AS PointCountSimplified,
                        //            First(extractedFishingTracks.CombinedTrack) AS CombinedTrack
                        //            FROM extractedFishingTracks
                        //            GROUP BY 
                        //                extractedFishingTracks.DeviceName, 
                        //                extractedFishingTracks.DateEnd, 
                        //                extractedFishingTracks.SourceType
                        //            ORDER BY 
                        //                extractedFishingTracks.SourceType, 
                        //                First(extractedFishingTracks.SourceID)";

                        query = @"SELECT 
                                    First(ID) AS ID,
                                    DeviceName,
                                    First(DateAdded) AS DateAdded,
                                    First(DateStart) AS DateStart,
                                    DateEnd,
                                    Count(ID) AS DuplicateCount,
                                    SourceType,
                                    First(SourceID) AS SourceID,
                                    First(SerializedTrack) AS SerializedTrack,
                                    First(SerializedTrackUTM) AS SerializedTrackUTM,
                                    First(LengthOriginal) AS LengthOriginal,
                                    First(LenghtSimplified) AS LenghtSimplified,
                                    First(AverageSpeed) AS AverageSpeed,
                                    First(PointCountOriginal) AS PointCountOriginal,
                                    First(PointCountSimplified) AS PointCountSimplified,
                                    First(CombinedTrack) AS CombinedTrack
                                    FROM extractedFishingTracks
                                    GROUP BY 
                                        DeviceName, 
                                        DateEnd, 
                                        SourceType
                                    ORDER BY 
                                        SourceType, 
                                        First(SourceID)";
                    }

                    var adapter = new OleDbDataAdapter(query, conection);
                    adapter.Fill(dt);
                    if (dt.Rows.Count > 0)
                    {
                        list.Clear();
                        foreach (DataRow dr in dt.Rows)
                        {
                            ExtractedFishingTrack eft = new ExtractedFishingTrack();
                            eft.ID = int.Parse(dr["ID"].ToString());
                            eft.DateAdded = (DateTime)dr["DateAdded"];
                            eft.DeviceName = dr["DeviceName"].ToString();
                            eft.Start = (DateTime)dr["DateStart"];
                            eft.End = (DateTime)dr["DateEnd"];
                            eft.TrackSourceType = (ExtractedTrackSourceType)Enum.Parse(typeof(ExtractedTrackSourceType), dr["SourceType"].ToString());
                            eft.TrackSourceID = (int)dr["SourceID"];
                            eft.LengthOriginal = (double)dr["LengthOriginal"];
                            eft.LengthSimplified = (double)dr["LenghtSimplified"];
                            eft.AverageSpeed = (double)dr["AverageSpeed"];
                            eft.SerializedTrack = dr["SerializedTrack"].ToString();
                            eft.SerializedTrackUTM = dr["SerializedTrackUTM"].ToString();
                            eft.TrackPointCountOriginal = (int)dr["PointCountOriginal"];
                            eft.TrackPointCountSimplified = (int)dr["PointCountSimplified"];
                            eft.CombinedTrack = dr["CombinedTrack"].ToString() != "0";

                            if(eft.TrackSourceType == ExtractedTrackSourceType.TrackSourceTypeCTX)
                            {
                                var ctxfile = Entities.CTXFileViewModel.GetFile(eft.TrackSourceID);
                                eft.LandingSite = ctxfile.LandingSite;
                                eft.Gear = ctxfile.Gear;
                            }
                            list.Add(eft);
                        }

                        
                    }
                }
                catch (OleDbException dbex)
                {
                    switch (dbex.ErrorCode)
                    {
                        case -2147217865:
                            //table not found
                           if( CreateTable())
                            {
                                return GetTracks();
                            }
                            break;
                        case -2147217904:
                            //No value given for one or more required parameters.
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);

                }
            }
            return list;
        }

        public bool Update(ExtractedFishingTrack f)
        {
            return true;
        }

        public bool Delete(int id)
        {
            bool success = false;
            using (OleDbConnection conn = new OleDbConnection(Global.ConnectionString))
            {
                conn.Open();
                var sql = $"Delete * from extractedFishingTracks where ID={id}";
                using (OleDbCommand update = new OleDbCommand(sql, conn))
                {
                    try
                    {
                        success = update.ExecuteNonQuery() > 0;
                    }
                    catch (OleDbException)
                    {
                        success = false;
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex);
                        success = false;
                    }
                }
            }
            return success;
        }
        public int MaxRecordNumber()
        {
            int max_rec_no = 0;
            using (OleDbConnection conn = new OleDbConnection(Global.ConnectionString))
            {
                conn.Open();
                const string sql = "SELECT Max(ID) AS max_id FROM extractedFishingTracks";
                using (OleDbCommand getMax = new OleDbCommand(sql, conn))
                {
                    max_rec_no = (int)getMax.ExecuteScalar();
                }
            }
            return max_rec_no;
        }

        private bool CreateTable()
        {
            using (var conn = new OleDbConnection(Global.ConnectionString))
            {
                conn.Open();
                string sql = @"CREATE TABLE extractedFishingTracks 
                             (
                                ID Int NOT NULL PRIMARY KEY, 
                                DeviceName VarChar,
                                DateAdded DateTime, 
                                DateStart DateTime, 
                                DateEnd DateTime, 
                                SourceType Int ,
                                SourceID Int, 
                                SerializedTrack LongText, 
                                SerializedTrackUTM LongText,
                                LengthOriginal Double, 
                                LenghtSimplified Double, 
                                AverageSpeed Double,
                                PointCountOriginal Int, 
                                PointCountSimplified Int,
                                CombinedTrack Bit
                             )";
                OleDbCommand cmd = new OleDbCommand();
                cmd.Connection = conn;
                cmd.CommandText = sql;

                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch (OleDbException dbex)
                {
                    //ignore
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                    return false;
                }

                cmd.Connection.Close();
                conn.Close();
            }
            return true;
        }

        public bool Add(ExtractedFishingTrack eft)
        {
            bool success = false;
            using (OleDbConnection conn = new OleDbConnection(Global.ConnectionString))
            {
                conn.Open();
                var sql = $@"Insert into extractedFishingTracks 
                            (ID, DeviceName, DateAdded, DateStart, DateEnd, SourceType,
                            SourceID, SerializedTrack, LengthOriginal, LenghtSimplified, AverageSpeed,
                            PointCountOriginal, PointCountSimplified, SerializedTrackUTM, CombinedTrack)
                            Values (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

                using (OleDbCommand update = new OleDbCommand(sql, conn))
                {
                    update.Parameters.Add("@rowID", OleDbType.Integer).Value = eft.ID;
                    update.Parameters.Add("@deviceName", OleDbType.VarChar).Value = eft.DeviceName;
                    update.Parameters.Add("@dateAdded", OleDbType.Date).Value = eft.DateAdded;
                    update.Parameters.Add("@dateStart", OleDbType.Date).Value = eft.Start;
                    update.Parameters.Add("@dateEnd", OleDbType.Date).Value = eft.End;
                    update.Parameters.Add("@sourceType", OleDbType.Integer).Value = (int)eft.TrackSourceType;
                    update.Parameters.Add("@sourceID", OleDbType.Integer).Value = eft.TrackSourceID;
                    update.Parameters.Add("@serializedTrack", OleDbType.VarChar).Value = eft.SerializedTrack;
                    update.Parameters.Add("@lengthOriginal", OleDbType.Double).Value = eft.LengthOriginal;
                    update.Parameters.Add("@lengthSimplified", OleDbType.Double).Value = eft.LengthSimplified;
                    update.Parameters.Add("@avaerageSpeed", OleDbType.Double).Value = eft.AverageSpeed;
                    update.Parameters.Add("@pointCountOriginal", OleDbType.Integer).Value = eft.TrackPointCountOriginal;
                    update.Parameters.Add("@pointCountSimplified", OleDbType.Integer).Value = eft.TrackPointCountSimplified;
                    update.Parameters.Add("@serializedUTM", OleDbType.VarWChar).Value = eft.SerializedTrackUTM;
                    update.Parameters.Add("@combined", OleDbType.Boolean).Value = eft.CombinedTrack;
                    try
                    {
                        success = update.ExecuteNonQuery() > 0;
                    }
                    catch (OleDbException dbex)
                    {

                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex);
                    }
                }
            }
            return success;
        }
    }
}
