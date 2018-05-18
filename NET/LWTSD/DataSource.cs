using System;
using System.Xml;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using DXMPP;
using LWTSD.ResourceTypes;

namespace LWTSD
{
    public class DataSource : IDisposable
    {
		public class AccessViolation : Exception
		{
		}
		
        class Subscriber
        {
            public string SubscriptionID;
			public string AccessToken;
            public JID SubscriberAddress;
            public DateTime LastInvalidated = DateTime.MinValue;
            public DateTime LastResubscribed = DateTime.UtcNow;
            public List<ResourceSubscription> Triggers = new List<ResourceSubscription>();
        }

        ConcurrentDictionary<string, Subscriber> Subscribers = new ConcurrentDictionary<string, Subscriber>();
        ConcurrentDictionary<ResourcePath, Resource> Resources = new ConcurrentDictionary<ResourcePath, Resource>();
		ConcurrentDictionary<string, AccessTokenSession> AccessTokens = new ConcurrentDictionary<string, AccessTokenSession>();

		Connection Uplink;

        public DataSource(Connection Uplink, bool ForceAccessTokenSessions)
        {
			this.Uplink = Uplink;
			this.Uplink.OnStanzaIQ += Uplink_OnStanzaIQ;
			this.Uplink.OnStanzaMessage += Uplink_OnStanzaMessage;
			this.ForceAccessTokenSessions = ForceAccessTokenSessions;
        }

		public ICollection<Resource> GetAllResources()
		{
			return Resources.Values;
		}

        SimplifiedSchema Schema = new SimplifiedSchema();
		bool ForceAccessTokenSessions = true;

		List<ResourceAccess> GetAccessRights(DXMPP.JID Actor, AccessToken Token)
		{
			try
			{
				AccessTokenSession TV = AccessTokens[Actor.GetBareJID() + Token];

				if (TV.ExpiresAtUTC <= DateTime.UtcNow)
				{
					AccessTokens.TryRemove(Token, out TV);
					throw new AccessViolation();
				}

				if (TV.Actor.GetBareJID() != Actor.GetBareJID())
					throw new AccessViolation();

				return TV.Rights;
			}
			catch
			{
				if(ForceAccessTokenSessions)
					throw new AccessViolation();
			}

			throw new AccessViolation();
		}

        void HandleReadSchema(StanzaIQ Request)
		{
			XElement ReadSchemaElement = Request.Payload.Elements().First();
			try
            {
				List<ResourceAccess> LimitedAccess = GetAccessRights(Request.From, LWTSD.GetAccessToken(ReadSchemaElement));
				
                SchemaFormat RequestedFormat = 
					SchemaFormatMethods.LoadFromString(ReadSchemaElement.Attribute("format").Value);
                if (RequestedFormat != SchemaFormat.Simplified)
                    throw new NotSupportedException("Extended schema not supported");

                int startindex = XmlConvert.ToInt32(ReadSchemaElement.Attribute("startindex").Value);
				int maxitems = XmlConvert.ToInt32(ReadSchemaElement.Attribute("maxresources").Value);

                StanzaIQ Response = new StanzaIQ(StanzaIQ.StanzaIQType.Result);
                Response.ID = Request.ID;
                Response.To = Request.From;

                lock (Schema)
                {
                    Response.Payload.Add(Schema.GetSerializedElement(startindex, maxitems, LimitedAccess));
                }
                Uplink.SendStanza(Response);
            }
            catch (System.Exception ex)
            {
                StanzaIQ ErrorResponse = new StanzaIQ(StanzaIQ.StanzaIQType.Error);
                ErrorResponse.ID = Request.ID;
                ErrorResponse.To = Request.From;
                XElement ErroReasonElement = new XElement( LWTSD.Namespace + "errorreason");
                ErroReasonElement.SetAttributeValue("reason", ErrorReason.InvalidData);
                ErroReasonElement.Value = ex.ToString();
                ErrorResponse.Payload.Add (ErroReasonElement);
                Uplink.SendStanza(ErrorResponse);
            }
		}

