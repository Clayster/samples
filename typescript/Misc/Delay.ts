export function Delay(milliseconds : number) : Promise<void>
{
    return new Promise<void>(function(resolve) {
        setTimeout(resolve, milliseconds);
    });
}