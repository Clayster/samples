using System;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Collections.Generic;

namespace LWTSD
{

    public enum SimplifiedType
	{
		String,
		Boolean,
		Integer,
		Decimal,
		Double,
		Float,
		Duration,
		Time,
		DateTime,
		Base64Binary 
	}

	public static class SimplifiedTypeMethods
	{
		public static string GetSerializedValue(this SimplifiedType a)
		{
			switch (a)
			{
                case SimplifiedType.String:
                    return "string";
				case SimplifiedType.Integer:
					return "integer";
				case SimplifiedType.Boolean:
					return "boolean";
				case SimplifiedType.Decimal:
					return "decimal";
				case SimplifiedType.Double:
					return "double";
                case SimplifiedType.Float:
					return "float";
				case SimplifiedType.Duration:
					return "duration";
				case SimplifiedType.Time:
					return "time";
				case SimplifiedType.DateTime:
					return "dateTime";
				case SimplifiedType.Base64Binary:
					return "base64Binary";

				default:
					throw new Exception("Simplified type has invalid value: " + a.ToString());
			}
		}

		public static SimplifiedType LoadFromString(string b)
		{
			switch (b)
			{
				case "string":
                    return SimplifiedType.String;
				case "integer":
					return SimplifiedType.Integer;
				case "boolean":
					return SimplifiedType.Boolean;
				case "decimal":
                    return SimplifiedType.Decimal;
				case "double":
					return SimplifiedType.Double;
				case "float":
                    return SimplifiedType.Float;
				case "duration":
					return SimplifiedType.Duration;
				case "time":
					return SimplifiedType.Time;
				case "dateTime":
					return SimplifiedType.DateTime;
				case "base64Binary":
					return SimplifiedType.Base64Binary;
				default:
					throw new Exception("Invalid string for simplified type: " + b);
			}
		}
	}
}