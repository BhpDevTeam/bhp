﻿using Bhp.Cryptography;
using Bhp.Ledger;
using Bhp.Network.P2P.Payloads;
using Bhp.Wallets;
using System.Collections.Generic;
using System.Linq;

namespace Bhp.Mining
{
    /// <summary>
    /// Mining transaction output (block output)
    /// </summary>
    public class MiningTransaction
    {   
        public MinerTransaction MakeMinerTransaction(Wallet wallet, uint blockIndex, ulong nonce, Fixed8 amount_servicefee,Fixed8 amount_netfee )
        {
            List<TransactionOutput> outputs = new List<TransactionOutput>();
            List<TransactionAttribute> attributes = new List<TransactionAttribute>();

            if (blockIndex > 0)
            {
                AddMiningTransaction(wallet, blockIndex, outputs, attributes);
                AddServiceFee(outputs, amount_servicefee, blockIndex);
                AddNetFee(wallet, outputs, amount_netfee);
            } 

            MinerTransaction tx = new MinerTransaction
            {
                Nonce = (uint)(nonce % (uint.MaxValue + 1ul)),
                Attributes = attributes.ToArray(),
                Inputs = new CoinReference[0],
                Outputs = outputs.ToArray(),
                Witnesses = new Witness[0]
            };
            return tx;
        }

        private void AddMiningTransaction(Wallet wallet, uint blockIndex, List<TransactionOutput> outputs, List<TransactionAttribute> attributes)
        {
            MiningOutput miningOutput = new MiningOutput
            {
                AssetId = Blockchain.GoverningToken.Hash,
                Value = MiningSubsidy.GetMiningSubsidy(blockIndex),                
                ScriptHash = MiningParams.PoSAddress.Length > 0 ? MiningParams.PoSAddress[blockIndex % (uint)MiningParams.PoSAddress.Length].ToScriptHash() : wallet.GetChangeAddress()
            };

            TransactionOutput output = new TransactionOutput
            {
                AssetId = miningOutput.AssetId,
                Value = miningOutput.Value,
                ScriptHash = miningOutput.ScriptHash
            }; 

            byte[] signatureOfMining = null; 

            //Signature 
            WalletAccount account = wallet.GetAccount(wallet.GetChangeAddress());
            if (account?.HasKey == true)
            {
                byte[] hashDataOfMining = miningOutput.GetHashData();
                KeyPair key = account.GetKey();
                signatureOfMining = Crypto.Default.Sign(hashDataOfMining, key.PrivateKey, key.PublicKey.EncodePoint(false).Skip(1).ToArray()); 
            }

            if (signatureOfMining != null)
            {
                attributes.Add(new TransactionAttribute
                {
                    Usage = TransactionAttributeUsage.MinerSignature,
                    Data = signatureOfMining
                });

                outputs.Add(output);
            }
        }

        private void AddServiceFee(List<TransactionOutput> outputs, Fixed8 amount_servicefee, uint blockIndex)
        { 
            if (amount_servicefee != Fixed8.Zero)
            {
                outputs.Add(TransactionFeeOutput(amount_servicefee, blockIndex));
            }
        }

        private void AddNetFee(Wallet wallet, List<TransactionOutput> outputs, Fixed8 amount_netfee)
        {
            if (amount_netfee != Fixed8.Zero)
            {
                outputs.Add(NetFeeOutput(wallet, amount_netfee));
            }
        }  

        /// <summary>
        /// Network Fee
        /// </summary>
        /// <param name="wallet"></param>
        /// <param name="amount_netfee"></param>
        /// <returns></returns>
        private TransactionOutput NetFeeOutput(Wallet wallet, Fixed8 amount_netfee)
        {
            return new TransactionOutput
            {
                AssetId = Blockchain.UtilityToken.Hash,
                Value = amount_netfee,
                ScriptHash = wallet.GetChangeAddress()
            };
        }
         
        private TransactionOutput TransactionFeeOutput(Fixed8 service_netfee, uint blockIndex)
        {
            return new TransactionOutput
            {
                AssetId = Blockchain.GoverningToken.Hash,
                Value = service_netfee,
                ScriptHash = MiningParams.PoSAddress[blockIndex % (uint)MiningParams.PoSAddress.Length].ToScriptHash()
            };
        }
    }
}
