namespace Own.Blockchain.Public.Core.Tests

open System
open Newtonsoft.Json
open Own.Common
open Own.Blockchain.Common
open Own.Blockchain.Public.Core.DomainTypes
open Own.Blockchain.Public.Core.Dtos
open Own.Blockchain.Public.Crypto

module Helpers =

    let networkCode = "UNIT_TESTS"

    let getNetworkId () =
        Hashing.networkId networkCode

    let verifySignature = Signing.verifySignature getNetworkId

    let randomString () = Guid.NewGuid().ToString("N")

    let maxActionCountPerTx = 1000

    let minTxActionFee = ChxAmount 0.001m

    let extractActionData<'T> = function
        | TransferChx action -> box action :?> 'T
        | TransferAsset action -> box action :?> 'T
        | CreateAssetEmission action -> box action :?> 'T
        | CreateAccount -> failwith "CreateAccount TxAction has no data to extract."
        | CreateAsset -> failwith "CreateAsset TxAction has no data to extract."
        | SetAccountController action -> box action :?> 'T
        | SetAssetController action -> box action :?> 'T
        | SetAssetCode action -> box action :?> 'T
        | ConfigureValidator action -> box action :?> 'T
        | RemoveValidator -> failwith "RemoveValidator TxAction has no data to extract."
        | DelegateStake action -> box action :?> 'T
        | SubmitVote action -> box action :?> 'T
        | SubmitVoteWeight action -> box action :?> 'T
        | SetAccountEligibility action -> box action :?> 'T
        | SetAssetEligibility action -> box action :?> 'T
        | ChangeKycControllerAddress action -> box action :?> 'T
        | AddKycProvider action -> box action :?> 'T
        | RemoveKycProvider action -> box action :?> 'T

    let newPendingTxInfo
        (txHash : TxHash)
        (senderAddress : BlockchainAddress)
        (nonce : Nonce)
        (actionFee : ChxAmount)
        (actionCount : int16)
        (appearanceOrder : int64)
        =

        {
            PendingTxInfo.TxHash = txHash
            Sender = senderAddress
            Nonce = nonce
            ActionFee = actionFee
            ActionCount = actionCount
            AppearanceOrder = appearanceOrder
        }

    let newRawTxDto
        (BlockchainAddress senderAddress)
        (nonce : int64)
        (actionFee : decimal)
        (actions : obj list)
        =

        let json =
            sprintf
                """
                {
                    SenderAddress: "%s",
                    Nonce: %i,
                    ActionFee: %s,
                    Actions: %s
                }
                """
                senderAddress
                nonce
                (actionFee.ToString())
                (JsonConvert.SerializeObject(actions))

        Conversion.stringToBytes json

    let newTx
        (sender : WalletInfo)
        (Nonce nonce)
        (ChxAmount actionFee)
        (actions : obj list)
        =

        let rawTx = newRawTxDto sender.Address nonce actionFee actions

        let txHash =
            rawTx |> Hashing.hash |> TxHash

        let (Signature signature) = Signing.signHash getNetworkId sender.PrivateKey txHash.Value

        let txEnvelopeDto =
            {
                Tx = rawTx |> Convert.ToBase64String
                Signature = signature
            }

        (txHash, txEnvelopeDto)

    let verifyMerkleProofs (MerkleTreeRoot merkleRoot) leafs =
        let leafs = leafs |> List.map Hashing.decode

        // Performance is not priority in unit tests, so avoid exposing hashBytes out of Crypto assembly.
        let hashBytes = Hashing.hash >> Hashing.decode

        [
            for leaf in leafs ->
                MerkleTree.calculateProof hashBytes leafs leaf
                |> MerkleTree.verifyProof hashBytes (Hashing.decode merkleRoot) leaf
        ]
