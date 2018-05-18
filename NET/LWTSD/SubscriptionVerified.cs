using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Collections.Generic;

namespace LWTSD
{
	public class SubscriptionVerified
	{
		public readonly string SubscriptionID;

		public SubscriptionVerified(string SubscriptionID)
		{
			this.SubscriptionID = SubscriptionID;
		}

		public SubscriptionVerified(XElement Element)
		{
			SubscriptionID = Element.Attribute("subscriptionid").Value;
		}
	}
}