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
            ExtractedFishingTracks = GetTracks();
        }
        private List<ExtractedFishingTrack> GetTracks()
        {
            var list = new List<ExtractedFishingTrack>();
            var dt = new DataTable();
            using (var conection = new OleDbConnection(Global.ConnectionString))
            {
                try
                {
                    conection.Open();
                    string query = $"Select * from extractedFishingTracks";


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
                                PointCountSimplified Int
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
                            PointCountOriginal, PointCountSimplified, SerializedTrackUTM)
                            Values (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

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
