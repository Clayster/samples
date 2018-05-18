using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Xml;
using System.Linq;

namespace CDO
{
	public class ActionRequest
	{
		public string ID = Guid.NewGuid().ToString();
		public string Name;
		public DateTime TimesOut;
		public DateTime AckTimesOut;
		public XElement Payload;

		public XElement GetXML()
		{
			XElement RVal = new XElement(CDO.Namespace + "actionrequest");
			RVal.SetAttributeValue("id", ID);
			RVal.SetAttributeValue("name", Name);
			RVal.SetAttributeValue("timeouts", XmlConvert.ToString(TimesOut, XmlDateTimeSerializationMode.Utc));
			RVal.SetAttributeValue("acktimeouts", XmlConvert.ToString(AckTimesOut, XmlDateTimeSerializationMode.Utc));

			RVal.Add(Payload);
			return RVal;
		}

		public ActionRequest()
		{
		}

		public ActionRequest(XElement Element)
		{
			this.Name = Element.Attribute("name").Value;
			this.ID = Element.Attribute("id").Value;
			this.TimesOut = XmlConvert.ToDateTime(Element.Attribute("timeouts").Value, XmlDateTimeSerializationMode.Utc);
			this.AckTimesOut = XmlConvert.ToDateTime(Element.Attribute("acktimeouts").Value, XmlDateTimeSerializationMode.Utc);
			this.Payload = Element.Elements().First();
		}

	}
}
