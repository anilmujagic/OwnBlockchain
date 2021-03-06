namespace Own.Blockchain.Common

open System
open System.Collections.Concurrent
open Own.Common.FSharp

module Stats =

    type Counter =
        | PeerRequests
        | PeerRequestTimeouts
        | PeerRequestFailures
        | FailedMessageSendouts
        | RequestedBlocks
        | RequestedTxs
        | PeerResponses
        | ReceivedBlocks
        | ReceivedTxs
        | SentConsensusMessages
        | ReceivedConsensusMessages

    type Counter with
        member __.CaseName =
            match __ with
            | PeerRequests -> "PeerRequests"
            | PeerRequestTimeouts -> "PeerRequestTimeouts"
            | PeerRequestFailures -> "PeerRequestFailures"
            | FailedMessageSendouts -> "FailedMessageSendouts"
            | RequestedBlocks -> "RequestedBlocks"
            | RequestedTxs -> "RequestedTxs"
            | PeerResponses -> "PeerResponses"
            | ReceivedBlocks -> "ReceivedBlocks"
            | ReceivedTxs -> "ReceivedTxs"
            | SentConsensusMessages -> "SentConsensusMessages"
            | ReceivedConsensusMessages -> "ReceivedConsensusMessages"

    type StatsSummaryEntry = {
        Counter : string
        Value : int64
    }

    type StatsSummary = {
        NodeStartTime : string
        NodeUpTime : string
        NodeCurrentTime : string
        Counters : StatsSummaryEntry list
    }

    let private nodeStartTime = DateTime.UtcNow
    let private counters = new ConcurrentDictionary<Counter, int64>()

    let incrementBy value counter =
        counters.AddOrUpdate(counter, value, fun _ c -> c + value) |> ignore

    let increment = incrementBy 1L

    let decrementBy value counter =
        counters.AddOrUpdate(counter, -value, fun _ c -> c - value) |> ignore

    let decrement = decrementBy 1L

    let getCurrent () =
        let currentTime = DateTime.UtcNow

        {
            NodeStartTime = nodeStartTime.ToString("u")
            NodeUpTime = currentTime.Subtract(nodeStartTime).ToString("d\.hh\:mm\:ss")
            NodeCurrentTime = currentTime.ToString("u")
            Counters =
                counters
                |> List.ofDict
                |> List.map (fun (k, v) -> {Counter = k.CaseName; Value = v})
        }
