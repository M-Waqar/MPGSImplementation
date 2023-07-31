using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MPGS.Controllers;

namespace MPGS.Models
{
    public class SecureIdEnrollmentResponseModel
    {
        public string Id { get; set; }
        public string Status { get; set; }
        public string recomm { get; set; }
        public string ResponseUrl { get; set; }
        public string AcsUrl { get; set; }
        public string PaReq { get; set; }
        public string MdValue { get; set; }

        public SecureIdEnrollmentResponseModel toSecureIdEnrollmentResponseModel(string response)
        {
            JObject jObject = JObject.Parse(response);
            SecureIdEnrollmentResponseModel model = new SecureIdEnrollmentResponseModel();
            model.AcsUrl = jObject["authentication"]["redirectHtml"].Value<string>();
            return model;
        }

        public string generateSampleId()
        {
            return Guid.NewGuid().ToString().Replace("-", string.Empty).Substring(0, 10);
        }
    }
}