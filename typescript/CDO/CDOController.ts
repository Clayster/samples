
var xmpp = require('../node-xmpp/packages/client').xmpp;
var xml = require('../node-xmpp/packages/xml');
var Guid = require('../Misc/Guid');
import {CDO} from './CDO';
import {DataVerb} from './DataVerb';
import {ResourceAccess} from './ResourceAccess';
import {ActionResponse} from './ActionResponse';
import {Session} from './Session';
import {ActionRequester} from './ActionRequester'


// This is just a sample class
// For now; only supports requesting actions (not responding to them
// For full example see C# version
export class CDOController 
{
    private Requester:ActionRequester;
    private Orchestrator : string; // JID
    private ActiveSessions : Map<string, Session> = new Map<string,Session>();
//    private SessionStarted : (Session : Session) => void;
    private SessionTerminated : (Session : Session) => void;
    private Uplink : any;    

    public constructor( Uplink:any, 
        Orchestrator : string,
        //SessionStarted: ((Sesbsion : Session) => void),
        SessionTerminated: ((Session : Session) => void) )
    {
        this.Uplink = Uplink;
        this.Requester = new ActionRequester(Uplink, Orchestrator);
        
        // Todo; handle presnce subscriptions?
        this.Orchestrator = Orchestrator;
        //this.SessionStarted = SessionStarted;
        this.SessionTerminated = SessionTerminated;
    }
    

    public async SetClaimKey(Key : string, TimeoutSeconds: number = 60) : Promise<boolean>
    {
        let Payload = xml("entitysetclaimkey", {/*"xmlns": CDO.Namespace */ });

        CDO.AddString(Payload, "claimkey", Key);

        let Resp = await this.Requester.Request("setclaimkey", Payload, TimeoutSeconds);

        return Resp.Successful;
    }

    public async RequestSession(TargetEntityID: string,
        Resources : Array<ResourceAccess>,
        SessionLengthSeconds: number,
        TimeoutSeconds: number = 60)
        : Promise<Session>
    {
       let Payload = xml("entityrequestsession", {/*"xmlns": CDO.Namespace */ });
       CDO.AddEntityID(Payload, "targetentity", TargetEntityID);
       let ResEl = CDO.StartList(Payload, "resourceaccessrights");
       for(let it of Resources)
       {
           it.AddToElement(ResEl);
       }
        CDO.AddInt(Payload, "sessionlength" , SessionLengthSeconds);
        let Resp = await this.Requester.Request("requestsession", Payload, TimeoutSeconds);

        if (Resp.Successful != true)
            throw new Error("Request session failed");

        return new Session(Resp);
    }

    
    // Returns address to source or null
    public async StartSession( SessionID: string, 
        TimeoutSeconds : number = 60) 
        : Promise<string>
    {
        let Payload = xml("entitystartsession", {/*"xmlns": CDO.Namespace*/ });
        CDO.AddString(Payload, "sessionid", SessionID);

        var Resp = await this.Requester.Request("startsession", Payload, TimeoutSeconds);

        if (!Resp.Successful)
            throw new Error("Start session failed");

        let BareJID = CDO.GetAddress(Resp.Payload, "targetaddress");

        /*
        TODO FIX ROSTER
        if (Uplink.Roster.AggregatedPresence.ContainsKey(BareJID.GetBareJID()))
        {
            return Uplink.Roster.AggregatedPresence[BareJID.GetBareJID()].Last().FullJID;
        }


        Uplink.Roster.Subscribe(BareJID);

        while (!Uplink.Roster.AggregatedPresence.ContainsKey(BareJID.GetBareJID()))
        {
            await Task.Delay(25);
        }
        

        return Uplink.Roster.AggregatedPresence[BareJID.GetBareJID()].Last().FullJID;
        */
        console.log("Warning: Bare jid returned from StartSession (CDOController) - this will not be usefull for LWTSD");
        return BareJID;
    }
}
