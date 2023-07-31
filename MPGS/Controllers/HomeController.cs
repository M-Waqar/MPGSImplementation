using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using MPGS.Models;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Collections.Specialized;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Threading;

namespace MPGS.Controllers
{
    public class HomeController : Controller
    {
        private static Dictionary<string, string> FakeSession = new Dictionary<string, string>();
        //SecureIdEnrollmentResponseModel model;

        static SecureIdEnrollmentResponseModel model;
        static string sessionid;
        string secureId;
        static string Orderid;
        static string TransactionId;
        string Payload;
        static string amount = "1.00";
        static string currency = "AED";
        string paRes;
        static bool is3d;
        static bool is3d2;
        static string ordertype;

        public async Task<ActionResult> Index()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.Expect100Continue = true;
            string responsee;
            Orderid = generateSampleId();
            ordertype = "PURCHASE";
            using (var httpClient = new HttpClient())
            {
                using (var request = new HttpRequestMessage(new HttpMethod("POST"), "https://test-gateway.mastercard.com/api/nvp/version/61"))  //https://test-gateway.mastercard.com/api/nvp/version/61
                {
                    var contentList = new List<string>();
                    contentList.Add("apiOperation=CREATE_CHECKOUT_SESSION");
                    contentList.Add("apiPassword=e16091db04828f4f6e4150a62adf4583");//e16091db04828f4f6e4150a62adf4583
                    contentList.Add("apiUsername=merchant.TEST999999955");//TEST999999955
                    contentList.Add("merchant=TEST999999955");//TEST999999955
                    contentList.Add("interaction.operation=PURCHASE");
                    contentList.Add("order.id=" + Orderid);
                    contentList.Add("order.amount=1.00");
                    contentList.Add("transaction.reference=" + generatetransactionref("1.00"));
                    contentList.Add("order.currency=AED");
                    contentList.Add("interaction.returnUrl=http://localhost:65290/Home/SessionCheckout");
                    request.Content = new StringContent(string.Join("&", contentList));
                    request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded");

                    var response = await httpClient.SendAsync(request);

                    byte[] bytes = response.Content.ReadAsByteArrayAsync().Result;

                    var str = System.Text.Encoding.Default.GetString(bytes);

                    string[] values = str.Split('&');

                    string session = values[2];

                    string[] SeID = session.Split('=');

                    responsee = SeID[1];
                }
            }

            ViewBag.SessionID = responsee;

            return View();
        }

        public ActionResult About()
        {

            is3d = true;
            is3d2 = false;
            Payload = payload("CreateSession");
            var respo = executeHTTPMethod("https://network.gateway.mastercard.com/api/rest/version/61/merchant/200200006504/session", Payload, "POST", "application/json; charset=iso-8859-1"); //https://test-gateway.mastercard.com/api/rest/version/61/merchant/TEST999999955/session //https://test-network.mtf.gateway.mastercard.com/api/rest/version/61/merchant/TEST200200006504/session
            crearesession session = JsonConvert.DeserializeObject<crearesession>(respo);
            ViewBag.SessionID = session.session.id;
            return View();
        }

        public ActionResult ThreeDStwo()
        {
            is3d2 = true;
            Payload = payload("CreateSession");
            //var respo = executeHTTPMethod("https://network.gateway.mastercard.com/api/rest/version/61/merchant/200200006504/session", Payload, "POST", "application/json; charset=iso-8859-1"); //https://test-network.mtf.gateway.mastercard.com/api/rest/version/61/merchant/TEST200200006504/session
            var respo = executeHTTPMethod("https://ap-gateway.mastercard.com/api/rest/version/57/merchant/TEST999999124/session", Payload, "POST", "application/json; charset=iso-8859-1"); //https://test-network.mtf.gateway.mastercard.com/api/rest/version/61/merchant/TEST200200006504/session
            crearesession session = JsonConvert.DeserializeObject<crearesession>(respo);
            ViewBag.SessionID = session.session.id;
            return View();
        }

        public ActionResult Pay()
        {
            is3d = false;
            Orderid = generateSampleId();
            TransactionId = generateSampleId();
            return View();
        }

