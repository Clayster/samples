var xml = require('../../node-xmpp/packages/xml');

import { LWTSD } from '../index';
import { SimplifiedType } from '../SimplifiedType';
import { ResourceDescription } from '../ResourceDescription'
import { ResourceRestriction } from '../ResourceRestriction';
import { Resource } from './Resource'

export class ResourceDateTime extends Resource
{
    public readonly HistoricalValues :  Array< [Date, Date] > = new Array< [Date, Date] >();

    InnerValue : Date;

    public GetValue() : Date
    {
        return this.InnerValue;
    }

    public SetValue(Value : Date) : void
    {
        if(this.InnerMinExclusive && Value < this.InnerMinExclusive)
            throw new Error("Value not within limits");
        if(this.InnerMaxExclusive && Value > this.InnerMaxExclusive)
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
            let PointValue : Date = new Date(it.text());
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
        return this.GetValue().toISOString();
    }

    public GetDescription() : ResourceDescription
    {
        return super.GetDescriptionFromSimpleType(SimplifiedType.DateTime);
    }

    public LoadFromWrite(WriteElement : any) : void
    {
        this.SetValue( WriteElement.text() );
    }
}
