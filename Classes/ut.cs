using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileSystemGlobbing.Internal.PathSegments;
using System;
using System.Data;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using WebAPI.Classes;

namespace AromaRetail_API.Classes
{
    public static class ut
    {
        public const string ResponseBegin = "**BEGIN**\r\n";
        public const string ResponseEnd = "|\r\n**END**\r\n";

        public enum eActionCodes
        {
            eNoOp,
            eOverride,
            eOverrideCancel,
            eEventUpdated,
            eFWUpdate,
            eVacationHold,
            eFriendlyName
        }

        public enum eFanSpeed
        {
            eOff,
            eLow,
            eMedium,
            eHigh,
        }

        public static int ToInt(string sValue)
        {
            int iTemp = 0;
            int.TryParse(sValue, out iTemp);
            return iTemp;
        }

        public static bool ToBool(string sValue)
        {
            bool bTemp = false;
            bool.TryParse(sValue, out bTemp);
            return bTemp;
        }

        public static DateTime ToDateTime(string sValue)
        {
            DateTime dtTemp = new DateTime();
            DateTime.TryParse(sValue, out dtTemp);
            return dtTemp;
        }

        public static string SendOK()
        {
            return ResponseBegin + "OK" + ResponseEnd;

        }

        public static string SendERR()
        {
            return ResponseBegin + "ERR" + ResponseEnd;

        }

        public static Task<string> FW_ReturnEmptyCall(Stream sBody)
        {
            return Task.Run(() => "Aroma Retail");
        }

        public static string FW_ReturnEmptyCall()
        {
            return "Aroma Retail";
        }

        public static async Task<string> FW_UpdateEventByID(Stream sBody)
        {
            using (StreamReader reader = new StreamReader(sBody, Encoding.UTF8))
            {
                string sTemp = "Upload\r\n" + await reader.ReadToEndAsync();
                return sTemp;
            }
        }

        public static int ConvertDT_Weekday_To_STM32_RTC(int iDT_Day_Of_Week)
        {
            return ((((int)iDT_Day_Of_Week + 6) % 7) + 1);
        }
        
        public static string FW_GetUpdateInfo(string sDeviceID)
        {
            //UpdateType 0=Wifi, 1=Onboarding
            //Since this API is dedicated to the FW, use UpdateType of 0.
            string sSQL = "SELECT TOP 1 BinaryData FROM FirmwareRevs WHERE UpdateType=0 ORDER BY ID DESC";
            byte[] abFW = (byte[])sql.DoSQLCommandAndReturnObject(sSQL, "BinaryData");
            ushort CRC = CRC16Checksum.GenerateChecksum(abFW);
            return ResponseBegin +
                    abFW.Length.ToString() + "|" +          // FW update image length in bytes
                    CRC.ToString() +                        // CRC
                    ResponseEnd;
        }

        public static string FW_GetUpdatePacket(string sDeviceID, string sPacketNum)
        {
            const int iPacketSize = 128;
            int iPacketNum = 0;
            int.TryParse(sPacketNum, out iPacketNum);
            string sSQL = "SELECT TOP 1 BinaryData FROM FirmwareRevs WHERE UpdateType=0 ORDER BY ID DESC";
            byte[] abFW = (byte[])sql.DoSQLCommandAndReturnObject(sSQL, "BinaryData");
            string sHexEncodedFile = BitConverter.ToString(abFW).Replace("-", string.Empty);
            string sPacket = "";
            int iNumPackets = sHexEncodedFile.Length / iPacketSize;
            if (iPacketNum < iNumPackets)
            {
                sPacket = sHexEncodedFile.Substring(iPacketNum * iPacketSize, iPacketSize);
            }
            else
            {
                // Pad the file string to a size of packetsize
                int iPigTail = sHexEncodedFile.Length % iPacketSize;
                for (int i = 0; i < iPacketSize - iPigTail; i++)
                {
                    sHexEncodedFile += "FF";
                }
                sPacket = sHexEncodedFile.Substring(iNumPackets * iPacketSize, iPacketSize);
            }

            return ResponseBegin +
                    sPacket +                       
                    ResponseEnd;
        }

