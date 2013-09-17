using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace FlubitMerchantApiClient
{
    public interface ClientInterface
    {
        XmlDocument GetAccountStatus();

        XmlDocument DispatchOrderByFlubitId(string id, DateTime dateTime, Dictionary<string, string> parameters);

        XmlDocument DispatchOrderByMerchantOrderId(string id, DateTime dateTime, Dictionary<string, string> parameters);

        XmlDocument CancelOrderByFlubitId(string id, string reason);

        XmlDocument CancelOrderByMerchantOrderId(string id, string reason);

        XmlDocument RefundOrderByFlubitId(string id);

        XmlDocument RefundOrderByMerchantOrderId(string id);

        XmlDocument GetOrders(DateTime from, string status);

        XmlDocument GetProductsFeed(string feedID);

        XmlDocument GetProducts(bool isActive, string sku, int limit, int page);

        XmlDocument CreateProducts(XmlDocument productXml);

        XmlDocument UpdateProducts(XmlDocument productXml);
    }
}
