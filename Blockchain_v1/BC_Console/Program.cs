using BC_Library.Dto;
using BC_Library.Service;

class Program
{
    static void Main(string[] args)
    {
        BlockService myCoin = new BlockService();

        Console.WriteLine("Mining block 1...");
        BlockDto block1 = new BlockDto(1, "Amount: 50", myCoin.GetLatestBlock().Hash);
        CommonService.MineBlock(block1, 3);
        myCoin.AddBlock(block1);

        Console.WriteLine("Mining block 2...");
        BlockDto block2 = new BlockDto(2, "Amount: 100", myCoin.GetLatestBlock().Hash);
        CommonService.MineBlock(block2, 3);
        myCoin.AddBlock(block2);

        Console.WriteLine("Is blockchain valid? " + myCoin.IsChainValid());
    }
}
