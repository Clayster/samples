using System;
using System.Xml;
using System.Xml.Linq;

namespace LWTSD.ResourceTypes
{
	public abstract class Resource
	{
		public ResourcePath Path;
		public DateTime ModifiedAt;

		public string Description;
		public string Displayname;
		public string Unit;
		public bool SupportsRead;
		public bool SupportsWrite;

		protected object InnerMinExclusive;
		protected object InnerMaxExclusive;
		protected int? Length;

		public readonly int TotalPointsForWindow = 1;

		public virtual XElement GetPoint()
		{
			XElement RVal = new XElement(LWTSD.Namespace + "point");
			RVal.SetAttributeValue("timestamp",
									XmlConvert.ToString(ModifiedAt, XmlDateTimeSerializationMode.Utc));

			return RVal;

		}

		protected ResourceDescription GetDescription(SimplifiedType SimpleType)
		{
			return new ResourceDescription()
			{
				Description = Description,
				DisplayName = Displayname,
				Path = Path,
				SimpleType = SimpleType,
				SupportsRead = SupportsRead,
				SupportsWrite = SupportsWrite,
				Restrictions = new ResourceDescription.Restriction()
				{
					MinExclusive = InnerMinExclusive,
					MaxExclusive = InnerMaxExclusive,
					Length = Length
				}
			};
		}
		public abstract ResourceDescription GetDescription();
		public abstract void LoadFromWrite(XElement WriteElement);
		public abstract string GetWriteValue();

		public Resource(XElement Element)
		{
			TotalPointsForWindow = XmlConvert.ToInt32(Element.Attribute("totalpoints").Value);
			Path = Element.Attribute("path").Value;
		}

		public Resource()
		{
		}
	}
}
