var xmpp = require('../node-xmpp/packages/client').xmpp;
var xml = require('../node-xmpp/packages/xml');
var Guid = require('../Misc/Guid');
import {CDO} from './CDO';
import {DataVerb} from './DataVerb';

export class ResourceAccess
{
    public ResourcePath : string;
    public Subordinates : boolean = false;
    public Verbs : Array<DataVerb> = new Array<DataVerb>();
    // TODO add time windows
    //public WindowFrom? : Date = undefined;
    //public WindowTo? : Date = undefined;

    
    public AddToElement(Element:any) : void
    {
        let MyEl  =xml( "resourceaccess", {/*"xmlns" : CDO.Namespace*/});
        CDO.AddResourcePath(MyEl, "path", this.ResourcePath);
        CDO.AddBoolean(MyEl, "subordinates", this.Subordinates);

        //CDO.SetTimeframe(MyEl, "window", WindowFrom, WindowTo);

        let VerbsEl = CDO.StartList(MyEl, "verbs");
        for (let it of this.Verbs)
        {
            CDO.SetDataVerb(VerbsEl, it);
        }
        Element.append(MyEl);
    }


    public constructor(Element?:any)
    {
        if(Element)
        { 
            this.ResourcePath = CDO.GetResourcePath(Element, "path");
            this.Subordinates = CDO.GetBoolean(Element, "subordinates");
            // TODO: Tuple<DateTime?, DateTime?> Window = CDO.GetTimeframe(Element, "window");
            /*if (Window != null)
            {
                WindowFrom = Window.Item1;
                WindowTo = Window.Item2;
            }*/

            let VerbsEl = CDO.GetList(Element, "verbs");
            if(VerbsEl == null)
                return;

            var VerbsElIt = VerbsEl.getChildren("dataverb");
            for (var i = 0; i < VerbsElIt.length; i++) 
            {
                let verbel = VerbsElIt[i];
                this.Verbs.push(CDO.GetDataVerb(verbel));
            
            }
        }                       
    }
}
