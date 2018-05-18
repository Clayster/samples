using System;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Collections.Generic;

namespace LWTSD
{
	public class SubscriptionCancelled
	{
		public readonly string SubscriptionID;

		public SubscriptionCancelled(string SubscriptionID)
		{
			this.SubscriptionID = SubscriptionID;
		}

		public SubscriptionCancelled(XElement Element)
		{
			SubscriptionID = Element.Attribute("subscriptionid").Value;
		}
	}
}