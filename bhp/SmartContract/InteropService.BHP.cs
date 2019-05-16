﻿using Bhp.Cryptography;
using Bhp.Ledger;
using Bhp.Network.P2P;
using Bhp.Network.P2P.Payloads;
using Bhp.SmartContract.Enumerators;
using Bhp.SmartContract.Iterators;
using Bhp.SmartContract.Native;
using Bhp.VM;
using Bhp.VM.Types;
using System;
using System.IO;
using System.Linq;
using VMArray = Bhp.VM.Types.Array;

namespace Bhp.SmartContract
{
    static partial class InteropService
    {
        public static readonly uint Bhp_Native_Deploy = Register("Bhp.Native.Deploy", Native_Deploy, 0);
        public static readonly uint Bhp_Crypto_CheckSig = Register("Bhp.Crypto.CheckSig", Crypto_CheckSig, 100);
        public static readonly uint Bhp_Crypto_CheckMultiSig = Register("Bhp.Crypto.CheckMultiSig", Crypto_CheckMultiSig);
        public static readonly uint Bhp_Header_GetVersion = Register("Bhp.Header.GetVersion", Header_GetVersion, 1);
        public static readonly uint Bhp_Header_GetMerkleRoot = Register("Bhp.Header.GetMerkleRoot", Header_GetMerkleRoot, 1);        
        public static readonly uint Bhp_Header_GetNextConsensus = Register("Bhp.Header.GetNextConsensus", Header_GetNextConsensus, 1);
        public static readonly uint Bhp_Transaction_GetWitnesses = Register("Bhp.Transaction.GetWitnesses", Transaction_GetWitnesses, 200);
        public static readonly uint Bhp_Transaction_GetScript = Register("Bhp.Transaction.GetScript", Transaction_GetScript, 1);
        public static readonly uint Bhp_Witness_GetVerificationScript = Register("Bhp.Witness.GetVerificationScript", Witness_GetVerificationScript, 100);
        public static readonly uint Bhp_Account_IsStandard = Register("Bhp.Account.IsStandard", Account_IsStandard, 100);
        public static readonly uint Bhp_Contract_Create = Register("Bhp.Contract.Create", Contract_Create);
        public static readonly uint Bhp_Contract_Migrate = Register("Bhp.Contract.Migrate", Contract_Migrate);
        public static readonly uint Bhp_Contract_GetScript = Register("Bhp.Contract.GetScript", Contract_GetScript, 1);
        public static readonly uint Bhp_Contract_IsPayable = Register("Bhp.Contract.IsPayable", Contract_IsPayable, 1);
        public static readonly uint Bhp_Storage_Find = Register("Bhp.Storage.Find", Storage_Find, 1);
        public static readonly uint Bhp_Enumerator_Create = Register("Bhp.Enumerator.Create", Enumerator_Create, 1);
        public static readonly uint Bhp_Enumerator_Next = Register("Bhp.Enumerator.Next", Enumerator_Next, 1);
        public static readonly uint Bhp_Enumerator_Value = Register("Bhp.Enumerator.Value", Enumerator_Value, 1);
        public static readonly uint Bhp_Enumerator_Concat = Register("Bhp.Enumerator.Concat", Enumerator_Concat, 1);
        public static readonly uint Bhp_Iterator_Create = Register("Bhp.Iterator.Create", Iterator_Create, 1);
        public static readonly uint Bhp_Iterator_Key = Register("Bhp.Iterator.Key", Iterator_Key, 1);
        public static readonly uint Bhp_Iterator_Keys = Register("Bhp.Iterator.Keys", Iterator_Keys, 1);
        public static readonly uint Bhp_Iterator_Values = Register("Bhp.Iterator.Values", Iterator_Values, 1);
        public static readonly uint Bhp_Iterator_Concat = Register("Bhp.Iterator.Concat", Iterator_Concat, 1);

