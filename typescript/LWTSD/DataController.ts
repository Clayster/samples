var xmpp = require('../node-xmpp/packages/client').xmpp;
var xml = require('../node-xmpp/packages/xml');
var jid = require('../node-xmpp/packages/jid');
var Guid = require('../Misc/Guid');

import {LWTSD} from './LWTSD';
import {SchemaFormat, ParseSchemaFormat, SchemaFormatToString} from './SchemaFormat';
import {SimplifiedSchema} from './SimplifiedSchema'
import {SimplifiedType} from './SimplifiedType'
import {Resource} from './ResourceTypes/Resource'

import {ResourceBase64Binary} from './ResourceTypes/ResourceBase64Binary';
import {ResourceBoolean} from './ResourceTypes/ResourceBoolean';
import {ResourceDateTime} from './ResourceTypes/ResourceDateTime';
import {ResourceDecimal} from './ResourceTypes/ResourceDecimal';
import {ResourceDouble} from './ResourceTypes/ResourceDouble';
//import {ResourceDuration} from './ResourceTypes/ResourceDuration';
import {ResourceFloat} from './ResourceTypes/ResourceFloat';
import {ResourceInteger} from './ResourceTypes/ResourceInteger';
import {ResourceString} from './ResourceTypes/ResourceString';
import { NewGuid } from '../Misc/Guid';
//import {ResourceTime} from './ResourceTypes/ResourceTime';


class SchemaMissMatchException
{
}

export class DataPage
{
	public static readonly PointsPerPage : number = 500;
	
	public Data : Array<Resource> = new Array<Resource>();
	public Schema : SimplifiedSchema |null = null;
	public NrTotalPages : number = 1;
	public Page : number = 0;

	public constructor(Element? : any, KnownSchema? : SimplifiedSchema, Page? : number)
	{
		if(!Element)
			return;

		if (KnownSchema == null)
			throw new SchemaMissMatchException();

		let NewSchemaVersion = Element.attrs.schemaversion;
		if (NewSchemaVersion != KnownSchema.GetVersion())
			throw new SchemaMissMatchException();

		this.Schema = KnownSchema;
		this.NrTotalPages = Math.round((parseInt(Element.attrs.totalpoints)+ 0.5)/DataPage.PointsPerPage);

		let resources = Element.getChildren("resource");

		for(let i = 0; i < resources.length; i++)
		{
			let it = resources[i];
			let Path = it.attrs.path;

			let rdesc = KnownSchema.Resources.get(Path);
			if(!rdesc)
				continue;

			switch (rdesc.SimpleType)
			{
				case SimplifiedType.String:
					this.Data.push(new ResourceString(it));
					break;
				case SimplifiedType.Integer:
					this.Data.push(new ResourceInteger(it));
					break;
				case SimplifiedType.Base64Binary:
					this.Data.push(new ResourceBase64Binary(it));
					break;
				case SimplifiedType.DateTime:
					this.Data.push(new ResourceDateTime(it));
					break;
				case SimplifiedType.Boolean:
					this.Data.push(new ResourceBoolean(it));
					break;
				case SimplifiedType.Decimal:
					this.Data.push(new ResourceDecimal(it));
					break;
				case SimplifiedType.Duration:
					// TODO this.Data.push(new ResourceDuration(it));
					break;
				case SimplifiedType.Float:
					this.Data.push(new ResourceFloat(it));
					break;
				case SimplifiedType.Time:
					// TODO this.Data.push(new ResourceTime(it));
					break;
			}
		}
	}
}


class Subscription
{
	public ID : string;
	public NewData : (ID : string, Data :  DataPage) => void;
	public SubscriptionCancelled : (ID : string) => void;
	public Resources : Array<string>;
	public KnownSchema : SimplifiedSchema | null;
	public AccessToken : string | null;
	public LastReSubscription : Date = new Date();
}


export class DataController
{
	private DataSourceAddress : string; // JID
	private Uplink : any; // xmp

