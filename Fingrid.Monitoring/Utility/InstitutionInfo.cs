﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Fingrid.Monitoring.Utility
{
    public class Institution
    {
        private string CodeTemp;
        public string Code { get; set; }
        public string Name { get; set; }
        public string InstitutionCode { get { return CodeTemp; } set { CodeTemp = value.PadLeft(3, '0'); } }
    }

    public class InstitutionInfo
    {
        private const string GET_BY_INSTITUTION_CODE = "mfbservice/GetMfbByInstitutionCode?institutionCode=";
        private const string GET_ALL_INSTITUTIONS = "mfbservice/GetAll";

        public static Institution GetInstitutionByCode(string institutionCode)
        {
            string apiPrefix = ConfigurationManager.AppSettings["BANKONEAPI"];
            Institution result = null;
            try
            {
                string uri = String.Concat(apiPrefix, GET_BY_INSTITUTION_CODE, institutionCode);
                Logger.Log(uri);
                WebRequest request = WebRequest.Create(uri);

                request.Method = "POST";
                byte[] byteArray = Encoding.UTF8.GetBytes(uri);
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = byteArray.Length;
                Stream dataStream = request.GetRequestStream();

                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                WebResponse response = request.GetResponse();
                dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string responseFromServer = reader.ReadToEnd();
                result = Newtonsoft.Json.JsonConvert.DeserializeObject<Institution>(responseFromServer);
                response.Dispose();
                return result;
            }
            catch (Exception ex)
            {
                //throw; 
                Logger.Log(ex.Message);
                return null;
            }
        }

        public static List<Institution> GetInstitutions()
        {
            string apiPrefix = ConfigurationManager.AppSettings["BANKONEAPI"];
            List<Institution> result = null;
            try
            {
                string uri = String.Concat(apiPrefix, GET_ALL_INSTITUTIONS);
                Logger.Log(uri);
                WebRequest request = WebRequest.Create(uri);

                request.Method = "POST";
                byte[] byteArray = Encoding.UTF8.GetBytes(uri);
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = byteArray.Length;
                Stream dataStream = request.GetRequestStream();

                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                WebResponse response = request.GetResponse();
                dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string responseFromServer = reader.ReadToEnd();
                result = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Institution>>(responseFromServer);
                response.Dispose();
                return result;
            }
            catch { throw; }
        }

        public static String GetInstitutionName(string institutionCode, Dictionary<string, Institution> institutionsDict)
        {
            Institution inst = null;
            if (!institutionsDict.TryGetValue(institutionCode.Trim(), out inst))
            {
                inst = InstitutionInfo.GetInstitutionByCode(institutionCode);
            }

            if (inst == null) inst = new Institution { Name = institutionCode, Code = institutionCode };

            return !String.IsNullOrEmpty(inst.Name) ? inst.Name : "Unknown Institution";
        }
    }
}
