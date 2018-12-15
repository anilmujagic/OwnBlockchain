﻿namespace Own.Blockchain.Public.Net

open System.Collections.Concurrent
open Own.Common

module Timers =

    let createTimer tInterval callback =
        let timer = new System.Timers.Timer(float tInterval)
        timer.AutoReset <- false
        timer.Enabled <- true
        timer.Elapsed |> Observable.subscribe callback |> ignore
        timer

    let getTimer timers timerId =
        let timer =
            timers
            |> Map.ofDict
            |> Map.filter (fun key _ -> key = timerId)
            |> Seq.toList

        match timer with
            | [t] -> Some t.Value
            | _ -> None

    let restartTimer<'T when 'T : comparison>
        (timers : ConcurrentDictionary<'T, System.Timers.Timer>)
        timerId
        tInterval
        callback
        =

        match getTimer timers timerId with
        | Some t ->
            t.Stop()
            t.Dispose()
        | None -> ()

        let timer = createTimer tInterval callback
        timer.Start()
        timers.AddOrUpdate (timerId, timer, fun _ _ -> timer) |> ignore
