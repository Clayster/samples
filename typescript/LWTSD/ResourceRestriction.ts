var xmpp = require('../node-xmpp/packages/client').xmpp;
var xml = require('../node-xmpp/packages/xml');

import {LWTSD} from './index';
import { SimplifiedType } from './SimplifiedType';

export class ResourceRestriction
{
    public MinExclusive : any;
    public MaxExclusive : any;
    public Length : number = Number.MAX_SAFE_INTEGER;

    static GetSimpleType(Element : any) : string
    {
        return Element.attrs.base;
    }

    public constructor(Element? : any)
    {
        if(!Element)
            return;                
        
        let minexclusive = Element.attrs.minexclusive;
        let maxexclusive = Element.attrs.maxexclusive;
        if(Element.attrs.length)
            this.Length = parseInt(Element.attrs.length);

        switch (ResourceRestriction.GetSimpleType(Element))
        {
            case "decimal":
                this.MinExclusive = minexclusive ? parseFloat(minexclusive) : Number.MIN_VALUE;
                this.MaxExclusive = maxexclusive ? parseFloat(maxexclusive) : Number.MAX_SAFE_INTEGER;
                break;
            case "integer":
                this.MinExclusive = minexclusive ? parseFloat(minexclusive) : Number.MIN_VALUE;
                this.MaxExclusive = maxexclusive ? parseFloat(maxexclusive) : Number.MAX_SAFE_INTEGER;
                break;
            case "float":
                this.MinExclusive = minexclusive ? parseFloat(minexclusive) : Number.MIN_VALUE;
                this.MaxExclusive = maxexclusive ? parseFloat(maxexclusive) : Number.MAX_SAFE_INTEGER;
                break;
            case "double":
                this.MinExclusive = minexclusive ? parseFloat(minexclusive) : Number.MIN_VALUE;
                this.MaxExclusive = maxexclusive ? parseFloat(maxexclusive) : Number.MAX_SAFE_INTEGER;
                break;
            case "boolean":
                this.MinExclusive = minexclusive ? (minexclusive=="true") : false;
                this.MaxExclusive = maxexclusive ? (maxexclusive=="true") : true;
                break;
            case "dateTime":
                this.MinExclusive = minexclusive ? new Date(minexclusive) : new Date(1900,1,1);
                this.MaxExclusive = maxexclusive ? new Date(maxexclusive) : new Date(3000,1,1);
                break;
            case "duration":
                throw new Error("Not implemented: Simplifed type duration restriction");
            case "time":
                throw new Error("Not implemented: Simplifed type time restriction");
            case "string":
                return;
            case "base64Binary":
                return;
        }
    }

    public static GetSerializedLimit(Value : any, SimpleType: SimplifiedType) : string
    {
        switch (SimpleType)
        {
            case SimplifiedType.String:
                throw new Error("Not implemented: Simplifed type string restriction");
            case SimplifiedType.Integer:
                return (Value as number).toString();
            case SimplifiedType.Decimal:
                return (Value as number).toString();
            case SimplifiedType.Double:
                return (Value as number).toString();
            case SimplifiedType.Float:
                return (Value as number).toString();
            case SimplifiedType.Duration:
                throw new Error("Not implemented: Simplifed type duration restriction");
            case SimplifiedType.Time:
                throw new Error("Not implemented: Simplifed type time restriction");
            case SimplifiedType.DateTime:
                return (Value as Date).toISOString();
            case SimplifiedType.Base64Binary:
                throw new Error("Not implemented: Simplifed type base64binary restriction");
        }

        throw new Error("Not implemented: Simplifed type " + SimplifiedType + " restriction");
    }

    public SerializeToSimpleType(TypeElement : any, SimpleType : SimplifiedType) : void
    {
        if(this.MinExclusive)
            TypeElement.attrs.minexclusive = ResourceRestriction.GetSerializedLimit(this.MinExclusive, SimpleType);
        if (this.MaxExclusive)
            TypeElement.attrs.maxexclusive = ResourceRestriction.GetSerializedLimit(this.MaxExclusive, SimpleType);
        if (this.Length)
            TypeElement.attrs.length = this.Length.toString();
        
    }
}
