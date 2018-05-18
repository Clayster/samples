using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Xml.Linq;
using System.Threading.Tasks;

using DXMPP;

namespace CDO
{
	// This is just an example. 
	// It does not have perfect error handling nor does it handle
	// ack-timeouts.

	public class ActionRequester
	{
		public Connection Uplink;
		JID Orchestrator;
		ConcurrentDictionary<string, TaskCompletionSource<ActionResponse>> ActionResponses
			= new ConcurrentDictionary<string, TaskCompletionSource<ActionResponse>>();

		ConcurrentDictionary<string, TaskCompletionSource<StanzaIQ>> IQRequests = 
			new ConcurrentDictionary<string, TaskCompletionSource<StanzaIQ>>();

		double TimeDifferenceSeconds = 0;
		DateTime LastSynchronizedTime = DateTime.MinValue;

		async Task<bool> SynchTime()
		{
			StanzaIQ TimeRequest = new StanzaIQ(StanzaIQ.StanzaIQType.Get);
			XNamespace XEP202 = "urn:xmpp:time";
			XElement Time = new XElement(XEP202 + "time");
			TimeRequest.To = Orchestrator;
			TimeRequest.Payload.Add(Time);
			var Signal = new TaskCompletionSource<StanzaIQ>();
			IQRequests[TimeRequest.ID] = Signal;

			DateTime SentAt = DateTime.UtcNow;

			Uplink.SendStanza(TimeRequest);
			if ( Signal.Task != await Task.WhenAny(Signal.Task, Task.Delay(10*1000)) )
			{
				return false; // Timeout
			}

			if (Signal.Task.Result == null)
				return false;

			try
			{
				string UTCValue = Signal.Task.Result.Payload.Element(XEP202 + "time").Element(XEP202 + "utc").Value;
				DateTime TimeValue = System.Xml.XmlConvert.ToDateTime(UTCValue, System.Xml.XmlDateTimeSerializationMode.Utc);
				TimeDifferenceSeconds = (TimeValue - SentAt).TotalSeconds;
				//Console.WriteLine("Time synchronized: {0} - {1} = {2} s", TimeValue, SentAt, TimeDifferenceSeconds);
				LastSynchronizedTime = DateTime.UtcNow;
				return true;
			}
			catch(System.Exception ex)
			{
				Console.WriteLine("Exception in SynchTime: " + ex.ToString());
				return false;
			}
		}

		public ActionRequester(Connection Uplink, JID Orchestrator)
		{
            this.Orchestrator = Orchestrator;
			this.Uplink = Uplink;
			this.Uplink.OnStanzaMessage += Uplink_OnStanzaMessage;
			this.Uplink.OnStanzaIQ += Uplink_OnStanzaIQ;
		}

		public async Task<ActionResponse> Request(string Name, XElement Payload, int TimeoutSeconds = 60)
		{
			if (LastSynchronizedTime < DateTime.UtcNow.AddMinutes(-60))
			{
				await SynchTime();
			}

			ActionRequest Request = new ActionRequest()
			{
				TimesOut = DateTime.UtcNow.AddSeconds(TimeoutSeconds + TimeDifferenceSeconds),
				AckTimesOut = DateTime.UtcNow.AddSeconds(5 + TimeDifferenceSeconds ),
				Name = Name,
				Payload = Payload
			};

			TaskCompletionSource<ActionResponse> Signal = new TaskCompletionSource<ActionResponse>();

			ActionResponses[Request.ID] = Signal;

			StanzaMessage Message = new StanzaMessage();
			Message.MessageType = StanzaMessage.StanzaMessageType.Normal;
			Message.To = Orchestrator;
			Message.Payload.Add(Request.GetXML());
			Uplink.SendStanza(Message);

			if (Signal.Task != await Task.WhenAny(Signal.Task, Task.Delay(TimeoutSeconds * 1000)))
			{
				ActionResponses.TryRemove(Request.ID, out Signal);
				return new ActionResponse() 
				{ 
					ID = Request.ID, 
					Successful = false,
					TimedOut = true
				};
			}

			if (Signal.Task.Result == null)
				throw new Exception("Failed to make request");

			return Signal.Task.Result;
		}

		void Uplink_OnStanzaMessage(StanzaMessage Data)
		{
			//Console.WriteLine(Data.ToString());
			
			XElement RespEl = Data.Payload.Element(CDO.Namespace + "actionresponse");
			if (RespEl == null)
				return;

			ActionResponse Response = new ActionResponse(RespEl);

			TaskCompletionSource<ActionResponse> Signal;
			if (!ActionResponses.TryRemove(Response.ID, out Signal))
				return;

			Signal.SetResult(Response);
		}

		void Uplink_OnStanzaIQ(StanzaIQ Data)
		{
			TaskCompletionSource<StanzaIQ> Signal;
			if (!IQRequests.TryRemove(Data.ID, out Signal))
				return;
			Signal.SetResult(Data);


		}
	}
}
