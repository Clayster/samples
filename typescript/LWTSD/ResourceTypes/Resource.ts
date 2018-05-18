var xml = require('../../node-xmpp/packages/xml');

import {LWTSD} from '../index';
import {SimplifiedType} from '../SimplifiedType';
import {ResourceDescription} from '../ResourceDescription'
import { ResourceRestriction } from '../ResourceRestriction';

export abstract class Resource
{
    public Path : string;
    public ModifiedAt : Date;

    public Description : string;
    public Displayname : string;
    public Unit : string;
    public SupportsRead : boolean;
    public SupportsWrite : boolean;

    protected InnerMinExclusive : object;
    protected InnerMaxExclusive: object;
    protected Length : number;

    public readonly TotalPointsForWindow : number = 1;

    protected GetTimestampPoint(SimpleValue : string) : any
    {
        let RVal = xml("point",{ /*"xmlns" : LWTSD.name*/}, SimpleValue);
        RVal.attrs.timestamp = this.ModifiedAt.toISOString();

        return RVal;
    }

    protected GetDescriptionFromSimpleType(SimpleType: SimplifiedType) : ResourceDescription
    {
        let RVal = new ResourceDescription();
    
        RVal.Description = this.Description;
        RVal.DisplayName = this.Displayname;
        RVal.Path = this.Path;
        RVal.SimpleType = SimpleType;
        RVal.SupportsRead = this.SupportsRead;
        RVal.SupportsWrite = this.SupportsWrite;
        RVal.Restrictions = new ResourceRestriction();
        RVal.Restrictions.MinExclusive = this.InnerMinExclusive;
        RVal.Restrictions.MaxExclusive = this.InnerMaxExclusive;
        RVal.Restrictions.Length = this.Length;

        return RVal;
    }
    
    public abstract GetDescription() : ResourceDescription;
    public abstract LoadFromWrite(WriteElement : any) : void;
    public abstract GetWriteValue() : string;

    public constructor(Element? : any)
    {
        if(Element)
        {
            this.TotalPointsForWindow = parseInt(Element.attrs.totalpoints);
            this.Path = Element.attrs.path;
        }
    }

}