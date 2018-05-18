using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DXMPP;

namespace CDOLWTSD
{
    public class CDODataStorageEntity : IDisposable
    {
        public CDO.CDOController SessionController;
        LWTSD.DataSource LowlevelSource;

        void SessionTerminated(CDO.Session Session)
        {
            LowlevelSource.RemoveAccessToken(Session.Requester, Session.ID);
        }

        void SessionStarted(CDO.Session Session)
        {
            LWTSD.AccessTokenSession LowlevelSession = new LWTSD.AccessTokenSession();
            LowlevelSession.Actor = Session.Requester;
            LowlevelSession.ExpiresAtUTC = Session.SessionEndsAtUTC;
            LowlevelSession.Token = Session.ID;
            LowlevelSession.Rights = new List<LWTSD.ResourceAccess>();
            foreach (var Res in Session.Resources)
            {
                LWTSD.ResourceAccess ResourceRights = new LWTSD.ResourceAccess();
                ResourceRights.SupportsRead = Res.Verbs.Contains(CDO.DataVerb.GET);
                ResourceRights.SupportsWrite = Res.Verbs.Contains(CDO.DataVerb.SET);
                ResourceRights.Path = Res.ResourcePath;
                ResourceRights.Subordinates = Res.Subordinates;
                LowlevelSession.Rights.Add(ResourceRights);
            }
            LowlevelSource.SetAccessTokenSession(Session.Requester, LowlevelSession);
        }

        public void Dispose()
        {
            SessionController.Dispose();
            LowlevelSource.Dispose();
        }



        public void AddResource(LWTSD.ResourceTypes.Resource Data)
        {
            LowlevelSource.AddResource(Data);
        }

        public async Task<bool> RegisterResources()
        {
            var AllResources = LowlevelSource.GetAllResources();

            List<CDO.Resource> Resources = new List<CDO.Resource>();
            foreach (LWTSD.ResourceTypes.Resource Res in AllResources)
            {
                CDO.Resource CDORes = new CDO.Resource();
                CDORes.Path = Res.Path;
                CDORes.Capabilities.Add("urn:clayster:lwtsd");
                if (Res.SupportsRead)
                    CDORes.SupportedVerbs.Add(CDO.DataVerb.GET);
                if (Res.SupportsWrite)
                    CDORes.SupportedVerbs.Add(CDO.DataVerb.SET);

                CDORes.MetaAttributes["software"] = "Clayster sample 1.0";

                Resources.Add(CDORes);
            }
            return await SessionController.RegisterResources(Resources);
        }

        public void InvalidateSubscriptions()
        {
            LowlevelSource.InvalidateData(null);
        }

        public CDODataStorageEntity(Connection Uplink, JID Orchestrator)
        {
            LowlevelSource = new LWTSD.DataSource(Uplink, true);
            SessionController = new CDO.CDOController(Uplink, Orchestrator, SessionStarted, SessionTerminated);
        }
    }
}
