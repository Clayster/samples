export function GetHashCode(data: string): number 
{
  var hash = 0, i, chr;
  if (data.length === 0) return hash;

  for (i = 0; i < data.length; i++) 
  {
    chr   = data.charCodeAt(i);
    hash  = ((hash << 5) - hash) + chr;
    hash |= 0; // Convert to 32bit integer
  }
  return hash;
};