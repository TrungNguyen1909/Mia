using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Data
{
    public class Knowledge
    {
        private static string WAK2 = "LKLTPK-98QWG32A5X";
        private static string WAK1 = "U8UY5L-WAAGPAARQU";
        private static string KGK = "AIzaSyACbHtnNF5jshputRfDFH_BfJWPgWSN0n0";
        private static string LocationParameter()
        {
            string loc = Location.GetCurrentLocation();
            if (loc == "autoip") return null;
            else loc = "&latlong=" + loc;
            return loc;
        }
        public static Dictionary<string,string> GetKnowledge(string s)
        {
            return GetWolframAlphaKnowledge(s);
        }
        public static Dictionary<string,string> GetGoogleKnowledge(string s)
        {
            string url = "https://kgsearch.googleapis.com/v1/entities:search?query=" + s + "&key="+KGK+"&limit=1&indent=True"+LocationParameter();
            
            HttpWebRequest connection = (HttpWebRequest)WebRequest.Create(url);
            connection.ContentType = "application/json";
            
            HttpWebResponse response;
            try
            {
                response = (HttpWebResponse)connection.GetResponse();
                // Get the stream containing content returned by the server.
                Stream dataStream = response.GetResponseStream();
                // Open the stream using a StreamReader for easy access.
                StreamReader reader = new StreamReader(dataStream);
                // Read the content.
                string responseFromServer = reader.ReadToEnd();
                // Clean up the streams and the response.

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    reader.Close();
                    response.Close();
                    return null;
                }
                reader.Close();
                response.Close();
                JObject parseJson = JObject.Parse(responseFromServer);
                var getlist = parseJson["itemListElement"];
                Console.WriteLine();
                var getdetail = getlist[0]["result"]["detailedDescription"]["articleBody"];
                string result = getdetail.ToString();
                int count = 0;
                Dictionary<string, string> final = new Dictionary<string, string>();
                var imageurl = getlist[0]["result"]["image"]["contentUrl"].ToString();
                final.Add("imageurl", imageurl);
                for (int i = 0; i < result.Length; i++)
                {
                    if (result[i] == '.') count++;
                    if (count == 3)
                    {
                        final.Add("data", AccentsRemover.RemoveAccents(result.Remove(i + 1)));

                        return final;
                    }
                }
                final.Add("data", AccentsRemover.RemoveAccents(result));

                return final;

            }
            catch { return null; }
            
                
                //catch { return null; }
                
            //}
        }
        public static Dictionary<string, string> GetWolframAlphaKnowledge(string q)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            string url = "http://api.wolframalpha.com/v1/spoken?input=" + q + "&appid=" + WAK2+LocationParameter();
            HttpWebRequest connection = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse response;
            string responseFromServer=null;
            try
            {
                response = (HttpWebResponse)connection.GetResponse();
                // Get the stream containing content returned by the server.
                Stream dataStream = response.GetResponseStream();
                // Open the stream using a StreamReader for easy access.
                StreamReader reader = new StreamReader(dataStream);
                // Read the content.
                responseFromServer = reader.ReadToEnd();
                // Clean up the streams and the response.

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    reader.Close();
                    response.Close();
                    
                }
                reader.Close();
                response.Close();

            }
            catch {  }
            if (responseFromServer != null && responseFromServer.Contains("Information about"))
            {
                responseFromServer = responseFromServer.Replace("Information about", "");

                var kg = GetGoogleKnowledge(q);
                if (kg != null)
                {
                    responseFromServer = kg["data"];
                    result.Add("imageurl", kg["imageurl"]);
                }
                
            }
            result.Add("response",AccentsRemover.RemoveAccents( responseFromServer));
            
            
            q = WebUtility.UrlEncode(q);
            string uri = (@"https://api.wolframalpha.com/v2/simple?input=" + q + "&appid=" + WAK1 + "&fontsize=12&width=330");
            result.Add("simpleurl",uri);
            return result;
        }
    }
}
