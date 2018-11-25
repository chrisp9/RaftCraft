module public Dict

open System.Collections.Generic

let public tryRemove f (dictionary : Dictionary<_, _>) =
    let valueToRemove = dictionary |> Seq.tryFind(f)

    match valueToRemove with
        | Some kvp -> 
            dictionary.Remove(kvp.Key) |> ignore
            Some kvp
        | None -> None