        public static string FW_GetStatus(string sDeviceID)
        {
            eActionCodes eCode = eActionCodes.eNoOp;
            DateTime oDt = DateTime.UtcNow.AddHours(-8).AddSeconds(4);  // default
            try
            {
                // Read device table to determine what action code to send
                // Order of priority: VacationHold, Overrides, Events, Updates
                int nID = GetDeviceRowID(sDeviceID);
                bool bEventUpdated = (bool)sql.GetSingleFieldContents("Devices", "EventsUpdated", "WHERE SerialNumber='" + sDeviceID + "'");
                bool bApplyUpdate = (bool)sql.GetSingleFieldContents("Devices", "ApplyUpdateFlag", "WHERE SerialNumber='" + sDeviceID + "'");
                int iTimezoneOffset = (int)sql.GetSingleFieldContents("Devices", "TimeZoneOffset", "WHERE SerialNumber='" + sDeviceID + "'");
                bool bTimezoneDST = (bool)sql.GetSingleFieldContents("Devices", "TimezoneDST", "WHERE SerialNumber='" + sDeviceID + "'");
                iTimezoneOffset = bTimezoneDST ? iTimezoneOffset + 1 : iTimezoneOffset;
                bool bApplyFriendlyName = (bool)sql.GetSingleFieldContents("Devices", "FriendlyNameUpdateFlag", "WHERE SerialNumber='" + sDeviceID + "'");
                oDt = DateTime.UtcNow.AddHours(iTimezoneOffset).AddSeconds(4);
                DataTable oOverrides = sql.GetDataTable("Override", "SELECT TOP 1 ID, CancelFlag FROM Override WHERE DeviceID = " + nID + " ORDER BY ID DESC");

                if (GetVacationHoldInProgress(GetDeviceOwnerID(sDeviceID)))
                {
                    eCode = eActionCodes.eVacationHold;
                }
                else if (oOverrides.Rows.Count > 0)
                {
                    bool bCancelFlag = (bool)oOverrides.Rows[0]["CancelFlag"];
                    eCode = bCancelFlag ? eActionCodes.eOverrideCancel : eActionCodes.eOverride;
                }
                else if (bEventUpdated)
                {
                    eCode = eActionCodes.eEventUpdated;
                }
                else if (bApplyUpdate)
                {
                    eCode = eActionCodes.eFWUpdate;
                }
                else if (bApplyFriendlyName)
                {
                    eCode = eActionCodes.eFriendlyName;
                }

            }
            catch (Exception e)
            {
                return ResponseBegin +
                        "-1|" +                         // OpCode
                        oDt.Hour.ToString("00") + "|" +                             // Current hours in 24hr
                        oDt.Minute.ToString("00") + "|" +                           // Current minutes
                        oDt.Second.ToString("00") + "|" +                           // Current seconds
                        ConvertDT_Weekday_To_STM32_RTC((int)oDt.DayOfWeek) + "|" +  // Day of Week in int format 0=Sunday (convert to STM32 RTC format 1=Monday)
                        "30" +                                                      // Update rate
                        ResponseEnd;
            }
            return ResponseBegin +
                    ((int)eCode).ToString("00") + "|" +                         // OpCode
                    oDt.Hour.ToString("00") + "|" +                             // Current hours in 24hr
                    oDt.Minute.ToString("00") + "|" +                           // Current minutes
                    oDt.Second.ToString("00") + "|" +                           // Current seconds
                    ConvertDT_Weekday_To_STM32_RTC((int)oDt.DayOfWeek) + "|" +  // Day of Week in int format 0=Sunday (convert to STM32 RTC format 1=Monday)
                    "30" +                                                      // Update rate
                    ResponseEnd;
        }

        public static string FW_GetFriendlyName(string sDeviceID)
        {
            if (string.IsNullOrEmpty(sDeviceID)) return ResponseBegin + "AROMA DISPENSER" + ResponseEnd.Replace("|", ""); 
            string sFN;
            try
            {
                int nID = GetDeviceRowID(sDeviceID);
                sFN = sql.GetSingleFieldContents("Devices", "FriendlyName", "WHERE SerialNumber='" + sDeviceID + "'").ToString();

            }
            catch (Exception e)
            {
                return ResponseBegin + "AROMA DISPENSER" + ResponseEnd.Replace("|", "");
            }
            return ResponseBegin + sFN.Trim() + ResponseEnd.Replace("|", "");
        }


        public static bool GetVacationHoldInProgress(int iAccountID)
        {

            string sSQL = "SELECT * FROM VacationHolds WHERE OwnerID=" + iAccountID;
            DataTable oDT = sql.GetDataTable("Vacations", sSQL);
            foreach (DataRow oRow in oDT.Rows)
            {
                DateTime oStart = DateTime.Parse(oRow["StartTS"].ToString());
                DateTime oEnd = DateTime.Parse(oRow["EndTS"].ToString());

                DateTime oNow = DateTime.UtcNow;
                if(oStart <= oNow && oEnd >= oNow)
                {
                    //VH hold has started
                    return true;
                }
                if (oNow > oEnd)
                {
                    sSQL = "DELETE FROM VacationHolds WHERE ID=" + oRow["ID"];
                    sql.DoSqlCommand(sSQL);
                }
            }
            return false;
        }

