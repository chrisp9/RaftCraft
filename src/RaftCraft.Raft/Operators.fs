module RaftCraft.Operators

let inline (!?) value =
    match value with
        | null -> None
        | _ -> Some value 
       