using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.OleDb;

namespace GPXManager.entities
{
    public class FisherRepository
    {
        public List<Fisher> Fishers { get; private set; }

        public FisherRepository()
        {
            Fishers = getFishers();
        }
        public int MaxRecordNumber()
        {
            int max_rec_no = 0;
            using (OleDbConnection conn = new OleDbConnection(Global.ConnectionString))
            {
                conn.Open();
                const string sql = "SELECT Max(FisherID) AS max_record_no FROM fishers";
                using (OleDbCommand getMax = new OleDbCommand(sql, conn))
                {
                    max_rec_no = (int)getMax.ExecuteScalar();
                }
            }
            return max_rec_no;
        }
        private List<Fisher> getFishers()
        {
            var thisList = new List<Fisher>();
            var dt = new DataTable();
            using (var conection = new OleDbConnection(Global.ConnectionString))
            {
                try
                {
                    conection.Open();
                    string query = $"Select * from fishers";


                    var adapter = new OleDbDataAdapter(query, conection);
                    adapter.Fill(dt);
                    if (dt.Rows.Count > 0)
                    {
                        thisList.Clear();
                        foreach (DataRow dr in dt.Rows)
                        {
                            Fisher item = new Fisher();
                            item.FisherID = int.Parse(dr["FisherID"].ToString());
                            item.Name = dr["FisherName"].ToString();
                            item.Vessels = dr["Boats"].ToString().Split('|').ToList();
                            thisList.Add(item);
                        }
                    }
                }
                catch (OleDbException dbex)
                {
                    if (dbex.ErrorCode == -2147217865)
                    {
                        //table not found so we create one
                        CreateTable();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                }
            }

            return thisList;
        }

        public bool Add(Fisher fisher)
        {
            bool success = false;
            using (OleDbConnection conn = new OleDbConnection(Global.ConnectionString))
            {
                conn.Open();
                var sql = "";

                sql = $@"Insert into fishers(FisherID, FisherName, Boats, DateAdded)
                        Values (
                         {fisher.FisherID},  
                        '{fisher.Name}',
                        '{fisher.VesselList}',
                        '{DateTime.Now.ToString("dd-MMMM-yyyyy HH:mm:ss")}'
                        )";


                using (OleDbCommand update = new OleDbCommand(sql, conn))
                {
                    success = update.ExecuteNonQuery() > 0;
                }
            }
            return success;
        }

        public bool Update(Fisher fisher)
        {
            bool success = false;
            using (OleDbConnection conn = new OleDbConnection(Global.ConnectionString))
            {
                conn.Open();
                var sql = $@"Update fishers set
                            FisherName = '{fisher.Name}',
                            Boats ='{fisher.VesselList}'
                            WHERE FisherID = {fisher.FisherID}";
                using (OleDbCommand update = new OleDbCommand(sql, conn))
                {
                    success = update.ExecuteNonQuery() > 0;
                }
            }
            return success;

        }

        public bool Delete(Fisher fisher)
        {
            bool success = false;
            using (OleDbConnection conn = new OleDbConnection(Global.ConnectionString))
            {
                conn.Open();
                var sql = $"Delete * from fishers where FisherID={fisher.FisherID}";
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

        public static void CreateTable()
        {
            using (var conn = new OleDbConnection(Global.ConnectionString))
            {
                conn.Open();
                string sql = @"CREATE TABLE fishers 
                                (
                                FisherID Int NOT NULL PRIMARY KEY,
                                FisherName VarChar,
                                Boats Memo,
                                DateAdded DateTime
                                )";
                OleDbCommand cmd = new OleDbCommand();
                cmd.Connection = conn;
                cmd.CommandText = sql;

                try
                {
                    cmd.ExecuteNonQuery();

                    sql = "ALTER TABLE trips ALTER COLUMN NameOfOperator INT";
                    cmd.CommandText = sql;
                    cmd.ExecuteNonQuery();

                    sql = "ALTER TABLE trips ADD CONSTRAINT fisherID_FK FOREIGN KEY (NameOfOperator) REFERENCES fishers(FisherID)";
                    cmd.CommandText = sql;
                    cmd.ExecuteNonQuery();
                }
                catch(OleDbException)
                {
                    //ignore
                }
                catch(Exception ex)
                {
                    Logger.Log(ex);
                }

                cmd.Connection.Close();
                conn.Close();
            }
        }
    }
}
