using System;
using System.Xml;
using System.Xml.Linq;
using System.Collections.Generic;

namespace LWTSD.ResourceTypes
{
	public class ResourceDuration : Resource
	{
		public readonly List<Tuple<DateTime, TimeSpan>> HistoricalValues = null;

		TimeSpan InnerValue;
		public TimeSpan Value
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

		public TimeSpan? MinExclusive
		{
			get
			{
				if (InnerMinExclusive == null)
					return null;

				return (TimeSpan) InnerMinExclusive;
			}

			set
			{
				InnerMinExclusive = value;
			}
		}
		public TimeSpan? MaxExclusive
		{
			get
			{
				if (InnerMaxExclusive == null)
					return null;

				return (TimeSpan) InnerMaxExclusive;
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
			return base.GetDescription(SimplifiedType.Duration);
		}

		public override void LoadFromWrite(XElement WriteElement)
		{
			this.Value = XmlConvert.ToTimeSpan(WriteElement.Value);
		}

		public override string GetWriteValue()
		{
			return Value.ToString();
		}

		public ResourceDuration(XElement Element)
			: base(Element)
		{
			ModifiedAt = DateTime.MinValue;
			foreach (var it in Element.Elements(LWTSD.Namespace + "point"))
			{
				if (HistoricalValues == null)
					HistoricalValues = new List<Tuple<DateTime, TimeSpan>>();
				DateTime TimeStamp = XmlConvert.ToDateTime(it.Attribute("timestamp").Value,
														   XmlDateTimeSerializationMode.Utc);
				TimeSpan PointValue = XmlConvert.ToTimeSpan(it.Value);
				HistoricalValues.Add(new Tuple<DateTime, TimeSpan>(TimeStamp, PointValue));

				if (TimeStamp > ModifiedAt)
				{
					ModifiedAt = TimeStamp;
					InnerValue = PointValue;
				}

			}
		}

		public ResourceDuration()
		{
		}

	}
}
