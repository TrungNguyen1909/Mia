using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace Data
{
    public class History
    {
        public static List<Dictionary<string,object>> GetHistory(DateTime reqdt)
        {
            WebClient wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            string URL = "http://history.muffinlabs.com/date/" + reqdt.Month.ToString() + "/" + reqdt.Day.ToString();
            string result = wc.DownloadString(URL);
            Dictionary<string,object> values= JsonConvert.DeserializeObject<Dictionary<string, object>>(result);
            values = JsonConvert.DeserializeObject<Dictionary<string, object>>(values["data"].ToString());
            var strEvents = values["Events"].ToString();
            var events = JsonConvert.DeserializeObject<List<Dictionary<string,object>>>(strEvents);
            return events;
        }
    }
}
