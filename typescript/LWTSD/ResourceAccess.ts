
export class ResourceAccess
{
    public Path : string;
    public Subordinates : boolean = false;
    public SupportsRead : boolean = false;
    public SupportsWrite : boolean = false;

    public AllowsRead( Path : string) : boolean
    {
        if (!this.SupportsRead)
            return false;

        if (!this.Subordinates)
            return this.Path == Path;

        return this.Path.indexOf(Path) == 0; // TODO CHECK / IN THE END
    }

    public AllowsWrite(Path : string) : boolean
    {
        if (!this.SupportsWrite)
            return false;

        if (!this.Subordinates)
            return this.Path == Path;
        
        return this.Path.indexOf(Path) == 0; // TODO CHECK / IN THE END
    }

    public static AllowsRead(Access : Array<ResourceAccess>, Path : string) : boolean
    {
        if (Access == null)
            return true;
        
        for (let ra of Access)
        {
            if (ra.AllowsRead(Path))
                return true;
        }
        return false;
    }

    public static AllowsWrite(Access : Array<ResourceAccess>, Path : string) : boolean
    {
        if (Access == null)
            return true;
        
        for (let ra of Access)
        {
            if (ra.AllowsWrite(Path))
                return true;
        }
        return false;
    }
}
