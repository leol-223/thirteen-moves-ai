using System;

public class Zobrist
{
    public ulong[,] piecesArray;

    public void Init() {
        Random rand = new Random(1);
        piecesArray = new ulong[2, BoardUI.width * BoardUI.height];
        for (int i = 0; i < BoardUI.width * BoardUI.height; i++) {
            piecesArray[0, i] = LongRandom(rand);
            piecesArray[1, i] = LongRandom(rand);
        }
    }

    public ulong CalculateZobristKey(Board board)
    {
        ulong zobristKey = 0;

        for (int squareIndex = 0; squareIndex < BoardUI.width * BoardUI.height; squareIndex++)
        {
            int val = board.Square[squareIndex];
            if (val == 1)
                zobristKey ^= piecesArray[0, squareIndex];
            else if (val == 2)
                zobristKey ^= piecesArray[1, squareIndex];
        }
        return zobristKey;
    }

    ulong LongRandom(Random rand)
    {
        byte[] buf = new byte[8];
        rand.NextBytes(buf);
        return BitConverter.ToUInt64(buf, 0);
    }
}
