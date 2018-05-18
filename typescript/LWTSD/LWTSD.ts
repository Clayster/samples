var xmpp = require('../node-xmpp/packages/client').xmpp;
var xml = require('../node-xmpp/packages/xml');

export class LWTSD
{
    public static Namespace:string = "urn:clayster:lwtsd";

    public static GetAccessToken(Element:any) : string|null
    {
        var Tokens = Element.getChildren("accesstoken");
        if (Tokens.length > 1)
            throw new Error("This implementation does not support multiple accesstokens");
        if (Tokens.length == 0)
            return null;

        return Tokens[0].text();
    }

}