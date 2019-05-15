﻿using Bhp.IO;
using Bhp.Ledger;
using Bhp.SmartContract.Native.Tokens;
using Bhp.VM;
using System;
using System.Collections.Generic;
using System.Linq;
using VMArray = Bhp.VM.Types.Array;

namespace Bhp.SmartContract.Native
{
    public abstract class NativeContract
    {
        private static readonly List<NativeContract> contracts = new List<NativeContract>();

        public static IReadOnlyCollection<NativeContract> Contracts { get; } = contracts;
        public static BhpToken Bhp { get; } = new BhpToken();
        public static GasToken GAS { get; } = new GasToken();

        public abstract string ServiceName { get; }
        public uint ServiceHash { get; }
        public byte[] Script { get; }
        public UInt160 ScriptHash { get; }
        public virtual ContractPropertyState Properties => ContractPropertyState.NoProperty;
        public virtual string[] SupportedStandards { get; } = { "BRC-10" };

        protected NativeContract()
        {
            this.ServiceHash = ServiceName.ToInteropMethodHash();
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitSysCall(ServiceHash);
                this.Script = sb.ToArray();
            }
            this.ScriptHash = Script.ToScriptHash();
            contracts.Add(this);
        }

        protected StorageKey CreateStorageKey(byte prefix, byte[] key = null)
        {
            StorageKey storageKey = new StorageKey
            {
                ScriptHash = ScriptHash,
                Key = new byte[sizeof(byte) + (key?.Length ?? 0)]
            };
            storageKey.Key[0] = prefix;
            if (key != null)
                Buffer.BlockCopy(key, 0, storageKey.Key, 1, key.Length);
            return storageKey;
        }

        protected StorageKey CreateStorageKey(byte prefix, ISerializable key)
        {
            return CreateStorageKey(prefix, key.ToArray());
        }

        internal bool Invoke(ApplicationEngine engine)
        {
            if (!engine.CurrentScriptHash.Equals(ScriptHash))
                return false;
            string operation = engine.CurrentContext.EvaluationStack.Pop().GetString();
            VMArray args = (VMArray)engine.CurrentContext.EvaluationStack.Pop();
            StackItem result = Main(engine, operation, args);
            engine.CurrentContext.EvaluationStack.Push(result);
            return true;
        }

        protected virtual StackItem Main(ApplicationEngine engine, string operation, VMArray args)
        {
            if (operation == "supportedStandards")
                return SupportedStandards.Select(p => (StackItem)p).ToList();
            throw new NotSupportedException();
        }

        internal virtual bool Initialize(ApplicationEngine engine)
        {
            if (engine.Trigger != TriggerType.Application)
                throw new InvalidOperationException();
            return true;
        }
    }
}