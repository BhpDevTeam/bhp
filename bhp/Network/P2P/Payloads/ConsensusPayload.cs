﻿using Bhp.Consensus;
using Bhp.Cryptography;
using Bhp.Cryptography.ECC;
using Bhp.IO;
using Bhp.Persistence;
using Bhp.SmartContract;
using Bhp.SmartContract.Native;
using System;
using System.IO;

namespace Bhp.Network.P2P.Payloads
{
    public class ConsensusPayload : IInventory
    {
        public uint Version;
        public UInt256 PrevHash;
        public uint BlockIndex;
        public ushort ValidatorIndex;
        public byte[] Data;
        public Witness Witness { get; set; }

        private ConsensusMessage _deserializedMessage = null;
        public ConsensusMessage ConsensusMessage
        {
            get
            {
                if (_deserializedMessage is null)
                    _deserializedMessage = ConsensusMessage.DeserializeFrom(Data);
                return _deserializedMessage;
            }
            internal set
            {
                if (!ReferenceEquals(_deserializedMessage, value))
                {
                    _deserializedMessage = value;
                    Data = value?.ToArray();
                }
            }
        }

        private UInt256 _hash = null;
        public UInt256 Hash
        {
            get
            {
                if (_hash == null)
                {
                    _hash = new UInt256(Crypto.Default.Hash256(this.GetHashData()));
                }
                return _hash;
            }
        }

        InventoryType IInventory.InventoryType => InventoryType.Consensus;

        public int Size =>
            sizeof(uint) +      //Version
            PrevHash.Size +     //PrevHash
            sizeof(uint) +      //BlockIndex
            sizeof(ushort) +    //ValidatorIndex
            sizeof(uint) +      //Timestamp
            Data.GetVarSize() + //Data
            Witness.Size;       //Witness

        public T GetDeserializedMessage<T>() where T : ConsensusMessage
        {
            return (T)ConsensusMessage;
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            ((IVerifiable)this).DeserializeUnsigned(reader);            
            Witness = reader.ReadSerializable<Witness>();
        }

        void IVerifiable.DeserializeUnsigned(BinaryReader reader)
        {
            Version = reader.ReadUInt32();
            PrevHash = reader.ReadSerializable<UInt256>();
            BlockIndex = reader.ReadUInt32();
            ValidatorIndex = reader.ReadUInt16();
            Data = reader.ReadVarBytes();
        }

        UInt160 IVerifiable.GetScriptHashForVerification(Snapshot snapshot)
        {
            ECPoint[] validators = NativeContract.Bhp.GetNextBlockValidators(snapshot);
            if (validators.Length <= ValidatorIndex)
                throw new InvalidOperationException();
            return Contract.CreateSignatureRedeemScript(validators[ValidatorIndex]).ToScriptHash();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            ((IVerifiable)this).SerializeUnsigned(writer);
            writer.Write(Witness);
        }

        void IVerifiable.SerializeUnsigned(BinaryWriter writer)
        {
            writer.Write(Version);
            writer.Write(PrevHash);
            writer.Write(BlockIndex);
            writer.Write(ValidatorIndex);
            writer.WriteVarBytes(Data);
        }

        public bool Verify(Snapshot snapshot)
        {
            if (BlockIndex <= snapshot.Height)
                return false;
            return this.VerifyWitness(snapshot, 0_02000000);
        }
    }
}