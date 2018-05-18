using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Linq;
using DXMPP;

namespace CDO
{
    // This is just a sample class
    public class CDOController : IDisposable
    {
        ActionRequester Requester;
        JID Orchestrator;
        Dictionary<string, Session> ActiveSessions = new Dictionary<string, Session>();
        Action<Session> SessionStarted;
        Action<Session> SessionTerminated;
        Connection Uplink;


        public CDOController(Connection Uplink,
                             JID Orchestrator,
                             Action<Session> SessionStarted,
                             Action<Session> SessionTerminated)
        {
            this.Uplink = Uplink;
            Requester = new ActionRequester(Uplink, Orchestrator);
            Uplink.OnStanzaMessage += Uplink_OnStanzaMessage;
            //Uplink.Roster.OnSubscribe += Roster_OnSubscribe;
            //Uplink.Roster.OnUnsubscribed += Roster_OnUnsubscribed;
            this.Orchestrator = Orchestrator;
            this.SessionStarted = SessionStarted;
            this.SessionTerminated = SessionTerminated;
        }

        DateTime NextSweepNeeded = DateTime.MaxValue;
        void UnsafeSweepOldSessions()
        {
            DateTime NowUTC = DateTime.UtcNow;
            if (NowUTC < NextSweepNeeded)
                return;

            DateTime NewNextSwpeeNeeded = DateTime.MaxValue;

            List<string> SessionsToRemove = new List<string>();
            foreach (var jox in ActiveSessions)
            {
                if (jox.Value.SessionEndsAtUTC < NowUTC)
                {
                    SessionsToRemove.Add(jox.Key);
                    if (SessionTerminated != null)
                        SessionTerminated.Invoke(jox.Value);

                    continue;
                }

                if (jox.Value.SessionEndsAtUTC < NewNextSwpeeNeeded)
                    NewNextSwpeeNeeded = jox.Value.SessionEndsAtUTC;
            }

            foreach (string ToRemove in SessionsToRemove)
                ActiveSessions.Remove(ToRemove);
        }



        void HandleSessionStarted(ActionRequest Req)
        {
            Session Data = new Session(Req);
            lock (ActiveSessions)
            {
                ActiveSessions[Data.Requester.GetBareJID() + Data.ID] = Data;
                if (Data.SessionEndsAtUTC < NextSweepNeeded)
                    NextSweepNeeded = Data.SessionEndsAtUTC;
            }
            if (SessionStarted != null)
                SessionStarted.Invoke(Data);

            ActionResponse Resp = new ActionResponse();
            Resp.ID = Req.ID;
            Resp.Successful = true;
            StanzaMessage RespMessage = new StanzaMessage();
            RespMessage.To = Orchestrator;
            RespMessage.Payload.Add(Resp.GetXML("genericresponse", Req.Name));
            RespMessage.MessageType = StanzaMessage.StanzaMessageType.Normal;
            Uplink.SendStanza(RespMessage);
        }

        void HandleSessionTerminated(ActionRequest Req)
        {
            Console.WriteLine(Req.Payload.ToString());
            string SessionID = CDO.GetString(Req.Payload, "sessionid");
            Session Terminated = null;
            lock (ActiveSessions)
            {
                if (ActiveSessions.ContainsKey(SessionID))
                {
                    Terminated = ActiveSessions[SessionID];
                    ActiveSessions.Remove(SessionID);
                }
            }
            if (this.SessionTerminated != null)
                this.SessionTerminated(Terminated);
        }

        Roster.SubscribeResponse Roster_OnSubscribe(JID From)
        {
            return Roster.SubscribeResponse.AllowAndSubscribe;
        }

        void Roster_OnUnsubscribed(JID From)
        {
            Uplink.Roster.Unsubscribe(From);
        }

