using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AromaRetail_API.Classes;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Classes;


namespace AromaRetail_API.Controllers
{
    [Route("api/v1/ar_Test")]
    [ApiController]
    public class ar_Test : ControllerBase
    {
        [HttpPost]
        public async Task<string> Post()
        {
            using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                string sTemp = ut.ResponseBegin + await reader.ReadToEndAsync() + ut.ResponseEnd;
                return sTemp;
            }
        }

        [HttpGet]
        [Route("")]
        [Route("{deviceid?}/{rowindex?}")]
        [ProducesResponseType(typeof(IEnumerable<string>), 200)]
        public string GetEvents(int? deviceid, int? rowindex)
        {
            string sError = "";
            bool bError = false;
            if (deviceid == null)
            {
                sError += "Device ID can not be null\r\n";
                bError = true;
            }
            if (rowindex == null)
            {
                sError += "Row Index can not be null\r\n";
                bError = true;
            }
            if (bError)
            {
                return sError;
            }
            else
            {
                return ut.ResponseBegin + (new Event { ID = (short)rowindex }).ToString() + ut.ResponseEnd;
            }
        }

    }
}
