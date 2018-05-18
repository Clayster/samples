import { DataVerb } from "./DataVerb";

var xmpp = require('../node-xmpp/packages/client').xmpp;
var xml = require('../node-xmpp/packages/xml');
import {DataVerbToString} from './DataVerb';

export class CDO 
{
    public static Namespace:string = "urn:clayster:cdo";
    
    public static GetBoolean(Element : any, Name : string, DefaultValue?:boolean ) : boolean
    {
        var it = Element.getChild(Name);
        if(!it && DefaultValue == undefined) 
        {
            throw new Error("Element not found: " + Name);
        }
        if(!it) 
            return DefaultValue as boolean;
        
        return it.getChildText("boolean") == "true";
    }

    public static GetInt(Element : any, Name : string, DefaultValue?:number ) : number
    {
        var it = Element.getChild(Name);
        if(!it && DefaultValue == undefined) 
        {
            throw new Error("Element not found: " + Name);
        }
        if(!it) 
            return DefaultValue as number;
        
        return parseInt( it.getChildText("integer") );
    }

    public static GetTimestamp(Element : any, Name : string, DefaultValue?:Date ) : Date
    {
        var it = Element.getChild(Name);
        if(!it && DefaultValue == undefined) 
        {
            throw new Error("Element not found: " + Name);
        }
        if(!it) 
            return DefaultValue as Date;
        
        return new Date( it.getChildText("timestamp") );
    }

    public static GetString(Element : any, Name : string, DefaultValue?:string ) : string
    {
        var it = Element.getChild(Name);
        if(!it && DefaultValue == undefined) 
        {
            throw new Error("Element not found: " + Name);
        }
        if(!it) 
            return DefaultValue as string;
        
        return it.getChildText("string");
    }

    public static GetAddress(Element : any, Name : string, DefaultValue?:string ) : string
    {
        var it = Element.getChild(Name);
        if(!it && DefaultValue == undefined) 
        {
            throw new Error("Element not found: " + Name);
        }
        if(!it) 
            return DefaultValue as string;
        
        return it.getChildText("address");
    }


    public static GetResourcePath(Element : any, Name : string, DefaultValue?:string ) : string
    {
        var it = Element.getChild(Name);
        if(!it && DefaultValue == undefined) 
        {
            throw new Error("Element not found: " + Name);
        }
        if(!it) 
            return DefaultValue as string;
        
        return it.getChildText("resourcepath");
    }

    public static GetDataVerb(Element : any) : DataVerb
    {
        switch (Element.text())
        {
            case "GET":
                return DataVerb.GET;
            case "DELETE":
                return DataVerb.DELETE;
            case "ADD":
                return DataVerb.ADD;
            case "SET":
                return DataVerb.SET;
            default:
                throw new Error("Invalid verb: " + Element.text());
        }
    }
/*
    export static System.Tuple<DateTime?, DateTime?> GetTimeframe(XElement Element, string Name) {
        
        XElement ValElement = new XElement(CDO.Namespace + Name);
        if (ValElement == null)
            return null;
        DateTime? ValueFrom = null;
        DateTime? ValueTo = null;

        try
        {
            ValueFrom = GetTimestamp(ValElement, "from");
        }
        catch
        {
            // No
        }

        try
        {
            ValueTo = GetTimestamp(ValElement, "to");
        }
        catch
        {
            // No
        }


        return new Tuple<DateTime?, DateTime?>(ValueFrom, ValueTo);
    }

    }*/
    public static SetDataVerb( Element:any, Value:DataVerb) : void
    {
        let El = xml("dataverb", {/*"xmlns" : CDO.Namespace*/}, DataVerbToString(Value) );
        Element.append(El);
    }

    public static AddDataVerb( Element:any, Name:string, Value:DataVerb) : void
    {
        let ValElement = xml(Name, {/*"xmlns" : CDO.Namespace*/});
        this.SetDataVerb(ValElement, Value);
        Element.append(ValElement);
    }
    public static SetResourcePath(Element : any, Value : string) : void
    {
        let El = xml("resourcepath", {/*"xmlns" : CDO.Namespace*/}, Value);
        Element.append(El);
    }

