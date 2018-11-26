module Operators

// Wraps a reference type as Option<T>. Needed for interop with classes passed in from C# since
// in C# things can be null. We want those to be Option.None in F# so that we can pattern match nicely.
let inline (!?) value =
    match value with
        | null -> None
        | _ -> Some value