        public static string FW_GetOverride(string sDeviceID)
        {
            int iTemp = (int)sql.GetSingleFieldContents("Devices", "ID", "WHERE SerialNumber='" + sDeviceID + "'");
            DataTable oDT = sql.GetDataTable("Override", "SELECT TOP 1 * FROM Override WHERE DeviceID = " + iTemp);
            DateTime StartTime = DateTime.Now;
            DateTime EndTime = DateTime.Now.AddMinutes(10);
            int iWork = 10;
            int iPause = 10;
            int iHold = 0;
            eFanSpeed iFanSpeed = eFanSpeed.eLow;

            if (oDT.Rows.Count > 0)
            {
                foreach (DataRow oRow in oDT.Rows)
                {
                    StartTime = ToDateTime(oRow["StartTime"].ToString());
                    EndTime = ToDateTime(oRow["EndTime"].ToString());
                    iWork = ToInt(oRow["WorkInterval"].ToString());
                    iPause = ToInt(oRow["PauseInterval"].ToString());
                    iFanSpeed = (eFanSpeed)ToInt(oRow["FanSpeedEnum"].ToString());
                    var bHold = ToBool(oRow["HoldFlag"].ToString());
                    iHold = bHold ? 1 : 0;
                }
            }
            return ResponseBegin +
                    StartTime.ToString("HH|mm") + "|" +     // Start Hour|Minute
                    EndTime.ToString("HH|mm") + "|" +       // End Hour|Minute
                    iWork.ToString("0") + "|" +             // Work
                    iPause.ToString("0") + "|" +            // Pause
                    (int)iFanSpeed + "|" +                // Fan Speed
                    iHold.ToString("0") + "|" +            // Hold
                    ResponseEnd;
        }

        public static bool FW_SetOverride(string deviceid, DateTime start, DateTime end, int work, int pause, int fan, int hold)
        {
            int iID = GetDeviceRowID(deviceid);
            if (iID > 0)
            {
                // Remove any previous override request
                FW_RemoveOverride(deviceid);
                string sSQL = 
                    "INSERT INTO Override (DeviceID, StartTime, EndTime, WorkInterval, PauseInterval, FanSpeedEnum, HoldFlag) VALUES " +
                    "(" +
                    iID + ", '" +
                    start +"', '" +
                    end + "', " +
                    work + ", " +
                    pause + ", " +
                    fan + ", " +
                    hold + ")";
                sql.DoSqlCommand(sSQL);
            }
            return false;
        }

        public static int GetDeviceRowID(string deviceid)
        {
            int nID = 0;
            // Get a device row ID from device table if it exists, otherwise 0
            object oID = sql.GetSingleFieldContents("Devices", "ID", "WHERE SerialNumber='" + deviceid + "'");
            if (oID != null)
            {
                int.TryParse(oID.ToString(), out nID);
                return nID;
            }
            return nID;
        }

        public static int GetDeviceOwnerID(string deviceid)
        {
            int nID = 0;
            // Get a device row ID from device table if it exists, otherwise 0
            object oID = sql.GetSingleFieldContents("Devices", "OwnerID", "WHERE SerialNumber='" + deviceid + "'");
            if (oID != null)
            {
                int.TryParse(oID.ToString(), out nID);
                return nID;
            }
            return nID;
        }

        public static bool FW_SetOverrideCancel(string deviceid)
        {
            int iID = GetDeviceRowID(deviceid);
            if (iID > 0)
            {
                FW_RemoveOverride(deviceid);
                string sSQL = "INSERT INTO Override (DeviceID, StartTime, EndTime, WorkInterval, PauseInterval, FanSpeedEnum, CancelFlag) VALUES " +
                    "(" + iID + ", '1/1/1990', '1/1/1990', 0, 0, 0, 'true')";
                sql.DoSqlCommand(sSQL);
            }
            return true;
        }

        public static string FW_RemoveOverride(string sDeviceID)
        {
            try
            {
                int iID = GetDeviceRowID(sDeviceID);
                string sSQL = "DELETE FROM Override WHERE DeviceID=" + iID;
                sql.DoSqlCommand(sSQL);
            }
            catch (Exception ex)
            {
                return SendERR();
            }
            return SendOK();

        }

        public static string FW_ClearEventFlag(string sDeviceID)
        {
            try
            {
                int iID = GetDeviceRowID(sDeviceID);
                string sSQL = "UPDATE Devices SET EventsUpdated='false' WHERE ID=" + iID;
                sql.DoSqlCommand(sSQL);
            }
            catch (Exception ex)
            {
                return SendERR();
            }
            return SendOK();
        }