        static InteropService()
        {
            foreach (NativeContract contract in NativeContract.Contracts)
                Register(contract.ServiceName, contract.Invoke);
        }

        private static bool Native_Deploy(ApplicationEngine engine)
        {
            if (engine.Trigger != TriggerType.Application) return false;
            if (engine.Snapshot.PersistingBlock.Index != 0) return false;
            foreach (NativeContract contract in NativeContract.Contracts)
            {
                engine.Snapshot.Contracts.Add(contract.ScriptHash, new ContractState
                {
                    Script = contract.Script,
                    ContractProperties = contract.Properties
                });
                contract.Initialize(engine);
            }
            return true;
        }

        private static bool Crypto_CheckSig(ApplicationEngine engine)
        {
            byte[] pubkey = engine.CurrentContext.EvaluationStack.Pop().GetByteArray();
            byte[] signature = engine.CurrentContext.EvaluationStack.Pop().GetByteArray();

            try
            {
                engine.CurrentContext.EvaluationStack.Push(Crypto.Default.VerifySignature(engine.ScriptContainer.GetHashData(), signature, pubkey));
            }
            catch (ArgumentException)
            {
                engine.CurrentContext.EvaluationStack.Push(false);
            }
            return true;
        }

        private static bool Crypto_CheckMultiSig(ApplicationEngine engine)
        {
            int n;
            byte[][] pubkeys;
            StackItem item = engine.CurrentContext.EvaluationStack.Pop();

            if (item is VMArray array1)
            {
                pubkeys = array1.Select(p => p.GetByteArray()).ToArray();
                n = pubkeys.Length;
                if (n == 0) return false;
            }
            else
            {
                n = (int)item.GetBigInteger();
                if (n < 1 || n > engine.CurrentContext.EvaluationStack.Count) return false;
                pubkeys = new byte[n][];
                for (int i = 0; i < n; i++)
                    pubkeys[i] = engine.CurrentContext.EvaluationStack.Pop().GetByteArray();
            }

            int m;
            byte[][] signatures;
            item = engine.CurrentContext.EvaluationStack.Pop();
            if (item is VMArray array2)
            {
                signatures = array2.Select(p => p.GetByteArray()).ToArray();
                m = signatures.Length;
                if (m == 0 || m > n) return false;
            }
            else
            {
                m = (int)item.GetBigInteger();
                if (m < 1 || m > n || m > engine.CurrentContext.EvaluationStack.Count) return false;
                signatures = new byte[m][];
                for (int i = 0; i < m; i++)
                    signatures[i] = engine.CurrentContext.EvaluationStack.Pop().GetByteArray();
            }
            byte[] message = engine.ScriptContainer.GetHashData();
            bool fSuccess = true;
            try
            {
                for (int i = 0, j = 0; fSuccess && i < m && j < n;)
                {
                    if (Crypto.Default.VerifySignature(message, signatures[i], pubkeys[j]))
                        i++;
                    j++;
                    if (m - i > n - j)
                        fSuccess = false;
                }
            }
            catch (ArgumentException)
            {
                fSuccess = false;
            }
            engine.CurrentContext.EvaluationStack.Push(fSuccess);
            return true;
        }

        private static bool Header_GetVersion(ApplicationEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                BlockBase header = _interface.GetInterface<BlockBase>();
                if (header == null) return false;
                engine.CurrentContext.EvaluationStack.Push(header.Version);
                return true;
            }
            return false;
        }

