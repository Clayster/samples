using System;
using System.Xml;
using System.Xml.Linq;
using System.Collections.Generic;

namespace LWTSD.ResourceTypes
{
	public class ResourceBoolean : Resource
	{
		public readonly List<Tuple<DateTime, bool>> HistoricalValues = null;

		public bool Value;

		public override XElement GetPoint()
		{
			XElement Point = base.GetPoint();
			Point.Value = XmlConvert.ToString(Value);
			return Point;
		}

		public override ResourceDescription GetDescription()
		{
			return base.GetDescription(SimplifiedType.Boolean);
		}

		public override void LoadFromWrite(XElement WriteElement)
		{
			this.Value = XmlConvert.ToBoolean(WriteElement.Value);
		}

		public override string GetWriteValue()
		{
			return Value.ToString();
		}

		public ResourceBoolean(XElement Element)
			: base(Element)
		{
			ModifiedAt = DateTime.MinValue;
			foreach (var it in Element.Elements(LWTSD.Namespace + "point"))
			{
				if (HistoricalValues == null)
					HistoricalValues = new List<Tuple<DateTime, bool>>();
				DateTime TimeStamp = XmlConvert.ToDateTime(it.Attribute("timestamp").Value,
														   XmlDateTimeSerializationMode.Utc);
				bool PointValue = XmlConvert.ToBoolean(it.Value);
				HistoricalValues.Add(new Tuple<DateTime, bool>(TimeStamp, PointValue));

				if (TimeStamp > ModifiedAt)
				{
					ModifiedAt = TimeStamp;
					Value = PointValue;
				}

			}
		}

		public ResourceBoolean()
		{
		}

	}
}
