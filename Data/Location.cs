using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;


namespace Data
{
    public class Location
    {
        public static string GetCurrentLocation()
        {
            GeoCoordinateWatcher watcher = new GeoCoordinateWatcher();

            // Do not suppress prompt, and wait 1000 milliseconds to start.
            watcher.TryStart(false, TimeSpan.FromMilliseconds(1000));

            GeoCoordinate coord = watcher.Position.Location;

            if (coord.IsUnknown != true)
            {
                return (coord.Latitude.ToString() + "," + coord.Longitude.ToString());
            }
            else
            {
                return "autoip";
            }

        }
        public static string GetCurrentAddress()
        {
            var latlong = GetCurrentLocation();
            if (latlong != "autoip")
            {
                var strLatitude = latlong.Substring(0, latlong.IndexOf(','));
                strLatitude = strLatitude.Trim(',');
                var strLongitude = latlong.Substring(latlong.IndexOf(','));
                strLongitude = strLongitude.TrimStart(',');
                WebClient client = new WebClient();
                client.Encoding = Encoding.UTF8;
                string Url = "http://maps.googleapis.com/maps/api/geocode/json?latlng=" + strLatitude + "," + strLongitude + "&sensor=true";
                var getResult = client.DownloadString(Url);
                JObject parseJson = JObject.Parse(getResult);
                var getJsonres = parseJson["results"][0];
                //var getJson = getJsonres["address_components"][1];
                var getAddress = getJsonres["formatted_address"];
                string Address = getAddress.ToString();
                return Address;
            }
            else return null;
        }
    }
}
