using System;
using System.Xml;
using System.Xml.Linq;
using System.Collections.Generic;

namespace LWTSD.ResourceTypes
{
	public class ResourceDecimal : Resource
	{
		public readonly List<Tuple<DateTime, Decimal>> HistoricalValues = null;

		Decimal InnerValue;
		public Decimal Value
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

		public Decimal? MinExclusive
		{
			get
			{
				if (InnerMinExclusive == null)
					return null;

				return (Int32) InnerMinExclusive;
			}

			set
			{
				InnerMinExclusive = value;
			}
		}
		public Decimal? MaxExclusive
		{
			get
			{
				if (InnerMaxExclusive == null)
					return null;

				return (Decimal) InnerMaxExclusive;
			}

			set
			{
				InnerMaxExclusive = value;
			}
		}

		public override XElement GetPoint()
		{
			XElement Point = base.GetPoint();
			Point.Value = XmlConvert.ToString(Value);
			return Point;
		}

		public override ResourceDescription GetDescription()
		{
			return base.GetDescription(SimplifiedType.Decimal);
		}

		public override void LoadFromWrite(XElement WriteElement)
		{
			this.Value = XmlConvert.ToInt32(WriteElement.Value);
		}

		public override string GetWriteValue()
		{
			return Value.ToString();
		}

		public ResourceDecimal(XElement Element)
			: base(Element)
		{
			ModifiedAt = DateTime.MinValue;
			foreach (var it in Element.Elements(LWTSD.Namespace + "point"))
			{
				if (HistoricalValues == null)
					HistoricalValues = new List<Tuple<DateTime, Decimal>>();
				DateTime TimeStamp = XmlConvert.ToDateTime(it.Attribute("timestamp").Value,
														   XmlDateTimeSerializationMode.Utc);
				int PointValue = XmlConvert.ToInt32(it.Value);
				HistoricalValues.Add(new Tuple<DateTime, Decimal>(TimeStamp, PointValue));

				if (TimeStamp > ModifiedAt)
				{
					ModifiedAt = TimeStamp;
					InnerValue = PointValue;
				}

			}
		}

		public ResourceDecimal()
		{
		}

	}
}
