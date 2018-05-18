var xml = require('../node-xmpp/packages/xml');

import {ResourceDescription} from './ResourceDescription';
import {GetHashCode} from '../Misc/GetHashCode'

export class SimplifiedSchema
{
    public Resources : Map<string /* Path */, ResourceDescription> = new Map<string, ResourceDescription>();
    LoadedVersion : string = "";
    LoadedTotalResources : number = -1;
    LoadedNrResources : number = -1;

    GetTotalResources() : number
    {
        if (this.LoadedTotalResources != -1)
            return this.LoadedTotalResources;

        return this.Resources.size;
    }

    GetResourcesInSchema() : number
    {			
        return this.Resources.size;
    }

    public GetVersion() : string
    {
        if (this.LoadedVersion != "")
            return this.LoadedVersion;
        
        let Temp = xml("temp");
        
        for (let entry of this.Resources)
        {
            let Res = entry[1];
            Temp.append(Res.GetSimplifiedSchemaResource());
        }

        return GetHashCode( Temp.ToString() ).toString()
    }

    /*
    public XElement GetSerializedElement(int startindex, 
                                            int maxitems, 
                                            List<ResourceAccess> LimitedRights = null)
    {
        if (startindex < 0)
            throw new InvalidOperationException("SimplifiedSchema: Start index < 0");
        if (maxitems < 0)
            throw new InvalidOperationException("SimplifiedSchema: Max items < 0");
        
        XElement RVal = new XElement( LWTSD.Namespace+ "simplified-schema");


        int Index = 0;
        int ReturnedResources = 0;
        int TotalResources = 0;
        foreach (ResourceDescription Res in Resources.Values)
        {
            if (!ResourceAccess.AllowsRead(LimitedRights, Res.Path) 
                && !ResourceAccess.AllowsWrite(LimitedRights, Res.Path))
            {
                continue;
            }
            TotalResources++;

            if (Index < startindex)
            {
                Index++;
                continue;
            }
            if (maxitems <= (Index + startindex))
                break;
            Index++;

            RVal.Add(Res.GetSimplifiedSchemaResource());
            ReturnedResources++;
        }

        RVal.SetAttributeValue("returnedresources", XmlConvert.ToString(ReturnedResources));
        RVal.SetAttributeValue("totalresources", XmlConvert.ToString(TotalResources));
        RVal.SetAttributeValue("version", GetVersion());

        return RVal;
    }*/


    public constructor( Element? :any )
    {
        if(!Element)
            return;

        this.LoadedVersion = Element.attrs.version;
        this.LoadedNrResources = parseInt(Element.attrs.returnedresources);
        this.LoadedTotalResources = parseInt(Element.attrs.totalresources);
        let resources = Element.getChildren("resource");
        for(let i = 0; i < resources.length; i++)
        {
            let It = resources[i];
            let LoadedDesc = new ResourceDescription(It);
            this.Resources.set(LoadedDesc.Path, LoadedDesc);
        }
    }
}
