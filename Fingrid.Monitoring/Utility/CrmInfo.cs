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
    public class Crm
    {
        public string Institution { get; set; }
        public string CRM { get; set; }
        public string InstitutionCode { get; set; }
    }
    public class CrmResultList
    {
        public string IsSuccessful { get; set; }
        public List<Crm> ResponseMessage { get; set; }
    }

    public class CrmInfo
    {
        private const string GET_ALL_CRMs = "institution/getcrms";
        private static List<Crm> AllCrms;

        private static List<Crm> GetAllCrmsAPI()
        {
            string apiPrefix = ConfigurationManager.AppSettings["BANKONEWEBAPI"];
            CrmResultList result = null;
            try
            {
                string uri = String.Concat(apiPrefix, GET_ALL_CRMs);
                Logger.Log(uri);
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                | SecurityProtocolType.Tls11
                | SecurityProtocolType.Tls12
                | SecurityProtocolType.Ssl3;
                WebRequest request = WebRequest.Create(uri);

                //ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                //byte[] byteArray = Encoding.UTF8.GetBytes(uri);
                request.ContentType = "application/x-www-form-urlencoded";
                //request.ContentLength = byteArray.Length;
                //Stream dataStream = request.GetRequestStream();

                //dataStream.Write(byteArray, 0, byteArray.Length);
                //dataStream.Close();
                WebResponse response = request.GetResponse();
                Logger.Log("I NO FAIL AGAIN OOOOOO!!!!!!!!!!");

                Stream dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string responseFromServer = reader.ReadToEnd();
                result = Newtonsoft.Json.JsonConvert.DeserializeObject<CrmResultList>(responseFromServer);
                response.Dispose();
                if (result == null|| result.ResponseMessage == null) return new List<Crm>();
                return result.ResponseMessage;
            }
            catch(Exception ex) {
                //    throw;
                Logger.Log("I AM HERE BOY!!!!!!" + ex.Message);
                Logger.Log(ex.Message);
                return null;
            }
        }

        public static List<Crm> GetAllCrms()
        {
            if(AllCrms == null || AllCrms.Count == 0)
            {
                AllCrms = GetAllCrmsAPI();
            }
            return AllCrms ?? new List<Crm>();
        }
        public static Crm GetCrmByInstitutionCode(string institutionCode)
        {
            if (AllCrms == null || AllCrms.Count == 0)
            {
                AllCrms = GetAllCrmsAPI();
            }
            return AllCrms?.FirstOrDefault(c => c.InstitutionCode == institutionCode);
        }
    }
}