        private static bool Header_GetMerkleRoot(ApplicationEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                BlockBase header = _interface.GetInterface<BlockBase>();
                if (header == null) return false;
                engine.CurrentContext.EvaluationStack.Push(header.MerkleRoot.ToArray());
                return true;
            }
            return false;
        }
        
        private static bool Header_GetNextConsensus(ApplicationEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                BlockBase header = _interface.GetInterface<BlockBase>();
                if (header == null) return false;
                engine.CurrentContext.EvaluationStack.Push(header.NextConsensus.ToArray());
                return true;
            }
            return false;
        }

        private static bool Transaction_GetWitnesses(ApplicationEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                Transaction tx = _interface.GetInterface<Transaction>();
                if (tx == null) return false;
                if (tx.Witnesses.Length > engine.MaxArraySize)
                    return false;
                engine.CurrentContext.EvaluationStack.Push(WitnessWrapper.Create(tx, engine.Snapshot).Select(p => StackItem.FromInterface(p)).ToArray());
                return true;
            }
            return false;
        }

        private static bool Transaction_GetScript(ApplicationEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                Transaction tx = _interface.GetInterface<Transaction>();
                if (tx == null) return false;
                engine.CurrentContext.EvaluationStack.Push(tx.GetHashData());
                return true;
            }
            return false;
        }

        private static bool Witness_GetVerificationScript(ApplicationEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                WitnessWrapper witness = _interface.GetInterface<WitnessWrapper>();
                if (witness == null) return false;
                engine.CurrentContext.EvaluationStack.Push(witness.VerificationScript);
                return true;
            }
            return false;
        }

        private static bool Account_IsStandard(ApplicationEngine engine)
        {
            UInt160 hash = new UInt160(engine.CurrentContext.EvaluationStack.Pop().GetByteArray());
            ContractState contract = engine.Snapshot.Contracts.TryGet(hash);
            bool isStandard = contract is null || contract.Script.IsStandardContract();
            engine.CurrentContext.EvaluationStack.Push(isStandard);
            return true;
        }

        private static bool Contract_Create(ApplicationEngine engine)
        {
            if (engine.Trigger != TriggerType.Application) return false;
            byte[] script = engine.CurrentContext.EvaluationStack.Pop().GetByteArray();
            if (script.Length > 1024 * 1024) return false;
            ContractPropertyState contract_properties = (ContractPropertyState)(byte)engine.CurrentContext.EvaluationStack.Pop().GetBigInteger();
            UInt160 hash = script.ToScriptHash();
            ContractState contract = engine.Snapshot.Contracts.TryGet(hash);
            if (contract == null)
            {
                contract = new ContractState
                {
                    Script = script,
                    ContractProperties = contract_properties
                };
                engine.Snapshot.Contracts.Add(hash, contract);
            }
            engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(contract));
            return true;
        }

        private static bool Contract_Migrate(ApplicationEngine engine)
        {
            if (engine.Trigger != TriggerType.Application) return false;
            byte[] script = engine.CurrentContext.EvaluationStack.Pop().GetByteArray();
            if (script.Length > 1024 * 1024) return false;
            ContractPropertyState contract_properties = (ContractPropertyState)(byte)engine.CurrentContext.EvaluationStack.Pop().GetBigInteger();
            UInt160 hash = script.ToScriptHash();
            ContractState contract = engine.Snapshot.Contracts.TryGet(hash);
            if (contract == null)
            {
                contract = new ContractState
                {
                    Script = script,
                    ContractProperties = contract_properties
                };
                engine.Snapshot.Contracts.Add(hash, contract);
                if (contract.HasStorage)
                {
                    foreach (var pair in engine.Snapshot.Storages.Find(engine.CurrentScriptHash.ToArray()).ToArray())
                    {
                        engine.Snapshot.Storages.Add(new StorageKey
                        {
                            ScriptHash = hash,
                            Key = pair.Key.Key
                        }, new StorageItem
                        {
                            Value = pair.Value.Value,
                            IsConstant = false
                        });
                    }
                }
            }
            engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(contract));
            return Contract_Destroy(engine);
        }

        private static bool Contract_GetScript(ApplicationEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                ContractState contract = _interface.GetInterface<ContractState>();
                if (contract == null) return false;
                engine.CurrentContext.EvaluationStack.Push(contract.Script);
                return true;
            }
            return false;
        }

        private static bool Contract_IsPayable(ApplicationEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                ContractState contract = _interface.GetInterface<ContractState>();
                if (contract == null) return false;
                engine.CurrentContext.EvaluationStack.Push(contract.Payable);
                return true;
            }
            return false;
        }

        private static bool Storage_Find(ApplicationEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                StorageContext context = _interface.GetInterface<StorageContext>();
                if (!CheckStorageContext(engine, context)) return false;
                byte[] prefix = engine.CurrentContext.EvaluationStack.Pop().GetByteArray();
                byte[] prefix_key;
                using (MemoryStream ms = new MemoryStream())
                {
                    int index = 0;
                    int remain = prefix.Length;
                    while (remain >= 16)
                    {
                        ms.Write(prefix, index, 16);
                        ms.WriteByte(0);
                        index += 16;
                        remain -= 16;
                    }
                    if (remain > 0)
                        ms.Write(prefix, index, remain);
                    prefix_key = context.ScriptHash.ToArray().Concat(ms.ToArray()).ToArray();
                }
                StorageIterator iterator = engine.AddDisposable(new StorageIterator(engine.Snapshot.Storages.Find(prefix_key).Where(p => p.Key.Key.Take(prefix.Length).SequenceEqual(prefix)).GetEnumerator()));
                engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(iterator));
                return true;
            }
            return false;
        }

        private static bool Enumerator_Create(ApplicationEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is VMArray array)
            {
                IEnumerator enumerator = new ArrayWrapper(array);
                engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(enumerator));
                return true;
            }
            return false;
        }

        private static bool Enumerator_Next(ApplicationEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                IEnumerator enumerator = _interface.GetInterface<IEnumerator>();
                engine.CurrentContext.EvaluationStack.Push(enumerator.Next());
                return true;
            }
            return false;
        }

        private static bool Enumerator_Value(ApplicationEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                IEnumerator enumerator = _interface.GetInterface<IEnumerator>();
                engine.CurrentContext.EvaluationStack.Push(enumerator.Value());
                return true;
            }
            return false;
        }

        private static bool Enumerator_Concat(ApplicationEngine engine)
        {
            if (!(engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface1)) return false;
            if (!(engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface2)) return false;
            IEnumerator first = _interface1.GetInterface<IEnumerator>();
            IEnumerator second = _interface2.GetInterface<IEnumerator>();
            IEnumerator result = new ConcatenatedEnumerator(first, second);
            engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(result));
            return true;
        }

        private static bool Iterator_Create(ApplicationEngine engine)
        {
            IIterator iterator;
            switch (engine.CurrentContext.EvaluationStack.Pop())
            {
                case VMArray array:
                    iterator = new ArrayWrapper(array);
                    break;
                case Map map:
                    iterator = new MapWrapper(map);
                    break;
                default:
                    return false;
            }
            engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(iterator));
            return true;
        }

        private static bool Iterator_Key(ApplicationEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                IIterator iterator = _interface.GetInterface<IIterator>();
                engine.CurrentContext.EvaluationStack.Push(iterator.Key());
                return true;
            }
            return false;
        }

        private static bool Iterator_Keys(ApplicationEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                IIterator iterator = _interface.GetInterface<IIterator>();
                engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(new IteratorKeysWrapper(iterator)));
                return true;
            }
            return false;
        }

        private static bool Iterator_Values(ApplicationEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                IIterator iterator = _interface.GetInterface<IIterator>();
                engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(new IteratorValuesWrapper(iterator)));
                return true;
            }
            return false;
        }

        private static bool Iterator_Concat(ApplicationEngine engine)
        {
            if (!(engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface1)) return false;
            if (!(engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface2)) return false;
            IIterator first = _interface1.GetInterface<IIterator>();
            IIterator second = _interface2.GetInterface<IIterator>();
            IIterator result = new ConcatenatedIterator(first, second);
            engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(result));
            return true;
        }
    }
}