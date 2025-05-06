using BC_Library.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;

namespace BC_Library.Service
{
    public class BlockchainService
    {
        public List<BlockDto> Chain { get; set; }
        public List<TransactionDto> PendingTransactions { get; set; } = new List<TransactionDto>();
        public int Difficulty { get; set; } = 3;
        public decimal MiningReward { get; set; } = 10;

        public BlockchainService()
        {
            Chain = new List<BlockDto> { CreateGenesisBlock() };
        }

        private BlockDto CreateGenesisBlock()
        {
            return new BlockDto(0, new List<TransactionDto>(), "0");
        }

        public BlockDto GetLatestBlock() => Chain.Last();

        public void MinePendingTransactions(string minerAddress)
        {
            var block = new BlockDto(Chain.Count, new List<TransactionDto>(PendingTransactions), GetLatestBlock().Hash);
            MineBlock(block);
            Chain.Add(block);
            PendingTransactions.Clear();
            PendingTransactions.Add(new TransactionDto(null, minerAddress, MiningReward));
        }

        public void MineBlock(BlockDto block)
        {
            string target = new string('0', Difficulty);
            while (!block.Hash.StartsWith(target))
            {
                block.Nonce++;
                block.Hash = block.CalculateHash();
            }

            Console.WriteLine($"✅ Block mined: {block.Hash}");
        }

        public void CreateTransaction(TransactionDto tx) => PendingTransactions.Add(tx);

        public decimal GetBalance(string address)
        {
            decimal balance = 0;
            foreach (var block in Chain)
            {
                foreach (var tx in block.Transactions)
                {
                    if (tx.FromAddress == address)
                        balance -= tx.Amount;
                    if (tx.ToAddress == address)
                        balance += tx.Amount;
                }
            }
            return balance;
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
