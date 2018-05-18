var xmpp = require('../node-xmpp/packages/client').xmpp;
var xml = require('../node-xmpp/packages/xml');
var Guid = require('../Misc/Guid');
import {CDO} from './CDO';
import {DataVerb} from './DataVerb';

export class Resource 
{
    public Path:string;
    public Capabilities : Array<string> = new  Array<string>();
    public MetaAttributes : {[key: string] : string} =  {}; 
    public SupportedVerbs: Array<DataVerb> = new Array<DataVerb>();

    
    public GetXML() : any
    {
        let RVal = xml("resource", {xmlns:CDO.Namespace});

        CDO.AddResourcePath(RVal, "path", this.Path);
        let CapEL = CDO.StartList(RVal, "capabilities");
        for (let Cap of this.Capabilities)
        {
            CDO.SetString(CapEL, Cap);
        }
        if( Object.keys(this.MetaAttributes).length > 0)
        {
            let DictEl = CDO.StartDictionary(RVal, "metaattributes");
            for(let item in this.MetaAttributes)
            {
                let ItemEl = CDO.StartDictionaryItem(DictEl);
                CDO.AddString(ItemEl, "key", item);
                CDO.AddString(ItemEl, "value", this.MetaAttributes[item]);
            }
        }
        let SupVerbEl = CDO.StartList(RVal, "supportedverbs");
        for(let verb of this.SupportedVerbs)
        {
            CDO.SetDataVerb(SupVerbEl, verb);
        }

        return RVal;
    }
}
