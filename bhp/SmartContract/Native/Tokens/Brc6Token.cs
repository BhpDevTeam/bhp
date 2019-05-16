﻿using Bhp.Ledger;
using Bhp.VM;
using System;
using System.Numerics;
using VMArray = Bhp.VM.Types.Array;

namespace Bhp.SmartContract.Native.Tokens
{
    public abstract class Brc6Token<TState> : NativeContract
        where TState : Brc6AccountState, new()
    {
        public override ContractPropertyState Properties => ContractPropertyState.HasStorage;
        public override string[] SupportedStandards { get; } = { "BRC-5", "BRC-10" };
        public abstract string Name { get; }
        public abstract string Symbol { get; }
        public abstract int Decimals { get; }
        public BigInteger Factor { get; }

        protected const byte Prefix_TotalSupply = 11;
        protected const byte Prefix_Account = 20;

        protected Brc6Token()
        {
            this.Factor = BigInteger.Pow(10, Decimals);
        }

        protected StorageKey CreateAccountKey(UInt160 account)
        {
            return CreateStorageKey(Prefix_Account, account);
        }

        protected override StackItem Main(ApplicationEngine engine, string operation, VMArray args)
        {
            switch (operation)
            {
                case "name":
                    return Name;
                case "symbol":
                    return Symbol;
                case "decimals":
                    return Decimals;
                case "totalSupply":
                    return TotalSupply(engine);
                case "balanceOf":
                    return BalanceOf(engine, new UInt160(args[0].GetByteArray()));
                case "transfer":
                    return Transfer(engine, new UInt160(args[0].GetByteArray()), new UInt160(args[1].GetByteArray()), args[2].GetBigInteger());
                default:
                    return base.Main(engine, operation, args);
            }
        }

        internal protected virtual void MintTokens(ApplicationEngine engine, UInt160 account, BigInteger amount)
        {
            if (amount.Sign < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (amount.IsZero) return;
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(CreateAccountKey(account), () => new StorageItem
            {
                Value = new TState().ToByteArray()
            });
            TState state = new TState();
            state.FromByteArray(storage.Value);
            OnBalanceChanging(engine, account, state, amount);
            state.Balance += amount;
            storage.Value = state.ToByteArray();
            storage = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_TotalSupply), () => new StorageItem
            {
                Value = BigInteger.Zero.ToByteArray()
            });
            BigInteger totalSupply = new BigInteger(storage.Value);
            totalSupply += amount;
            storage.Value = totalSupply.ToByteArray();
            engine.SendNotification(ScriptHash, new StackItem[] { "Transfer", StackItem.Null, account.ToArray(), amount });
        }

        protected virtual BigInteger TotalSupply(ApplicationEngine engine)
        {
            StorageItem storage = engine.Snapshot.Storages.TryGet(CreateStorageKey(Prefix_TotalSupply));
            if (storage is null) return BigInteger.Zero;
            return new BigInteger(storage.Value);
        }

        protected virtual BigInteger BalanceOf(ApplicationEngine engine, UInt160 account)
        {
            StorageItem storage = engine.Snapshot.Storages.TryGet(CreateAccountKey(account));
            if (storage is null) return BigInteger.Zero;
            Brc6AccountState state = new Brc6AccountState(storage.Value);
            return state.Balance;
        }

        protected virtual bool Transfer(ApplicationEngine engine, UInt160 from, UInt160 to, BigInteger amount)
        {
            if (engine.Trigger != TriggerType.Application) throw new InvalidOperationException();
            if (amount.Sign < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (!from.Equals(engine.CallingScriptHash) && !InteropService.CheckWitness(engine, from))
                return false;
            ContractState contract_to = engine.Snapshot.Contracts.TryGet(to);
            if (contract_to?.Payable == false) return false;
            StorageKey key_from = CreateAccountKey(from);
            StorageItem storage_from = engine.Snapshot.Storages.TryGet(key_from);
            if (amount.IsZero)
            {
                if (storage_from != null)
                {
                    TState state_from = new TState();
                    state_from.FromByteArray(storage_from.Value);
                    OnBalanceChanging(engine, from, state_from, amount);
                }
            }
            else
            {
                if (storage_from is null) return false;
                TState state_from = new TState();
                state_from.FromByteArray(storage_from.Value);
                if (state_from.Balance < amount) return false;
                if (from.Equals(to))
                {
                    OnBalanceChanging(engine, from, state_from, BigInteger.Zero);
                }
                else
                {
                    OnBalanceChanging(engine, from, state_from, -amount);
                    if (state_from.Balance == amount)
                    {
                        engine.Snapshot.Storages.Delete(key_from);
                    }
                    else
                    {
                        state_from.Balance -= amount;
                        storage_from = engine.Snapshot.Storages.GetAndChange(key_from);
                        storage_from.Value = state_from.ToByteArray();
                    }
                    StorageKey key_to = CreateAccountKey(to);
                    StorageItem storage_to = engine.Snapshot.Storages.GetAndChange(key_to, () => new StorageItem
                    {
                        Value = new TState().ToByteArray()
                    });
                    TState state_to = new TState();
                    state_to.FromByteArray(storage_to.Value);
                    OnBalanceChanging(engine, to, state_to, amount);
                    state_to.Balance += amount;
                    storage_to.Value = state_to.ToByteArray();
                }
            }
            engine.SendNotification(ScriptHash, new StackItem[] { "Transfer", from.ToArray(), to.ToArray(), amount });
            return true;
        }

        protected virtual void OnBalanceChanging(ApplicationEngine engine, UInt160 account, TState state, BigInteger amount)
        {
        }
    }
}
