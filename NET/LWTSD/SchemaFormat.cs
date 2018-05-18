using System;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Collections.Generic;

namespace LWTSD
{

	public enum SchemaFormat
	{
		Simplified,
		Extended
	}

	public static class SchemaFormatMethods
	{
		public static string GetSerializedValue(this SchemaFormat a)
		{
			switch (a)
			{
				case SchemaFormat.Extended:
					return "extended";
				case SchemaFormat.Simplified:
					return "simplified";
				default:
					throw new Exception("Schema format has invalid value: " + a.ToString());
			}
		}

		public static SchemaFormat LoadFromString(string b)
		{
			switch (b)
			{
				case "extended":
					return SchemaFormat.Extended;
				case "simplified":
					return SchemaFormat.Simplified;
				default:
					throw new Exception("Invalid string for schema format: " + b);
			}
		}
	}
}