        public void Paynow(string id)
        {
            string response = "";
            sessionid = id;

            loop:
            Payload = payload("PAY");
            response = executeHTTPMethod("https://test-gateway.mastercard.com/api/rest/version/61/merchant/TEST999999124/order/" + Orderid + "/transaction/" + TransactionId, Payload, "PUT", "application/json; charset=iso-8859-1");

            if (response.Contains("Form Session not found or expired"))
            {
                Thread.Sleep(2000);
                goto loop;
            }

            setSessionValue("Operation", "PAY");
            setSessionValue("Method", "PUT");
            setSessionValue("RequestUrl", "https://test-gateway.mastercard.com/api/rest/version/61/merchant/TEST999999955/order/" + Orderid + "/transaction/" + TransactionId);
            setSessionValue("Payload", Payload);
            setSessionValue("Response", JsonHelper.prettyPrint(response));

            RedirectToAction("Contactt", "Home");
        }

        public ActionResult PayWithToken()
        {
            is3d = false;
            Orderid = generateSampleId();
            TransactionId = generateSampleId();
            return View();
        }

        public ActionResult RetriveOrder()
        {
            return View();
        }

        public ActionResult Void()
        {
            return View();
        }

        public ActionResult VoidOrder(CheckoutSessionModel model)
        {
            ordertype = "VOID";
            TransactionId = model.Transactionid;
            Orderid = model.Id;
            return RedirectToAction("SessionCheckout", "Home");
        }

        [HttpPost]
        public ActionResult GetOrder(CheckoutSessionModel model)
        {
            ordertype = "RETRIEVE_ORDER";
            Orderid = model.Id;
            return RedirectToAction("SessionCheckout", "Home");
        }

        [HttpPost]
        public void check3dsenrolled(string id)
        {
            Thread.Sleep(15000);
            sessionid = id;
            is3d2 = false;
            
            secureId = generateSampleId();
            Orderid = generateSampleId();
            TransactionId = generateSampleId();
            setSessionValue("secureId", secureId);
            setSessionValue("sessionId", sessionid);
            setSessionValue("amount", amount);
            setSessionValue("currency", currency);
            setSessionValue("orderId", Orderid);
            setSessionValue("transactionId", TransactionId);

            string respo = "";

            loop:
            Payload = payload("INITIATE_AUTHENTICATION");
            respo = executeHTTPMethod("https://network.gateway.mastercard.com/api/rest/version/61/merchant/200200006504/order/" + Orderid + "/transaction/" + TransactionId, Payload, "PUT", "application/json; charset=iso-8859-1");//test-gateway.mastercard.com

            //Payload = payload("CHECK_3DS_ENROLLMENT");
            //respo = executeHTTPMethod("https://test-gateway.mastercard.com/api/rest/version/61/merchant/TEST999999955/3DSecureId/" + secureId, Payload, "PUT", "application/json; charset=iso-8859-1");

            if (respo.Contains("Form Session not found or expired"))
            {
               Thread.Sleep(2000);
                goto loop;
            }

            if (respo.Contains("veResEnrolled"))
            {
            loop1:
                Payload = "{\r\n  \"apiOperation\": \"AUTHENTICATE_PAYER\",\r\n  \"authentication\": {\r\n    \"redirectResponseUrl\": \"http://localhost:65290/Home/Contact\"\r\n  },\r\n  \"order\": {\r\n    \"amount\": \""+ getSessionValueAsString("amount") + "\",\r\n    \"currency\": \""+ getSessionValueAsString("currency") + "\"\r\n  },\r\n  \"device\": {\r\n    \"browser\": \"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/92.0.4515.159 Safari/537.36\",\r\n    \"browserDetails\": {\r\n      \"3DSecureChallengeWindowSize\": \"FULL_SCREEN\",\r\n      \"acceptHeaders\": \"application/json\",\r\n      \"colorDepth\": 24,\r\n      \"javaEnabled\": true,\r\n      \"language\": \"en-US\",\r\n      \"screenHeight\": 1000,\r\n      \"screenWidth\": 1000,\r\n      \"timeZone\": 273\r\n    }\r\n  },\r\n  \"session\": {\r\n    \"id\": \"" + sessionid+"\"\r\n  }\r\n}";
                respo = executeHTTPMethod("https://network.gateway.mastercard.com/api/rest/version/61/merchant/200200006504/order/" + Orderid + "/transaction/" + TransactionId, Payload, "PUT", "application/json; charset=iso-8859-1");

                if (respo.Contains("Form Session not found or expired"))
                {
                    Thread.Sleep(4000);
                    goto loop1;
                }

            }

            model = new SecureIdEnrollmentResponseModel();

            try
            {
                model = model.toSecureIdEnrollmentResponseModel(respo);
            }
            catch (Exception e)
            {

                RedirectToAction("Error", new ErrorViewModel
                {
                    RequestId = getRequestId(),
                    Cause = e.InnerException != null ? e.InnerException.StackTrace : e.StackTrace,
                    Message = e.Message
                });
            }

             RedirectToAction("SecureIdPayerAuthenticationForm", "Home");
        }

