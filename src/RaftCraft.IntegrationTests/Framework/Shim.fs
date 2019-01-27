namespace RaftCraft.IntegrationTests.Framework

open System.Collections.Generic

type Shim<'a>() =
    let mutable (value : 'a option) = None

    member __.Create(valueFactory) =        
            match value with
            | Some v -> v
            | None ->
                let v = valueFactory()
                value <- Some v
                v

    member __.ForceGet() =
        match value with
            | Some v -> v
            | None -> failwith "Cannot retrieve value before it has been created"

type KeyedShim<'a, 'b when 'a : equality>() =
    let lookup = Dictionary<'a,'b>()
   
    member __.Create key valueFactory =
        match lookup.TryGetValue key with
            | true, v -> v
            | false, _ -> 
                let value = valueFactory(key)
                lookup.[key] <- value
                value

    member __.ForceGet(key) =
        match lookup.TryGetValue key with
            | true, v -> v
            | _ -> failwith("Key does not exist")
   
    member __.ForAll(func) = 
        lookup 
        |> Seq.map(fun v -> v.Value) 
        |> Seq.iter(func)
