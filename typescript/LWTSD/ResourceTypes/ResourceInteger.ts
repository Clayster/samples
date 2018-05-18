var xml = require('../../node-xmpp/packages/xml');

import { LWTSD } from '../index';
import { SimplifiedType } from '../SimplifiedType';
import { ResourceDescription } from '../ResourceDescription'
import { ResourceRestriction } from '../ResourceRestriction';
import { Resource } from './Resource'

// This should have some proper integer checks..
export class ResourceInteger extends Resource
{
    public readonly HistoricalValues :  Array< [Date, number] > = new  Array< [Date, number] > ();

    InnerValue : number;

    public GetValue() : number
    {
        return this.InnerValue;
    }

    public SetValue(Value : number) : void
    {
        if(this.InnerMinExclusive && Value < (this.InnerMinExclusive as Number))
            throw new Error("Value not within limits");
        if(this.InnerMaxExclusive && Value > (this.InnerMaxExclusive as Number))
            throw new Error("Value not within limits");

        this.InnerValue = Value;
        this.ModifiedAt = new Date();
    }

    public constructor(Element? : any)
    {
        super(Element);

        let points = Element.getChildren("point");
        for (let i = 0; i < points.length; i++)
        {
            let it = points[i];
            let TimeStamp = new Date(it.attrs.timestamp);
            let PointValue : number = parseInt(it.text());
            this.HistoricalValues.push( [TimeStamp, PointValue] );
            if (TimeStamp > this.ModifiedAt)
            {
                this.ModifiedAt = TimeStamp;
                this.InnerValue = PointValue;
            }
        }
    }

    public GetPoint() : any
    {
        let RVal = xml("point", { /*"xmlns": LWTSD.Namespace */}, this.GetValue());
        RVal.attrs.timestamp = this.ModifiedAt;
        return RVal;
    }

    public GetWriteValue() : string
    {
        return this.GetValue().toString();
    }

    public GetDescription() : ResourceDescription
    {
        return super.GetDescriptionFromSimpleType(SimplifiedType.Float);
    }

    public LoadFromWrite(WriteElement : any) : void
    {
        this.SetValue( WriteElement.text() );
    }
}
