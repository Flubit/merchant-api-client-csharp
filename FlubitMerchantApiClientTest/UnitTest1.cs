using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FlubitMerchantApiClient;
using FlubitMerchantApiClient.Exception;
using System.Xml;

namespace FlubitMerchantApiClientTest
{
    [TestClass]
    public class UnitTest1
    {
        protected  XmlDocument CreateXmlDocument(string xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            return doc;
        }
        [TestMethod]
        public void TestMethod1()
        {
            Client client = new Client("3852-7829-6555", "ujgdkrmngi8c00ssgk04ss0ssc08k0cg40w8cgo0", "api.sandbox.weflubit.com");
            try
            {
                XmlDocument result = client.GetAccountStatus();
                var s = result.OuterXml;
                int.Parse(result.GetElementsByTagName("active_products").Item(0).InnerText);
            }
            catch (Exception)
            {
                Assert.Fail();
            }


            try
            {
                XmlDocument result = client.GetProductsFeed("1234");
            }
            catch (BadMethodCallException e)
            {
                Assert.IsTrue(e.ErrorCode == 404);
            }
            catch (Exception e)
            {
                Assert.Fail();
            }


            string xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><products><product sku=\"123456SKU\"><title>iPhone 5 Hybrid Rubberised Back Cover Case</title><identifiers>" +
            "<identifier type=\"ASIN\">B008OSEQ64</identifier></identifiers></product></products>";
            string feedId = null;
            try
            {

                XmlDocument result = client.CreateProducts(CreateXmlDocument(xml));
            }
            catch (Exception)
            {
                Assert.Fail();
            }

            try
            {
                XmlDocument result = client.GetOrders(DateTime.Now.AddYears(-1), "awaiting_dispatch");
            }
            catch (Exception)
            {
                Assert.Fail();
            }
        }
    }
}
