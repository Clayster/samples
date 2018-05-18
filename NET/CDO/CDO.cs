using System;
using System.Xml.Linq;
using System.Xml;

namespace CDO
{
	public class CDO
	{
        public static XNamespace Namespace = "urn:clayster:cdo";


		public static bool GetBoolean(XElement Element, string Name, bool? DefaultValue = null)
		{
			XElement it =  Element.Element( Namespace + Name);
			if (it == null && DefaultValue == null)
			{
				throw new Exception("Element not found: " + Name);
			}
			if (it == null)
				return DefaultValue.Value;

			XElement Val = it.Element(Namespace + "boolean");
			return XmlConvert.ToBoolean(Val.Value);
		}

		public static int GetInt(XElement Element, string Name, int? DefaultValue = null)
		{
			XElement it = Element.Element(Namespace + Name);
			if (it == null && DefaultValue == null)
			{
				throw new Exception("Element not found: " + Name);
			}
			if (it == null)
				return DefaultValue.Value;

			XElement Val = it.Element(Namespace + "integer");
			return XmlConvert.ToInt32(Val.Value);
		}

		public static DateTime GetTimestamp(XElement Element, string Name, DateTime ?DefaultValue = null)
		{
			XElement it = Element.Element(Namespace + Name);

			if (it == null && DefaultValue == null)
			{
				throw new Exception("Element not found: " + Name);
			}
			if (it == null)
				return DefaultValue.Value;

			XElement Val = it.Element(Namespace + "timestamp");
			return XmlConvert.ToDateTime(Val.Value, XmlDateTimeSerializationMode.Utc);
		}

		public static string GetString(XElement Element, string Name, string DefaultValue = null)
		{
			XElement it = Element.Element(Namespace + Name);

			if (it == null)
				return DefaultValue;

			XElement Val = it.Element(Namespace + "string");
			return Val.Value;
		}

		public static DXMPP.JID GetAddress(XElement Element, string Name, DXMPP.JID DefaultValue = null)
		{
			XElement it = Element.Element(Namespace + Name);

			if (it == null)
				return DefaultValue;

			XElement Val = it.Element(Namespace + "address");
			return new DXMPP.JID( Val.Value );
		}

		public static string GetResourcePath(XElement Element, string Name, string DefaultValue = null)
		{
			XElement it = Element.Element(Namespace + Name);

			if (it == null)
				return DefaultValue;

			XElement Val = it.Element(Namespace + "resourcepath");
			return Val.Value;
		}

		public static string GetString(XElement Element)
		{
			return Element.Value;
		}
		public static DataVerb GetDataVerb(XElement Element)
		{
			switch (Element.Value)
			{
				case "GET":
					return DataVerb.GET;
				case "DELETE":
					return DataVerb.DELETE;
				case "ADD":
					return DataVerb.ADD;
				case "SET":
					return DataVerb.SET;
				default:
					throw new Exception("Invalid verb: " + Element.Value);
			}
		}

		public static System.Tuple<DateTime?, DateTime?> GetTimeframe(XElement Element, string Name)
		{
			
			XElement ValElement = new XElement(CDO.Namespace + Name);
			if (ValElement == null)
				return null;
			DateTime? ValueFrom = null;
			DateTime? ValueTo = null;

			try
			{
				ValueFrom = GetTimestamp(ValElement, "from");
			}
			catch
			{
				// No
			}

			try
			{
				ValueTo = GetTimestamp(ValElement, "to");
			}
			catch
			{
			 	// No
			}


			return new Tuple<DateTime?, DateTime?>(ValueFrom, ValueTo);
		}

		public static void SetDataVerb(XElement Element, DataVerb Value)
		{
			XElement El = new XElement(CDO.Namespace + "dataverb");
			El.Value = Value.ToString();
			Element.Add(El);
		}

