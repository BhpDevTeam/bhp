﻿using Bhp.Cryptography.ECC;
using Bhp.IO.Caching;
using Bhp.IO.Wrappers;
using Bhp.Ledger;

namespace Bhp.Persistence
{
    public abstract class Store : IPersistence
    {
        DataCache<UInt256, TrimmedBlock> IPersistence.Blocks => GetBlocks();
        DataCache<UInt256, TransactionState> IPersistence.Transactions => GetTransactions();
        DataCache<UInt256, UnspentCoinState> IPersistence.UnspentCoins => GetUnspentCoins();
        DataCache<UInt256, AssetState> IPersistence.Assets => GetAssets();
        DataCache<UInt160, ContractState> IPersistence.Contracts => GetContracts();
        DataCache<StorageKey, StorageItem> IPersistence.Storages => GetStorages();
        DataCache<UInt32Wrapper, HeaderHashList> IPersistence.HeaderHashList => GetHeaderHashList();
        MetaDataCache<HashIndexState> IPersistence.BlockHashIndex => GetBlockHashIndex();
        MetaDataCache<HashIndexState> IPersistence.HeaderHashIndex => GetHeaderHashIndex();

        public abstract byte[] Get(byte[] key);
        public abstract DataCache<UInt256, TrimmedBlock> GetBlocks();
        public abstract DataCache<UInt256, TransactionState> GetTransactions();
        public abstract DataCache<UInt256, UnspentCoinState> GetUnspentCoins();
        public abstract DataCache<UInt256, AssetState> GetAssets();
        public abstract DataCache<UInt160, ContractState> GetContracts();
        public abstract DataCache<StorageKey, StorageItem> GetStorages();
        public abstract DataCache<UInt32Wrapper, HeaderHashList> GetHeaderHashList();
        public abstract MetaDataCache<HashIndexState> GetBlockHashIndex();
        public abstract MetaDataCache<HashIndexState> GetHeaderHashIndex();
        public abstract void Put(byte[] key, byte[] value);
        public abstract void PutSync(byte[] key, byte[] value);

        public abstract StoreView GetSnapshot();
    }
}