	private IQResolvers: Map<string, (IQElement : any) => void> = new  Map<string, (IQElement : any) => void> ();
	private IQRejecters: Map<string, (IQElement : any) => void> = new  Map<string, (IQElement : any) => void> ();

	private Subscriptions : Map<string, Subscription> = new Map<string, Subscription>();

	private OnStanzaIQ(iq :any) : void
	{
		try
        {
			if(iq.attrs.from != this.DataSourceAddress)			
	        {
	            console.log("Warning; got iq from not DataSource? From = " + iq.attrs.from);
	            return;
	        }


	        if(this.IQResolvers.has(iq.attrs.id))
	        {
	            console.log("Running resolver on " + iq.attrs.id);
	            let resolver = this.IQResolvers.get(iq.attrs.id);
	            if(resolver)
	                resolver( iq );                
	            this.IQResolvers.delete(iq.attrs.id);
	            this.IQRejecters.delete(iq.attrs.id);
	            return;
	        }

	        console.log("Warning; got iq from unregistered request from " + iq.attrs.from + " with id  = " + iq.attrs.id);
	    }
	    catch(err)
	    {
	        console.log("Unhandled exception i DataController: " + err);
	    }
    
	}

	private OnStanzaMessage(Message : any) : void
	{
		try
        {
			if(Message.attrs.from != this.DataSourceAddress)			
	        {
	            console.log("Warning; got message from not DataSource? From = " + Message.attrs.from);
	            return;
	        }

			let LWTSDElement = Message.getChildren()[0];

			if (LWTSDElement.getNS() != LWTSD.Namespace)
				return;

			switch (LWTSDElement.name)
			{
				case "subscription-cancelled":
					this.HandleSubscriptionCancelled(LWTSDElement);
					break;
				case "subscription-triggered":
					this.HandleSubscriptionTriggered(LWTSDElement);
					break;
			}

	    }
	    catch(err)
	    {
	        console.log("Unhandled exception i DataController: " + err);
	    }	
	}

 	private OnStanza ( Message : any) : void
    {
    	if(Message.is('iq'))
    		this.OnStanzaIQ(Message);

    	if(Message.is('message'))
    		this.OnStanzaMessage(Message);
    }

    private async GetIQResponse(Request :any) 
    : Promise<any>
    {
    	let Resolves = this.IQResolvers;
		let Rejects = this.IQRejecters;
		let Uplink = this.Uplink;

		let TProm = new Promise<any>(
			function(resolve, reject)
			{
				Resolves.set(Request.attrs.id, resolve);
				Rejects.set(Request.attrs.id, reject);
				Uplink.send(Request);
			});

		return TProm;

    }

	public async  GetSchema(Page? :number, 
		AccessToken? :string |null, 
		Resources? : Array<string>) 
		: Promise<SimplifiedSchema>
	{
		if(!Page) 
			Page = 0;
		let MaxResources = 500;
		
		let Request = xml('iq');
		Request.attrs.id = NewGuid();
		Request.attrs.type = "get";
		Request.attrs.to = this.DataSourceAddress;
		let Payload = xml( "read-schema", { "xmlns": LWTSD.Namespace });
		Payload.attrs.format = SchemaFormatToString(SchemaFormat.Simplified);
		Payload.attrs.startindex =  (Page * MaxResources).toString();
		Payload.attrs.maxresources = MaxResources.toString();

		if (AccessToken)
		{
			let AToken = xml( "accesstoken", { /*"xmlns": LWTSD.Namespace*/ }, AccessToken); 
			AToken.attrs.name = "urn:clayster:cdo:sessionid";
			Payload.append(AToken);
		}

		if (Resources)
		{
			for (let res of Resources)
			{
				let Resource = xml( "resource", { /*"xmlns": LWTSD.Namespace*/ }); 
				Resource.attrs.path = res;
				Payload.append(Resource);
			}
		}

		Request.append(Payload);

		let Response = await this.GetIQResponse(Request);

		let ReturnedData = Response.getChild("simplified-schema");
		if(!ReturnedData)
			throw new Error("Failed to get schema");

		return new SimplifiedSchema(ReturnedData);
	}

