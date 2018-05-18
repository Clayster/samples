using System;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Collections.Generic;

namespace LWTSD
{
	public class SubscriptionTriggered
	{
		public readonly string SubscriptionID;

		public SubscriptionTriggered(string SubscriptionID)
		{
			this.SubscriptionID = SubscriptionID;
		}

		public SubscriptionTriggered(XElement Element)
		{
			SubscriptionID = Element.Attribute("subscriptionid").Value;
		}
	}
}