namespace Chainium.Blockchain.Common

open System

module Utils =

    let getUnixTimestamp () =
        System.DateTimeOffset.UtcNow.ToUnixTimeSeconds()