        [HttpPost]
        public void check3dsenrolledtwo(string id)
        {
            Thread.Sleep(15000);
            sessionid = id;
            is3d2 = true;
            secureId = generateSampleId();
            Orderid = generateSampleId();
            TransactionId = generateSampleId();
            setSessionValue("secureId", secureId);
            setSessionValue("sessionId", sessionid);
            setSessionValue("amount", amount);
            setSessionValue("currency", currency);
            setSessionValue("orderId", Orderid);
            setSessionValue("transactionId", TransactionId);

            string respo = "";

        loop:
            Payload = payload("INITIATE_AUTHENTICATION");
            respo = executeHTTPMethod("https://ap-gateway.mastercard.com/api/rest/version/57/merchant/TEST999999124/order/" + Orderid + "/transaction/" + TransactionId, Payload, "PUT", "application/json; charset=iso-8859-1");
            
            if (respo.Contains("Form Session not found or expired"))
            {
                Thread.Sleep(2000);
                goto loop;
            }

            if (respo.Contains("SUPPORTED") && respo.Contains("2.1.0"))
            {
            loop1:
                Payload = "{\r\n  \"apiOperation\": \"AUTHENTICATE_PAYER\",\r\n  \"authentication\": {\r\n    \"redirectResponseUrl\": \"http://localhost:65290/Home/Contact\"\r\n  },\r\n  \"order\": {\r\n    \"amount\": \"" + getSessionValueAsString("amount") + "\",\r\n    \"currency\": \"" + getSessionValueAsString("currency") + "\"\r\n  },\r\n  \"device\": {\r\n    \"browser\": \"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/92.0.4515.159 Safari/537.36\",\r\n    \"browserDetails\": {\r\n      \"3DSecureChallengeWindowSize\": \"FULL_SCREEN\",\r\n      \"acceptHeaders\": \"application/json\",\r\n      \"colorDepth\": 24,\r\n      \"javaEnabled\": true,\r\n      \"language\": \"en-US\",\r\n      \"screenHeight\": 1000,\r\n      \"screenWidth\": 1000,\r\n      \"timeZone\": 273\r\n    }\r\n  },\r\n  \"session\": {\r\n    \"id\": \"" + sessionid + "\"\r\n  }\r\n}"; //https://test-gateway.mastercard.com
                respo = executeHTTPMethod("https://ap-gateway.mastercard.com/api/rest/version/57/merchant/TEST999999124/order/" + Orderid + "/transaction/" + TransactionId, Payload, "PUT", "application/json; charset=iso-8859-1");

                if (respo.Contains("Form Session not found or expired") || respo.Contains("The method complete notification hasn't been received"))
                {
                    Thread.Sleep(4000);
                    goto loop1;
                }

            }

            //Frictionless Response
            //If in the response if you receive the transactionStatus": "Y" and payerInteraction": "NOT_REQUIRED"  then its mean that no need to redirect the customer to the OTP page simply you need to run the “PAY” API.

            if (respo.Contains("Y") && respo.Contains("NOT_REQUIRED"))
            {
                RedirectToAction("Contact", "Home");
            }

            //Challenge Flow
            //In the response if you receive the transactionStatus": "C" and payerInteraction": "REQUIRED"  then its mean that you need to challenge the customer to enter the OTP for this you need to use the parameter “Redirect.HTML” remove the escape characters and through HTML DOM POST it to your customer.

           model = new SecureIdEnrollmentResponseModel();

            try
            {
                model = model.toSecureIdEnrollmentResponseModel(respo);
            }
            catch (Exception e)
            {

                RedirectToAction("Error", new ErrorViewModel
                {
                    RequestId = getRequestId(),
                    Cause = e.InnerException != null ? e.InnerException.StackTrace : e.StackTrace,
                    Message = e.Message
                });
            }

            RedirectToAction("SecureIdPayerAuthenticationForm", "Home");
        }

