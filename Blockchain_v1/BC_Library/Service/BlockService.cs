using BC_Library.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BC_Library.Service
{
    public class BlockService
    {
        public List<BlockDto> Chain { get; set; }

        public BlockService()
        {
            Chain = new List<BlockDto> { CreateGenesisBlock() };
        }

        private BlockDto CreateGenesisBlock()
        {
            return new BlockDto(0, "Genesis Block", "0");
        }

        public BlockDto GetLatestBlock()
        {
            return Chain.Last();
        }

        public void AddBlock(BlockDto newBlock)
        {
            newBlock.PreviousHash = GetLatestBlock().Hash;
            newBlock.Hash = newBlock.CalculateHash();
            Chain.Add(newBlock);
        }

        public bool IsChainValid()
        {
            for (int i = 1; i < Chain.Count; i++)
            {
                var current = Chain[i];
                var previous = Chain[i - 1];

                if (current.Hash != current.CalculateHash())
                    return false;

                if (current.PreviousHash != previous.Hash)
                    return false;
            }

            return true;
        }
    }

}
