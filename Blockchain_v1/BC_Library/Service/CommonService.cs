using BC_Library.Dto;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace BC_Library.Service
{
    public static class CommonService
    {
        public static void MineBlock(BlockDto block, int difficulty)
        {
            string target = new string('0', difficulty);
            while (!block.Hash.StartsWith(target))
            {
                block.Nonce++;
                block.Hash = block.CalculateHash();
            }
            Console.WriteLine($"✅ Block mined: {block.Hash}");
        }
    }
}