        public ActionResult Error(ErrorViewModel model)
        {
            return View(model);
        }

        public ActionResult SecureIdPayerAuthenticationForm()
        {
            if(is3d)
            {
                int start = model.AcsUrl.IndexOf("<form");
                string test = model.AcsUrl.Substring(start);
                model.AcsUrl = test.Replace("</div>", "");
            }
            if(is3d2)
            {
                int start = model.AcsUrl.IndexOf("<form");
                string test = model.AcsUrl.Substring(start);
                model.AcsUrl = test.Replace("</div>", "");
                model.AcsUrl = model.AcsUrl.Replace("challengeFrame", "");
            }
            
            return View(HomeController.model);
        }

        public async Task<string> GetSessionID()
        {
            string responsee;

            using (var httpClient = new HttpClient())
            {
                using (var request = new HttpRequestMessage(new HttpMethod("POST"), "https://test-gateway.mastercard.com/api/nvp/version/61"))
                {
                    var contentList = new List<string>();
                    contentList.Add("apiOperation=CREATE_CHECKOUT_SESSION");
                    contentList.Add("apiPassword=e16091db04828f4f6e4150a62adf4583");
                    contentList.Add("apiUsername=merchant.TEST999999955");
                    contentList.Add("merchant=TEST999999955");
                    contentList.Add("interaction.operation=PURCHASE");
                    contentList.Add("order.id=asd234tr56");
                    contentList.Add("order.amount=1.00");
                    contentList.Add("order.currency=AED");
                    request.Content = new StringContent(string.Join("&", contentList));
                    request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded");

                    var response = await httpClient.SendAsync(request);

                    responsee = response.ToString();
                }
            }

            return responsee;
        }

        protected string getRequestId()
        {
            return Activity.Current?.Id ?? HttpContext.Timestamp.ToString();
        }

        public String executeHTTPMethod(String Url, String parms, string method, string contanttype)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.Expect100Continue = true;

            string body = String.Empty;
            HttpWebRequest request = WebRequest.Create(Url) as HttpWebRequest;
            request.Method = method;
            request.ContentType = contanttype;

            //Prod
            //string credentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes("merchant.879992000:aadcb97382643ae56ae3749d023f34d6"));
            //request.Headers.Add("Authorization", "Basic " + credentials);

            //UAT
            //string credentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes("merchant.TEST999999955:e16091db04828f4f6e4150a62adf4583"));
            //request.Headers.Add("Authorization", "Basic " + credentials);

            //marfa
            //string credentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes("merchant.TEST200200006504:9c3ff77d6a68ea4c0886e9029d697285"));
            //request.Headers.Add("Authorization", "Basic " + credentials);

            //marfa
            //string credentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes("merchant.200200006504:74f65f473641043fb46042e571a2ee73"));
            //request.Headers.Add("Authorization", "Basic " + credentials);

