using System;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Collections.Generic;

namespace LWTSD
{
	public enum ErrorReason
	{
		Forbidden,
		Timeout,
		ResourceDoesNotExist,
		InvalidData
	}

	static class ErrorReasonMethods
	{
		public static string GetSerializedValue(this ErrorReason a)
		{
			switch (a)
			{
				case ErrorReason.Forbidden:
					return "forbidden";
				case ErrorReason.Timeout:
					return "timeout";
				case ErrorReason.ResourceDoesNotExist:
					return "resource-does-not-exist";
				case ErrorReason.InvalidData:
					return "invalid-data";
				default:
					throw new Exception("Error reason has invalid value: " + a.ToString());
			}
		}

		public static ErrorReason LoadFromString(string b)
		{
			switch (b)
			{
				case "forbidden":
					return ErrorReason.Forbidden;
				case "timeout":
					return ErrorReason.Timeout;
				case "resource-does-not-exist":
					return ErrorReason.ResourceDoesNotExist;
				case "invalid-data":
					return ErrorReason.InvalidData;

				default:
					throw new Exception("Invalid string for sort order: " + b);
			}
		}
	}
}