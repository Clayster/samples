using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace CDO
{
	public class Resource
	{
		public string Path;
		public List<string> Capabilities = new List<string>();
		public Dictionary<string, string> MetaAttributes = new Dictionary<string, string>();
		public List<DataVerb> SupportedVerbs = new List<DataVerb>();

		public XElement GetXML()
		{
			XElement RVal = new XElement(CDO.Namespace + "resource");

			CDO.SetResourcePath(RVal, "path", Path);
			XElement CapEL = CDO.StartList(RVal, "capabilities");
			foreach (string Cap in Capabilities)
			{
				CDO.SetString(CapEL, Cap);
			}

			if (MetaAttributes != null && MetaAttributes.Count > 0)
			{
				XElement DictEl = CDO.StartDictionary(RVal, "metaattributes");
				foreach (var item in MetaAttributes)
				{
					XElement ItemEl = CDO.StartDictionaryItem(DictEl);
					CDO.SetString(ItemEl, "key", item.Key);
					CDO.SetString(ItemEl, "value", item.Value);
				}
			}

			XElement SupVerbEl = CDO.StartList(RVal, "supportedverbs");
			foreach (DataVerb verb in SupportedVerbs)
			{
				CDO.SetDataVerb(SupVerbEl, verb);
			}

			return RVal;
		}
	}
}
