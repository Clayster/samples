var xmpp = require('../node-xmpp/packages/client').xmpp;
var xml = require('../node-xmpp/packages/xml');

import { LWTSD } from './index';
import { SimplifiedType, ParseSimplifiedType, SimplifiedTypeToString } from './SimplifiedType';
import { ResourceRestriction } from './ResourceRestriction';


export class ResourceDescription
{    
    public Path : string;
    public XMLType : string;
    public SimpleType : SimplifiedType;
    public Restrictions : ResourceRestriction;

    public Unit : string;
    public Description : string;
    public DisplayName : string;

    public SupportsRead : boolean;
    public SupportsWrite : boolean;

    public SupportedFilters : Array<string> = new Array<string>();

    public constructor( Element? : any)
    {
        if(!Element)
            return;

        this.Path = Element.attrs.path;
        if(Element.attrs.unit)
            this.Unit = Element.attrs.unit;
        if (Element.attrs.description)
            this.Description = Element.attrs.description;

        let TypeDesc = Element.getChild("type");
        this.SimpleType =  ParseSimplifiedType(TypeDesc.attrs.base);
        this.Restrictions = new ResourceRestriction(TypeDesc);

        let Supports = Element.getChild("supports");
        this.SupportsRead = Supports.attrs.read == "true";
        this.SupportsWrite = Supports.attrs.write == "true";

        for(let i = 0; i < Supports.length; i++)
        {
            let It = Supports[i];
            if (It.name != "filter")
                continue;				
            this.SupportedFilters.push(It.attrs.name);
        }
    }

    public GetSupportsElement() : any
    {
        let SupportsElement = xml("supports", {/*"xmlns": LWTSD.Namespace*/});
        SupportsElement.attrs.read = this.SupportsRead ? "true": "false";
        SupportsElement.attrs.write = this.SupportsWrite ? "true": "false";

        for (let FilterName of this.SupportedFilters)
        {
            let FilterElement = xml("filter", {/*"xmlns": LWTSD.Namespace*/});
            FilterElement.attrs.name = FilterName;
            SupportsElement.append(FilterElement);
        }

        return SupportsElement;
    }

    public GetSimplifiedSchemaResource() : any
    {
        let RVal = xml("resource", {/*"xmlns": LWTSD.Namespace*/});
        RVal.attrs.path = this.Path;
        RVal.attrs.unit = this.Unit;
        RVal.attrs.description = this.Description;

        let TypeDesc = xml("type", {/*"xmlns": LWTSD.Namespace*/});
        TypeDesc.attrs.base =  SimplifiedTypeToString(this.SimpleType);
        if (this.Restrictions)
        {
            this.Restrictions.SerializeToSimpleType(TypeDesc, this.SimpleType);                
        }

        RVal.append(TypeDesc);
        RVal.append(this.GetSupportsElement());

        return RVal;
    }
}
