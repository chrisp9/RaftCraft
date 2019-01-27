namespace RaftCraft.IntegrationTests.Common

type Poller() =
    static let rec until func retries =
        if(func()) then true
        elif retries <= 0 then false
        else
            System.Threading.Thread.Sleep(100)
            until func (retries - 1)

    static member Until(func) = until func