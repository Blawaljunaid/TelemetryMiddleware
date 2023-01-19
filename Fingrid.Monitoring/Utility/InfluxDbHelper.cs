using InfluxDB.Net.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Fingrid.Monitoring.Utility
{
    public class QueryResultObj
    {
        [Newtonsoft.Json.JsonProperty("results")]
        public List<QueryResultInnerObj> ResultList { get; set; }
    }
    public class QueryResultInnerObj
    {
        [Newtonsoft.Json.JsonProperty("statement_id")]
        public long StatementID { get; set; }

        [Newtonsoft.Json.JsonProperty("series")]
        public List<Serie> Records { get; set; }
    }

    public class InfluxDbHelper
    {
        public List<Serie> QueryAPI(string database, string query)
        {
            string apiPrefix = ConfigurationManager.AppSettings["InfluxDBUrl"];
            List<Serie> result = null;
            try
            {
                string uri = $"{apiPrefix}/query?db={database}&q={query}";
                Logger.Log(uri);
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                | SecurityProtocolType.Tls11
                | SecurityProtocolType.Tls12
                | SecurityProtocolType.Ssl3;
                WebRequest request = WebRequest.Create(uri);

                //byte[] byteArray = Encoding.UTF8.GetBytes(uri);
                request.ContentType = "application/x-www-form-urlencoded";
                //request.ContentLength = byteArray.Length;
                //Stream dataStream = request.GetRequestStream();

                //dataStream.Write(byteArray, 0, byteArray.Length);
                //dataStream.Close();
                WebResponse response = request.GetResponse();
                Stream dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string responseFromServer = reader.ReadToEnd();
                var resultObj = Newtonsoft.Json.JsonConvert.DeserializeObject<QueryResultObj>(responseFromServer);

                if (resultObj == null) return result;
                result = resultObj?.ResultList[0]?.Records;
                response.Dispose();
                return result;
            }
            catch (Exception ex)
            {
                //    throw;
                Logger.Log(ex.Message);
                return null;
            }
        }
    }
}

