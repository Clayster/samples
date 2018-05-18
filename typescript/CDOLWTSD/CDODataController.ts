import * as CDO from '../CDO/index';
import * as LWTSD from '../LWTSD/index';
import {AggregatedPresence, PresenceSubscribeResponse} from '../Misc/AggregatedPresence'

var xmpp = require('../node-xmpp/packages/client').xmpp;
var xml = require('../node-xmpp/packages/xml');

export class CDODataController
{
	SessionController : CDO.CDOController;
	LowlevelDataController : LWTSD.DataController | null;
	PresenceHandler : AggregatedPresence;
	Uplink : any;
	Orchestrator : string;
	TargetEntityID : string;

	public RequestedResources : Map<string, CDO.ResourceAccess> = new Map<string, CDO.ResourceAccess> ();

	KnownSchema : LWTSD.SimplifiedSchema | null;
	ActiveSession : CDO.Session | null;


	SessionTerminated(Session : CDO.Session) : void
	{
		if(!this.ActiveSession)
			return;

		if (Session.ID == this.ActiveSession.ID)
			this.ActiveSession = null;

		// TODO: check if any ongoing or pending new sessions towards JID ; if not - unsubscribe
	}

	public async RequestNewSession() 
	: Promise<boolean>
	{
		let NewSession = await this.SessionController.RequestSession(this.TargetEntityID, 
			Array.from(this.RequestedResources.values()), 
			60);
		if (NewSession == null)
			throw new Error("New session was actively denied");

		let BareJID = await this.SessionController.StartSession(NewSession.ID);

		let FullJids : Set<string> = await this.PresenceHandler.GetFullJIDs(BareJID);
		if(!FullJids || FullJids.size == 0)
		{
			throw new Error("Session failed to start; no full jid available");
		}

		let TargetJID = Array.from(FullJids.keys())[0];
		console.log("TargetJID = " + TargetJID);

		if (!TargetJID)
			throw new Error("Session failed to start");
		
		if (this.LowlevelDataController)
		{
			// TODO: unbind stuff against Client -- Dispose stuff...
			this.LowlevelDataController = null;
		}

		this.LowlevelDataController = new LWTSD.DataController(this.Uplink, TargetJID);
		this.ActiveSession = NewSession;
		return true;
	}

	public async ReadData(Page? : number, Resources? : Array<string>)
	: Promise<LWTSD.DataPage>
	{
		if(!Page)
			Page = 0;

		if (this.RequestedResources.size == 0)
			throw new Error("No requested resources");

		if (this.KnownSchema)
		{
			for (let rkey of Array.from(this.RequestedResources.keys()) )
			{
				if (!this.KnownSchema.Resources.has(rkey))
				{
					this.KnownSchema = null;
					this.ActiveSession = null;
					break;
				}

			}
		}

		if (this.ActiveSession == null)
		{
			let t = await this.RequestNewSession();
			// not use t..			
		}

		if(!Resources)
			Resources = Array.from(this.RequestedResources.keys());

		for (let Res of Resources)
		{
			if (!this.RequestedResources.has(Res))
				throw new Error("Cannot request resource not in requested resources");
		}

		if(!this.LowlevelDataController)
			throw new Error("LowlevelDataController is null")
		if(!this.ActiveSession)
			throw new Error("ActiveSession is null")

		/*if(!this.KnownSchema)
		{
			this.KnownSchema = await this.LowlevelDataController.GetSchema();
		}*/

		return await this.LowlevelDataController.ReadData(this.KnownSchema,
			Resources,
			this.ActiveSession.ID);

	}

	private HandlePresenceSubscribeRequest( From : string) : PresenceSubscribeResponse
	{
		return PresenceSubscribeResponse.AllowAndSubscribe;
	}
	
	public constructor(Uplink :any, 
		Orchestrator : string, 
		TargetEntityID : string, 
		RequestedResources? : Array<CDO.ResourceAccess> )
	{
		this.PresenceHandler = new AggregatedPresence(Uplink, this.HandlePresenceSubscribeRequest.bind(this));
		this.SessionController = new CDO.CDOController(Uplink, Orchestrator, this.SessionTerminated.bind(this));
		this.TargetEntityID = TargetEntityID;
		this.Orchestrator = Orchestrator;
		this.Uplink = Uplink;

		if (RequestedResources != null)
			for (let res of RequestedResources)
				this.RequestedResources.set(res.ResourcePath, res);
	}
}
