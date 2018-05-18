using System;
using System.Xml;
using System.Xml.Linq;
using System.Collections.Generic;

namespace LWTSD
{
	public class SimplifiedSchema
	{
		public Dictionary<ResourcePath, ResourceDescription> Resources;
		string LoadedVersion = string.Empty;
		int LoadedTotalResources = -1;
		int LoadedNrResources = -1;

		int GetTotalResources()
		{
			if (LoadedTotalResources != -1)
				return LoadedTotalResources;

			return Resources.Count;
		}

		int GetResourcesInSchema()
		{			
			return Resources.Count;
		}

        public string GetVersion()
        {
			if (LoadedVersion != string.Empty)
				return LoadedVersion;
			
            XElement Temp = new XElement(LWTSD.Namespace +"temp");
            foreach (ResourceDescription Res in Resources.Values)
            {
                Temp.Add(Res.GetSimplifiedSchemaResource());
            }

            return Temp.ToString().GetHashCode().ToString("X");
        }

        public XElement GetSerializedElement(int startindex, 
		                                     int maxitems, 
		                                     List<ResourceAccess> LimitedRights = null)
        {
            if (startindex < 0)
                throw new InvalidOperationException("SimplifiedSchema: Start index < 0");
			if (maxitems < 0)
				throw new InvalidOperationException("SimplifiedSchema: Max items < 0");
			
            XElement RVal = new XElement( LWTSD.Namespace+ "simplified-schema");


            int Index = 0;
            int ReturnedResources = 0;
			int TotalResources = 0;
            foreach (ResourceDescription Res in Resources.Values)
            {
				if (!ResourceAccess.AllowsRead(LimitedRights, Res.Path) 
				    && !ResourceAccess.AllowsWrite(LimitedRights, Res.Path))
				{
					continue;
				}
				TotalResources++;

                if (Index < startindex)
                {
                    Index++;
                    continue;
                }
                if (maxitems <= (Index + startindex))
                    break;
                Index++;

                RVal.Add(Res.GetSimplifiedSchemaResource());
                ReturnedResources++;
            }

            RVal.SetAttributeValue("returnedresources", XmlConvert.ToString(ReturnedResources));
            RVal.SetAttributeValue("totalresources", XmlConvert.ToString(TotalResources));
            RVal.SetAttributeValue("version", GetVersion());

            return RVal;
        }

        public SimplifiedSchema()
        {
            Resources = new Dictionary<ResourcePath, ResourceDescription>();
        }

		public SimplifiedSchema(XElement Element)
		{
			Resources = new Dictionary<ResourcePath, ResourceDescription>();
			LoadedVersion = Element.Attribute("version").Value;
			LoadedNrResources = XmlConvert.ToInt32(Element.Attribute("returnedresources").Value);
			LoadedTotalResources = XmlConvert.ToInt32(Element.Attribute("totalresources").Value);
			foreach (XElement It in Element.Elements())
			{
				if (It.Name.LocalName != "resource")
					continue;
				ResourceDescription LoadedDesc = new ResourceDescription(It);
				Resources[LoadedDesc.Path] = LoadedDesc;
			}
		}


	}
}
