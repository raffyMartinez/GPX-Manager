using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.OleDb;

namespace GPXManager.entities
{
    public class FisherDeviceAssignmentRepository
    {
        public List<FisherDeviceAssignment> FisherDeviceAssignments { get; set; }
        public FisherDeviceAssignmentRepository()
        {
            FisherDeviceAssignments = getFisherDeviceAssignments();
        }
        public bool Add(FisherDeviceAssignment fda)
        {
            return true;
        }

        public bool Delete(int id)
        {
            return true;

        }

        public bool Update(FisherDeviceAssignment fda)
        {
            return true;
        }

        private List<FisherDeviceAssignment> getFisherDeviceAssignments()
        {
            List<FisherDeviceAssignment> list = new List<FisherDeviceAssignment>();
            var dt = new DataTable();
            using (var conection = new OleDbConnection(Global.ConnectionString))
            {
                try
                {
                    conection.Open();
                    string query = $"Select * from FisherDeviceAssignment";


                    var adapter = new OleDbDataAdapter(query, conection);
                    adapter.Fill(dt);
                    if (dt.Rows.Count > 0)
                    {
                        list.Clear();
                        foreach (DataRow dr in dt.Rows)
                        {
                            FisherDeviceAssignment fda = new FisherDeviceAssignment();
                            fda.RowID = (int)dr["RowID"];
                            fda.Fisher = Entities.FisherViewModel.GetFisher((int)dr["FisherID"]);
                            fda.DeviceID = dr["DeviceID"].ToString();
                            fda.AssignedDate = (DateTime)dr["DateAssigned"];
                            if (dr["DateReturned"] != null)
                            {
                                fda.RetunDate = (DateTime)dr["DateReturned"];
                            }
                            list.Add(fda);
                        }
                    }
                }
                catch (OleDbException dbex)
                {
                    switch (dbex.ErrorCode)
                    {
                        case -2147217904:
                            //No value given for one or more required parameters.
                            break;
                        case -2147217865:
                            //table not found
                            CreateTable();
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

        private void CreateTable()
        {
            using (var conn = new OleDbConnection(Global.ConnectionString))
            {
                conn.Open();
                string sql = @"CREATE TABLE FisherDeviceAssignment 
                                (
                                RowID Int NOT NULL PRIMARY KEY,
                                FisherID int,
                                DeviceID VarChar,
                                DateAssigned DateTime,
                                DateReturned DateTime
                                )";
                OleDbCommand cmd = new OleDbCommand();
                cmd.Connection = conn;
                cmd.CommandText = sql;

                try
                {
                    cmd.ExecuteNonQuery();


                    sql = "ALTER TABLE FisherDeviceAssignment ADD CONSTRAINT fisherID_FK1 FOREIGN KEY (FisherID) REFERENCES fishers(FisherID)";
                    cmd.CommandText = sql;
                    cmd.ExecuteNonQuery();
                }
                catch (OleDbException)
                {
                    //ignore
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                }

                cmd.Connection.Close();
                conn.Close();
            }
        }
    }
}
