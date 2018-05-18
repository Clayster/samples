using System;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Collections.Generic;
using System.Linq;

namespace LWTSD
{
	public class LWTSD
	{
		public static readonly XNamespace Namespace = "urn:clayster:lwtsd";

		public static string GetAccessToken(XElement Element)
		{
			var Tokens = Element.Elements(Namespace + "accesstoken");
			if (Tokens.Count() > 1)
				throw new Exception("This implementation does not support multiple accesstokens");
			if (Tokens.Count() == 0)
				return null;

			return Tokens.First().Value;
		}

	}
}