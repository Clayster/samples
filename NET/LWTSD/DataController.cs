using System;
using System.Xml;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using DXMPP;
using LWTSD.ResourceTypes;
using System.Threading;
using System.Threading.Tasks;

namespace LWTSD
{
	public class DataController : IDisposable
	{
		JID DataSourceAddress;
		Connection Uplink;

		ConcurrentDictionary<string, TaskCompletionSource<StanzaIQ>> IQRequests
			= new ConcurrentDictionary<string, TaskCompletionSource<StanzaIQ>>();

		class Subscription
		{
			public string ID;
			public Action<Tuple<string, DataPage>> NewData;
			public Action<string> SubscriptionCancelled;
			public List<ResourcePath> Resources;
			public SimplifiedSchema KnownSchema;
			public string AccessToken;
			public DateTime LastReSubscription = DateTime.UtcNow;
		}


		ConcurrentDictionary<string, Subscription> Subscriptions =
			new ConcurrentDictionary<string, Subscription>();


		public async Task<SimplifiedSchema> GetSchema(int Page = 0, string AccessToken = null, List<ResourcePath> Resources = null)
		{
			const int MaxResources = 500;

			TaskCompletionSource<StanzaIQ> Signal = new TaskCompletionSource<StanzaIQ>();

			StanzaIQ Request = new StanzaIQ(StanzaIQ.StanzaIQType.Get);
			Request.To = DataSourceAddress;
			XElement Payload = new XElement(LWTSD.Namespace + "read-schema");
			Payload.SetAttributeValue("format", SchemaFormat.Simplified.GetSerializedValue());
			Payload.SetAttributeValue("startindex", Page * MaxResources);
			Payload.SetAttributeValue("maxresources", MaxResources);

			if (AccessToken != null)
			{
				XElement AToken = new XElement(LWTSD.Namespace + "accesstoken");
				AToken.SetAttributeValue("name", "urn:clayster:cdo:sessionid");
				AToken.Value = AccessToken;
				Payload.Add(AToken);
			}

			if (Resources != null)
			{
				foreach (var res in Resources)
				{
					XElement Resource = new XElement(LWTSD.Namespace + "resource");
					Resource.SetAttributeValue("path", res);
					Payload.Add(Resource);
				}
			}

			Request.Payload.Add(Payload);
			IQRequests[Request.ID] = Signal;

			Uplink.SendStanza(Request);

			await Signal.Task;

			// Parse signal data

			if (Signal.Task.Result == null)
				throw new Exception("Failed to get schema");

			XElement ReturnedData = Signal.Task.Result.Payload.Element(LWTSD.Namespace + "simplified-schema");
			if (ReturnedData == null)
				throw new Exception("Failed to get schema");

			SimplifiedSchema RVal = new SimplifiedSchema(ReturnedData);

			return RVal;
		}


		public class DataPage
		{
			public const int PointsPerPage = 500;

			public class SchemaMissMatchException : System.Exception
			{
			}

			public List<Resource> Data = new List<Resource>();
			public SimplifiedSchema Schema;
			public int NrTotalPages;
			public int Page;

			public DataPage(XElement Element, SimplifiedSchema KnownSchema, int Page)
			{
				if (KnownSchema == null)
					throw new SchemaMissMatchException();

				string NewSchemaVersion = Element.Attribute("schemaversion").Value;
				if (NewSchemaVersion != KnownSchema.GetVersion())
					throw new SchemaMissMatchException();

				Schema = KnownSchema;
				this.NrTotalPages = (int)(((double)Convert.ToInt32(Element.Attribute("totalpoints").Value) + 0.5) / (double)PointsPerPage);


				foreach (var it in Element.Elements(LWTSD.Namespace + "resource"))
				{
					ResourcePath Path = it.Attribute("path").Value;
					switch (KnownSchema.Resources[Path].SimpleType)
					{
						case SimplifiedType.String:
							Data.Add(new ResourceString(it));
							break;
						case SimplifiedType.Integer:
							Data.Add(new ResourceInteger(it));
							break;
						case SimplifiedType.Base64Binary:
							Data.Add(new ResourceBase64Binary(it));
							break;
						case SimplifiedType.DateTime:
							Data.Add(new ResourceDateTime(it));
							break;
						case SimplifiedType.Boolean:
							Data.Add(new ResourceBoolean(it));
							break;
						case SimplifiedType.Decimal:
							Data.Add(new ResourceDecimal(it));
							break;
						case SimplifiedType.Duration:
							Data.Add(new ResourceDuration(it));
							break;
						case SimplifiedType.Float:
							Data.Add(new ResourceFloat(it));
							break;
						case SimplifiedType.Time:
							Data.Add(new ResourceTime(it));
							break;
                        case SimplifiedType.Double:
                            Data.Add(new ResourceDouble(it));
                            break;

                    }
                }
			}

			public DataPage()
			{
				Page = 0;
				NrTotalPages = 1;
				Schema = null;
			}
		}



