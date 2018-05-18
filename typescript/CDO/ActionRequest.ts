var xmpp = require('../node-xmpp/packages/client').xmpp;
var xml = require('../node-xmpp/packages/xml');
var Guid = require('../Misc/Guid');
import {CDO} from './CDO';

export class ActionRequest 
{
    public ID:string = Guid.NewGuid();
    public Name:string;
    public TimesOut: Date;
    public AckTimesOut : Date;
    public Payload : any;

    public GetXML() : any
    {
        let RVal = xml("actionrequest", {xmlns:CDO.Namespace});
        RVal.attrs.id = this.ID;
        RVal.attrs.name = this.Name;
        RVal.attrs.timeouts = this.TimesOut.toISOString();
        RVal.attrs.acktimeouts = this.AckTimesOut.toISOString();

        RVal.append(this.Payload);
        return RVal;
    }

    public constructor(Data? : any)
    {
        if(Data)
        {
            this.Name = Data.attrs.name;
            this.ID = Data.attrs.id;
            this.TimesOut = new Date(Data.attrs.timesout);
            this.AckTimesOut = new Date(Data.attrs.acktimesout);
            this.Payload = Data.children[0];
        }
    }

}
