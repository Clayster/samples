using System;
using System.Xml;
using System.Xml.Linq;
using System.Collections.Generic;

namespace LWTSD.ResourceTypes
{
	public class ResourceDateTime : Resource
	{
		public readonly List<Tuple<DateTime, DateTime>> HistoricalValues = null;

		DateTime InnerValue;
		public DateTime Value
		{
			get
			{
				return InnerValue;
			}

			set
			{
				if (MinExclusive != null)
					if (value < MinExclusive)
						throw new InvalidOperationException("Value not within limits");
				if (MaxExclusive != null)
					if (value > MaxExclusive)
						throw new InvalidOperationException("Value not within limits");

				InnerValue = value;

				ModifiedAt = DateTime.UtcNow;
			}
		}

		public DateTime? MinExclusive
		{
			get
			{
				if (InnerMinExclusive == null)
					return null;

				return (DateTime) InnerMinExclusive;
			}

			set
			{
				InnerMinExclusive = value;
			}
		}
		public DateTime? MaxExclusive
		{
			get
			{
				if (InnerMaxExclusive == null)
					return null;

				return (DateTime) InnerMaxExclusive;
			}

			set
			{
				InnerMaxExclusive = value;
			}
		}

		public override XElement GetPoint()
		{
			XElement Point = base.GetPoint();
			Point.Value = XmlConvert.ToString(Value, XmlDateTimeSerializationMode.Utc);
			return Point;
		}

		public override ResourceDescription GetDescription()
		{
			return base.GetDescription(SimplifiedType.DateTime);
		}

		public override void LoadFromWrite(XElement WriteElement)
		{
			this.Value = XmlConvert.ToDateTime(WriteElement.Value, XmlDateTimeSerializationMode.Utc);
		}

		public override string GetWriteValue()
		{
			return Value.ToString();
		}

		public ResourceDateTime(XElement Element)
			: base(Element)
		{
			ModifiedAt = DateTime.MinValue;
			foreach (var it in Element.Elements(LWTSD.Namespace + "point"))
			{
				if (HistoricalValues == null)
					HistoricalValues = new List<Tuple<DateTime, DateTime>>();
				DateTime TimeStamp = XmlConvert.ToDateTime(it.Attribute("timestamp").Value,
														   XmlDateTimeSerializationMode.Utc);
				DateTime PointValue = XmlConvert.ToDateTime(it.Value, XmlDateTimeSerializationMode.Utc);
				HistoricalValues.Add(new Tuple<DateTime, DateTime>(TimeStamp, PointValue));

				if (TimeStamp > ModifiedAt)
				{
					ModifiedAt = TimeStamp;
					InnerValue = PointValue;
				}

			}
		}

		public ResourceDateTime()
		{
		}

	}
}