		async Task<DataPage> InnerReadData(SimplifiedSchema KnownSchema,
										   List<ResourcePath> Resources,
										   int Page = 0,
										   List<string> ResubscriptionIDs = null,
										   string AccessToken = null)
		{
			if (KnownSchema == null)
				KnownSchema = await GetSchema(0, AccessToken, Resources);

			TaskCompletionSource<StanzaIQ> Signal = new TaskCompletionSource<StanzaIQ>();

			StanzaIQ Request = new StanzaIQ(StanzaIQ.StanzaIQType.Get);
			Request.To = DataSourceAddress;
			XElement Payload = new XElement(LWTSD.Namespace + "read-data");
			Payload.SetAttributeValue("maxpoints", DataPage.PointsPerPage.ToString());
			Payload.SetAttributeValue("startindex", (Page * DataPage.PointsPerPage).ToString());
			Payload.SetAttributeValue("relativetimeout", "10");

			if (AccessToken != null)
			{
				XElement AToken = new XElement(LWTSD.Namespace + "accesstoken");
				AToken.SetAttributeValue("name", "urn:clayster:cdo:sessionid");
				AToken.Value = AccessToken;
				Payload.Add(AToken);
			}

			if (ResubscriptionIDs != null)
			{
				foreach (string sid in ResubscriptionIDs)
				{
					XElement el = new XElement(LWTSD.Namespace + "re-subscribe");
					el.SetAttributeValue("subscriptionid", sid);
					Payload.Add(el);
				}
			}

			foreach (ResourcePath Path in Resources)
			{
				XElement el = new XElement(LWTSD.Namespace + "read");
				el.SetAttributeValue("resource-path", Path);
				el.SetAttributeValue("maxpoints", "1");
				Payload.Add(el);
			}

			Request.Payload.Add(Payload);
			IQRequests[Request.ID] = Signal;

			Uplink.SendStanza(Request);

			await Signal.Task;

			// Parse signal data

			if (Signal.Task.Result == null)
				throw new Exception("Failed to get data: no return value");

			XElement ReturnedData = Signal.Task.Result.Payload.Element(LWTSD.Namespace + "data");
			if (ReturnedData == null)
				throw new Exception("Failed to get data: invalid data: " + Signal.Task.Result.Payload.ToString());


			// This is wrong for more than one or two iterations, Need to re-read the data. Recurse?
			DataPage RVal = new DataPage(ReturnedData, KnownSchema, Page);
			return RVal;

		}


		public async Task<DataPage> ReadData(SimplifiedSchema KnownSchema,
											 List<ResourcePath> Resources,
											 string AccessToken = null,
											 int Page = 0)
		{
			const int NrSchemaMissMatchRetries = 10;
			bool SchemaMissMatched = true;
			for (int i = 0; i < NrSchemaMissMatchRetries && SchemaMissMatched; i++)
			{
				SchemaMissMatched = false;
				try
				{
					return await InnerReadData(KnownSchema, Resources, Page, new List<string>(), AccessToken);
				}
				catch (DataPage.SchemaMissMatchException)
				{
					SchemaMissMatched = true;
				}
			}
			throw new Exception("Failed to read data" + (SchemaMissMatched ? ": schema miss matched" : ""));
		}

		public async Task<Tuple<string,DataPage>> SubscribeToData(SimplifiedSchema KnownSchema,
		                                                          List<ResourcePath> Resources,
		                                                          Action<Tuple<string, DataPage>> NewDataAction,
		                                                          Action<string> SubscriptionCancelled,
		                                                          string AccessToken = null)
		{
			string SubscriptionID = Guid.NewGuid().ToString();

			XElement Payload = new XElement(LWTSD.Namespace + "subscribe");
			Payload.SetAttributeValue("subscriptionid", SubscriptionID);

			foreach (ResourcePath res in Resources)
			{
				XElement Trigger = new XElement(LWTSD.Namespace + "trigger");
				Trigger.SetAttributeValue("onresource", res);
				Payload.Add(Trigger);
			}
			StanzaIQ Request = new StanzaIQ(StanzaIQ.StanzaIQType.Set);
			Request.To = DataSourceAddress;

			Subscription NewSubscription = new Subscription();

			NewSubscription.ID = SubscriptionID;
			NewSubscription.NewData = NewDataAction;

			TaskCompletionSource<StanzaIQ> Signal = new TaskCompletionSource<StanzaIQ>();
			Request.Payload.Add(Payload);
			IQRequests[Request.ID] = Signal;

			Uplink.SendStanza(Request);

			await Signal.Task;

			if (Signal.Task.Result == null)
				throw new Exception("Failed to subscribe: no data");
			if (Signal.Task.Result.IQType != StanzaIQ.StanzaIQType.Result)
				throw new Exception("Subscription failed");


			DataPage Page = await ReadData(KnownSchema, Resources, AccessToken);
			NewSubscription.KnownSchema = Page.Schema;
			NewSubscription.Resources = Resources;
			NewSubscription.AccessToken = AccessToken;
			NewSubscription.SubscriptionCancelled = SubscriptionCancelled;

			Subscriptions[NewSubscription.ID] = NewSubscription;

			return new Tuple<string, DataPage>( SubscriptionID, Page);
		}

