using System;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Collections.Generic;

namespace LWTSD
{


	public class AccessToken
	{
		readonly string Value;
		public AccessToken(string value)
		{
			this.Value = value;
		}
		public static implicit operator string(AccessToken b)
		{
			return b.ToString();
		}
		public static implicit operator AccessToken(string b)
		{
			return new AccessToken(b);
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