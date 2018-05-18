using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace CDO
{
	public class Session
	{
		public string ID;
		public List<ResourceAccess> Resources = new List<ResourceAccess>();
		public DateTime SessionEndsAtUTC;
		public DXMPP.JID Requester;
		public DXMPP.JID Source;

		public Session()
		{
		}

		public Session(ActionResponse Resp)
		{
			ID = CDO.GetString(Resp.Payload, "sessionid");
			int SessionLengthSeconds = CDO.GetInt(Resp.Payload, "sessionlength", 0);

			SessionEndsAtUTC = DateTime.MaxValue;

			if(SessionLengthSeconds > 0)
				SessionEndsAtUTC =DateTime.UtcNow.AddSeconds(CDO.GetInt(Resp.Payload, "sessionlength"));

			XElement ResEl = CDO.GetList(Resp.Payload, "resourceaccessrights");
			var ResourceAccessRights = ResEl.Elements(CDO.Namespace + "resourceaccess");
			foreach (var ResAcc in ResourceAccessRights)
			{
				Resources.Add(new ResourceAccess(ResAcc));
			}

			foreach (var el in Resp.Payload.Elements())
			{
				if (el.Name.LocalName.Contains("contract"))
					throw new Exception("Contracts not supported");
			}
		}

		public Session(ActionRequest Req)
		{
			ID = CDO.GetString(Req.Payload, "sessionid");
			int SessionLengthSeconds = CDO.GetInt(Req.Payload, "sessionlength", 0);

			SessionEndsAtUTC = DateTime.MaxValue;

			if (SessionLengthSeconds > 0)
				SessionEndsAtUTC = DateTime.UtcNow.AddSeconds(CDO.GetInt(Req.Payload, "sessionlength"));

#warning sessionexpiration is a spelling error in cdo, should be sessionexpires
			SessionEndsAtUTC = CDO.GetTimestamp(Req.Payload, "sessionexpiration", SessionEndsAtUTC);

			XElement ResEl = CDO.GetList(Req.Payload, "resourceaccessrights");
			var ResourceAccessRights = ResEl.Elements(CDO.Namespace + "resourceaccess");
			foreach (var ResAcc in ResourceAccessRights)
			{
				Resources.Add(new ResourceAccess(ResAcc));
			}

			foreach (var el in Req.Payload.Elements())
			{
				if (el.Name.LocalName.Contains("contract"))
					throw new Exception("Contracts not supported");
			}
			Requester = CDO.GetAddress(Req.Payload, "requesteeaddress");
		}
	}
}
