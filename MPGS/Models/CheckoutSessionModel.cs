using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;


namespace MPGS.Models
{
    public class CheckoutSessionModel
    {
        public string Id { get; set; }
        public string Version { get; set; }
        public string SuccessIndicator { get; set; }
        public string Transactionid { get; set; }

        public static CheckoutSessionModel toCheckoutSessionModel(string response)
        {
            JObject jObject = JObject.Parse(response);
            CheckoutSessionModel model = jObject["session"].ToObject<CheckoutSessionModel>();
            model.SuccessIndicator = jObject["successIndicator"] != null ? jObject["successIndicator"].ToString() : "";
            return model;
        }
    }

    public class crearesession
    {
        public string merchant { get; set; }
        public string result { get; set; }
        public sessio session { get; set; }

        public class sessio
        {
            public string aes256Key { get; set; }
            public string authenticationLimit { get; set; }
            public string id { get; set; }
            public string updateStatus { get; set; }
            public string version { get; set; }
        }
    }

    

}