        string GetSafeSubscriptionID(JID From, string LocalSubscriptionID)
        {
            return From.ToString() + LocalSubscriptionID;
        }

        void HandleReadData(StanzaIQ Request)
        {
            XElement ReadDataElement = Request.Payload.Elements().First();
            try
            {
				List<ResourceAccess> LimitedAccess = GetAccessRights(Request.From, LWTSD.GetAccessToken(ReadDataElement));

                int maxpoints = XmlConvert.ToInt32(ReadDataElement.Attribute("maxpoints").Value);
                int startindex = XmlConvert.ToInt32(ReadDataElement.Attribute("startindex").Value);
                SortOrder OrderByTime = SortOrder.None;
                if (ReadDataElement.Attribute("orderedbytime") != null)
                    OrderByTime = SortOrderMethods.LoadFromString(ReadDataElement.Attribute("orderedbytime").Value);

                StanzaIQ Response = new StanzaIQ(StanzaIQ.StanzaIQType.Result);
                Response.ID = Request.ID;
                Response.To = Request.From;

                XElement DataElement = new XElement( LWTSD.Namespace + "data");

                List<Resource> MatchingResources = new List<Resource>();

                foreach (XElement ReadElement in ReadDataElement.Elements(LWTSD.Namespace + "read"))
                {
                    if (ReadElement.Attribute("maxpoints") != null)
                    {
                        if (XmlConvert.ToInt32(ReadElement.Attribute("maxpoints").Value) < 1)
                            throw new InvalidOperationException("Maxpoints < 0 in read");
                    }
                    if (ReadElement.Attribute("startindex") != null)
                    {
                        int localstartindex = XmlConvert.ToInt32(ReadElement.Attribute("startindex").Value);
                        if (localstartindex < 1)
                            throw new InvalidOperationException("startindex < 0 in read");
                        if (localstartindex > 1)
                            continue; // We only have one point / resource
                    }

                    ResourcePath Path = ReadElement.Attribute("resource-path").Value;

					if (!ResourceAccess.AllowsRead(LimitedAccess, Path))
						throw new AccessViolation();

                    if (!Resources.ContainsKey(Path))
                    {
                        // Todo: Explicit exception to set proper error code
                        throw new Exception("Path does not exist: " + Path);
                    }

                    Resource Res = Resources[Path];
                    if (!Res.SupportsRead)
                    {
                        throw new InvalidOperationException("Resource does not support read: " + Path);
                    }
                        
                    MatchingResources.Add(Res);
                }

                lock (Schema)
                {
                    int Index = 0;
                    int ReturnedPoints = 0;

                    foreach (Resource Res in MatchingResources)
                    {
                        if (Index < startindex)
                        {
                            Index++;
                            continue;
                        }
                        if ((Index - startindex) >= maxpoints)
                        {
                            Index++;
                            break;
                        }

                        Index++;
                        ReturnedPoints++;

                        XElement ResourceElement = new XElement(LWTSD.Namespace + "resource");
                        ResourceElement.SetAttributeValue("path", Res.Path);
                        ResourceElement.SetAttributeValue("returnedpoints", "1");
                        ResourceElement.SetAttributeValue("totalpoints", "1");
                        XElement PointElement = Res.GetPoint();
                        ResourceElement.Add(PointElement);

                        DataElement.Add(ResourceElement);
                    }

                    DataElement.SetAttributeValue("schemaversion", Schema.GetVersion());
                    DataElement.SetAttributeValue("returnedpoints", XmlConvert.ToString(ReturnedPoints));
                    DataElement.SetAttributeValue("totalpoints", XmlConvert.ToString(MatchingResources.Count));
                }

				foreach (XElement ReSubscribeElement in ReadDataElement.Elements(LWTSD.Namespace + "re-subscribe"))
				{
                    string localsid = ReSubscribeElement.Attribute("subscriptionid").Value;
                    string sid = GetSafeSubscriptionID(Request.From, localsid);

                    if (!Subscribers.ContainsKey(sid))
                    {
                        throw new InvalidOperationException("Subscription ID does not exist: " + localsid);
                    }

                    Subscriber Subscription = Subscribers[sid];
                    Subscription.LastResubscribed = DateTime.UtcNow;
				}

				Response.Payload.Add(DataElement);
                Uplink.SendStanza(Response);
            }
            catch (System.Exception ex)
            {
                StanzaIQ ErrorResponse = new StanzaIQ(StanzaIQ.StanzaIQType.Error);
                ErrorResponse.ID = Request.ID;
                ErrorResponse.To = Request.From;
                XElement ErroReasonElement = new XElement(LWTSD.Namespace + "errorreason");
                ErroReasonElement.SetAttributeValue("reason", ErrorReason.InvalidData);
                ErroReasonElement.Value = ex.ToString();
                ErrorResponse.Payload.Add(ErroReasonElement);
                Uplink.SendStanza(ErrorResponse);
            }
        }

