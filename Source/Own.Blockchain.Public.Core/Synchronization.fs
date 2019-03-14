namespace Own.Blockchain.Public.Core

open System.Collections.Concurrent
open Own.Common.FSharp
open Own.Blockchain.Common
open Own.Blockchain.Public.Core.DomainTypes
open Own.Blockchain.Public.Core.Dtos
open Own.Blockchain.Public.Core.Events

module Synchronization =

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    // Network Time
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    let updateNetworkTimeOffset getNetworkTimeOffset =
        Utils.networkTimeOffset <- getNetworkTimeOffset ()
        Log.infof "Network time offset set to %i" Utils.networkTimeOffset

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    // Blockchain Head
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    let synchronizeBlockchainHead
        (getLastStoredBlockNumber : unit -> BlockNumber option)
        (getLastAppliedBlockNumber : unit -> BlockNumber)
        (getBlock : BlockNumber -> Result<BlockEnvelopeDto, AppErrors>)
        requestBlockchainHeadFromPeer
        blockchainHeadPollInterval
        =

        getLastStoredBlockNumber ()
        |?> getLastAppliedBlockNumber
        |> getBlock
        |> Result.map Blocks.extractBlockFromEnvelopeDto
        |> Result.handle
            (fun block ->
                let currentTimestamp = Utils.getNetworkTimestamp ()
                if currentTimestamp - block.Header.Timestamp.Value >= int64 blockchainHeadPollInterval then
                    requestBlockchainHeadFromPeer ()
            )
            Log.appErrors

    let handleReceivedBlockchainHead
        blockExists
        requestBlockFromPeer
        blockNumber
        =

        if not (blockExists blockNumber) then
            requestBlockFromPeer blockNumber

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    // Blockchain State
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    let unverifiedBlocks = new ConcurrentDictionary<BlockNumber, BlockEnvelopeDto>()

    let private requestBlock requestFromPeer publishEvent blockNumber =
        match unverifiedBlocks.TryRemove(blockNumber) with
        | true, blockEnvelopeDto -> (blockNumber, blockEnvelopeDto) |> BlockFetched |> publishEvent
        | _ -> requestFromPeer blockNumber

    let fetchMissingBlocks
        (getLastAppliedBlockNumber : unit -> BlockNumber)
        (getLastStoredBlockNumber : unit -> BlockNumber option)
        getStoredBlockNumbers
        getBlock
        blockExists
        txExists
        equivocationProofExists
        requestBlockFromPeer
        requestTxFromPeer
        requestEquivocationProofFromPeer
        publishEvent
        maxNumberOfBlocksToFetchInParallel
        =

        let lastAppliedBlockNumber = getLastAppliedBlockNumber ()
        let lastStoredBlockNumber = getLastStoredBlockNumber ()
        let lastVerifiedConfigBlock =
            lastStoredBlockNumber
            |? lastAppliedBlockNumber
            |> getBlock
            |> Result.map Blocks.extractBlockFromEnvelopeDto
            >>= (fun b ->
                if b.Configuration.IsSome then
                    Ok b
                else
                    getBlock b.Header.ConfigurationBlockNumber
                    |> Result.map Blocks.extractBlockFromEnvelopeDto
            )
            |> Result.handle id (fun _ -> failwith "Cannot get last verified configuration block.")

        let lastVerifiedConfiguration =
            lastVerifiedConfigBlock.Configuration
            |?> fun _ ->
                failwithf "Cannot find configuration in config block %i" lastVerifiedConfigBlock.Header.Number.Value

        let nextConfigBlockNumber =
            lastVerifiedConfigBlock.Header.Number + lastVerifiedConfiguration.ConfigurationBlockDelta

        unverifiedBlocks.Keys
        |> Seq.sortDescending
        |> Seq.tryHead
        |> Option.map (min nextConfigBlockNumber) // Because we cannot verify blocks after next missing config block.
        |> Option.orElse lastStoredBlockNumber
        |> Option.iter (fun lastVerifiableBlockNumber ->
            // Fetch next config block to build config chain in advance.
            if nextConfigBlockNumber <= lastVerifiableBlockNumber then
                requestBlock requestBlockFromPeer publishEvent nextConfigBlockNumber

            // Fetch verifiable blocks
            [lastAppliedBlockNumber + 1 .. lastVerifiableBlockNumber]
            |> Seq.except [nextConfigBlockNumber] // Config block is already requested above.
            |> Seq.filter (blockExists >> not)
            |> Seq.truncate maxNumberOfBlocksToFetchInParallel
            |> Seq.iter (requestBlock requestBlockFromPeer publishEvent)
        )

        // Fetch TXs and EquivocationProofs for verified blocks
        getStoredBlockNumbers ()
        |> List.sort
        |> List.iter (fun bn ->
            getBlock bn
            |> Result.map Blocks.extractBlockFromEnvelopeDto
            |> Result.handle
                (fun block ->
                    let missingTxs =
                        block.TxSet
                        |> List.filter (txExists >> not)

                    let missingEquivocationProofs =
                        block.EquivocationProofs
                        |> List.filter (equivocationProofExists >> not)

                    match missingTxs, missingEquivocationProofs with
                    | [], [] ->
                        if block.Header.Number = lastAppliedBlockNumber + 1 then
                            BlockReady block.Header.Number |> publishEvent
                    | _ ->
                        missingTxs |> List.iter requestTxFromPeer
                        missingEquivocationProofs |> List.iter requestEquivocationProofFromPeer
                )
                Log.appErrors
        )

    let tryApplyNextBlock
        (getLastAppliedBlockNumber : unit -> BlockNumber)
        getBlock
        applyBlock
        txExists
        equivocationProofExists
        removeOrphanTxResults
        removeOrphanEquivocationProofResults
        publishEvent
        =

        getLastAppliedBlockNumber () + 1
        |> getBlock
        |> Result.iter (fun blockEnvelopeDto ->
            result {
                let block = Blocks.extractBlockFromEnvelopeDto blockEnvelopeDto
                if block.TxSet |> List.forall txExists
                    && block.EquivocationProofs |> List.forall equivocationProofExists
                then
                    Log.noticef "Applying block %i" block.Header.Number.Value
                    do! applyBlock block.Header.Number
                    return (block.Header.Number |> BlockApplied |> Some)
                else
                    return None
            }
            |> Result.handle
                (Option.iter publishEvent)
                (fun errors ->
                    Log.appErrors errors
                    removeOrphanTxResults ()
                    removeOrphanEquivocationProofResults ()
                )
        )
