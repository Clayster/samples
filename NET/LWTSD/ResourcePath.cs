using System;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Collections.Generic;

namespace LWTSD
{

	public class ResourcePath
	{
		readonly string Value;
		public ResourcePath(string value)
		{
			this.Value = value;
		}
		public static implicit operator string(ResourcePath b)
		{
			return b.ToString();
		}
		public static implicit operator ResourcePath(string b)
		{
			return new ResourcePath(b);
		}
        public override string ToString()
        {
            return Value;
        }
        public override bool Equals(object obj)
        {
            return obj.ToString() == Value;
        }
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
	}
}