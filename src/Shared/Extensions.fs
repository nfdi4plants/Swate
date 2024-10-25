[<AutoOpen>]
module Extensions

module Array =

    /// <summary>
    /// Take "count" many items from array if existing. if not enough items return as many as possible
    /// </summary>
    /// <param name="count"></param>
    /// <param name="array"></param>
    let takeSafe (count: int) (array: 'a []) =
       let count = System.Math.Min(count, array.Length)
       Array.take count array

module Option =
    /// <summary>
    /// If function returns `true` then return `Some x` otherwise `None`
    /// </summary>
    /// <param name="f"></param>
    /// <param name="x"></param>
    let where    f x = if f x then Some x else None
    /// <summary>
    /// If function return `true` then return `None` otherwise `Some x`
    /// </summary>
    /// <param name="f"></param>
    /// <param name="x"></param>
    let whereNot f x = if f x then None   else Some x
