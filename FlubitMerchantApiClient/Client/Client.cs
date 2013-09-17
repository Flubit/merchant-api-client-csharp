using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Xml;
using FlubitMerchantApiClient.Exception;

namespace FlubitMerchantApiClient
{
    public class Client : ClientInterface
    {
        private string _apiSecret;
        private string _apiKey;
        private string _domain;

        public Client(string apiKey, string apiSecret, string domain = "api.weflubit.com")
        {
            this._apiKey = apiKey;
            this._apiSecret = apiSecret;
            this._domain = domain;
        }
        
        protected string ToDFTString(DateTime dateTime)
        {
            return dateTime.ToUniversalTime().ToString("u").Replace(" ", "T");
        }
        protected string GetAuthHeader()
        {
            string created = ToDFTString(DateTime.Now);
            RandomNumberGenerator rng = new RNGCryptoServiceProvider();
            byte[] tokenData = new byte[32];
            rng.GetBytes(tokenData);

            string nonce = Convert.ToBase64String(tokenData);

            SHA1 sha1 = SHA1.Create();

            string signature = Convert.ToBase64String(sha1.ComputeHash(tokenData.Concat(System.Text.UTF8Encoding.UTF8.GetBytes(created + _apiSecret)).ToArray()));
            return  string.Format("key=\"{0}\", signature=\"{1}\", nonce=\"{2}\", created=\"{3}\"", _apiKey, signature, nonce, created);
        }
        protected XmlDocument HandleRequest(HttpWebRequest request)
        {
            request.KeepAlive = false;
            request.Headers["auth-token"] = GetAuthHeader();
            HttpWebResponse response = null;
            try
            {
                response = (HttpWebResponse)request.GetResponse();
                var responseValue = string.Empty;

                var responseStream = response.GetResponseStream();

                if (responseStream != null)
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(responseStream);
                    return doc;
                }
            }
            catch (WebException e)
            {
                response = (HttpWebResponse)e.Response;
            }
            if (response != null)
            {
                var responseStream = response.GetResponseStream();

                if (responseStream != null)
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(responseStream);
                    var error = doc.GetElementsByTagName("error").Item(0);
                    string message = error.Attributes["message"].Value;

                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        int code = int.Parse(error.Attributes["code"].Value);
                        throw new UnauthorizedException(message, code);
                    }
                    else
                        throw new BadMethodCallException(message, (int)response.StatusCode);
                }
            }
            return null;
        }
        protected XmlDocument GetRequest(string uri, Dictionary<string, string> parameters)
        {
            var request = (HttpWebRequest)WebRequest.Create(string.Format("http://{0}/1/{1}{2}", _domain, uri, BuildQueryParams(parameters)));
            request.Method = "GET";
            request.ContentType = "application/atom+xml";
            return HandleRequest(request);
        }
        protected XmlDocument PostRequest(string uri, XmlDocument xml, Dictionary<string, string> parameters)
        {
            var request = (HttpWebRequest)WebRequest.Create(string.Format("http://{0}/1/{1}{2}", _domain, uri, BuildQueryParams(parameters)));
            request.Method = "POST";
            request.Accept = "application/xml";
            if (xml != null)
            {
                var stream = request.GetRequestStream();
                xml.Save(stream);
                stream.Flush();
            }
            return HandleRequest(request);
        }
        protected string BuildQueryParams(Dictionary<string, string> parameters)
        {
            if (parameters != null && parameters.Any())
            {
                StringBuilder builder = new StringBuilder();
                builder.Append("?");
                builder.Append(parameters.First().Key);
                builder.Append("=");
                builder.Append(parameters.First().Value);

                foreach(var param in parameters.Skip(1))
                {
                    builder.Append("&");
                    builder.Append(param.Key);
                    builder.Append("=");
                    builder.Append(param.Value);
                }
                return builder.ToString();
            }
            return "";
        }
        protected XmlDocument GenerateCancelOrderPayload(string reason)
        {
            return CreateXmlDocument(string.Format("<?xml version=\"1.0\" encoding=\"UTF-8\"?><cancel><reason>{0}</reason></cancel>", reason));
        }
        protected XmlDocument GenerateDispatchOrderPayload(DateTime dateTime, Dictionary<string, string> parameters)
        {
            var courier = parameters.ContainsKey("courier") ? parameters["courier"] : "";
            var consignmentNumber = parameters.ContainsKey("consignment_number") ? parameters["consignment_number"] : "";
            var trackingUrl = parameters.ContainsKey("tracking_url") ? parameters["tracking_url"] : "";

            return CreateXmlDocument(string.Format("<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
@"<dispatch>
    <dispatched_at>{0}</dispatched_at>
    <courier>{1}</courier>
    <consignment_number>{2}</consignment_number>
    <tracking_url>{3}</tracking_url>
</dispatch>", ToDFTString(dateTime), courier, consignmentNumber, trackingUrl));
        }
        protected Dictionary<string, string>  CreateDictionary(string key, string value)
        {
            Dictionary<string, string> dic = new Dictionary<string,string>();
            dic.Add(key, value);
            return dic;
        }
        protected XmlDocument CreateXmlDocument(string xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            return doc;
        }
        public XmlDocument GetAccountStatus()
        {
            return GetRequest("account/status.xml", null);
        }
        public XmlDocument DispatchOrderByFlubitId(string id, DateTime dateTime, Dictionary<string, string> parameters)
        {
            var xml = GenerateDispatchOrderPayload(dateTime, parameters);
            return PostRequest("orders/dispatch.xml", xml, CreateDictionary("flubit_order_id", id));
        }

        public XmlDocument DispatchOrderByMerchantOrderId(string id, DateTime dateTime, Dictionary<string, string> parameters)
        {
            var xml = GenerateDispatchOrderPayload(dateTime, parameters);
            return PostRequest("orders/dispatch.xml", xml, CreateDictionary("merchant_order_id", id));
        }

        public XmlDocument CancelOrderByFlubitId(string id, string reason)
        {
            var xml = GenerateCancelOrderPayload(reason);
            return PostRequest("orders/cancel.xml", xml, CreateDictionary("flubit_order_id", id));
        }

        public XmlDocument CancelOrderByMerchantOrderId(string id, string reason)
        {
            var xml = GenerateCancelOrderPayload(reason);
            return PostRequest("orders/cancel.xml", xml, CreateDictionary("merchant_order_id", id));
        }

        public XmlDocument RefundOrderByFlubitId(string id)
        {
            return PostRequest("orders/refund.xml", null, CreateDictionary("flubit_order_id", id));
        }

        public XmlDocument RefundOrderByMerchantOrderId(string id)
        {
            return PostRequest("orders/refund.xml", null, CreateDictionary("merchant_order_id", id));
        }

        public XmlDocument GetOrders(DateTime from, string status)
        {
            var dic = CreateDictionary("from", ToDFTString(from));
            dic.Add("status", status);
            return GetRequest("orders/filter.xml", dic);
        }

        public XmlDocument GetProductsFeed(string feedID)
        {
            return GetRequest(string.Format("products/feed/{0}.xml", feedID), null);
        }

        public XmlDocument CreateProducts(XmlDocument productXml)
        {
            return PostRequest("products/feed.xml", productXml, CreateDictionary("type", "create"));
        }

        public XmlDocument UpdateProducts(XmlDocument productXml)
        {
            return PostRequest("products/feed.xml", productXml, null);
        }


        public XmlDocument GetProducts(bool isActive, string sku, int limit, int page)
        {
            var dic = CreateDictionary("is_active", isActive ? "1" : "0");
            dic.Add("sku", sku);
            dic.Add("limit", limit.ToString());
            dic.Add("page", page.ToString());
            return GetRequest("products/filter.xml", dic);
        }
    }
}
