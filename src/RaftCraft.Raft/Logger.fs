namespace RaftCraft.Logging

open RaftCraft.Interfaces

type Log() =
    static let mutable instance : ILogger = null

    static member Instance = instance

    static member SetInstance(log : ILogger) =
        instance <- log