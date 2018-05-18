using System;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Collections.Generic;

namespace LWTSD
{

	public class ResourceDescription
	{
		public class Restriction
		{
			public object MinExclusive;
			public object MaxExclusive;
			public int? Length;

			static string GetSimpleType(XElement Element)
			{
				XDocument Doc = new XDocument();
				XmlSchemaSet Set = new XmlSchemaSet();

				return Element.Attribute("base").Value;
			}

            public Restriction()
            {
            }

			public Restriction(XElement Element)
			{                
                
				if (Element.Attribute("minexlusive") != null)
					MinExclusive = Element.Attribute("minexlusive").Value;

				if (Element.Attribute("maxexlusive") != null)
					MinExclusive = Element.Attribute("maxexclusive").Value;

				if (Element.Attribute("length") != null)
					Length = XmlConvert.ToInt32(Element.Attribute("length").Value);

				switch (GetSimpleType(Element))
				{
					case "decimal":
						MinExclusive = MinExclusive == null ? decimal.MinValue
							 : XmlConvert.ToDecimal(MinExclusive as string);
						MaxExclusive = MaxExclusive == null ? decimal.MaxValue
							 : XmlConvert.ToDecimal(MinExclusive as string);
						break;
					case "integer":
						MinExclusive = MinExclusive == null ? Int32.MinValue
							 : XmlConvert.ToInt32(MinExclusive as string);
						MaxExclusive = MaxExclusive == null ? Int32.MaxValue
							 : XmlConvert.ToInt32(MinExclusive as string);
						break;
					case "float":
						MinExclusive = MinExclusive == null ? double.MinValue
							 : XmlConvert.ToDouble(MinExclusive as string);
						MaxExclusive = MaxExclusive == null ? double.MaxValue
							 : XmlConvert.ToDouble(MinExclusive as string);
						break;
					case "double":
						MinExclusive = MinExclusive == null ? double.MinValue
							 : XmlConvert.ToDouble(MinExclusive as string);
						MaxExclusive = MaxExclusive == null ? double.MaxValue
							 : XmlConvert.ToDouble(MinExclusive as string);
						break;
					case "boolean":
						MinExclusive = MinExclusive == null ? false
							 : XmlConvert.ToBoolean(MinExclusive as string);
						MaxExclusive = MaxExclusive == null ? true
							 : XmlConvert.ToBoolean(MinExclusive as string);
						break;
					case "dateTime":
						MinExclusive = MinExclusive == null ? DateTime.MinValue
							: XmlConvert.ToDateTime(MinExclusive as string, XmlDateTimeSerializationMode.Utc);
						MaxExclusive = MaxExclusive == null ? DateTime.MaxValue
							 : XmlConvert.ToDateTime(MinExclusive as string, XmlDateTimeSerializationMode.Utc);
						break;
					case "duration":
						MinExclusive = MinExclusive == null ? TimeSpan.MinValue
							 : XmlConvert.ToTimeSpan(MinExclusive as string);
						MaxExclusive = MaxExclusive == null ? TimeSpan.MaxValue
							 : XmlConvert.ToTimeSpan(MinExclusive as string);
						break;
					case "time":
						MinExclusive = MinExclusive == null ? TimeSpan.MinValue
							 : XmlConvert.ToTimeSpan(MinExclusive as string);
						MaxExclusive = MaxExclusive == null ? TimeSpan.MaxValue
							 : XmlConvert.ToTimeSpan(MinExclusive as string);
						break;
					case "string":
						break;
					case "base64Binary":
						break;
				}
			}

