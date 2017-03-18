using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Device.Location;
using System.Threading;

namespace Data
{
    public class Weather
    {
        string wunderground_key = "97fca79bb0f45e0e"; // You'll need to goto http://www.wunderground.com/weather/api/, and get a key to use the API.
        
        public Dictionary<string, object> GetCurrentWeather(string location = "autoip")
        {

            var cli = new WebClient();
            if (location != "autoip")
            {
                location = AutoComplete(location);
            }
            else
            {
                location=Location.GetCurrentLocation();
            }
            string weather = cli.DownloadString("http://api.wunderground.com/api/" + wunderground_key + "/conditions/q/" + location + ".xml");
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(weather);
            string json = JsonConvert.SerializeXmlNode(doc);
            Dictionary<string, object> values = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            values = JsonConvert.DeserializeObject<Dictionary<string, object>>(values["response"].ToString());
            values = JsonConvert.DeserializeObject<Dictionary<string, object>>(values["current_observation"].ToString());
            return values;
        }
        public Dictionary<string, object> GetForecastWeather(DateTime reqdt, string location = "autoip")
        {
            var cli = new WebClient();
            if (location != "autoip")
            {
                location = AutoComplete(location);
            }
            else
            {
                location = Location.GetCurrentLocation();
            }
            string forecast = cli.DownloadString("http://api.wunderground.com/api/" + wunderground_key + "/forecast10day/q/" + location + ".xml");
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(forecast);
            string json = JsonConvert.SerializeXmlNode(doc);
            Dictionary<string, object> fvalues = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            fvalues = JsonConvert.DeserializeObject<Dictionary<string, object>>(fvalues["response"].ToString());
            fvalues = JsonConvert.DeserializeObject<Dictionary<string, object>>(fvalues["forecast"].ToString());
            fvalues = JsonConvert.DeserializeObject<Dictionary<string, object>>(fvalues["simpleforecast"].ToString());
            fvalues = JsonConvert.DeserializeObject<Dictionary<string, object>>(fvalues["forecastdays"].ToString());
            var list = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(fvalues["forecastday"].ToString());
            foreach (var subdata in list)
            {
                var converted = JsonConvert.DeserializeObject<Dictionary<string, object>>(subdata["date"].ToString());
                var Year = Convert.ToInt32(converted["year"]);
                var Month = Convert.ToInt32(converted["month"]);
                var Day = Convert.ToInt32(converted["day"]);
                var parsed = new DateTime(Year, Month, Day);

                subdata["date"] = parsed.ToLongDateString();
                var low = JsonConvert.DeserializeObject<Dictionary<string, object>>(subdata["low"].ToString());
                var high = JsonConvert.DeserializeObject<Dictionary<string, object>>(subdata["high"].ToString());
                subdata.Add("low_c", low["celsius"]);
                subdata.Add("high_c", high["celsius"]);
                subdata.Add("low_f", low["fahrenheit"]);
                subdata.Add("high_f", high["fahrenheit"]);
                if (parsed.Date == reqdt)
                    return subdata;
            }
            return null;
        }
        public string AutoComplete(string s)
        {
            string location = "autoip";
            var cli = new WebClient();
            string SelectedLocation = cli.DownloadString("http://autocomplete.wunderground.com/aq?format=xml&query=" + s);
            using (XmlReader reader = XmlReader.Create(new StringReader(SelectedLocation)))
            {
                while (reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (reader.Name.Equals("name"))
                            {
                                reader.Read();
                                return location = reader.Value;
                            }
                            break;
                    }
                }
            }
            return s;
        }
        
        
    }
}

