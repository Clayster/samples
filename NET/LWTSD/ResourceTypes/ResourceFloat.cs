﻿using System;
using System.Xml;
using System.Xml.Linq;
using System.Collections.Generic;

namespace LWTSD.ResourceTypes
{
	public class ResourceFloat : Resource
	{
		public readonly List<Tuple<DateTime, float>> HistoricalValues = null;

		float InnerValue;
		public float Value
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

		public float? MinExclusive
		{
			get
			{
				if (InnerMinExclusive == null)
					return null;

				return (float) InnerMinExclusive;
			}

			set
			{
				InnerMinExclusive = value;
			}
		}
		public float? MaxExclusive
		{
			get
			{
				if (InnerMaxExclusive == null)
					return null;

				return (float) InnerMaxExclusive;
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
			return base.GetDescription(SimplifiedType.Float);
		}

		public override void LoadFromWrite(XElement WriteElement)
		{
			this.Value = XmlConvert.ToInt32(WriteElement.Value);
		}

		public override string GetWriteValue()
		{
			return Value.ToString();
		}

		public ResourceFloat(XElement Element)
			: base(Element)
		{
			ModifiedAt = DateTime.MinValue;
			foreach (var it in Element.Elements(LWTSD.Namespace + "point"))
			{
				if (HistoricalValues == null)
					HistoricalValues = new List<Tuple<DateTime, float>>();
				DateTime TimeStamp = XmlConvert.ToDateTime(it.Attribute("timestamp").Value,
														   XmlDateTimeSerializationMode.Utc);
				float PointValue = (float)XmlConvert.ToDouble(it.Value);
				HistoricalValues.Add(new Tuple<DateTime, float>(TimeStamp, PointValue));

				if (TimeStamp > ModifiedAt)
				{
					ModifiedAt = TimeStamp;
					InnerValue = PointValue;
				}

			}
		}

		public ResourceFloat()
		{
		}

	}
}
