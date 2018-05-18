
import { ResourceAccess } from './ResourceAccess'
export class AccessTokenSession
{
    public Actor : string; // JID
    public AccessToken : string;
    public ExpiresAt : Date;
    public Rights: Array<ResourceAccess>;
}