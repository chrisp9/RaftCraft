namespace RaftCraft.IntegrationTests.Common

type Poller() =
    static let rec until func retries =
        if(func()) then ()
        elif retries <= 0 then failwith "Poll condition failed"
        else
            System.Threading.Thread.Sleep(100)
            until func (retries - 1)

    static let rec untilMatch func retries =
        match func() with
            | Some v -> v
            | None when retries <= 0 -> failwith "Poll condition failed"
            | None -> 
                System.Threading.Thread.Sleep(100)
                untilMatch func (retries - 1)
    
    static member Until(func) = until func 10

    static member UntilMatch(func) = untilMatch func 10
