﻿using Bhp.Cryptography.ECC;
using Bhp.IO.Caching;
using Bhp.IO.Wrappers;
using Bhp.Ledger;

namespace Bhp.Persistence
{
    public interface IPersistence
    {
        DataCache<UInt256, BlockState> Blocks { get; }
        DataCache<UInt256, TransactionState> Transactions { get; }
        DataCache<UInt160, AccountState> Accounts { get; }
        DataCache<UInt256, UnspentCoinState> UnspentCoins { get; }
        DataCache<UInt256, SpentCoinState> SpentCoins { get; }
        DataCache<ECPoint, ValidatorState> Validators { get; }
        DataCache<UInt256, AssetState> Assets { get; }
        DataCache<UInt160, ContractState> Contracts { get; }
        DataCache<StorageKey, StorageItem> Storages { get; }
        DataCache<UInt32Wrapper, HeaderHashList> HeaderHashList { get; }
        MetaDataCache<ValidatorsCountState> ValidatorsCount { get; }
        MetaDataCache<HashIndexState> BlockHashIndex { get; }
        MetaDataCache<HashIndexState> HeaderHashIndex { get; }
    }
}
