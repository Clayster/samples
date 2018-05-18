using System;
using System.Collections.Generic;

namespace LWTSD
{
	public class AccessTokenSession
	{
		public DXMPP.JID Actor;
		public AccessToken Token;
		public DateTime ExpiresAtUTC;
		public List<ResourceAccess> Rights;
	}
}