        public static string FW_FriendlyNameUpdateFlagFlag(string sDeviceID)
        {
            try
            {
                int iID = GetDeviceRowID(sDeviceID);
                string sSQL = "UPDATE Devices SET FriendlyNameUpdateFlag='false' WHERE ID=" + iID;
                sql.DoSqlCommand(sSQL);
            }
            catch (Exception ex)
            {
                return SendERR();
            }
            return SendOK();
        }


        public static string FW_ClearApplyUpdateFlag(string sDeviceID)
        {
            try
            {
                int iID = GetDeviceRowID(sDeviceID);
                string sSQL = "UPDATE Devices SET ApplyUpdateFlag='false' WHERE ID=" + iID;
                sql.DoSqlCommand(sSQL);
            }
            catch (Exception ex)
            {
                return SendERR();
            }
            return SendOK();
        }

        public static bool FW_SetEvent(
            string deviceid, 
            int DeviceOrder, 
            DateTime start, 
            DateTime end, 
            int DOWBM, 
            int work, 
            int pause, 
            int fan)
        {
            int iDevID = GetDeviceRowID(deviceid);
            TimeSpan ts = end - start;
            int iDuration = (int)ts.TotalMinutes;
            bool Active = DOWBM != 0;
            if (iDevID > 0)
            {
                // Update the event
                string sSQL =
                    "UPDATE DeviceEvents SET " +
                    "DeviceOrder=" + DeviceOrder + "," +
                    "StartTime='" + start + "'," +
                    "DurationMinutes='" + iDuration + "'," +
                    "DayOfWeekBitMap=" + DOWBM + "," +
                    "WorkSeconds=" + work + "," +
                    "PauseSeconds=" + pause + "," +
                    "FanSpeed=" + fan + "," +
                    "Active='" + Active + "' " +
                    "WHERE DeviceID=" + iDevID + " AND DeviceOrder=" + DeviceOrder;
                sql.DoSqlCommand(sSQL);
            }
            return false;
        }

        public static string FW_GetEventsByID(string sDeviceID)
        {
            int iTemp = (int)sql.GetSingleFieldContents("Devices", "ID", "WHERE SerialNumber='" + sDeviceID + "'");
            if (string.IsNullOrEmpty(sDeviceID))
            {
                return "Device ID can not be null\r\n";
            }


            DataTable oDT = sql.GetDataTable("Events", "SELECT * FROM DeviceEvents WHERE DeviceID = " + iTemp + " ORDER BY DeviceOrder");
            string sReturn = "";
            if (oDT.Rows.Count > 0)
            {

                foreach (DataRow oRow in oDT.Rows)
                {
                    var StartTime = ToDateTime(oRow["StartTime"].ToString());
                    var iDuration = ToInt(oRow["DurationMinutes"].ToString());
                    var iDOWBM = ToInt(oRow["DayOfWeekBitMap"].ToString());
                    var iWork = ToInt(oRow["WorkSeconds"].ToString());
                    var iPause = ToInt(oRow["PauseSeconds"].ToString());
                    var iFanSpeed = (eFanSpeed)ToInt(oRow["FanSpeed"].ToString());
                    var EndTime = StartTime.AddMinutes(iDuration);
                    var ActiveFlag = ToBool(oRow["Active"].ToString());
                    iDOWBM = ActiveFlag ? iDOWBM : 0;

                    // STHR|STMIN|EHR|EMIN|DOWBM|W|P|F|..(4 more sets)
                    sReturn += StartTime.ToString("HH|mm") + "|" +      // Start Hour|Minute
                               EndTime.ToString("HH|mm") + "|" +        // End Hour|Minute
                               iDOWBM.ToString("0") + "|" +             // DOWBM
                               iWork.ToString("0") + "|" +              // Work
                               iPause.ToString("0") + "|" +             // Pause
                               ((int) iFanSpeed) + "|";                 // Fan Speed
                }
            }
            return ResponseBegin + sReturn + ResponseEnd;
        }
    }

    public class DeChunkerMiddleware
    {
        private readonly RequestDelegate _next;