		public static void SetDataVerb(XElement Element, string Name, DataVerb Value)
		{
			XElement ValElement = new XElement(CDO.Namespace + Name);
			SetDataVerb(ValElement, Value);
			Element.Add(ValElement);
		}

		public static void SetResourcePath(XElement Element, string Value)
		{
			XElement El = new XElement(CDO.Namespace + "resourcepath");
			El.Value = Value.ToString();
			Element.Add(El);
		}

		public static void SetResourcePath(XElement Element, string Name, string Value)
		{
			XElement ValElement = new XElement(CDO.Namespace + Name);
			SetResourcePath(ValElement, Value);
			Element.Add(ValElement);
		}

		public static void SetEntityID(XElement Element, string Value)
		{
			XElement El = new XElement(CDO.Namespace + "entityid");
			El.Value = Value.ToString();
			Element.Add(El);
		}

		public static void SetEntityID(XElement Element, string Name, string Value)
		{
			XElement ValElement = new XElement(CDO.Namespace + Name);
			SetEntityID(ValElement, Value);
			Element.Add(ValElement);
		}

		public static void SetString(XElement Element, string Value)
		{
			XElement El = new XElement(CDO.Namespace + "string");
			El.Value = Value;
			Element.Add(El);
		}

		public static void SetString(XElement Element, string Name, string Value)
		{
			XElement ValElement = new XElement(CDO.Namespace + Name);
			SetString(ValElement, Value);
			Element.Add(ValElement);
		}

		public static void SetBoolean(XElement Element, string Name, bool Value)
		{
			XElement ValElement = new XElement(CDO.Namespace + Name);
			XElement El = new XElement(CDO.Namespace + "boolean");
			El.Value = System.Xml.XmlConvert.ToString( Value );
			ValElement.Add(El);
			Element.Add(ValElement);
		}

		public static void SetInt(XElement Element, string Name, int Value)
		{
			XElement ValElement = new XElement(CDO.Namespace + Name);
			XElement El = new XElement(CDO.Namespace + "integer");
			El.Value = System.Xml.XmlConvert.ToString(Value);
			ValElement.Add(El);
			Element.Add(ValElement);
		}

		public static void SetDateTime(XElement Element, string Name, DateTime Value)
		{
			XElement ValElement = new XElement(CDO.Namespace + Name);
			XElement El = new XElement(CDO.Namespace + "timestamp");
			El.Value = System.Xml.XmlConvert.ToString(Value, XmlDateTimeSerializationMode.Utc);
			ValElement.Add(El);
			Element.Add(ValElement);
		}

		public static void SetTimeframe(XElement Element, string Name, DateTime ?ValueFrom, DateTime ?ValueTo)
		{
			if (ValueFrom == null && ValueTo == null)
				return;
			
			XElement ValElement = new XElement(CDO.Namespace + Name);

			if(ValueFrom != null)
				SetDateTime(ValElement, "from", ValueFrom.Value);
			if (ValueTo != null)
				SetDateTime(ValElement, "to", ValueTo.Value);

			Element.Add(ValElement);
		}




		public static XElement StartList(XElement Element, string Name)
		{
			XElement Container = new XElement(Namespace + Name);			
			XElement RVal = new XElement(Namespace + "list");
			Container.Add(RVal);
			Element.Add(Container);

			return RVal;
		}

		public static XElement StartDictionary(XElement Element, string Name)
		{
			XElement Container = new XElement(Namespace + Name);
			XElement RVal = new XElement(Namespace + "dictionary");
			Container.Add(RVal);
			Element.Add(Container);

			return RVal;
		}

		public static XElement StartDictionaryItem(XElement Element)
		{
			XElement RVal = new XElement(Namespace + "item");
			Element.Add(RVal);
			return RVal;
		}

		public static XElement GetList(XElement Element, string Name)
		{
			XElement it = Element.Element(Namespace + Name);
			if (it == null)
				return null;

			return it.Element(Namespace + "list");
		}

	}
}