		void HandleWriteData(StanzaIQ Request)
		{
			XElement WriteDataElement = Request.Payload.Elements().First();
			try
			{
				List<ResourceAccess> LimitedAccess = GetAccessRights(Request.From, LWTSD.GetAccessToken(WriteDataElement));

				int NrWrittenValues = 0;
				foreach (XElement WriteElement in WriteDataElement.Elements(LWTSD.Namespace + "write"))
				{
                    ResourcePath Path = WriteElement.Attribute("resource-path").Value;

					if (!ResourceAccess.AllowsWrite(LimitedAccess, Path))
						throw new AccessViolation();

                    if (!Resources.ContainsKey(Path))
                        throw new InvalidOperationException("Path does not exist: " + Path); // todo: explicit type to make it possible to set correct error code
                    Resource Res = Resources[Path];
                    if (!Res.SupportsWrite)
                        throw new InvalidOperationException("Resource is not writeable: " + Path); // todo: explicit type to make it possible to set correct error code
					Res.LoadFromWrite(WriteElement);
					NrWrittenValues++;
				}

				if (NrWrittenValues == 0)
					throw new Exception("No values found");
				
				StanzaIQ Response = new StanzaIQ(StanzaIQ.StanzaIQType.Result);
				Response.ID = Request.ID;
				Response.To = Request.From;
				Uplink.SendStanza(Response);
			}
			catch (System.Exception ex)
			{
				StanzaIQ ErrorResponse = new StanzaIQ(StanzaIQ.StanzaIQType.Error);
				ErrorResponse.ID = Request.ID;
				ErrorResponse.To = Request.From;
				XElement ErroReasonElement = new XElement(LWTSD.Namespace + "errorreason");
				ErroReasonElement.SetAttributeValue("reason", ErrorReason.InvalidData);
				ErroReasonElement.Value = ex.ToString();
				ErrorResponse.Payload.Add(ErroReasonElement);
				Uplink.SendStanza(ErrorResponse);
			}
		}