        public DeChunkerMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var originalBodyStream = context.Response.Body;
            using (var responseBody = new MemoryStream())
            {
                context.Response.Body = responseBody;
                long length = 0;
                context.Response.OnStarting(() =>
                {
                    context.Response.Headers.ContentLength = length;
                    return Task.CompletedTask;
                });
                await _next(context);
                //if you want to read the body, uncomment these lines.
                //context.Response.Body.Seek(0, SeekOrigin.Begin);
                //var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
                length = context.Response.Body.Length;
                context.Response.Body.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
            }
        }
    }

    public static class CRC16Checksum
    {
        private static readonly ushort[] crc16tab = {
            0x0000,0x1021,0x2042,0x3063,0x4084,0x50a5,0x60c6,0x70e7,
            0x8108,0x9129,0xa14a,0xb16b,0xc18c,0xd1ad,0xe1ce,0xf1ef,
            0x1231,0x0210,0x3273,0x2252,0x52b5,0x4294,0x72f7,0x62d6,
            0x9339,0x8318,0xb37b,0xa35a,0xd3bd,0xc39c,0xf3ff,0xe3de,
            0x2462,0x3443,0x0420,0x1401,0x64e6,0x74c7,0x44a4,0x5485,
            0xa56a,0xb54b,0x8528,0x9509,0xe5ee,0xf5cf,0xc5ac,0xd58d,
            0x3653,0x2672,0x1611,0x0630,0x76d7,0x66f6,0x5695,0x46b4,
            0xb75b,0xa77a,0x9719,0x8738,0xf7df,0xe7fe,0xd79d,0xc7bc,
            0x48c4,0x58e5,0x6886,0x78a7,0x0840,0x1861,0x2802,0x3823,
            0xc9cc,0xd9ed,0xe98e,0xf9af,0x8948,0x9969,0xa90a,0xb92b,
            0x5af5,0x4ad4,0x7ab7,0x6a96,0x1a71,0x0a50,0x3a33,0x2a12,
            0xdbfd,0xcbdc,0xfbbf,0xeb9e,0x9b79,0x8b58,0xbb3b,0xab1a,
            0x6ca6,0x7c87,0x4ce4,0x5cc5,0x2c22,0x3c03,0x0c60,0x1c41,
            0xedae,0xfd8f,0xcdec,0xddcd,0xad2a,0xbd0b,0x8d68,0x9d49,
            0x7e97,0x6eb6,0x5ed5,0x4ef4,0x3e13,0x2e32,0x1e51,0x0e70,
            0xff9f,0xefbe,0xdfdd,0xcffc,0xbf1b,0xaf3a,0x9f59,0x8f78,
            0x9188,0x81a9,0xb1ca,0xa1eb,0xd10c,0xc12d,0xf14e,0xe16f,
            0x1080,0x00a1,0x30c2,0x20e3,0x5004,0x4025,0x7046,0x6067,
            0x83b9,0x9398,0xa3fb,0xb3da,0xc33d,0xd31c,0xe37f,0xf35e,
            0x02b1,0x1290,0x22f3,0x32d2,0x4235,0x5214,0x6277,0x7256,
            0xb5ea,0xa5cb,0x95a8,0x8589,0xf56e,0xe54f,0xd52c,0xc50d,
            0x34e2,0x24c3,0x14a0,0x0481,0x7466,0x6447,0x5424,0x4405,
            0xa7db,0xb7fa,0x8799,0x97b8,0xe75f,0xf77e,0xc71d,0xd73c,
            0x26d3,0x36f2,0x0691,0x16b0,0x6657,0x7676,0x4615,0x5634,
            0xd94c,0xc96d,0xf90e,0xe92f,0x99c8,0x89e9,0xb98a,0xa9ab,
            0x5844,0x4865,0x7806,0x6827,0x18c0,0x08e1,0x3882,0x28a3,
            0xcb7d,0xdb5c,0xeb3f,0xfb1e,0x8bf9,0x9bd8,0xabbb,0xbb9a,
            0x4a75,0x5a54,0x6a37,0x7a16,0x0af1,0x1ad0,0x2ab3,0x3a92,
            0xfd2e,0xed0f,0xdd6c,0xcd4d,0xbdaa,0xad8b,0x9de8,0x8dc9,
            0x7c26,0x6c07,0x5c64,0x4c45,0x3ca2,0x2c83,0x1ce0,0x0cc1,
            0xef1f,0xff3e,0xcf5d,0xdf7c,0xaf9b,0xbfba,0x8fd9,0x9ff8,
            0x6e17,0x7e36,0x4e55,0x5e74,0x2e93,0x3eb2,0x0ed1,0x1ef0
        };

        public static ushort GenerateChecksum(byte[] file)
        {
            ushort crc = 0;
            int iFileLength = file.Length;
            for (int i = 0; i < iFileLength; i++)
            {
                crc = (ushort)((crc << 8) ^ crc16tab[((crc >> 8) ^ (0xff & file[i]))]);
            }
            return crc;
        }
    }

}
