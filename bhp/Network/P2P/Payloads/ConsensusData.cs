﻿using Bhp.Cryptography;
using Bhp.IO;
using Bhp.IO.Json;
using Bhp.Ledger;
using System.IO;

namespace Bhp.Network.P2P.Payloads
{
    public class ConsensusData : ISerializable
    {
        public uint PrimaryIndex;
        public ulong Nonce;

        private UInt256 _hash = null;
        public UInt256 Hash
        {
            get
            {
                if (_hash == null)
                {
                    _hash = new UInt256(Crypto.Default.Hash256(this.ToArray()));
                }
                return _hash;
            }
        }

        public int Size => IO.Helper.GetVarSize((int)PrimaryIndex) + sizeof(ulong);

        void ISerializable.Deserialize(BinaryReader reader)
        {
            PrimaryIndex = (uint)reader.ReadVarInt(Blockchain.MaxValidators - 1);
            Nonce = reader.ReadUInt64();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.WriteVarInt(PrimaryIndex);
            writer.Write(Nonce);
        }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["primary"] = PrimaryIndex;
            json["nonce"] = Nonce.ToString("x16");
            return json;
        }
    }
}