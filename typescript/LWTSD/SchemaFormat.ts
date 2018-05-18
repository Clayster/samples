
export enum SchemaFormat
{
    Simplified,
    Extended
}

export function ParseSchemaFormat(data : string) : SchemaFormat
{
    switch(data)
    {
        case "extended":
            return SchemaFormat.Extended;
        case "simplified":
            return SchemaFormat.Simplified;

        default:
            throw new Error("Invalid string for schemaformat: " + data);    
    }
}

export function SchemaFormatToString(data : SchemaFormat): string
{
    switch(data)
    {
        case SchemaFormat.Extended:
            return "extended";
        case SchemaFormat.Simplified:
            return "simplified";

        default:
            throw new Error("SchemaFormat has invalid value: " + data);    
    }
}