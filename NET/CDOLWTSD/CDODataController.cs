using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Linq;
using DXMPP;

namespace CDOLWTSD
{
    public class CDODataController : IDisposable
    {
        CDO.CDOController SessionController;
        LWTSD.DataController LowlevelDataController;
        Connection Uplink;
        JID Orchestrator;
        string TargetEntityID;

        public ConcurrentDictionary<LWTSD.ResourcePath, CDO.ResourceAccess> RequestedResources =
            new ConcurrentDictionary<LWTSD.ResourcePath, CDO.ResourceAccess>();

        LWTSD.SimplifiedSchema KnownSchema = null;
        public volatile CDO.Session ActiveSession;


        void SessionTerminated(CDO.Session Session)
        {
            Console.WriteLine("Session terminated!");

            if (Session.ID == ActiveSession.ID)
                ActiveSession = null;
        }

        public async Task<bool> RequestNewSession(TimeSpan SessionLength, int TimeOutSeconds = 60)
        {
            Console.WriteLine("Await request session");
            CDO.Session NewSession = await SessionController.RequestSession(
                    TargetEntityID, RequestedResources.Values.ToList(), SessionLength, TimeOutSeconds);
            Console.WriteLine("Done");
            if (NewSession == null)
                throw new AccessViolationException("New session was actively denied");

            JID TargetJID = await SessionController.StartSession(NewSession.ID);
            if (TargetJID == null)
                throw new InvalidProgramException("Session failed to start");

            if (LowlevelDataController != null)
            {
                LowlevelDataController.Dispose();
                LowlevelDataController = null;
            }

            LowlevelDataController = new LWTSD.DataController(Uplink, TargetJID);
            ActiveSession = NewSession;
            return true;
        }

        public async Task<LWTSD.DataController.DataPage> ReadData(int Page = 0)
        {
            if (RequestedResources.Count() == 0)
                throw new InvalidOperationException("No requested resources");

            if (KnownSchema != null)
            {
                foreach (var rkey in RequestedResources.Keys)
                {
                    if (!KnownSchema.Resources.ContainsKey(rkey))
                    {
                        KnownSchema = null;
                        ActiveSession = null;
                        break;
                    }

                }
            }

            if (ActiveSession == null || ActiveSession.SessionEndsAtUTC < DateTime.UtcNow.AddSeconds(15) )
            {
                await RequestNewSession(new TimeSpan(1, 0, 0));
            }



            return await LowlevelDataController.ReadData(KnownSchema,
                                                         RequestedResources.Keys.ToList(),
                                                         ActiveSession.ID);

        }

        public async Task<LWTSD.DataController.DataPage> ReadData(List<LWTSD.ResourcePath> Resources, int Page = 0)
        {
            if (RequestedResources.Count() == 0)
                throw new InvalidOperationException("No requested resources");

            foreach (var Res in Resources)
            {
                if (!RequestedResources.ContainsKey(Res))
                    throw new InvalidOperationException("Cannot request resource not in requested resources");
            }

            if (KnownSchema != null)
            {
                foreach (var rkey in RequestedResources.Keys)
                {
                    if (!KnownSchema.Resources.ContainsKey(rkey))
                    {
                        KnownSchema = null;
                        ActiveSession = null;
                        break;
                    }
                }
            }

            if (ActiveSession == null || ActiveSession.SessionEndsAtUTC < DateTime.UtcNow.AddSeconds(15))
            {
                await RequestNewSession(new TimeSpan(1, 0, 0));
            }


            return await LowlevelDataController.ReadData(KnownSchema,
                                                         RequestedResources.Keys.ToList(),
                                                         ActiveSession.ID,
                                                         Page);
        }

        public void Dispose()
        {
            SessionController.Dispose();
            LowlevelDataController.Dispose();
            Uplink = null;
        }


        public CDODataController(Connection Uplink,
                                 JID Orchestrator,
                                 string TargetEntityID,
                                 List<CDO.ResourceAccess> RequestedResources = null)
        {
            SessionController = new CDO.CDOController(Uplink, Orchestrator, null, SessionTerminated);
            this.TargetEntityID = TargetEntityID;
            this.Orchestrator = Orchestrator;
            this.Uplink = Uplink;

            if (RequestedResources != null)
                foreach (var res in RequestedResources)
                    this.RequestedResources[res.ResourcePath] = res;
        }
    }
}
