using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace CDO
{	
	public class ResourceAccess
	{
		public string ResourcePath;
		public bool Subordinates;
		public List<DataVerb> Verbs = new List<DataVerb>();
		public DateTime ?WindowFrom;
		public DateTime ?WindowTo;

		public void AddToElement(XElement Element)
		{
			XElement MyEl = new XElement(CDO.Namespace + "resourceaccess");
			CDO.SetResourcePath(MyEl, "path", ResourcePath);
			CDO.SetBoolean(MyEl, "subordinates", Subordinates);
			CDO.SetTimeframe(MyEl, "window", WindowFrom, WindowTo);

			XElement VerbsEl = CDO.StartList(MyEl, "verbs");
			foreach (var it in Verbs)
			{
				CDO.SetDataVerb(VerbsEl, it);
			}
			Element.Add(MyEl);
		}

		public ResourceAccess()
		{
		}

		public ResourceAccess(XElement Element)
		{
			this.ResourcePath = CDO.GetResourcePath(Element, "path");
			this.Subordinates = CDO.GetBoolean(Element, "subordinates");
			Tuple<DateTime?, DateTime?> Window = CDO.GetTimeframe(Element, "window");
			if (Window != null)
			{
				WindowFrom = Window.Item1;
				WindowTo = Window.Item2;
			}
			XElement VerbsEl = CDO.GetList(Element, "verbs");
			var VerbsElIt = VerbsEl.Elements(CDO.Namespace + "dataverb");
			foreach (var verbel in VerbsElIt)
			{
				Verbs.Add(CDO.GetDataVerb(verbel));
			}
							   
		}
	}
}
