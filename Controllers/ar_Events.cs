using System;
using System.Threading.Tasks;
using AromaRetail_API.Classes;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [Route("api/v1/ar_GetEvents")]
    [ApiController]
    public class ar_GetEvents : ControllerBase
    {
        [HttpGet]
        [Route("")]
        [Route("{deviceid}")]
        public string Get(string deviceid)
        {
            try
            {
                sql.UpdateLastSeen(deviceid);
                sql.InsertStatus("api/v1/ar_Events", deviceid, "");
                return ut.FW_GetEventsByID(deviceid);
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }
    }

    [Route("api/v1/ar_SetEvent")]
    [ApiController]
    public class ar_SetEvent : ControllerBase
    {
        [HttpGet]
        [Route("{deviceid}/{deviceorder}/{starthour}/{startminute}/{endhour}/{endminute}/{dowbm}/{work}/{pause}/{fan}")]
        public string Get(string deviceid, string deviceorder, string starthour, string startminute, string endhour, string endminute, string dowbm, string work, string pause, string fan)
        {
            sql.UpdateLastSeen(deviceid);
            sql.InsertStatus("api/v1/ar_SetEvent", deviceid, "");
            int iDeviceOrder = 0;
            DateTime oStartDt;
            DateTime oEndDt;
            int iDOWBM = 0;
            int iWork = 0;
            int iPause = 0;
            int iFan = 0;

            int.TryParse(deviceorder, out iDeviceOrder);
            DateTime.TryParse(DateTime.Now.ToString("MM/dd/yy") + " " + starthour + ":" + startminute + ":00", out oStartDt);
            DateTime.TryParse(DateTime.Now.ToString("MM/dd/yy") + " " + endhour + ":" + endminute + ":00", out oEndDt);
            int.TryParse(dowbm, out iDOWBM);
            int.TryParse(work, out iWork);
            int.TryParse(pause, out iPause);
            int.TryParse(fan, out iFan);
            ut.FW_SetEvent(deviceid, iDeviceOrder, oStartDt, oEndDt, iDOWBM, iWork, iPause, iFan);
            //return deviceid + "|" + iDeviceOrder + "|" + oStartDt.ToString("HH:mm") + "|" + oEndDt.ToString("HH:mm") + "|" + iDOWBM + "|" + iWork + "|" + iPause + "|" + iFan;
            return ut.SendOK();
        }
    }

    [Route("api/v1/ar_GetFWUpdateInfo")]
    [ApiController]
    public class ar_GetFWUpdateInfo : ControllerBase
    {
        [HttpGet]
        [Route("")]
        [Route("{deviceid}")]
        public string Get(string deviceid)
        {
            sql.UpdateLastSeen(deviceid);
            sql.InsertStatus("api/v1/ar_GetFWUpdateInfo", deviceid, "");
            return ut.FW_GetUpdateInfo(deviceid);
        }
    }

    [Route("api/v1/ar_GetFWUpdatePacket")]
    [ApiController]
    public class ar_GetFWUpdatePacket : ControllerBase
    {
        [HttpGet]
        [Route("")]
        [Route("{deviceid}/{packetnum}")]
        public string Get(string deviceid, string packetnum)
        {
            sql.UpdateLastSeen(deviceid);
            sql.InsertStatus("api/v1/ar_GetFWUpdatePacket", deviceid, "");
            return ut.FW_GetUpdatePacket(deviceid, packetnum);
        }
    }

    [Route("api/v1/ar_Status")]
    [ApiController]
    public class ar_Status : ControllerBase
    {
        [HttpGet]
        [Route("")]
        [Route("{deviceid}")]
        public string Get(string deviceid)
        {
            sql.UpdateLastSeen(deviceid);
            sql.InsertStatus("api/v1/ar_Status", deviceid, "");
            return ut.FW_GetStatus(deviceid);
        }
    }

    [Route("api/v1/ar_FriendlyName")]
    [ApiController]
    public class ar_FriendlyName : ControllerBase
    {
        [HttpGet]
        [Route("")]
        [Route("{deviceid}")]
        public string Get(string deviceid)
        {
            sql.UpdateLastSeen(deviceid);
            sql.InsertStatus("api/v1/ar_FriendlyName", deviceid, "");
            return ut.FW_GetFriendlyName(deviceid);
        }
    }


    [Route("api/v1/ar_Override")]
    [ApiController]
    public class ar_Override : ControllerBase
    {
        [HttpGet]
        [Route("")]
        [Route("{deviceid}")]
        public string Get(string deviceid)
        {
            sql.UpdateLastSeen(deviceid);
            sql.InsertStatus("api/v1/ar_Override", deviceid, "");
            return ut.FW_GetOverride(deviceid);
        }
    }

    [Route("api/v1/ar_SetOverride")]
    [ApiController]
    public class ar_SetOverride : ControllerBase
    {
        [HttpGet]
        [Route("{deviceid}/{starthour}/{startminute}/{endhour}/{endminute}/{work}/{pause}/{fan}/{hold}")]
        public string Get(string deviceid, string starthour, string startminute, string endhour, string endminute, string work, string pause, string fan, string hold)
        {
            sql.UpdateLastSeen(deviceid);
            sql.InsertStatus("api/v1/ar_SetOverride", deviceid, "");
            DateTime oStartDt;
            DateTime oEndDt;
            int iWork = 0;
            int iPause = 0;
            int iFan = 0;
            int iHold = 0;

            DateTime.TryParse(DateTime.Now.ToString("MM/dd/yy") + " " + starthour + ":" + startminute + ":00", out oStartDt);
            DateTime.TryParse(DateTime.Now.ToString("MM/dd/yy") + " " + endhour + ":" + endminute + ":00", out oEndDt);
            int.TryParse(work, out iWork);
            int.TryParse(pause, out iPause);
            int.TryParse(fan, out iFan);
            int.TryParse(hold, out iHold);
            ut.FW_SetOverride(deviceid, oStartDt, oEndDt, iWork, iPause, iFan, iHold);
            return deviceid + "|" + oStartDt.ToString("HH:mm") + "|" + oEndDt.ToString("HH:mm") + "|" + iWork + "|" + iPause + "|" + iFan + "|" + iHold;
        }
    }

    [Route("api/v1/ar_SetOverrideCancel")]
    [ApiController]
    public class ar_SetOverrideCancel : ControllerBase
    {
        [HttpGet]
        [Route("{deviceid}")]
        public string Get(string deviceid)
        {
            sql.UpdateLastSeen(deviceid);
            sql.InsertStatus("api/v1/ar_SetOverrideCancel", deviceid, "");
            ut.FW_SetOverrideCancel(deviceid);
            return deviceid;
        }
    }
  
    [Route("api/v1/ar_ACK")]
    [ApiController]
    public class ar_ACK : ControllerBase
    {
        [HttpGet]
        [Route("")]
        [Route("{deviceid}/{ack}")]
        public string Get(string deviceid, string ack)
        {
            sql.UpdateLastSeen(deviceid);
            sql.InsertStatus("api/v1/ar_ACK", deviceid, "Ack Type: " + ack);
            switch (ack.ToLower())
            {
                case "ar_override":
                case "ar_overridecancel":
                    return ut.FW_RemoveOverride(deviceid);
                case "ar_getevents":
                    return ut.FW_ClearEventFlag(deviceid);
                case "ar_friendlyname":
                    return ut.FW_FriendlyNameUpdateFlagFlag(deviceid);
                case "ar_getfwupdateinfo":
                case "ar_getfwupdatepacket":
                    return ut.FW_ClearApplyUpdateFlag(deviceid);
                default:
                    return ut.SendERR();
            }
        }

    }

    [Route("api/v1/ar_Metrics")]
    [ApiController]
    public class ar_Metrics : ControllerBase
    {
        [HttpGet]
        [Route("")]
        [Route("{deviceid}/{revision}/{secsuse}")]
        public string Get(string deviceid, string revision, string secsuse)
        {
            sql.UpdateMetrics(deviceid, revision, secsuse);
            sql.InsertStatus("api/v1/ar_Metrics", deviceid, "Revision: " + revision + ", SecsUse = " + secsuse);
            return ut.SendOK();
        }

    }

}
