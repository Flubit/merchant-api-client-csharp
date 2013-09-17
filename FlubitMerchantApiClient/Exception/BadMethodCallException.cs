using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlubitMerchantApiClient.Exception
{
    public class BadMethodCallException : System.Exception
    {
        public BadMethodCallException(string message, int errorCode)
            : base(message)
        {
            ErrorCode = errorCode;
        }

        public int ErrorCode { get; protected set; }
    }
}
