#!/usr/bin/env node
console.log("starting");
var xmpp = require('./node-xmpp/packages/client').xmpp;
var xml = require('./node-xmpp/packages/xml');
import * as CDO from './CDO/index';
import * as LWTSD from './LWTSD/index';
import * as CDOLWTSD from './CDOLWTSD/index';

var fs = require('fs');

var credentials = { 
    key: fs.readFileSync('testcontroller.clayster.private.pem'),
    cert: fs.readFileSync('testcontroller.clayster.cert.pem'),
}; 

var options = { jid: "testcontroller.clayster@sandbox.clayster.com/jox" , 'credentials': credentials };

console.log("main", "creating uplink");

var uplink = xmpp().client;

// Useful for logging raw traffic
console.log("main", "registering loggers for xmpp");
uplink.on('input', (data:string)  => console.log('->', data));
uplink.on('output', (data:string)  => console.log('<-', data));

let CDOControl = new CDO.CDOController(uplink, "cdo.sandbox.clayster.com", SessionTerminated );
let targetentity = "757df3d81c8645a69bc22a8f2576bd30";
let DataController = new CDOLWTSD.CDODataController( uplink, "cdo.sandbox.clayster.com", targetentity);
/*let AR = new CDO.ActionRequester(uplink, "cdo.sandbox.clayster.com" );

async function SendSomeLowlevelReqest()
{
    try
    {
        console.log("Entering send some request");
        var setclaimkey = xml("entitysetclaimkey");
        CDO.CDO.AddString(setclaimkey, "claimkey", "random");
        console.log("awaiting reponse from actionrequester");
        let jox = await AR.Request("setclaimkey", setclaimkey, 10); // time out back in time so that we get error
        console.log("got response! successfull: " + jox.Successful);
    }
    catch(err)
    {
        console.log("error in SendSomeRequest: " + err);
    }
}*/

async function RegisterDevice()
{
    CDOControl.SetClaimKey("a random key which we can claim");
}


async function StartSession()
{
    try
    {
/*
        let targetentity = "5fdf45690a424c0eadfd724e76ca72de";
        let Resources = new Array<CDO.ResourceAccess>();
        let Resource = new CDO.ResourceAccess();
        Resource.ResourcePath = "/MeteringTopology/Test/BooleanNode"; 
*/
        
        //let targetentity = "757df3d81c8645a69bc22a8f2576bd30";
        let Resources = new Array<CDO.ResourceAccess>();
        let Resource = new CDO.ResourceAccess();
        Resource.ResourcePath = "meters/availablememory";
        //let Session = { "ID": null};
        //let BareJID = "testsource.clayster@sandbox.clayster.com"; ///jox
        
        Resource.Verbs.push( CDO.DataVerb.GET);
        DataController.RequestedResources.set(Resource.ResourcePath, Resource);
        console.log("Requesting data");
        let Data = await DataController.ReadData();
        console.log(Data);
        

    }
    catch(err)
    {
        console.log("error in SendSomeRequest: " + err);
    }
}

async function StartSessionLowlevel()
{
    try
    {
/*
        let targetentity = "5fdf45690a424c0eadfd724e76ca72de";
        let Resources = new Array<CDO.ResourceAccess>();
        let Resource = new CDO.ResourceAccess();
        Resource.ResourcePath = "/MeteringTopology/Test/BooleanNode"; 
*/
        
        //let targetentity = "757df3d81c8645a69bc22a8f2576bd30";
        let Resources = new Array<CDO.ResourceAccess>();
        let Resource = new CDO.ResourceAccess();
        Resource.ResourcePath = "meters/availablememory";

        //let Session = { "ID": null};
        //let BareJID = "testsource.clayster@sandbox.clayster.com"; ///jox
        
        Resource.Verbs.push( CDO.DataVerb.GET);
        Resources.push(Resource);
        
        console.log("requesting session");        
        let Session = await CDOControl.RequestSession(targetentity, Resources, 60*60 ); // one hour long
        console.log("Request successfull.");
        console.log(Session);

        console.log("Starting session");
        let BareJID = await CDOControl.StartSession(Session.ID);
        console.log("Session started! Address to data source: " + BareJID);


        console.log("Creating LWTSD controller")
        let LWTSDControl = new LWTSD.DataController(uplink, BareJID + "/jox");
        console.log("Requesting schema")
        let Schema = await LWTSDControl.GetSchema(0, Session.ID);
        console.log("Schema received!")
        console.log(Schema);
        
        console.log("Attempting to read data");
        let Data = await LWTSDControl.ReadData(Schema, [Resource.ResourcePath], Session.ID);
        console.log("Got data");
        console.log(Data);
        

    }
    catch(err)
    {
        console.log("error in SendSomeRequest: " + err);
    }
}

uplink.on('online', (jid:string) => {    
    console.log('jid', jid)
    uplink.send(xml('presence'))
    RegisterDevice();
    //StartSession();
})

uplink.on('stanza', function(stanza:any) {
    console.log("Got stanza in main")
});

function SessionTerminated(Session: CDO.Session) : void
{
    console.log("Session terminated:" + Session.ID);
}

uplink.on('error', function(e:any) {
    console.error(e);
});


uplink.handle('authenticate', (authenticate:any) => {
    return authenticate('testcontroller@sandbox.clayster.com', 'nopassword')
  })

uplink.start(
        {
            domain: "sandbox.clayster.com", 
            port:5222, 
            uri:"xmpp://sandbox.clayster.com:5222",
            cert:fs.readFileSync('testcontroller.clayster.cert.pem'), 
            key:fs.readFileSync('testcontroller.clayster.private.pem')
        });
