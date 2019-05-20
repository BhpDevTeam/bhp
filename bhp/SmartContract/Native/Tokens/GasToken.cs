﻿using System;
using System.Linq;
using System.Numerics;
using Bhp.Cryptography.ECC;
using Bhp.Ledger;
using Bhp.Network.P2P.Payloads;
using Bhp.VM;
using VMArray = Bhp.VM.Types.Array;

namespace Bhp.SmartContract.Native.Tokens
{
    public sealed class GasToken : Brc6Token<Brc6AccountState>
    {
        public override string ServiceName => "Bhp.Native.Tokens.GAS";
        public override string Name => "GAS";
        public override string Symbol => "gas";
        public override int Decimals => 8;

        private const byte Prefix_SystemFeeAmount = 15;

        internal GasToken()
        {
        }

        protected override StackItem Main(ApplicationEngine engine, string operation, VMArray args)
        {
            if (operation == "getSysFeeAmount")
                return GetSysFeeAmount(engine, (uint)args[0].GetBigInteger());
            else
                return base.Main(engine, operation, args);
        }

        protected override bool OnPersist(ApplicationEngine engine)
        {
            if (!base.OnPersist(engine)) return false;
            foreach (Transaction tx in engine.Snapshot.PersistingBlock.Transactions)
                Burn(engine, tx.Sender, tx.Gas + tx.NetworkFee);
            ECPoint[] validators = Bhp.GetNextBlockValidators(engine.Snapshot);
            UInt160 primary = Contract.CreateSignatureRedeemScript(validators[engine.Snapshot.PersistingBlock.ConsensusData.PrimaryIndex]).ToScriptHash();
            Mint(engine, primary, engine.Snapshot.PersistingBlock.Transactions.Sum(p => p.NetworkFee));
            BigInteger sys_fee = GetSysFeeAmount(engine, engine.Snapshot.PersistingBlock.Index - 1) + engine.Snapshot.PersistingBlock.Transactions.Sum(p => p.Gas);
            StorageKey key = CreateStorageKey(Prefix_SystemFeeAmount, BitConverter.GetBytes(engine.Snapshot.PersistingBlock.Index));
            engine.Snapshot.Storages.Add(key, new StorageItem
            {
                Value = sys_fee.ToByteArray(),
                IsConstant = true
            });
            return true;
        }

        internal BigInteger GetSysFeeAmount(ApplicationEngine engine, uint index)
        {
            if (index == 0) return Blockchain.GenesisBlock.Transactions.Sum(p => p.Gas);
            StorageKey key = CreateStorageKey(Prefix_SystemFeeAmount, BitConverter.GetBytes(index));
            StorageItem storage = engine.Snapshot.Storages.TryGet(key);
            if (storage is null) return BigInteger.Zero;
            return new BigInteger(storage.Value);
        }
    }
}
