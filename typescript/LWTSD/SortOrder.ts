

export enum SortOrder
{
    None,
    Ascending,
    Descending
}


export function ParseSortOrder(data : string) : SortOrder
{
    switch(data)
    {
        case "ascending":
            return SortOrder.Ascending;
        case "descending":
            return SortOrder.Descending;
        case "":
            return SortOrder.None;
        default:
            throw new Error("Invalid string for schemaformat: " + data);    
    }
}

export function SortOrderToString(data : SortOrder): string | null
{
    switch(data)
    {
        case SortOrder.Ascending:
            return "ascending";
        case SortOrder.Descending:
            return "descending";
        case SortOrder.None:
            return null;

        default:
            throw new Error("SortOrder has invalid value: " + data);    
    }
}