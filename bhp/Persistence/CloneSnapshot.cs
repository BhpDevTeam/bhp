﻿using Bhp.IO.Caching;
using Bhp.IO.Wrappers;
using Bhp.Ledger;

namespace Bhp.Persistence
{
    internal class CloneSnapshot : Snapshot
    {
        public override DataCache<UInt256, BlockState> Blocks { get; }
        public override DataCache<UInt256, TransactionState> Transactions { get; }
        public override DataCache<UInt256, UnspentCoinState> UnspentCoins { get; }
        public override DataCache<UInt256, AssetState> Assets { get; }
        public override DataCache<UInt160, ContractState> Contracts { get; }
        public override DataCache<StorageKey, StorageItem> Storages { get; }
        public override DataCache<UInt32Wrapper, HeaderHashList> HeaderHashList { get; }
        public override MetaDataCache<NextValidatorsState> NextValidators { get; }
        public override MetaDataCache<HashIndexState> BlockHashIndex { get; }
        public override MetaDataCache<HashIndexState> HeaderHashIndex { get; }

        public CloneSnapshot(Snapshot snapshot)
        {
            this.PersistingBlock = snapshot.PersistingBlock;
            this.Blocks = snapshot.Blocks.CreateSnapshot();
            this.Transactions = snapshot.Transactions.CreateSnapshot();
            this.UnspentCoins = snapshot.UnspentCoins.CreateSnapshot();
            this.Assets = snapshot.Assets.CreateSnapshot();
            this.Contracts = snapshot.Contracts.CreateSnapshot();
            this.Storages = snapshot.Storages.CreateSnapshot();
            this.HeaderHashList = snapshot.HeaderHashList.CreateSnapshot();
            this.NextValidators = snapshot.NextValidators.CreateSnapshot();
            this.BlockHashIndex = snapshot.BlockHashIndex.CreateSnapshot();
            this.HeaderHashIndex = snapshot.HeaderHashIndex.CreateSnapshot();
        }
    }
}
