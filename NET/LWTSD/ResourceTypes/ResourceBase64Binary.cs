using System;
using System.Xml;
using System.Xml.Linq;
using System.Collections.Generic;

namespace LWTSD.ResourceTypes
{
	public class ResourceBase64Binary : Resource
	{
		public readonly List<Tuple<DateTime, string>> HistoricalValues = null;

		string InnerValue;

		public string Value
		{
			get
			{
				return InnerValue;
			}

			set
			{
				InnerValue = value;
				ModifiedAt = DateTime.UtcNow;
			}
		}

		public ResourceBase64Binary()
		{
		}

		public ResourceBase64Binary(XElement Element)
			: base(Element)
		{
			foreach (var it in Element.Elements(LWTSD.Namespace + "point"))
			{
                if (HistoricalValues == null)
                    HistoricalValues = new List<Tuple<DateTime, string>>();

                DateTime TimeStamp = XmlConvert.ToDateTime(it.Attribute("timestamp").Value,
														   XmlDateTimeSerializationMode.Utc);
				string PointValue = it.Value;
				HistoricalValues.Add(new Tuple<DateTime, string>(TimeStamp, PointValue));
				if (TimeStamp > ModifiedAt)
				{
					ModifiedAt = TimeStamp;
					InnerValue = PointValue;
				}
			}
		}

		public override XElement GetPoint()
		{
			XElement Point = base.GetPoint();
			Point.Value = Value;
			return Point;
		}

		public override string GetWriteValue()
		{
			return Value;
		}

		public override ResourceDescription GetDescription()
		{
			return base.GetDescription(SimplifiedType.Base64Binary);
		}

		public override void LoadFromWrite(XElement WriteElement)
		{
			this.Value = WriteElement.Value;
		}
	}
}
