export enum SimplifiedType
{
    String,
    Boolean,
    Integer,
    Decimal,
    Double,
    Float,
    Duration,
    Time,
    DateTime,
    Base64Binary
}

export function ParseSimplifiedType(data : string) : SimplifiedType
{
    switch(data)
    {
        case "string":
            return SimplifiedType.String;
        case "integer":
            return SimplifiedType.Integer;
        case "boolean":
            return SimplifiedType.Boolean;
        case "decimal":
            return SimplifiedType.Decimal;
        case "double":
            return SimplifiedType.Double;
        case "float":
            return SimplifiedType.Float;
        case "duration":
            return SimplifiedType.Duration;
        case "time":
            return SimplifiedType.Time;
        case "dateTime":
            return SimplifiedType.DateTime;
        case "base64Binary":
            return SimplifiedType.Base64Binary;
        default:
            throw new Error("Invalid string for simplified type: " + data);    
    }
}

export function SimplifiedTypeToString(data : SimplifiedType): string
{
    switch(data)
    {
        case SimplifiedType.String:
            return "string";
        case SimplifiedType.Integer:
            return "integer";
        case SimplifiedType.Boolean:
            return "boolean";
        case SimplifiedType.Decimal:
            return "decimal";
        case SimplifiedType.Double:
            return "double";
        case SimplifiedType.Float:
            return "float";
        case SimplifiedType.Duration:
            return "duration";
        case SimplifiedType.Time:
            return "time";
        case SimplifiedType.DateTime:
            return "dateTime";
        case SimplifiedType.Base64Binary:
            return "base64Binary";

        default:
            throw new Error("Simplified type has invalid value: " + data);    
    }
}