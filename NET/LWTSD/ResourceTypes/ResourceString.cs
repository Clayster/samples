using System;
using System.Xml;
using System.Xml.Linq;
using System.Collections.Generic;

namespace LWTSD.ResourceTypes
{
	public class ResourceString : Resource
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

		public ResourceString()
		{
		}

		public ResourceString(XElement Element)
			: base(Element)
		{
			foreach (var it in Element.Elements( LWTSD.Namespace+ "point"))
			{
				DateTime TimeStamp = XmlConvert.ToDateTime(it.Attribute("timestamp").Value, 
				                                           XmlDateTimeSerializationMode.Utc);

                if (HistoricalValues == null)
                    HistoricalValues = new List<Tuple<DateTime, string>>();

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
			return base.GetDescription(SimplifiedType.String);
		}

		public override void LoadFromWrite(XElement WriteElement)
		{
			this.Value = WriteElement.Value;
		}
	}
}