    public static AddResourcePath(Element: any, Name:string, Value:string) : void
    {
        let ValElement = xml(Name, {/*"xmlns" : CDO.Namespace*/});
        this.SetResourcePath(ValElement, Value);
        Element.append(ValElement);
    }

    public static SetEntityID(Element : any, Value : string) : void
    {
        let El = xml("entityid", {/*"xmlns" : CDO.Namespace*/}, Value);
        Element.append(El);
    }

    public static AddEntityID(Element: any, Name:string, Value:string) : void
    {
        let ValElement = xml(Name, {/*"xmlns" : CDO.Namespace*/});
        this.SetEntityID(ValElement, Value);
        Element.append(ValElement);
    }

    public static SetString(Element : any, Value : string) : void
    {
        let El = xml("string", {/*"xmlns" : CDO.Namespace*/}, Value);
        Element.append(El);
    }

    public static AddString(Element: any, Name:string, Value:string) : void
    {
        let ValElement = xml(Name, {/*"xmlns" : CDO.Namespace*/});
        this.SetString(ValElement, Value);
        Element.append(ValElement);
    }

    public static SetBoolean(Element : any, Value : boolean) : void
    {
        let El = xml("boolean", {/*"xmlns" : CDO.Namespace*/}, Value?"true":"false");
        Element.append(El);
    }

    public static AddBoolean(Element: any, Name:string, Value:boolean) : void
    {
        let ValElement = xml(Name, {/*"xmlns" : CDO.Namespace*/});
        this.SetBoolean(ValElement, Value);
        Element.append(ValElement);
    }

    public static SetInt(Element : any, Value : number) : void
    {
        let El = xml("integer", {/*"xmlns" : CDO.Namespace*/}, Value.toString());
        Element.append(El);
    }

    public static AddInt(Element: any, Name:string, Value:number) : void
    {
        let ValElement = xml(Name, {/*"xmlns" : CDO.Namespace*/});
        this.SetInt(ValElement, Value);
        Element.append(ValElement);
    }

    public static SetDate(Element : any, Value : Date) : void
    {
        let El = xml("timestamp", {/*"xmlns" : CDO.Namespace*/}, Value.toISOString());
        Element.append(El);
    }

    public static AddDateTime(Element: any, Name:string, Value:Date) : void
    {
        let ValElement = xml(Name, {/*"xmlns" : CDO.Namespace*/});
        this.SetDate(ValElement, Value);
        Element.append(ValElement);
    }


/*  

    export static void SetTimeframe(XElement Element, string Name, DateTime ?ValueFrom, DateTime ?ValueTo) {
        if (ValueFrom == null && ValueTo == null)
            return;
        
        XElement ValElement = new XElement(CDO.Namespace + Name);

        if(ValueFrom != null)
            SetDateTime(ValElement, "from", ValueFrom.Value);
        if (ValueTo != null)
            SetDateTime(ValElement, "to", ValueTo.Value);

        Element.Add(ValElement);
    }

*/


    public static StartList(Element : any, Name:string) : any
    {
        let Container = xml(Name, {"xmlns": CDO.Namespace});
        let RVal = xml("list");
        Container.append(RVal);
        Element.append(Container);

        return RVal;
    }
    public static StartDictionary(Element:any, Name:string) : any
    {
        let Container = xml(Name, {"xmlns":CDO.Namespace});
        let RVal = xml("list");
        Container.append(RVal);
        Element.append(Container);

        return RVal;
    }

    public static StartDictionaryItem(Element:any) : any
    {
        let RVal = xml("item");
        Element.append(RVal);
        return RVal;
    }


    public static GetList(Element:any, Name:string) : any|null
    {
        let it = Element.getChild(Name);
        if (it == null)
            return null;
        if(it.length==0)
            return null;

        return it.getChild("list");
    }
}
