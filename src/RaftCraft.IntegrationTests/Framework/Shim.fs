namespace RaftCraft.IntegrationTests.Framework

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
