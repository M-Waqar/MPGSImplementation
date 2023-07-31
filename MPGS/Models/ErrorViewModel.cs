using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MPGS.Models
{
    public class ErrorViewModel
    {
        public string RequestId { get; set; }
        public string Cause { get; set; }
        public string Message { get; set; }
        public string ErrorCode { get; set; }
        public string Explanation { get; set; }
        public string Field { get; set; }
        public string ValidationType { get; set; }
        public string Result { get; set; }

        public static ErrorViewModel toErrorViewModel(string json)
        {
            ErrorViewModel model = new ErrorViewModel();

            JObject jObject = JObject.Parse(json);

            var error = jObject["error"];
            model.Cause = error["cause"].ToObject<String>();
            model.Explanation = error["explanation"].ToObject<String>();
            model.Message = error["explanation"].ToObject<String>();
            model.Field = error["field"].ToObject<String>();
            model.ValidationType = error["validationType"].ToObject<String>();
            model.Result = jObject["result"].ToObject<String>();

            return model;
        }

        public static ErrorViewModel toErrorViewModel(string requestId, string json)
        {
            ErrorViewModel model = new ErrorViewModel();

            JObject jObject = JObject.Parse(json);

            var error = jObject["error"];
            model.RequestId = requestId;
            model.Cause = error["cause"] != null ? error["cause"].ToObject<String>() : null;
            model.Explanation = error["explanation"] != null ? error["explanation"].ToObject<String>() : null;
            model.Message = error["explanation"] != null ? error["explanation"].ToObject<String>() : null;
            model.Field = error["field"] != null ? error["field"].ToObject<String>() : null;
            model.ValidationType = error["validationType"] != null ? error["validationType"].ToObject<String>() : null;
            model.Result = jObject["result"] != null ? jObject["result"].ToObject<String>() : null;

            return model;
        }
    }

}