	private async InnerReadData(KnownSchema : SimplifiedSchema | null,
		ResourcePaths : Array<string>,
		Page? :number,
		ResubscriptionIDs? : Array<string> | null,
		AccessToken? : string|null)
	: Promise<DataPage>
	{
		if(!Page)
			Page = 0;
		if(!ResubscriptionIDs)
			ResubscriptionIDs = null;
		if(!AccessToken)
			AccessToken = null;
		if (KnownSchema == null)
			KnownSchema = await this.GetSchema(0, AccessToken, ResourcePaths);
		

		let Request = xml('iq');
		Request.attrs.id = NewGuid();
		Request.attrs.type = "get";
		Request.attrs.to = this.DataSourceAddress;

		let Payload = xml( "read-data", { "xmlns": LWTSD.Namespace });
		Payload.attrs.maxpoints = DataPage.PointsPerPage.toString();
		Payload.attrs.startindex = (Page * DataPage.PointsPerPage).toString();
		Payload.attrs.relativetimeout = "10";

		if (AccessToken != null)
		{
			let AToken = xml( "accesstoken", { /*"xmlns": LWTSD.Namespace*/ }, AccessToken); 
			AToken.attrs.name = "urn:clayster:cdo:sessionid";
			Payload.append(AToken);
		}

		if (ResubscriptionIDs)
		{
			for (let sid of ResubscriptionIDs)
			{
				let el = xml( "re-subscribe", { /*"xmlns": LWTSD.Namespace*/ }, AccessToken);
				el.attrs.subscriptionid = sid;
				Payload.append(el);
			}
		}

		for (let Path of ResourcePaths)
		{
			let el = xml( "read", { /*"xmlns": LWTSD.Namespace*/ }, AccessToken);
			el.attrs["resource-path"] = Path;
			el.attrs.maxpoints = "1";
			Payload.append(el);
		}

		Request.append(Payload);
		let Response = await this.GetIQResponse(Request);
		let ReturnedData = Response.getChild("data");

		if (!ReturnedData == null)
			throw new Error("Failed to get data: invalid data: " + Response.toString());


		// This is wrong for more than one or two iterations, Need to re-read the data. Recurse?
		let RVal = new DataPage(ReturnedData, KnownSchema, Page);
		return RVal;
	}


	public async ReadData(KnownSchema : SimplifiedSchema|null,
		ResourcePaths : Array<string>,
		AccessToken? :string|null,
		Page ? :number)
	: Promise<DataPage>
	{
		if(!Page)
			Page = 0;
		if(!AccessToken)
			AccessToken = null;

		let NrSchemaMissMatchRetries = 10;
		let SchemaMissMatched = true;

		for (let i = 0; i < NrSchemaMissMatchRetries && SchemaMissMatched; i++)
		{
			SchemaMissMatched = false;
			try
			{
				return await this.InnerReadData(KnownSchema, ResourcePaths, Page, new Array<string>(), AccessToken);
			}
			catch (Err)
			{
				if(Err as SchemaMissMatchException)
					SchemaMissMatched = true;

				throw new Error("Unhandled error in DataController : ReadData: " + Err.toString());
			}
		}

		throw new Error("Failed to read data" + (SchemaMissMatched ? ": schema miss matched" : ""));
	}