            public string GetSerializedLimit(object Value, SimplifiedType SimpleType)
            {
                switch (SimpleType)
                {
					case SimplifiedType.String:
                        throw new NotImplementedException();
					case SimplifiedType.Integer:
                        return XmlConvert.ToString( (Int32) Value); 
					case SimplifiedType.Decimal:
						return XmlConvert.ToString((decimal)Value);
					case SimplifiedType.Double:
						return XmlConvert.ToString((double)Value);
					case SimplifiedType.Float:
						return XmlConvert.ToString((float)Value);
					case SimplifiedType.Duration:
						return XmlConvert.ToString((TimeSpan)Value);
					case SimplifiedType.Time:
						return XmlConvert.ToString((TimeSpan)Value);
					case SimplifiedType.DateTime:
						return XmlConvert.ToString((DateTime)Value, XmlDateTimeSerializationMode.Utc);
					case SimplifiedType.Base64Binary:
                        throw new NotImplementedException();
				}

                throw new NotImplementedException();
            }

            public void SerializeToSimpleType(XElement TypeElement, SimplifiedType SimpleType)
            {
                if(MinExclusive != null)
	                TypeElement.SetAttributeValue("minexclusive", 
	                                              GetSerializedLimit(MinExclusive, SimpleType));
				if (MaxExclusive != null)
					TypeElement.SetAttributeValue("maxexclusive",
												  GetSerializedLimit(MaxExclusive, SimpleType));
                if (Length != null)
                    TypeElement.SetAttributeValue("length", XmlConvert.ToString(Length.Value));
                
			}
		}

		public ResourcePath Path;
		public string XMLType;
		public SimplifiedType SimpleType;
        public Restriction Restrictions;

		public string Unit;
		public string Description;
		public string DisplayName;

		public bool SupportsRead;
		public bool SupportsWrite;

		public List<string> SupportedFilters = new List<string>();

        public ResourceDescription()
        {
        }

        public ResourceDescription(XElement Element)
        {
			this.Path = Element.Attribute("path").Value;
			if(Element.Attribute("unit") != null)
				this.Unit = Element.Attribute("unit").Value;
			if (Element.Attribute("description") != null)
				this.Description = Element.Attribute("description").Value;

			XElement TypeDesc = Element.Element(LWTSD.Namespace +"type");
			this.SimpleType = SimplifiedTypeMethods.LoadFromString(
				TypeDesc.Attribute("base").Value);
			this.Restrictions = new Restriction(TypeDesc);

			XElement Supports = Element.Element(LWTSD.Namespace +"supports");
			if(Supports.Attribute("read") != null)
				SupportsRead = XmlConvert.ToBoolean(Supports.Attribute("read").Value);
			if (Supports.Attribute("write") != null)
				SupportsWrite = XmlConvert.ToBoolean(Supports.Attribute("write").Value);
			foreach (XElement It in Supports.Elements())
			{
				if (It.Name.LocalName != "filter")
					continue;				
				this.SupportedFilters.Add(It.Attribute("name").Value);
			}
        }

        public XElement GetSupportsElement()
        {
            XElement SupportsElement = new XElement(LWTSD.Namespace + "supports");
            SupportsElement.SetAttributeValue("read", XmlConvert.ToString(SupportsRead));
            SupportsElement.SetAttributeValue("write", XmlConvert.ToString(SupportsWrite));

            foreach (string FilterName in SupportedFilters)
            {
                XElement FilterElement = new XElement(LWTSD.Namespace + "filter");
                FilterElement.SetAttributeValue("name", FilterName);
                SupportsElement.Add(FilterElement);
            }

            return SupportsElement;
        }

        public XElement GetSimplifiedSchemaResource()
        {
            XElement RVal = new XElement(LWTSD.Namespace +"resource");
            RVal.SetAttributeValue("path", Path);
            RVal.SetAttributeValue("unit", Unit);
            RVal.SetAttributeValue("description", Description);

            XElement TypeDesc = new XElement(LWTSD.Namespace + "type");
            TypeDesc.SetAttributeValue("base", SimpleType.GetSerializedValue());
            if (Restrictions != null)
            {
                Restrictions.SerializeToSimpleType(TypeDesc, SimpleType);                
            }

            RVal.Add(TypeDesc);
            RVal.Add(GetSupportsElement());

            return RVal;
        }
	}
}
