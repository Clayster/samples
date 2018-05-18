using System;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Collections.Generic;

namespace LWTSD
{

	public class VerifySubscription
	{
		public readonly string SubscriptionID;

		public VerifySubscription(string SubscriptionID)
		{
			this.SubscriptionID = SubscriptionID;
		}

		public VerifySubscription(XElement Element)
		{
			SubscriptionID = Element.Attribute("subscriptionid").Value;
		}
	}
}