﻿using Bhp.Cryptography.ECC;
using Bhp.IO.Caching;
using Bhp.IO.Data.LevelDB;
using Bhp.IO.Wrappers;
using Bhp.Ledger;
using LSnapshot = Bhp.IO.Data.LevelDB.Snapshot;

namespace Bhp.Persistence.LevelDB
{
    internal class DbSnapshot : Snapshot
    {
        private readonly DB db;
        private readonly LSnapshot snapshot;
        private readonly WriteBatch batch;

        public override DataCache<UInt256, TrimmedBlock> Blocks { get; }
        public override DataCache<UInt256, TransactionState> Transactions { get; }
        public override DataCache<UInt256, UnspentCoinState> UnspentCoins { get; }
        public override DataCache<UInt256, AssetState> Assets { get; }
        public override DataCache<UInt160, ContractState> Contracts { get; }
        public override DataCache<StorageKey, StorageItem> Storages { get; }
        public override DataCache<UInt32Wrapper, HeaderHashList> HeaderHashList { get; }
        public override MetaDataCache<HashIndexState> BlockHashIndex { get; }
        public override MetaDataCache<HashIndexState> HeaderHashIndex { get; }

        public DbSnapshot(DB db)
        {
            this.db = db;
            this.snapshot = db.GetSnapshot();
            this.batch = new WriteBatch();
            ReadOptions options = new ReadOptions { FillCache = false, Snapshot = snapshot };
            Blocks = new DbCache<UInt256, TrimmedBlock>(db, options, batch, Prefixes.DATA_Block);
            Transactions = new DbCache<UInt256, TransactionState>(db, options, batch, Prefixes.DATA_Transaction);
            UnspentCoins = new DbCache<UInt256, UnspentCoinState>(db, options, batch, Prefixes.ST_Coin);
            Assets = new DbCache<UInt256, AssetState>(db, options, batch, Prefixes.ST_Asset);
            Contracts = new DbCache<UInt160, ContractState>(db, options, batch, Prefixes.ST_Contract);
            Storages = new DbCache<StorageKey, StorageItem>(db, options, batch, Prefixes.ST_Storage);
            HeaderHashList = new DbCache<UInt32Wrapper, HeaderHashList>(db, options, batch, Prefixes.IX_HeaderHashList);
            BlockHashIndex = new DbMetaDataCache<HashIndexState>(db, options, batch, Prefixes.IX_CurrentBlock);
            HeaderHashIndex = new DbMetaDataCache<HashIndexState>(db, options, batch, Prefixes.IX_CurrentHeader);
        }

        public override void Commit()
        {
            base.Commit();
            db.Write(WriteOptions.Default, batch);
        }

        public override void Dispose()
        {
            snapshot.Dispose();
        }
    }
}
