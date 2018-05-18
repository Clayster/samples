var xmpp = require('../node-xmpp/packages/client').xmpp;
var xml = require('../node-xmpp/packages/xml');
var Guid = require('../Misc/Guid');
import {CDO} from './CDO';

export class ActionResponse
{
    public ID:string;
    public Successful:boolean;
    public TimedOut:boolean = false;
    /*
    public List<string> InformationMessages = new List<string>();
    public List<string> WarningMessages = new List<string>();
    public List<string> ErrorMessages= new List<string>();*/
    public Payload:any;

    public constructor(Element:any)
    {
        if(!Element)
            return;
        this.ID = Element.attrs.id as string;

        this.Payload = Element.children[0];
        this.Successful = CDO.GetBoolean(this.Payload, "successful");
    }

    public GetXML(ReturnTypename : string, ActionName : string) : any
    {
        let RespEl = xml("actionresponse", {"xmlns": CDO.Namespace});
        RespEl.attrs.id = this.ID;
        RespEl.attrs.name =  ActionName;
        let GenRespEl =  xml(ReturnTypename, {/*"xmlns": CDO.Namespace*/});
        CDO.AddBoolean(GenRespEl, "successful", this.Successful);
        RespEl.append(GenRespEl);
        return RespEl;
    }
}