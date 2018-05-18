using System;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Collections.Generic;

namespace LWTSD
{

	public enum SortOrder
	{
		None,
		Ascending,
		Descending
	}

	static class SortOrderMethods
	{
		public static string GetSerializedValue(this SortOrder a)
		{
			switch (a)
			{
				case SortOrder.Ascending:
					return "ascending";
				case SortOrder.Descending:
					return "descending";
				case SortOrder.None:
					return null;
				default:
					throw new Exception("Sort order has invalid value: " + a.ToString());
			}
		}

		public static SortOrder LoadFromString(string b)
		{
			switch (b)
			{
				case null:
					return SortOrder.None;
				case "ascending":
					return SortOrder.Ascending;
				case "descending":
					return SortOrder.Descending;
				case "":
					return SortOrder.None;
				default:
					throw new Exception("Invalid string for sort order: " + b);
			}
		}
	}
}