		void HandleSubscribe(StanzaIQ Request)
		{
			XElement SubscribeElement = Request.Payload.Elements().First();
			try
			{
				string AccessToken = LWTSD.GetAccessToken(SubscribeElement);
				List<ResourceAccess> LimitedAccess = GetAccessRights(Request.From, AccessToken);

                Subscriber Subscription = new Subscriber();
                Subscription.SubscriptionID = SubscribeElement.Attribute("subscriptionid").Value;
                Subscription.SubscriberAddress = Request.From;
				Subscription.AccessToken = AccessToken;

				foreach (XElement WriteElement in SubscribeElement.Elements(LWTSD.Namespace + "trigger"))
				{
					ResourcePath Path = WriteElement.Attribute("onresource").Value;

					if (!ResourceAccess.AllowsRead(LimitedAccess, Path))
						throw new AccessViolation();

					if (!Resources.ContainsKey(Path))
						throw new InvalidOperationException("Path does not exist: " + Path); // todo: explicit type to make it possible to set correct error code
					Resource Res = Resources[Path];
					if (!Res.SupportsRead)
						throw new InvalidOperationException("Resource is not readable: " + Path); // todo: explicit type to make it possible to set correct error code

                    ResourceSubscription Trigger = new ResourceSubscription()
                    {
                        Path = Path
                    };

                    Subscription.Triggers.Add(Trigger);
				}

                if (Subscription.Triggers.Count == 0)
                    throw new InvalidOperationException("No triggers");

                Subscribers[GetSafeSubscriptionID(Request.From, Subscription.SubscriptionID)] = Subscription;

                StanzaIQ Response = new StanzaIQ(StanzaIQ.StanzaIQType.Result);
				Response.ID = Request.ID;
				Response.To = Request.From;
				Uplink.SendStanza(Response);
			}
			catch (System.Exception ex)
			{
				StanzaIQ ErrorResponse = new StanzaIQ(StanzaIQ.StanzaIQType.Error);
				ErrorResponse.ID = Request.ID;
				ErrorResponse.To = Request.From;
				XElement ErroReasonElement = new XElement(LWTSD.Namespace + "errorreason");
				ErroReasonElement.SetAttributeValue("reason", ErrorReason.InvalidData);
				ErroReasonElement.Value = ex.ToString();
				ErrorResponse.Payload.Add(ErroReasonElement);
				Uplink.SendStanza(ErrorResponse);
			}
		}

        void HandleVerifySubscription(StanzaIQ Request)
		{
			XElement SubscribeElement = Request.Payload.Elements().First();
			try
			{
				string localsid = SubscribeElement.Attribute("subscriptionid").Value;
				string sid = GetSafeSubscriptionID(Request.From, localsid);
                bool Verified = Subscribers.ContainsKey(sid);

				StanzaIQ Response = new StanzaIQ(StanzaIQ.StanzaIQType.Result);
				Response.ID = Request.ID;
				Response.To = Request.From;

                XElement VerifiedElement = new XElement(LWTSD.Namespace + "verified-subscription");
                VerifiedElement.SetAttributeValue("subscriptionid", localsid);
                VerifiedElement.SetAttributeValue("isactive", XmlConvert.ToString(Verified));
				Uplink.SendStanza(Response);
			}
			catch (System.Exception ex)
			{
				StanzaIQ ErrorResponse = new StanzaIQ(StanzaIQ.StanzaIQType.Error);
				ErrorResponse.ID = Request.ID;
				ErrorResponse.To = Request.From;
				XElement ErroReasonElement = new XElement(LWTSD.Namespace + "errorreason");
				ErroReasonElement.SetAttributeValue("reason", ErrorReason.InvalidData);
				ErroReasonElement.Value = ex.ToString();
				ErrorResponse.Payload.Add(ErroReasonElement);
				Uplink.SendStanza(ErrorResponse);
			}
		}

		void HandleSubscriptionCancelled(StanzaMessage Request)
		{
			XElement SubscribeElement = Request.Payload.Elements().First();
			try
			{
                string localsid = SubscribeElement.Attribute("subscriptionid").Value;
                string sid = GetSafeSubscriptionID(Request.From, localsid);
                if(!Subscribers.ContainsKey(sid))
                    return;
                
                Subscriber Temp;
                Subscribers.TryRemove(sid, out Temp);
			}
			catch 
			{
                // No
			}
		}

		void Uplink_OnStanzaIQ(StanzaIQ Data)
		{
            if (!Data.Payload.HasElements)
                return;

            XElement LWTSDElement = Data.Payload.Elements().First();
                
			if (LWTSDElement.Name.Namespace != LWTSD.Namespace)
				return;

			Console.WriteLine("Data source processing " + 
			                  LWTSDElement.Name.LocalName + 
			                  " from " + Data.From.ToString());
			switch (LWTSDElement.Name.LocalName)
			{
				case "read-schema":
					HandleReadSchema(Data);
					break;
				case "write-data":
                    HandleWriteData(Data);
                    InvalidateData(Data.From);
					break;
				case "read-data":
                    HandleReadData(Data);
					break;
				case "subscribe":
                    HandleSubscribe(Data);
					break;
				case "verify-subscription":
                    HandleVerifySubscription(Data);
					break;
			}
			Console.WriteLine("Data source processing done");

		}

