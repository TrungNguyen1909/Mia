using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.IO;
using Newtonsoft.Json;
using System.Xml;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;

namespace Data
{
    public class Dictionary
    {

        private static string appId = "e5dcdbf6";
        private static string appKey = "6e645800815e36a51a0c12439e764c7a";
        static string lang = "en";
        public static List<LexicalEntry> Define(string word)
        {
            word = word.ToLower();
            string url = String.Concat("https://od-api.oxforddictionaries.com:443/api/v1/entries/" + lang + "/" + word + "/definitions");
            HttpWebRequest connection = (HttpWebRequest)WebRequest.Create(url);
            connection.ContentType = "application/json";

            connection.Headers["app_id"] = appId;
            connection.Headers["app_key"] = appKey;
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

                Dictionary<string, object> values = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseFromServer);
                var results = JsonConvert.DeserializeObject<List<object>>(values["results"].ToString());
                values = JsonConvert.DeserializeObject<Dictionary<string, object>>(results[0].ToString());
                var entries = (List<LexicalEntry>)JsonConvert.DeserializeObject(values["lexicalEntries"].ToString(), typeof(List<LexicalEntry>));


                return entries;
            }
            catch { return null; }
        }
    }
    public class sense
    {

        public List<string> definitions { get; set; }
        public string id { get; set; }
    }
    public class Entry
    {
        public string homographNumber { get; set; }
        public List<sense> senses { get; set; }
    }
    public class LexicalEntry
    {
        public List<Entry> entries { get; set; }
        public string language { get; set; }
        public string lexicalCategory { get; set; }
        public string text { get; set; }
        
    }   
}
