﻿using Bhp.IO;
using Bhp.IO.Json;
using Bhp.SmartContract;
using System;
using System.IO;

namespace Bhp.Network.P2P.Payloads
{
    public class Witness : ISerializable
    {
        public byte[] InvocationScript;
        public byte[] VerificationScript;

        private UInt160 _scriptHash;
        public virtual UInt160 ScriptHash
        {
            get
            {
                if (_scriptHash == null)
                {
                    _scriptHash = VerificationScript.ToScriptHash();
                }
                return _scriptHash;
            }
        }

        public int Size => InvocationScript.GetVarSize() + VerificationScript.GetVarSize();

        void ISerializable.Deserialize(BinaryReader reader)
        {
            InvocationScript = reader.ReadVarBytes(664);
            VerificationScript = reader.ReadVarBytes(360);
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.WriteVarBytes(InvocationScript);
            writer.WriteVarBytes(VerificationScript);
        }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["invocation"] = Convert.ToBase64String(InvocationScript);
            json["verification"] = Convert.ToBase64String(VerificationScript);
            return json;
        }

        public static Witness FromJson(JObject json)
        {
            Witness witness = new Witness();
            witness.InvocationScript = Convert.FromBase64String(json["invocation"].AsString());
            witness.VerificationScript = Convert.FromBase64String(json["verification"].AsString());
            return witness;
        }
    }
}
