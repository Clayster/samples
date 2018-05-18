var xml = require('../../node-xmpp/packages/xml');

import { LWTSD } from '../index';
import { SimplifiedType } from '../SimplifiedType';
import { ResourceDescription } from '../ResourceDescription'
import { ResourceRestriction } from '../ResourceRestriction';
import{ Resource } from './Resource'

export class ResourceBase64Binary extends Resource
{
    public readonly HistoricalValues :  Array< [Date, string] > = new Array< [Date, string] >();

    InnerValue : string;

    public GetValue() : string
    {
        return this.InnerValue;
    }

    public SetValue(Value : string) : void
    {
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
            let PointValue = it.text();
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
        return this.GetValue();
    }

    public GetDescription() : ResourceDescription
    {
        return super.GetDescriptionFromSimpleType(SimplifiedType.Base64Binary);
    }

    public LoadFromWrite(WriteElement : any) : void
    {
        this.SetValue( WriteElement.text() );
    }
}