		public async Task<bool> WriteData(DataPage Page, string AccessToken = null)
		{			
			XElement Payload = new XElement( LWTSD.Namespace + "write-data" );

			if (AccessToken != null)
			{
				XElement AToken = new XElement(LWTSD.Namespace + "accesstoken");
				AToken.SetAttributeValue("name", "urn:clayster:cdo:sessionid");
				AToken.Value = AccessToken;
				Payload.Add(AToken);
			}

			foreach (var res in Page.Data)
			{
				XElement write = new XElement(LWTSD.Namespace + "write");
				write.SetAttributeValue("resource-path", res.Path);
				write.SetAttributeValue("relativetimeout", "10");
				write.Value = res.GetWriteValue();
				Payload.Add(write);
			}

			StanzaIQ Request = new StanzaIQ(StanzaIQ.StanzaIQType.Set);
			Request.To = DataSourceAddress;
			TaskCompletionSource<StanzaIQ> Signal = new TaskCompletionSource<StanzaIQ>();
			Request.Payload.Add(Payload);
			IQRequests[Request.ID] = Signal;

			Uplink.SendStanza(Request);

			await Signal.Task;

			if (Signal.Task.Result == null)
				throw new Exception("Failed to write data: result is null");
			if (Signal.Task.Result.IQType == StanzaIQ.StanzaIQType.Result)
				return true;

			return false;
		}

		async Task<bool> HandleSubscriptionTriggered(XElement Element)
		{
			string SubscriptionID = Element.Attribute("subscriptionid").Value;
			Subscription Sub = Subscriptions[SubscriptionID];
			if (Sub == null)
				return false;

			DataPage Page = await InnerReadData(Sub.KnownSchema,
												Sub.Resources,
												0,
												new List<String>(new string[] { SubscriptionID }),
			                                    Sub.AccessToken);

			var t = Task.Run(() => Sub.NewData.Invoke(new Tuple<string, DataPage>(SubscriptionID, Page)));
			Sub.LastReSubscription = DateTime.UtcNow;
			return true;
		}

		public void CancelSubscription(string SubscriptionID)
		{
			Subscription Sub;
			if (!Subscriptions.ContainsKey(SubscriptionID))
				return;

			Subscriptions.TryRemove(SubscriptionID, out Sub);

			StanzaMessage Request = new StanzaMessage();
			Request.To = DataSourceAddress;
			XElement Payload = new XElement(LWTSD.Namespace + "cancel-subscription");
			Payload.SetAttributeValue("subscriptionid", Sub.ID);
			Request.Payload.Add(Payload);
			Uplink.SendStanza(Request);
		}

		public DataController( Connection Uplink, JID DataProducerAddress )
		{
			this.Uplink = Uplink;
			this.DataSourceAddress = DataProducerAddress;
			this.Uplink.OnStanzaIQ += Uplink_OnStanzaIQ;
			this.Uplink.OnStanzaMessage += Uplink_OnStanzaMessage;
		}

		void Uplink_OnStanzaIQ(StanzaIQ Data)
		{
			if (Data.From.GetBareJID() != DataSourceAddress.GetBareJID())
				return;

			Console.WriteLine("Controller received iq stanza");
			
			if (!IQRequests.ContainsKey(Data.ID))
				return;
			TaskCompletionSource<StanzaIQ> Signal;
			if (!IQRequests.TryRemove(Data.ID, out Signal))
				return;

			Signal.SetResult(Data);
		}

		void HandleSubscriptionCancelled(XElement CancelledElement)
		{
			string SubscriptionID = CancelledElement.Attribute("subscriptionid").Value;
			Subscription Sub = null;
			Subscriptions.TryRemove(SubscriptionID, out Sub);
			if (Sub == null)
				return;

			Sub.SubscriptionCancelled.Invoke(SubscriptionID);
		}

		void Uplink_OnStanzaMessage(StanzaMessage Data)
		{
			if (Data.From.GetBareJID() != DataSourceAddress.GetBareJID())
				return;
			
			Console.WriteLine("Controller received message stanza");
			XElement LWTSDElement = Data.Payload.Elements().First();

			if (LWTSDElement.Name.Namespace != LWTSD.Namespace)
				return;

			switch (LWTSDElement.Name.LocalName)
			{
				case "subscription-cancelled":
					HandleSubscriptionCancelled(LWTSDElement);
					break;
				case "subscription-triggered":
					HandleSubscriptionTriggered(LWTSDElement);
					break;
			}

		}

		public void Dispose()
		{
        	Uplink.OnStanzaIQ -= Uplink_OnStanzaIQ;
			Uplink.OnStanzaMessage -= Uplink_OnStanzaMessage;
			Uplink = null;
		}
	}
}
