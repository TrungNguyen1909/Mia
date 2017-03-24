using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Data
{
    public class Knowledge
    {
        public static Dictionary<string,string> GetKnowledge(string s)
        {
            string url = "https://kgsearch.googleapis.com/v1/entities:search?query=" + s + "&key=AIzaSyACbHtnNF5jshputRfDFH_BfJWPgWSN0n0&limit=1&indent=True";
            using (WebClient wc=new WebClient())
            {
                wc.Encoding = Encoding.UTF8;
                string response=wc.DownloadString(url);
                try
                {
                    JObject parseJson = JObject.Parse(response);
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
                
            }
        }
    }
}
