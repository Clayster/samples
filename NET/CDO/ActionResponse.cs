using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;

namespace CDO
{
	public class ActionResponse
	{
		public string ID;
		public bool Successful;
		public bool TimedOut = false;
		/*
		public List<string> InformationMessages = new List<string>();
		public List<string> WarningMessages = new List<string>();
		public List<string> ErrorMessages= new List<string>();*/
		public XElement Payload;

		public ActionResponse(XElement Element)
		{
			ID = Element.Attribute("id").Value;

			Payload = Element.Elements().First();
			Successful = CDO.GetBoolean(Payload, "successful");
		}

		public ActionResponse()
		{
		}

		public XElement GetXML(string ReturnTypename, string ActionName)
		{
			XElement RespEl = new XElement(CDO.Namespace + "actionresponse");
			RespEl.SetAttributeValue("id", ID);
			RespEl.SetAttributeValue("name", ActionName);
			XElement GenRespEl = new XElement(CDO.Namespace + ReturnTypename);
			CDO.SetBoolean(GenRespEl, "successful", Successful);
			RespEl.Add(GenRespEl);
			return RespEl;
		}
	}
}
