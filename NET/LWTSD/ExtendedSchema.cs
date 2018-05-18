using System;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Collections.Generic;

namespace LWTSD
{
	public class ExtendedSchema
	{
		public XmlSchema Schema;

		public Dictionary<ResourcePath, ResourceDescription> Resources;
	}
}