        void Uplink_OnStanzaMessage(StanzaMessage Data)
        {
            try
            {
                if (Data.From.GetBareJID() != Orchestrator.GetBareJID())
                    return;

                XElement ActEl = Data.Payload.Element(CDO.Namespace + "actionrequest");
                if (ActEl == null)
                    return;

                ActionRequest Req = new ActionRequest(ActEl);

                switch (Req.Name)
                {
                    case "sessionstarted":
                        HandleSessionStarted(Req);
                        break;
                    case "sessionterminated":
                        HandleSessionTerminated(Req);
                        break;
                }
            }
            catch (System.Exception ex)
            {
                Console.WriteLine("Uncatched exception in on stanzamessage: " + ex.ToString());
            }
        }

        public async Task<bool> RegisterResources(List<Resource> Resources, int TimeoutSeconds = 60)
        {
            XElement Payload = new XElement(CDO.Namespace + "entitysetresources");

            XElement ResourceEl = CDO.StartList(Payload, "resources");
            foreach (Resource Res in Resources)
            {
                ResourceEl.Add(Res.GetXML());
            }

            var Resp = await Requester.Request("setresources", Payload, TimeoutSeconds);

            return Resp.Successful;
        }

        public async Task<bool> SetClaimKey(string Key, int TimeoutSeconds = 60)
        {
            XElement Payload = new XElement(CDO.Namespace + "entitysetclaimkey");
            CDO.SetString(Payload, "claimkey", Key);

            var Resp = await Requester.Request("setclaimkey", Payload, TimeoutSeconds);

            return Resp.Successful;
        }

        public async Task<Session> RequestSession(string TargetEntityID,
                                               List<ResourceAccess> Resources,
                                               TimeSpan SessionLength,
                                               int TimeoutSeconds = 60)
        {
            XElement Payload = new XElement(CDO.Namespace + "entityrequestsession");

            CDO.SetEntityID(Payload, "targetentity", TargetEntityID);
            var ResEl = CDO.StartList(Payload, "resourceaccessrights");
            foreach (ResourceAccess it in Resources)
            {
                it.AddToElement(ResEl);
            }
            CDO.SetInt(Payload, "sessionlength", (int)SessionLength.TotalSeconds);

            var Resp = await Requester.Request("requestsession", Payload, TimeoutSeconds);

            if (Resp.Successful != true)
                return null;

            Session Data = new Session(Resp);
            lock (ActiveSessions)
            {
                ActiveSessions[Data.ID] = Data;
                if (Data.SessionEndsAtUTC < NextSweepNeeded)
                    NextSweepNeeded = Data.SessionEndsAtUTC;
            }


            return Data;

        }

        // Returns address to source or null
        public async Task<JID> StartSession(string SessionID, int TimeoutSeconds = 60)
        {
            DateTime StartedAt = DateTime.UtcNow;

            XElement Payload = new XElement(CDO.Namespace + "entitystartsession");
            CDO.SetString(Payload, "sessionid", SessionID);

            var Resp = await Requester.Request("startsession", Payload, TimeoutSeconds);

            if (!Resp.Successful)
                return null;

            JID BareJID = CDO.GetAddress(Resp.Payload, "targetaddress");

            if (Uplink.Roster.AggregatedPresence.ContainsKey(BareJID.GetBareJID()))
            {
                return Uplink.Roster.AggregatedPresence[BareJID.GetBareJID()].Last().FullJID;
            }


            Uplink.Roster.Subscribe(BareJID);

            while (!Uplink.Roster.AggregatedPresence.ContainsKey(BareJID.GetBareJID()))
            {
                await Task.Delay(25);
                if ((DateTime.UtcNow - StartedAt).TotalSeconds > TimeoutSeconds)
                    throw new System.TimeoutException();
            }

            return Uplink.Roster.AggregatedPresence[BareJID.GetBareJID()].Last().FullJID;

        }

        public void Dispose()
        {
            Uplink.OnStanzaMessage -= Uplink_OnStanzaMessage;
            Uplink = null;
        }
    }
}
