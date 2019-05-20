﻿namespace Bhp.Persistence.LevelDB
{
    internal static class Prefixes
    {
        public const byte DATA_Block = 0x01;
        public const byte DATA_Transaction = 0x02;

        public const byte ST_Coin = 0x44;
        public const byte ST_Asset = 0x4c;
        public const byte ST_Contract = 0x50;
        public const byte ST_Storage = 0x70;

        public const byte IX_HeaderHashList = 0x80;
        public const byte IX_CurrentBlock = 0xc0;
        public const byte IX_CurrentHeader = 0xc1;

        public const byte SYS_Version = 0xf0;

        /* Prefixes 0xf1 to 0xff are reserved for external use.
         *
         * Note: The saved consensus state uses the Prefix 0xf4
         */
    }
}
