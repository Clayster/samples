
var xmpp = require('../node-xmpp/packages/client').xmpp;
var xml = require('../node-xmpp/packages/xml');
var jid = require('../node-xmpp/packages/jid');
var Guid = require('../Misc/Guid');
import {CDO} from './CDO';
import {DataVerb} from './DataVerb';
import {ResourceAccess} from './ResourceAccess';
import {ActionResponse} from './ActionResponse';
import { Session } from './Session';
import { ActionRequest } from './index';

// TODO: The request ID:s are not safe (could be spoofed - prefix with barejid)

export class ActionRequester 
{
    //ActionRequester Requester;
    private Orchestrator : string; // JID
    private ActiveResponsesResolve : Map<string, (Response:ActionResponse) => void> = new Map<string, (Response:ActionResponse) => void>();
    private ActiveResponsesReject : Map<string, (Reason:string) => void> = new Map<string, (Reason:string) => void>();
    private Uplink : any;    


    private OnStanza ( Message : any) : void
    {
        try
        {
            //console.log("Got stanza message in ActionRequester from " + Message.attrs.from);
            if(!Message.is('message'))
            {
                //console.log("not message - returning");
                return;
            }
                        
            let from = jid.jid(Message.attrs.from);
            if(from.bare()!= this.Orchestrator)
            {
                //console.log("Warning; got message from not orchestrator? From = " + Message.attrs.from);
                return;
            }

            let arelement = Message.getChild("actionresponse");
            if(!arelement)
                return;

            let ar = new ActionResponse(arelement);
            if(this.ActiveResponsesResolve.has(ar.ID))
            {
                console.log("Running resolver on " + ar.ID);
                let resolver = this.ActiveResponsesResolve.get(ar.ID);
                if(resolver)
                    resolver( ar );                
                this.ActiveResponsesResolve.delete(ar.ID);
                this.ActiveResponsesReject.delete(ar.ID);
                return;
            }

            console.log("Warning; got action response from unregistered request from " + Message.attrs.from + " with id  = " + ar.ID);
        }
        catch(err)
        {
            console.log("Unhandled exception i ActionRequester: " + err);
        }
        
    }
    public constructor( Uplink:any, 
        Orchestrator : string)
    {
        this.Orchestrator = Orchestrator;
        this.Uplink = Uplink;
        this.Uplink.on('stanza', this.OnStanza.bind(this));
    }

    public async Request( Name : string , 
        Payload : any, TimeoutSeconds : number) 
        : Promise<ActionResponse>
    {
        // TODO: Sweep
        let Request = new ActionRequest();
        Request.Payload = Payload;
        Request.TimesOut = new Date( new Date().getTime() + TimeoutSeconds*1000);
        Request.AckTimesOut = new Date( new Date().getTime() + 5000);
        console.log("Timesout = " + Request.TimesOut.toISOString());
        console.log("AckTimesOut = " + Request.AckTimesOut.toISOString());
        Request.Name = Name;
        let Resolves = this.ActiveResponsesResolve;
        let Rejects = this.ActiveResponsesReject;
        let Orchestrator = this.Orchestrator;
        let Uplink = this.Uplink;

        let RVal = new Promise<ActionResponse>(
            function (resolve, reject) 
            {
                Resolves.set(Request.ID, resolve);
                Rejects.set(Request.ID, reject);

                let msg = xml('message');
                msg.attrs.to = Orchestrator;
                msg.append(Request.GetXML());
                Uplink.send(msg);
            });

        return RVal;
    }

}
