﻿namespace Chainium.Blockchain.Public.Core

open System
open Chainium.Common
open Chainium.Blockchain.Common
open Chainium.Blockchain.Public.Core.DomainTypes
open Chainium.Blockchain.Public.Core.Dtos

module Validation =

    let verifyTxSignature verifySignature (signedTx : SignedTx) : Result<ChainiumAddress * TxHash, AppErrors> =
        let addressBytes (RawChainiumAddress c) = c

        match verifySignature signedTx.Signature signedTx.RawTx with
        | Some rawaddress ->
            //TODO: implement proper encoding for address
            let address =
                rawaddress
                |> addressBytes
                |> System.Convert.ToBase64String
                |> ChainiumAddress

            Ok (address, TxHash "DUMMY_HASH") // TODO: Implement
        | None ->
            Error [AppError "Cannot verify signature"]

    let validateTx sender hash (txDto : TxDto) : Result<Tx, AppErrors> =
        // TODO: Implement
        Ok {
            TxHash = hash
            Sender = sender
            Nonce = txDto.Nonce
            Actions = []
            Fee = ChxAmount txDto.Fee
        }
