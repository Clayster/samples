
export enum DataVerb
{
    GET,
    SET,
    ADD,
    DELETE,
    RENAME
}

export function ParseDataVerb(data : string) : DataVerb
{
    switch(data)
    {
        case "GET":
            return DataVerb.GET;
        case "SET":
            return DataVerb.SET;
        case "ADD":
            return DataVerb.ADD;
        case "DELETE":
            return DataVerb.DELETE;
        case "RENAME":
            return DataVerb.RENAME;
    }

    throw new Error("Verb unknown: " + data);
}

export function DataVerbToString(data : DataVerb): string
{
    switch(data)
    {
        case DataVerb.GET:
            return "GET";
        case DataVerb.SET:
            return "SET";
        case DataVerb.ADD:
            return "ADD";
        case DataVerb.DELETE:
            return "DELETE";
        case DataVerb.RENAME:
            return "RENAME";
    }
    throw new Error("Verb unknown: " + data);
}