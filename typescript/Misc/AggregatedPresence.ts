var xmpp = require('../node-xmpp/packages/client').xmpp;
var xml = require('../node-xmpp/packages/xml');
var jid = require('../node-xmpp/packages/jid');
import {Delay} from '../Misc/Delay'
import { error } from 'util';

export enum PresenceSubscribeResponse
{
    Reject,
    Allow,
    AllowAndSubscribe
}

export class AggregatedPresence
{
    Uplink : any;

    // Bare jid -> full jid's
    AggregatedPresence : Map<string, Set<string>> = new Map<string, Set<string>>();
    OnSubscribeRequest : (From : string) => PresenceSubscribeResponse;

    public constructor(Uplink : any, OnSubscribeRequest : (From : string) => PresenceSubscribeResponse)
    {
        this.Uplink = Uplink;
        this.OnSubscribeRequest = OnSubscribeRequest;
        this.Uplink.on('stanza', this.OnStanza.bind(this));
    }


    private OnStanza ( Data : any) : void
    {
        // Subscribe etc aswell?
        if(!Data.is('presence'))
            return;

        if(!Data.attrs.type)
            Data.attrs.type = "available";

        console.log("presence handler of type: " + Data.attrs.type);

        switch(Data.attrs.type)
        {
            case "subscribe":
                this.HandleSubscribe(Data);
                break;
            case "unavailable":
                this.HandleUnavailable(Data);
                break;
            case "available":
                this.HandleAvailable(Data);
                break;
        }
    }

    private HandleUnavailable( Data: any ) : void
    {
        let from = jid.jid(Data.attrs.from);
        let BareJID = from.bare();

        if( !this.AggregatedPresence.has( BareJID ))
            return;
        let s = this.AggregatedPresence.get(BareJID);
        if(!s)
            return;
        if( s.has(from) )
            s.delete(from);
    }

    private HandleAvailable( Data: any ) : void
    {
        let from = Data.attrs.from
        let BareJID =  jid.jid(Data.attrs.from).bare().toString();


        if( !this.AggregatedPresence.has( BareJID ))
            this.AggregatedPresence.set(BareJID, new Set<string>());

        let s = this.AggregatedPresence.get(BareJID);
        if(!s)
            throw new Error("Unknown error in handle available presence");
        
        console.log("Adding available to '" + BareJID + "' with full jid '" + from + "'");
        s.add(from);
    }

    public async GetFullJIDs(BareJID : string) : Promise<Set<string>>
    {        
        if( this.AggregatedPresence.has( BareJID ))
        {
            return this.AggregatedPresence.get(BareJID) as Set<string>;
        }
        console.log("Bare jid '" + BareJID + "' not found will subscribe and wait");

        this.Subscribe(BareJID);
        for( let i = 0; i < 10; i++)
        {
            await Delay(1000);
            if( this.AggregatedPresence.has( BareJID ))
            {
                console.log("Retrieved full jid of " + BareJID);
                return this.AggregatedPresence.get(BareJID) as Set<string>;
            }
        }
        console.log("Could not retrieve full jid of " + BareJID);
        let RVal = new Set<string>();
        return RVal;
    }

    public Subscribe(JID : string) : void
    {
        let from = jid.jid(JID);
        let BareJID = from.bare();

        let PresenceTag = xml("presence");
        PresenceTag.attrs.to = BareJID;
        PresenceTag.attrs.type = "subscribe";

        this.Uplink.send(PresenceTag);        
    }

    public HandleSubscribe(Element : any) : void
    {
        let PresenceTag = xml ("presence");
        PresenceTag.attrs.to = Element.attrs.from;
        PresenceTag.attrs.type = "subscribed";

        switch(this.OnSubscribeRequest(Element.attrs.from))
        {
            case PresenceSubscribeResponse.Allow:
                this.Uplink.send(PresenceTag);
                break;
            case PresenceSubscribeResponse.AllowAndSubscribe:
                this.Uplink.send(PresenceTag);
                this.Subscribe(Element.attrs.from);
                break;
            case PresenceSubscribeResponse.Reject:
                break;
        }
    }

    public Unsubscribe(JID : string): void
    {
        let from = jid.jid(JID);
        let BareJID = from.bare();

        let PresenceTag = xml ("presence");
        PresenceTag.attrs.to = BareJID;
        PresenceTag.attrs.type = "unsubscribe";
        this.Uplink.send(PresenceTag);
    }
}