	public async  SubscribeToData(KnownSchema : SimplifiedSchema,
		ResourcePaths : Array<string>,
		NewData : (SubscriptionID: string, Data: DataPage)=>void,
		SubscriptionCancelled : (SubscriptionID: string)=>void,
		AccessToken?:string |null)
	: Promise<[string, DataPage]>
	{
		if(!AccessToken)
			AccessToken = null;
		
		let SubscriptionID = NewGuid();		
		let Payload = xml( "subscribe", { "xmlns": LWTSD.Namespace });
		Payload.attrs.subscriptionid = SubscriptionID;


		for (let res of ResourcePaths)
		{
			let Trigger = xml("trigger", { /*"xmlns": LWTSD.Namespace*/ });
			Trigger.attrs.onresource = res;
			Payload.append(Trigger);
		}

		let Request = xml('iq');
		Request.attrs.id = NewGuid();
		Request.attrs.type = "get";
		Request.attrs.to = this.DataSourceAddress;

		let NewSubscription = new Subscription();
		NewSubscription.ID = SubscriptionID;
		NewSubscription.NewData = NewData;

		Request.append(Payload);

		let Response = await this.GetIQResponse(Request);

		
		let Page = await this.ReadData(KnownSchema, ResourcePaths, AccessToken);
		NewSubscription.KnownSchema = Page.Schema;
		NewSubscription.Resources = ResourcePaths;
		if(AccessToken == null)
			NewSubscription.AccessToken = null;
		else
			NewSubscription.AccessToken = AccessToken as string;
		NewSubscription.SubscriptionCancelled = SubscriptionCancelled;

		this.Subscriptions.set(NewSubscription.ID, NewSubscription);

		throw new Error("DataController: SubscribeToData not implemented"); // Todo: must add invocation..
		//return [SubscriptionID, Page];
	}

	public async WriteData( Page : DataPage, AccessToken? : string  )
	: Promise<boolean>
	{			

		let Payload = xml( "write-data", { "xmlns": LWTSD.Namespace });
		if (AccessToken)
		{
			let AToken = xml( "accesstoken", { /*"xmlns": LWTSD.Namespace*/ }, AccessToken); 
			AToken.attrs.name = "urn:clayster:cdo:sessionid";
			Payload.append(AToken);
		}


		for (let res of Page.Data)
		{
			let write = xml( "write", { /*"xmlns": LWTSD.Namespace*/ }, res.GetWriteValue() ); 
			write.attrs["resource-path"] = res.Path;
			write.attrs.relativetimeout = "10";
			Payload.append(write);
		}

		let Request = xml('iq');
		Request.attrs.id = NewGuid();
		Request.attrs.type = "get";
		Request.attrs.to = this.DataSourceAddress;		
		Request.append(Payload);

		let Response = await this.GetIQResponse(Request);
		if(Response.attr.type = 'result')
			return true;

		return false;
	}


	private async HandleSubscriptionTriggered(Element : any)
	: Promise<boolean>
	{
		let SubscriptionID = Element.attrs.subscriptionid;
		if(!this.Subscriptions.has(SubscriptionID))
			return false;
		let Sub = this.Subscriptions.get(SubscriptionID);
		if(!Sub)
			return false;

		let Page = await this.InnerReadData(Sub.KnownSchema,
			Sub.Resources,
			0,
			[SubscriptionID],
			Sub.AccessToken);

		Sub.NewData(SubscriptionID, Page);
		Sub.LastReSubscription = new Date();
		return true;
	}

	public CancelSubscription(SubscriptionID : string) : void
	{
		if (!this.Subscriptions.has(SubscriptionID))
			return;

		this.Subscriptions.delete(SubscriptionID);

		let Request = xml("message");
		Request.attrs.to = this.DataSourceAddress;

		let Payload = xml( "cancel-subscription", { "xmlns": LWTSD.Namespace });
		Payload.attrs.subscriptionid = SubscriptionID;
		Request.append(Payload);
		this.Uplink.send(Request);
	}
	
	private HandleSubscriptionCancelled(CancelledElement : any) : void
	{
		let SubscriptionID = CancelledElement.attrs.subscriptionid;
		
		if (!this.Subscriptions.has(SubscriptionID))
		{
			return;
		}

		let Sub = this.Subscriptions.get(SubscriptionID);
		if(!Sub)
			return;
		
		this.Subscriptions.delete(SubscriptionID);

		Sub.SubscriptionCancelled(SubscriptionID);
	}


	public constructor( Uplink : any, DataSourceAddress : any )
	{
		this.Uplink = Uplink;
		this.DataSourceAddress = DataSourceAddress;
		this.Uplink.on('stanza', this.OnStanza.bind(this));
	}

}

