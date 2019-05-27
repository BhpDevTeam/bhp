﻿using Bhp.Cryptography.ECC;
using Bhp.IO.Json;
using Bhp.Ledger;
using Bhp.Network.P2P.Payloads;
using Bhp.Persistence;
using Bhp.VM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Bhp.SmartContract
{
    public class ContractParametersContext
    {
        public readonly IVerifiable Verifiable;
        private byte[] Script;
        private ContractParameter[] Parameters;
        private Dictionary<ECPoint, byte[]> Signatures;

        public bool Completed
        {
            get
            {
                if (Parameters is null) return false;
                return Parameters.All(p => p.Value != null);
            }
        }

        private UInt160 _ScriptHash = null;
        public UInt160 ScriptHash
        {
            get
            {
                if (_ScriptHash == null)
                    using (Snapshot snapshot = Blockchain.Singleton.GetSnapshot())
                    {
                        _ScriptHash = Verifiable.GetScriptHashForVerification(snapshot);
                    }
                return _ScriptHash;
            }
        }

        public ContractParametersContext(IVerifiable verifiable)
        {
            this.Verifiable = verifiable;
        }

        public bool Add(Contract contract, int index, object parameter)
        {
            if (!ScriptHash.Equals(contract.ScriptHash)) return false;
            if (Parameters is null)
            {
                Script = contract.Script;
                Parameters = contract.ParameterList.Select(p => new ContractParameter { Type = p }).ToArray();
            }
            Parameters[index].Value = parameter;
            return true;
        }

        public bool AddSignature(Contract contract, ECPoint pubkey, byte[] signature)
        {
            if (contract.Script.IsMultiSigContract())
            {
                if (!ScriptHash.Equals(contract.ScriptHash)) return false;
                if (Parameters is null)
                {
                    Script = contract.Script;
                    Parameters = contract.ParameterList.Select(p => new ContractParameter { Type = p }).ToArray();
                }
                if (Parameters.All(p => p.Value != null)) return false;
                if (Signatures == null)
                    Signatures = new Dictionary<ECPoint, byte[]>();
                else if (Signatures.ContainsKey(pubkey))
                    return false;
                List<ECPoint> points = new List<ECPoint>();
                {
                    int i = 0;
                    switch (contract.Script[i++])
                    {
                        case 1:
                            ++i;
                            break;
                        case 2:
                            i += 2;
                            break;
                    }
                    while (contract.Script[i++] == 33)
                    {
                        points.Add(ECPoint.DecodePoint(contract.Script.Skip(i).Take(33).ToArray(), ECCurve.Secp256));
                        i += 33;
                    }
                }
                if (!points.Contains(pubkey)) return false;
                Signatures.Add(pubkey, signature);
                if (Signatures.Count == contract.ParameterList.Length)
                {
                    Dictionary<ECPoint, int> dic = points.Select((p, i) => new
                    {
                        PublicKey = p,
                        Index = i
                    }).ToDictionary(p => p.PublicKey, p => p.Index);
                    byte[][] sigs = Signatures.Select(p => new
                    {
                        Signature = p.Value,
                        Index = dic[p.Key]
                    }).OrderByDescending(p => p.Index).Select(p => p.Signature).ToArray();
                    for (int i = 0; i < sigs.Length; i++)
                        if (!Add(contract, i, sigs[i]))
                            throw new InvalidOperationException();
                    Signatures = null;
                }
                return true;
            }
            else
            {
                int index = -1;
                for (int i = 0; i < contract.ParameterList.Length; i++)
                    if (contract.ParameterList[i] == ContractParameterType.Signature)
                        if (index >= 0)
                            throw new NotSupportedException();
                        else
                            index = i;

                if (index == -1)
                {
                    // unable to find ContractParameterType.Signature in contract.ParameterList 
                    // return now to prevent array index out of bounds exception
                    return false;
                }
                return Add(contract, index, signature);
            }
        }
        
        public static ContractParametersContext FromJson(JObject json)
        {
            IVerifiable verifiable = typeof(ContractParametersContext).GetTypeInfo().Assembly.CreateInstance(json["type"].AsString()) as IVerifiable;
            if (verifiable == null) throw new FormatException();
            using (MemoryStream ms = new MemoryStream(json["hex"].AsString().HexToBytes(), false))
            using (BinaryReader reader = new BinaryReader(ms, Encoding.UTF8))
            {
                verifiable.DeserializeUnsigned(reader);
            }
            return new ContractParametersContext(verifiable)
            {
                Script = json["script"]?.AsString().HexToBytes(),
                Parameters = ((JArray)json["parameters"])?.Select(p => ContractParameter.FromJson(p)).ToArray(),
                Signatures = json["signatures"]?.Properties.Select(p => new
                {
                    PublicKey = ECPoint.Parse(p.Key, ECCurve.Secp256r1),
                    Signature = p.Value.AsString().HexToBytes()
                }).ToDictionary(p => p.PublicKey, p => p.Signature)
            };
        }

        public ContractParameter GetParameter(int index)
        {
            return GetParameters()?[index];
        }

        public IReadOnlyList<ContractParameter> GetParameters()
        {            
            return Parameters;
        }

        public Witness GetWitness()
        {
            if (!Completed) throw new InvalidOperationException();
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                foreach (ContractParameter parameter in Parameters.Reverse())
                {
                    sb.EmitPush(parameter);
                }
                return new Witness
                {
                    InvocationScript = sb.ToArray(),
                    VerificationScript = Script ?? new byte[0]
                };
            }           
        }

        public static ContractParametersContext Parse(string value)
        {
            return FromJson(JObject.Parse(value));
        }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["type"] = Verifiable.GetType().FullName;
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms, Encoding.UTF8))
            {
                Verifiable.SerializeUnsigned(writer);
                writer.Flush();
                json["hex"] = ms.ToArray().ToHexString();
            }
            if (Script != null)
                json["script"] = Script.ToHexString();
            if (Parameters != null)
                json["parameters"] = new JArray(Parameters.Select(p => p.ToJson()));
            if (Signatures != null)
            {
                json["signatures"] = new JObject();
                foreach (var signature in Signatures)
                    json["signatures"][signature.Key.ToString()] = signature.Value.ToHexString();
            }
            return json;
        }

        public override string ToString()
        {
            return ToJson().ToString();
        }
    }
}
