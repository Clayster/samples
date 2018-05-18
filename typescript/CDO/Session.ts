var xmpp = require('../node-xmpp/packages/client').xmpp;
var xml = require('../node-xmpp/packages/xml');
var Guid = require('../Misc/Guid');
import {CDO} from './CDO';
import {DataVerb} from './DataVerb';
import {ResourceAccess} from './ResourceAccess';
import {ActionResponse} from './ActionResponse';
import { ActionRequest } from './index';


export class Session
{
    public ID:string;
    public Resources : Array<ResourceAccess> = new Array<ResourceAccess>();
    public SessionEndsAt : Date;
    public Requester : string; // JID
    public Source : string; // JID


    LoadFromResponse(Resp : ActionResponse) : void
    {
        this.ID = CDO.GetString(Resp.Payload, "sessionid");
        let SessionLengthSeconds = CDO.GetInt(Resp.Payload, "sessionlength", 0);

        this.SessionEndsAt = new Date(3000,1,1);

        if(SessionLengthSeconds > 0)
            this.SessionEndsAt = new Date(new Date().getTime()+CDO.GetInt(Resp.Payload, "sessionlength"));

            //new Date( new Date().getTime() + 60000);
        let ResEl = CDO.GetList(Resp.Payload, "resourceaccessrights");

        let ResourceAccessRights = ResEl.getChildren("resourceaccess");
        for( let i = 0; i<  ResourceAccessRights.length; i++) 
        {
            let ResAcc = ResourceAccessRights[i];
            this.Resources.push(new ResourceAccess(ResAcc));
        }
/* TODO
        for (var el in Resp.Payload.Elements())
        {
            if (el.Name.LocalName.Contains("contract"))
                throw new Exception("Contracts not supported");
        }*/
    }
    LoadFromRequest( Req : ActionRequest) : void
    {
        /*
        ID = CDO.GetString(Req.Payload, "sessionid");
        int SessionLengthSeconds = CDO.GetInt(Req.Payload, "sessionlength", 0);

        SessionEndsAtUTC = DateTime.MaxValue;

        if (SessionLengthSeconds > 0)
            SessionEndsAtUTC = DateTime.UtcNow.AddSeconds(CDO.GetInt(Req.Payload, "sessionlength"));

#warning sessionexpiration is a spelling error in cdo, should be sessionexpires
        SessionEndsAtUTC = CDO.GetTimestamp(Req.Payload, "sessionexpiration", SessionEndsAtUTC);

        XElement ResEl = CDO.GetList(Req.Payload, "resourceaccessrights");
        var ResourceAccessRights = ResEl.Elements(CDO.Namespace + "resourceaccess");
        foreach (var ResAcc in ResourceAccessRights)
        {
            Resources.Add(new ResourceAccess(ResAcc));
        }

        foreach (var el in Req.Payload.Elements())
        {
            if (el.Name.LocalName.Contains("contract"))
                throw new Exception("Contracts not supported");
        }
        Requester = CDO.GetAddress(Req.Payload, "requesteeaddress");
    */
    }
    public constructor(RespOrReq? : any)
    {
        if(RespOrReq as ActionRequest != null)
            this.LoadFromRequest(RespOrReq as ActionRequest);
        if(RespOrReq as ActionResponse != null)
            this.LoadFromResponse(RespOrReq as ActionResponse);
    }

}

