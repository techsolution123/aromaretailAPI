using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net.Mime;
using System.Text;

namespace AromaRetail_API.Classes
{
    public static class sql
    {

        private static SqlConnection MySqlConnection()
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
            builder.DataSource = "aroma-retail.cnefc3yocqek.ap-southeast-2.rds.amazonaws.com";
            builder.UserID = "admin";
            builder.Password = "!Welcome007";
            builder.InitialCatalog = "AromaRetail";
            //builder.DataSource = "127.0.0.1";
            //builder.UserID = "ARDBUser";
            //builder.Password = "XPkjV89ZAbqO";
            //builder.InitialCatalog = "AromaRetail";
            SqlConnection connection = new SqlConnection(builder.ConnectionString);
            return connection;
        }

        public static void UpdateLastSeen(string sSerialNum)
        {
            try
            {
                SqlConnection myConn = MySqlConnection();
                myConn.Open();
                string sSQL = 
                    "UPDATE Devices SET LastSeen='" + DateTime.UtcNow.ToString("MM/dd/yyyy HH:mm:ss") + "' " +
                    "WHERE SerialNumber='" + sSerialNum.Replace("'", "''") + "'";
                SqlCommand command = new SqlCommand(sSQL, myConn);
                SqlDataReader reader = command.ExecuteReader();
                myConn.Close();
            }
            catch (Exception ex)
            {
            }
        }

        public static void UpdateMetrics(string sSerialNum, string sRevision, string sSecsUse)
        {
            // update secs use
            try
            {
                int iSecsUse = 0;
                int.TryParse(sSecsUse, out iSecsUse);
                int iTotalSecsUse = (int)GetSingleFieldContents("Devices", "SecondsOfUse", "WHERE SerialNumber='" + sSerialNum.Replace("'", "''") + "'");
                SqlConnection myConn = MySqlConnection();
                myConn.Open();
                string sSQL =
                    "UPDATE Devices SET Revision ='" + sRevision + "', " +
                    "SecondsOfUse = " + (iSecsUse + iTotalSecsUse) + 
                    "WHERE SerialNumber='" + sSerialNum.Replace("'", "''") + "'";
                SqlCommand command = new SqlCommand(sSQL, myConn);
                SqlDataReader reader = command.ExecuteReader();
                myConn.Close();
            }
            catch (Exception ex)
            {
            }
        }

        public static void InsertStatus(string sAPICall, string sSerialNum, string sData)
        {
            try
            {
                UpdateLastSeen(sSerialNum);
                SqlConnection myConn = MySqlConnection();
                myConn.Open();
                string sSQL = "INSERT INTO TestStatus (APICall, SerialNumber, Data) VALUES ('" + sAPICall + "','" + sSerialNum + "','" + sData + "')";
                SqlCommand command = new SqlCommand(sSQL, myConn);
                SqlDataReader reader = command.ExecuteReader();
                myConn.Close();
            }
            catch (Exception ex)
            {
            }

        }

        public static DataTable GetDataTable(string sTableName, string sSQL)
        {
            var oAdapter = new SqlDataAdapter();
            var myConn = MySqlConnection();
            myConn.Open();

            oAdapter.TableMappings.Add("Table", sTableName);
            var oCommand = new SqlCommand(sSQL, myConn) { CommandType = CommandType.Text };
            oAdapter.SelectCommand = oCommand;

            //Fill the dataset
            var oDataset = new DataSet(sTableName);
            oAdapter.Fill(oDataset);

            var oDatatable = oDataset.Tables[sTableName];
            myConn.Close();

            return oDatatable;
        }

        public static object GetSingleFieldContents(string sTableName, string sFieldName, string sWhereClause)
        {
            var myConn = MySqlConnection();
            myConn.Open();
            var myCmd = new SqlCommand
            {
                CommandTimeout = 600,
                Connection = myConn,
                CommandText = "SELECT " + sFieldName + " FROM " + sTableName + " " + sWhereClause
            };
            var myReader = myCmd.ExecuteReader();
            var sResult = myReader.Read() ? myReader[sFieldName] : null;

            myReader.Close();
            myConn.Close();
            return sResult;
        }

        public static object GetSingleFieldContents(string sTableName, string parameters, string sFieldName, string sWhereClause)
        {
            var myConn = MySqlConnection();
            myConn.Open();
            var myCmd = new SqlCommand
            {
                CommandTimeout = 600,
                Connection = myConn,
                CommandText = "SELECT " + parameters + " FROM " + sTableName + " " + sWhereClause
            };
            var myReader = myCmd.ExecuteReader();
            var sResult = myReader.Read() ? myReader[sFieldName] : null;

            myReader.Close();
            myConn.Close();
            return sResult;
        }

        public static void DoSqlCommand(string sSQL)
        {
            var myConn = MySqlConnection();
            myConn.Open();
            var myCmd = new SqlCommand { CommandTimeout = 600, Connection = myConn, CommandText = sSQL };
            myCmd.ExecuteNonQuery();
            myConn.Close();
        }

        public static object DoSQLCommandAndReturnObject(string sSQL, string sFieldName)
        {
            var myConn = MySqlConnection();
            object oResult = null;
            try
            {
                SqlCommand myCmd = null;
                SqlDataReader myReader = null;
                myConn.Open();
                myCmd = new SqlCommand();
                myCmd.CommandTimeout = 600;
                myCmd.Connection = myConn;
                myCmd.CommandText = sSQL;
                myReader = myCmd.ExecuteReader();
                if (myReader.Read())
                {
                    oResult = (object)myReader[sFieldName];
                }
                else
                {
                    oResult = null;
                }
                myReader.Close();
                myConn.Close();
            }
            catch (Exception)
            {
                oResult = null;
            }
            return oResult;
        }

        public static int GetRecordCount(bool bDistinct, string TableName, string WhereClause)
        {
            int iCount = 0;
            try
            {
                SqlConnection myConn = MySqlConnection();
                myConn.Open();
                SqlCommand myCmd = new SqlCommand()
                {
                    CommandTimeout = 600,
                    Connection = myConn,
                    CommandText = "Select " + (bDistinct ? "DISTINCT " : "") + "COUNT(*) As NumRecs From " + TableName + " " + WhereClause
                };
                SqlDataReader myReader = myCmd.ExecuteReader();
                iCount = myReader.Read() ? (int)myReader["NumRecs"] : 0;

                myReader.Close();
                myConn.Close();
            }
            catch (Exception ex)
            {
                string s = ex.Message;
            }
            return iCount;
        }
    }
}
