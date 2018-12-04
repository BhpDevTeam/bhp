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

        public int Size
        {
            get
            {
                if (Usage == TransactionAttributeUsage.ContractHash || Usage == TransactionAttributeUsage.ECDH02 || Usage == TransactionAttributeUsage.ECDH03 || Usage == TransactionAttributeUsage.Vote || (Usage >= TransactionAttributeUsage.Hash1 && Usage <= TransactionAttributeUsage.Hash15))
                    return sizeof(TransactionAttributeUsage) + 32;
                else if (Usage == TransactionAttributeUsage.Script)
                    return sizeof(TransactionAttributeUsage) + 20;
                else if (Usage == TransactionAttributeUsage.DescriptionUrl)
                    return sizeof(TransactionAttributeUsage) + sizeof(byte) + Data.Length;
                else
                    return sizeof(TransactionAttributeUsage) + Data.GetVarSize();
            }
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Usage = (TransactionAttributeUsage)reader.ReadByte();
            if (Usage == TransactionAttributeUsage.ContractHash || Usage == TransactionAttributeUsage.Vote || (Usage >= TransactionAttributeUsage.Hash1 && Usage <= TransactionAttributeUsage.Hash15))
                Data = reader.ReadBytes(32);
            else if (Usage == TransactionAttributeUsage.ECDH02 || Usage == TransactionAttributeUsage.ECDH03)
                Data = new[] { (byte)Usage }.Concat(reader.ReadBytes(32)).ToArray();
            else if (Usage == TransactionAttributeUsage.Script)
                Data = reader.ReadBytes(20);
            else if (Usage == TransactionAttributeUsage.DescriptionUrl)
                Data = reader.ReadBytes(reader.ReadByte());
            else if (Usage == TransactionAttributeUsage.Description || Usage >= TransactionAttributeUsage.Remark)
                Data = reader.ReadVarBytes(ushort.MaxValue);
            else if (Usage >= TransactionAttributeUsage.MinerSignature && Usage <= TransactionAttributeUsage.SmartContractScript) //By BHP
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
            else if(Usage >= TransactionAttributeUsage.MinerSignature && Usage <= TransactionAttributeUsage.SmartContractScript)//By BHP
                writer.WriteVarInt(Data.Length);
            if (Usage == TransactionAttributeUsage.ECDH02 || Usage == TransactionAttributeUsage.ECDH03)
                writer.Write(Data, 1, 32);
            else
                writer.Write(Data);
        }

        //void ISerializable.Deserialize(BinaryReader reader)
        //{
        //    Usage = (TransactionAttributeUsage)reader.ReadByte();
        //    if (Usage == TransactionAttributeUsage.ContractHash || Usage == TransactionAttributeUsage.Vote || (Usage >= TransactionAttributeUsage.Hash1 && Usage <= TransactionAttributeUsage.Hash15))
        //        Data = reader.ReadBytes(32);
        //    else if (Usage == TransactionAttributeUsage.ECDH02 || Usage == TransactionAttributeUsage.ECDH03)
        //        Data = new[] { (byte)Usage }.Concat(reader.ReadBytes(32)).ToArray();
        //    else if (Usage == TransactionAttributeUsage.Script)
        //        Data = reader.ReadBytes(20);
        //    else if (Usage == TransactionAttributeUsage.DescriptionUrl)
        //        Data = reader.ReadBytes(reader.ReadByte());
        //    else if (Usage == TransactionAttributeUsage.Description || Usage >= TransactionAttributeUsage.Remark)
        //        Data = reader.ReadVarBytes(ushort.MaxValue);
        //    else if (Usage == TransactionAttributeUsage.MinerSignature) //By BHP
        //        Data = reader.ReadVarBytes(ushort.MaxValue);
        //    else
        //        throw new FormatException();
        //}

        //void ISerializable.Serialize(BinaryWriter writer)
        //{
        //    writer.Write((byte)Usage);
        //    if (Usage == TransactionAttributeUsage.DescriptionUrl)
        //        writer.Write((byte)Data.Length);
        //    else if (Usage == TransactionAttributeUsage.Description || Usage >= TransactionAttributeUsage.Remark)
        //        writer.WriteVarInt(Data.Length);
        //    else if (Usage == TransactionAttributeUsage.MinerSignature)//By BHP
        //        writer.WriteVarInt(Data.Length);
        //    if (Usage == TransactionAttributeUsage.ECDH02 || Usage == TransactionAttributeUsage.ECDH03)
        //        writer.Write(Data, 1, 32);
        //    else
        //        writer.Write(Data);
        //}

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["usage"] = Usage;
            json["data"] = Data.ToHexString();
            return json;
        }
    }
}