            string credentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes("merchant.TEST999999124:5a3731aa8ea102bc0fd1f42bb8660819"));
            request.Headers.Add("Authorization", "Basic " + credentials);

            try
            {
                if ((method == "PUT" || method == "POST") && !String.IsNullOrEmpty(parms))
                {
                    byte[] utf8bytes = Encoding.UTF8.GetBytes(parms);
                    byte[] iso8859bytes = Encoding.Convert(Encoding.UTF8, Encoding.GetEncoding("iso-8859-1"), utf8bytes);

                    request.ContentLength = iso8859bytes.Length;

                    using (Stream postStream = request.GetRequestStream())
                    {
                        postStream.Write(iso8859bytes, 0, iso8859bytes.Length);
                    }
                }
                try
                {
                    using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                    {
                        StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding("iso-8859-1"));
                        body = reader.ReadToEnd();
                    }
                }
                catch (WebException wex)
                {
                    StreamReader reader = new StreamReader(wex.Response.GetResponseStream(), Encoding.GetEncoding("iso-8859-1"));
                    body = reader.ReadToEnd();
                }

                return body;
            }
            catch (Exception ex)
            {
                return ex.Message + "\n\naddress:\n" + request.Address.ToString() + "\n\nheader:\n" + request.Headers.ToString() + "data submitted:\n" + parms;
            }

        }


        public string generateSampleId()
        {
            return Guid.NewGuid().ToString().Replace("-", string.Empty).Substring(0, 10);
        }

        public string generatetransactionref(string TransactionAmount)
        {
            double totalamount = double.Parse(TransactionAmount);

            double percenttage = 0.0095 * Math.Round((Double)totalamount, 2);

            double total = Math.Round((Double)percenttage, 2) + Math.Round((Double)totalamount, 2);

            string tref = Math.Round((Double)totalamount, 2) + ":" + Math.Round((Double)percenttage, 2) + ":" + Math.Round((Double)total, 2);

            return tref;
        }

        public string payload(string apioperation)
        {
            NameValueCollection nvc = new NameValueCollection();
            if (apioperation == "INITIATE_AUTHENTICATION")
            {
                nvc.Add("apiOperation", apioperation);
                if(is3d2)
                {
                    nvc.Add("authentication.acceptVersions", "3DS1,3DS2");
                }
                else
                {
                    nvc.Add("authentication.acceptVersions", "3DS1");
                }
                nvc.Add("authentication.channel", "PAYER_BROWSER");
                nvc.Add("authentication.purpose", "PAYMENT_TRANSACTION");
                nvc.Add("order.currency", "AED");
                nvc.Add("session.id", sessionid);
                //nvc.Add("transaction.reference", generatetransactionref("5000"));
            }
            else if (apioperation == "AUTHENTICATE_PAYER")
            {
                nvc.Add("apiOperation", "AUTHENTICATE_PAYER");
                nvc.Add("authentication.redirectResponseUrl", "http://localhost:65290/Home/Contact");
                nvc.Add("order.amount", getSessionValueAsString("amount"));
                nvc.Add("order.currency", getSessionValueAsString("currency"));
                nvc.Add("device.browser", "CHROME");
                nvc.Add("device.browserDetails.3DSecureChallengeWindowSize", "FULL_SCREEN");
                nvc.Add("device.browserDetails.acceptHeaders", "application/json");
                nvc.Add("device.browserDetails.colorDepth", "24");
                nvc.Add("device.browserDetails.javaEnabled", "true");
                nvc.Add("device.browserDetails.language", "en-US");
                nvc.Add("device.browserDetails.screenHeight", "640");
                nvc.Add("device.browserDetails.screenWidth", "480");
                nvc.Add("device.browserDetails.timeZone", "273");
                nvc.Add("session.id", sessionid);

            }
            else if (apioperation == "CHECK_3DS_ENROLLMENT")
            {
                nvc.Add("apiOperation", "CHECK_3DS_ENROLLMENT");
                nvc.Add("3DSecure.authenticationRedirect.responseUrl", "http://localhost:65290/Home/Contact");
                nvc.Add("3DSecure.authenticationRedirect.pageGenerationMode", "CUSTOMIZED");
                nvc.Add("order.currency", "AED");
                nvc.Add("order.amount", "1.00");
                nvc.Add("session.id", sessionid);

            }
            else if (apioperation == "PROCESS_ACS_RESULT")
            {
                nvc.Add("apiOperation", "PROCESS_ACS_RESULT");
                nvc.Add("3DSecure.paRes", paRes);

            }
            else if (apioperation == "PAY")
            {
                nvc.Add("apiOperation", "PAY");
                if (is3d || is3d2)
                {
                    nvc.Add("authentication.transactionId", TransactionId);
                    //nvc.Add("3DSecureId", secureId);
                }
                nvc.Add("order.amount", amount);
                nvc.Add("order.currency", currency);
                nvc.Add("session.id", sessionid);
                //nvc.Add("transaction.reference", generatetransactionref(amount));
            }
            else if (apioperation == "RETRIEVE_ORDER")
            {
                nvc.Add("apiOperation", "RETRIEVE_ORDER");
            }
            else if (apioperation == "VOID")
            {
                nvc.Add("apiOperation", "VOID");
                nvc.Add("transaction.targetTransactionId", TransactionId);
            }
            else if (apioperation == "CreateSession")
            {
                nvc.Add("session.authenticationLimit", "25");
            }

            Payload = JsonHelper.BuildJsonFromNVC(nvc);
            return Payload;
        }

        protected void setSessionValue(String key, String value)
        {
            removeSessionValue(key);
            FakeSession.Add(key, value);
        }

        protected String getSessionValueAsString(String key)
        {
            String value;

            if (FakeSession.ContainsKey(key))
            {
                value = FakeSession[key];
            }
            else
            {
                value = null;
            }

            return value;
        }

        protected void removeSessionValue(String key)
        {
            if (FakeSession.ContainsKey(key))
            {
                FakeSession.Remove(key);
            }

        }

        public ActionResult Contact()
        {
            String response = null;

            if (is3d || is3d2)
            {
                secureId = getSessionValueAsString("secureId");
                sessionid = getSessionValueAsString("sessionId");
                amount = getSessionValueAsString("amount");
                currency = getSessionValueAsString("currency");
                Orderid = getSessionValueAsString("orderId");
                TransactionId = getSessionValueAsString("transactionId");

                removeSessionValue("secureId");
                removeSessionValue("sessionId");
                removeSessionValue("amount");
                removeSessionValue("currency");
                removeSessionValue("orderId");
                removeSessionValue("transactionId");
                
            }

            string trnid = generateSampleId();

            if(is3d2)
            {
                Orderid = Request.Form["order.id"];
                TransactionId = Request.Form["transaction.id"];
            }

            loop:
            Payload = payload("PAY");
            response = executeHTTPMethod("https://test-network.mtf.gateway.mastercard.com/api/rest/version/61/merchant/TEST200200006504/order/" + Orderid + "/transaction/" + trnid, Payload, "PUT", "application/json; charset=iso-8859-1");

            if (response.Contains("Form Session not found or expired"))
            {
                Thread.Sleep(2000);
                goto loop;
            }

            setSessionValue("Operation", "PAY");
            setSessionValue("Method", "PUT");
            setSessionValue("RequestUrl", "https://test-gateway.mastercard.com/api/rest/version/61/merchant/TEST999999955/order/" + Orderid + "/transaction/" + trnid);
            setSessionValue("Payload", Payload);
            setSessionValue("Response", JsonHelper.prettyPrint(response));

            return RedirectToAction("Contactt","Home");
        }

        public ActionResult Contactt()
        {

            ViewBag.Operation =  getSessionValueAsString("Operation");
            ViewBag.Method =     getSessionValueAsString("Method"); 
            ViewBag.RequestUrl = getSessionValueAsString("RequestUrl"); 
            ViewBag.Payload =    getSessionValueAsString("Payload"); 
            ViewBag.Response =   getSessionValueAsString("Response");

            removeSessionValue("Operation");
            removeSessionValue("Method");
            removeSessionValue("RequestUrl");
            removeSessionValue("Payload");
            removeSessionValue("Response");

            return View();
        }

        public ActionResult SessionCheckout()
        {
            string response = "";

            if (ordertype == "RETRIEVE_ORDER")
            {
                Payload = payload("RETRIEVE_ORDER");
                response = executeHTTPMethod("https://test-gateway.mastercard.com/api/rest/version/61/merchant/TEST999999955/order/" + Orderid, Payload, "GET", "application/json; charset=iso-8859-1");
                ViewBag.Operation = "RETRIEVE_ORDER";
                ViewBag.Method = "GET";
                ViewBag.RequestUrl = "https://test-gateway.mastercard.com/api/rest/version/61/merchant/TEST999999955/order/" + Orderid;
            }
            else if (ordertype == "PURCHASE")
            {
                Payload = payload("RETRIEVE_ORDER");
                response = executeHTTPMethod("https://test-gateway.mastercard.com/api/rest/version/61/merchant/TEST999999955/order/" + Orderid, Payload, "GET", "application/json; charset=iso-8859-1");
                ViewBag.Operation = "PURCHASE";
                ViewBag.Method = "POST";
                ViewBag.RequestUrl = "https://test-gateway.mastercard.com/api/nvp/version/61";
                //ViewBag.Payload = Payload;

            }
            else if (ordertype == "VOID")
            {
                string txnid = generateSampleId();
                Payload = payload("VOID");
                response = executeHTTPMethod("https://test-gateway.mastercard.com/api/rest/version/61/merchant/TEST999999955/order/" + Orderid + "/transaction/" + txnid, Payload, "PUT", "application/json; charset=iso-8859-1");
                ViewBag.Operation = "VOID";
                ViewBag.Method = "PUT";
                ViewBag.RequestUrl = "https://test-gateway.mastercard.com/api/rest/version/61/merchant/TEST999999955/order/" + Orderid + "/transaction/" + txnid;
                //ViewBag.Payload = Payload;

            }
            ViewBag.Response = JsonHelper.prettyPrint(response);

            return View();
        }

    }
}