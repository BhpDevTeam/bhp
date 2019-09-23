﻿using Bhp.IO.Json;
using System.Linq;

namespace Bhp.Network.RPC.Models
{
    public class RpcRawMemPool
    {
        public uint Height { get; set; }

        public string[] Verified { get; set; }

        public string[] UnVerified { get; set; }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["height"] = Height;
            json["verified"] = new JArray(Verified.Select(p => (JObject)p));
            json["unverified"] = new JArray(UnVerified.Select(p => (JObject)p));
            return json;
        }

        public static RpcRawMemPool FromJson(JObject json, bool shouldGetUnverified)
        {
            RpcRawMemPool rawMemPool = new RpcRawMemPool();
            rawMemPool.Height = uint.Parse(json["height"].AsString());
            rawMemPool.Verified = ((JArray)json["verified"]).Select(p => p.AsString()).ToArray();
            rawMemPool.UnVerified = ((JArray)json["unverified"]).Select(p => p.AsString()).ToArray();
            if (shouldGetUnverified)
            {
                rawMemPool.Verified = ((JArray)json["verified"]).Select(p => p.AsString()).ToArray();
            }
            return rawMemPool;
        }
    }
}
