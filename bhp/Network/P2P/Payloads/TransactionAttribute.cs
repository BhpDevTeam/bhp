﻿using Bhp.IO;
using Bhp.IO.Json;
using Bhp.VM;
using System;
using System.IO;
using System.Linq;

namespace Bhp.Network.P2P.Payloads
{
    public class TransactionAttribute : ISerializable
    {
        public TransactionAttributeUsage Usage;
        public byte[] Data;

        public int Size => sizeof(TransactionAttributeUsage) + Data.GetVarSize();

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Usage = (TransactionAttributeUsage)reader.ReadByte();
            if (Usage == TransactionAttributeUsage.ContractHash || Usage == TransactionAttributeUsage.Vote || (Usage >= TransactionAttributeUsage.Hash1 && Usage <= TransactionAttributeUsage.Hash15))
                Data = reader.ReadBytes(32);
            else if (Usage == TransactionAttributeUsage.ECDH02 || Usage == TransactionAttributeUsage.ECDH03)
                Data = new[] { (byte)Usage }.Concat(reader.ReadBytes(32)).ToArray();            
            else if (Usage == TransactionAttributeUsage.DescriptionUrl)
                Data = reader.ReadBytes(reader.ReadByte());
            else if (Usage == TransactionAttributeUsage.Description || Usage >= TransactionAttributeUsage.Remark)
                Data = reader.ReadVarBytes(ushort.MaxValue);
            //By BHP
            else if (Usage >= TransactionAttributeUsage.MinerSignature && Usage <= TransactionAttributeUsage.SmartContractScript)
                Data = reader.ReadVarBytes(ushort.MaxValue);
            else
                throw new FormatException();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write((byte)Usage);           
            if (Usage == TransactionAttributeUsage.DescriptionUrl)
                writer.Write((byte)Data.Length);
            else if (Usage == TransactionAttributeUsage.Description || Usage >= TransactionAttributeUsage.Remark)
                writer.WriteVarInt(Data.Length);
            //By BHP
            else if (Usage >= TransactionAttributeUsage.MinerSignature && Usage <= TransactionAttributeUsage.SmartContractScript)
                writer.WriteVarInt(Data.Length);
            if (Usage == TransactionAttributeUsage.ECDH02 || Usage == TransactionAttributeUsage.ECDH03)
                writer.Write(Data, 1, 32);
            else
                writer.Write(Data);
        }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["usage"] = Usage;
            json["data"] = Data.ToHexString();
            return json;
        }

        public static TransactionAttribute FromJson(JObject json)
        {
            TransactionAttribute transactionAttribute = new TransactionAttribute();
            transactionAttribute.Usage = (TransactionAttributeUsage)(byte.Parse(json["usage"].AsString()));
            transactionAttribute.Data = json["data"].AsString().HexToBytes();
            return transactionAttribute;
        }
    }
}