		void Uplink_OnStanzaMessage(StanzaMessage Data)
		{
			if (!Data.Payload.HasElements)
				return;

			XElement LWTSDElement = Data.Payload.Elements().First();

			if (LWTSDElement.Name.Namespace != LWTSD.Namespace)
				return;

			switch (LWTSDElement.Name.LocalName)
			{				
				case "subscription-cancelled":
					HandleSubscriptionCancelled(Data);
					break;
			}
		}
        // Data operations
        public void AddResource(Resource Point)
        {
            Resources[Point.Path] = Point;
            RebuildSchema();
        }

        public void RemoveResource(ResourcePath Path)
        {
			// todo: if a resource is removed; invalidate subscribers and remove any subscription
			throw new NotImplementedException();
        }

        public void InvalidateData( JID ExcludeJID )
        {
			List<string> SubscriptionsToRemove = new List<string>();
            foreach (string key in Subscribers.Keys)
            {
				Subscriber s = Subscribers[key];
				try
				{
					if (ExcludeJID != null)
						if (s.SubscriberAddress == ExcludeJID)
							continue;

					if (s.LastInvalidated > s.LastResubscribed)
						continue;

					List<ResourceAccess> LimitedAccess = GetAccessRights(s.SubscriberAddress, s.AccessToken);

					bool Invalidate = false;
					foreach (ResourceSubscription trigger in s.Triggers)
					{
						if (!ResourceAccess.AllowsRead(LimitedAccess, trigger.Path))
							throw new AccessViolation();
						
						Resource res = Resources[trigger.Path];
						if (res.ModifiedAt > s.LastInvalidated)
						{
							Invalidate = true;
							break;
						}
					}

					if (!Invalidate)
						continue;

					// Invalidate
					StanzaMessage InvalidateMessage = new StanzaMessage();
					InvalidateMessage.MessageType = StanzaMessage.StanzaMessageType.Normal;
					InvalidateMessage.To = s.SubscriberAddress;
					s.LastInvalidated = DateTime.UtcNow;
					XElement SubscriptionTriggeredElement = new XElement(LWTSD.Namespace + "subscription-triggered");
					SubscriptionTriggeredElement.SetAttributeValue("subscriptionid", s.SubscriptionID);
					InvalidateMessage.Payload.Add(SubscriptionTriggeredElement);
					Uplink.SendStanza(InvalidateMessage);
				}
				catch (AccessViolation Violation)
				{
					SubscriptionsToRemove.Add(key);
				}
            }
			foreach (string ToRemove in SubscriptionsToRemove)
			{
				Subscriber Temp;
				Subscribers.TryRemove(ToRemove, out Temp);
			}
        }

		public void RebuildSchema()
		{
			lock (Schema)
			{
                Schema.Resources.Clear();
				foreach (Resource Res in Resources.Values)
				{
					Schema.Resources[Res.Path] = Res.GetDescription();
				}
			}
		}

		public void SetAccessTokenSession(JID Actor, AccessTokenSession Session)
		{
			AccessTokens[Actor.GetBareJID()+ Session.Token] = Session;
		}

		public void RemoveAccessToken(JID Actor, AccessToken Token)
		{
			AccessTokenSession Temp;
			AccessTokens.TryRemove(Actor.GetBareJID() + Token, out Temp);
		}

		public void Dispose()
		{
			Uplink.OnStanzaIQ -= Uplink_OnStanzaIQ;
			Uplink.OnStanzaMessage -= Uplink_OnStanzaMessage;
			Uplink = null;
		}
	}
}
