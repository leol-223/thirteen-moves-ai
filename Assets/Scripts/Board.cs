using System.Collections.Generic;

public class Board
{
    public PieceList blueTiles;
    public PieceList redTiles;

    public int[] Square;
    public int[] BlueCoveredSquare;
    public int[] RedCoveredSquare;
    public int width;
    public int height;

    public const int None = 0;
    public const int Blue = 1;
    public const int Red = 2;
    public float BlueAverageX;
    public float RedAverageX;

    Dictionary<int, List<int>> tileCoveredPositionsTableBlue = new Dictionary<int, List<int>>();
    Dictionary<int, List<int>> tileCoveredPositionsTableRed = new Dictionary<int, List<int>>();
    Dictionary<ushort, List<int>> TileMovePrecomputed = new Dictionary<ushort, List<int>>();

    public float[] BluePositionVals;
    public float[] RedPositionVals;

    Zobrist zobrist;
    public ulong zobristKey;
    Stack<bool> capturedPieceHistory;
    public bool capturedPiece;

    public void Initialize(int width_, int height_) {
        width = width_;
        height = height_;

        Square = new int[width * height];
        BlueCoveredSquare = new int[width * height];
        RedCoveredSquare = new int[width * height];
        blueTiles = new PieceList(width);
        redTiles = new PieceList(width);
        capturedPieceHistory = new Stack<bool>();
        BlueAverageX = (width-1f) / 2f;
        RedAverageX = (width - 1f) / 2f;

        if (width == 5 && height == 6)
        {
            BluePositionVals = TilePositionValues.VALUES_5_6_BLUE;
            RedPositionVals = TilePositionValues.VALUES_5_6_RED;
        }
        else {
            BluePositionVals = Zeros(width * height);
            RedPositionVals = Zeros(width * height);
        }

        for (int i = 0; i < width; i++) {
            blueTiles.AddPieceAtSquare(i * height);
            Square[i * height] = Blue;

            int redTile = (i + 1) * height - 1;
            redTiles.AddPieceAtSquare(redTile);
            Square[redTile] = Red;
        }

        for (int i = 0; i < width * height; i++) {
            tileCoveredPositionsTableBlue[i] = TileCoveredPositions(i, 1);
            tileCoveredPositionsTableRed[i] = TileCoveredPositions(i, -1);
            if (i % height != height - 1) {
                List<int> toTiles = GetValidTiles(i, Tile.Player.Blue);
                for (int j = 0; j < toTiles.Count; j++) {
                    int toTile = toTiles[j];
                    TileMovePrecomputed[new Move(i, toTile).Value] = TileRemovedPositions(i, toTile, 1);
                }
            }
            if (i % height != 0)
            {
                List<int> toTiles = GetValidTiles(i, Tile.Player.Red);
                for (int j = 0; j < toTiles.Count; j++)
                {
                    int toTile = toTiles[j];
                    TileMovePrecomputed[new Move(i, toTile).Value] = TileRemovedPositions(i, toTile, -1);
                }
            }
        }

        for (int i = 0; i < blueTiles.Count; i++) {
            AddRangeFromTile(blueTiles[i], Tile.Player.Blue);
        }
        for (int i = 0; i < redTiles.Count; i++)
        {
            AddRangeFromTile(redTiles[i], Tile.Player.Red);
        }

        zobrist = new Zobrist();
        zobrist.Init();
        zobristKey = zobrist.CalculateZobristKey(this);
    }

    public float[] Zeros(int num) {
        float[] zeros = new float[num];
        for (int i = 0; i < num; i++)
            zeros[i] = 0;
        return zeros;
    }

    public void DebugTable(int[] table)
    {
        string msg = "";
        for (int j = height - 1; j >= 0; j--)
        {
            for (int i = 0; i < width; i++)
            {
                int val = table[height * i + j];
                msg += (val.ToString() + " ");
            }
            msg += "\n";
        }
        UnityEngine.Debug.Log(msg);
    }

    public void DebugTable(float[] table)
    {
        string msg = "";
        for (int j = height - 1; j >= 0; j--)
        {
            for (int i = 0; i < width; i++)
            {
                float val = table[height * i + j];
                msg += (val.ToString("0.00") + " ");
            }
            msg += "\n";
        }
        UnityEngine.Debug.Log(msg);
    }

    public void AddRangeFromTile(int tile, Tile.Player player) {
        List<int> tileCoveredPositions;
        if (player == Tile.Player.Blue)
        {
            tileCoveredPositions = tileCoveredPositionsTableBlue[tile];
            for (int i = 0; i < tileCoveredPositions.Count; i++) {
                BlueCoveredSquare[tileCoveredPositions[i]] += 1;
            }
        }
        else {
            tileCoveredPositions = tileCoveredPositionsTableRed[tile];
            for (int i = 0; i < tileCoveredPositions.Count; i++)
            {
                RedCoveredSquare[tileCoveredPositions[i]] += 1;
            }
        }
    }

    public void RemoveRangeFromTile(int tile, Tile.Player player)
    {
        List<int> tileCoveredPositions;
        if (player == Tile.Player.Blue)
        {
            tileCoveredPositions = tileCoveredPositionsTableBlue[tile];
            for (int i = 0; i < tileCoveredPositions.Count; i++)
            {
                BlueCoveredSquare[tileCoveredPositions[i]] -= 1;
            }
        }
        else
        {
            tileCoveredPositions = tileCoveredPositionsTableRed[tile];
            for (int i = 0; i < tileCoveredPositions.Count; i++)
            {
                RedCoveredSquare[tileCoveredPositions[i]] -= 1;
            }
        }
    }

    public void UpdateRangeFromMovedTile(int fromTile, int toTile, Tile.Player player)
    {
        List<int> tileRemovedPositions;
        ushort val = (ushort)(fromTile | toTile << 8);
        tileRemovedPositions = TileMovePrecomputed[val];
        if (player == Tile.Player.Blue)
        {
            for (int i = 0; i < tileRemovedPositions.Count; i++)
            {
                BlueCoveredSquare[tileRemovedPositions[i]] -= 1;
            }
        }
        else
        {
            for (int i = 0; i < tileRemovedPositions.Count; i++)
            {
                RedCoveredSquare[tileRemovedPositions[i]] -= 1;
            }
        }
    }

    public void UpdateRangeFromMovedTileReverse(int movedTo, int movedFrom, Tile.Player player)
    {
        List<int> tileRemovedPositions;
        ushort val = (ushort)(movedFrom | movedTo << 8);
        tileRemovedPositions = TileMovePrecomputed[val];
        if (player == Tile.Player.Blue)
        {
            for (int i = 0; i < tileRemovedPositions.Count; i++)
            {
                BlueCoveredSquare[tileRemovedPositions[i]] += 1;
            }
        }
        else
        {
            for (int i = 0; i < tileRemovedPositions.Count; i++)
            {
                RedCoveredSquare[tileRemovedPositions[i]] += 1;
            }
        }
    }

    public List<int> TileCoveredPositions(int tile, int dir) {
        List<int> coveredPositions = new List<int>();

        int x = tile / height;
        int y = tile % height;
        for (int i = 0; i < width; i++) {
            for (int j = 0; j < height; j++) {
                if (dir * (j - y) >= System.Math.Abs(x-i)) {
                    coveredPositions.Add(i * height + j);
                }
            }
        }

        return coveredPositions;
    }

    public List<int> TileRemovedPositions(int start, int to, int dir) {
        List<int> startCovered = TileCoveredPositions(start, dir);
        List<int> toCovered = TileCoveredPositions(to, dir);
        List<int> removed = new List<int>();
        for (int i = 0; i < startCovered.Count; i++) {
            int tile = startCovered[i];
            if (!toCovered.Contains(tile))
                removed.Add(tile);
        }
        return removed;
    }

    public void MakeMove(Move move, Tile.Player player)
    {
        int fromTile = move.StartSquare;
        int toTile = move.TargetSquare;
        int capturedTile = Square[toTile];

        capturedPiece = false;
        if (player == Tile.Player.Blue)
        {
            if (capturedTile == Red)
            {
                capturedPiece = true;
                redTiles.RemovePieceAtSquare(toTile);
                RemoveRangeFromTile(toTile, Tile.Player.Red);
                if (redTiles.Count != 0)
                    RedAverageX += (RedAverageX - toTile / height) / redTiles.Count;
                else
                    RedAverageX = 0;
                zobristKey ^= zobrist.piecesArray[1, toTile];
            }
            // right
            if ((toTile - fromTile) > 1)
            {
                BlueAverageX += 1f / blueTiles.Count;
            }
            // left
            else if ((toTile - fromTile) < 1) {
                BlueAverageX -= 1f / blueTiles.Count;
            }
            blueTiles.MovePiece(fromTile, toTile);
            UpdateRangeFromMovedTile(fromTile, toTile, Tile.Player.Blue);
            zobristKey ^= zobrist.piecesArray[0, fromTile];
            zobristKey ^= zobrist.piecesArray[0, toTile];
            Square[toTile] = Blue;
        }
        else {
            if (capturedTile == Blue)
            {
                capturedPiece = true;
                blueTiles.RemovePieceAtSquare(toTile);
                RemoveRangeFromTile(toTile, Tile.Player.Blue);
                if (blueTiles.Count != 0)
                    BlueAverageX += (BlueAverageX - toTile / height) / blueTiles.Count;
                else
                    BlueAverageX = 0;
                zobristKey ^= zobrist.piecesArray[0, toTile];
            }
            // right
            if ((toTile - fromTile) > -1)
            {
                RedAverageX += 1f / redTiles.Count;
            }
            // left
            else if ((toTile - fromTile) < -1)
            {
                RedAverageX -= 1f / redTiles.Count;
            }
            redTiles.MovePiece(fromTile, toTile);
            UpdateRangeFromMovedTile(fromTile, toTile, Tile.Player.Red);
            zobristKey ^= zobrist.piecesArray[1, fromTile];
            zobristKey ^= zobrist.piecesArray[1, toTile];
            Square[toTile] = Red;
        }
        capturedPieceHistory.Push(capturedPiece);
        Square[fromTile] = None;
    }

    public void UnmakeMove(Move move, Tile.Player player) {
        int movedFrom = move.StartSquare;
        int movedTo = move.TargetSquare;

        capturedPiece = capturedPieceHistory.Pop(); // removes current state from history
        if (player == Tile.Player.Blue)
        {
            zobristKey ^= zobrist.piecesArray[0, movedFrom];
            zobristKey ^= zobrist.piecesArray[0, movedTo];
            if (capturedPiece)
            {
                redTiles.AddPieceAtSquare(movedTo);
                AddRangeFromTile(movedTo, Tile.Player.Red);
                if (redTiles.Count > 0)
                    RedAverageX -= (RedAverageX - movedTo / height) / redTiles.Count;
                else
                    RedAverageX = movedTo / height;
                zobristKey ^= zobrist.piecesArray[1, movedTo];
                Square[movedTo] = Red;
            }
            else
            {
                Square[movedTo] = None;
            }
            if ((movedTo - movedFrom) > 1)
            {
                BlueAverageX -= 1f / blueTiles.Count;
            }
            else if ((movedTo - movedFrom) < 1)
            {
                BlueAverageX += 1f / blueTiles.Count;
            }
            blueTiles.MovePiece(movedTo, movedFrom);
            UpdateRangeFromMovedTileReverse(movedTo, movedFrom, Tile.Player.Blue);
            Square[movedFrom] = Blue;
        }
        else {
            zobristKey ^= zobrist.piecesArray[1, movedFrom];
            zobristKey ^= zobrist.piecesArray[1, movedTo];
            if (capturedPiece)
            {
                blueTiles.AddPieceAtSquare(movedTo);
                AddRangeFromTile(movedTo, Tile.Player.Blue);
                if (blueTiles.Count > 0)
                    BlueAverageX -= (BlueAverageX - movedTo / height) / blueTiles.Count;
                else
                    BlueAverageX = movedTo / height;
                zobristKey ^= zobrist.piecesArray[0, movedTo];
                Square[movedTo] = Blue;
            }
            else
            {
                Square[movedTo] = None;
            }
            if ((movedTo - movedFrom) > -1)
            {
                RedAverageX -= 1f / redTiles.Count;
            }
            else if ((movedTo - movedFrom) < -1)
            {
                RedAverageX += 1f / redTiles.Count;
            }
            redTiles.MovePiece(movedTo, movedFrom);
            UpdateRangeFromMovedTileReverse(movedTo, movedFrom, Tile.Player.Red);
            Square[movedFrom] = Red;
        }        
    }

    public List<int> GetValidTiles(int tile, Tile.Player player) {
        List<int> validTiles = new List<int>();
        if (player == Tile.Player.Blue)
        {
            validTiles.Add(tile + 1);
            int x = tile / height;
            // left is valid
            if (x > 0)
                validTiles.Add(tile + 1 - height);
            // right is valid
            if (x < width - 1)
                validTiles.Add(tile + 1 + height);
        }
        else {
            validTiles.Add(tile - 1);
            int x = tile / height;
            // left is valid
            if (x > 0)
                validTiles.Add(tile - 1 - height);
            // right is valid
            if (x < width - 1)
                validTiles.Add(tile - 1 + height);
        }

        return validTiles;
    }

    public List<int> GetValidTilesConstraint(int tile, Tile.Player player)
    {
        List<int> validTiles = new List<int>();
        if (player == Tile.Player.Blue)
        {
            if (tile % height == height - 1)
                return validTiles;
            validTiles.Add(tile + 1);
            int x = tile / height;
            // left is valid
            if (x > 0)
                validTiles.Add(tile + 1 - height);
            // right is valid
            if (x < width - 1)
                validTiles.Add(tile + 1 + height);
        }
        else
        {
            if (tile % height == 0)
                return validTiles;
            validTiles.Add(tile - 1);
            int x = tile / height;
            // left is valid
            if (x > 0)
                validTiles.Add(tile - 1 - height);
            // right is valid
            if (x < width - 1)
                validTiles.Add(tile - 1 + height);
        }

        return validTiles;
    }

    public List<Move> GetValidMoves(Tile.Player player) {
        List<Move> validMoves = new List<Move>();
        if (player == Tile.Player.Blue)
        {
            for (int i = 0; i < blueTiles.Count; i++)
            {
                int tile = blueTiles[i];
                List<int> validTiles = GetValidTiles(tile, player);
                for (int j = 0; j < validTiles.Count; j++)
                {
                    int toTile = validTiles[j];
                    if (Square[toTile] != Blue)
                        validMoves.Add(new Move(tile, toTile));
                }
            }
        }
        else {
            for (int i = 0; i < redTiles.Count; i++)
            {
                int tile = redTiles[i];
                List<int> validTiles = GetValidTiles(tile, player);
                for (int j = 0; j < validTiles.Count; j++)
                {
                    int toTile = validTiles[j];
                    if (Square[toTile] != Red)
                        validMoves.Add(new Move(tile, toTile));
                }
            }
        }
        return validMoves;
    }

    public Tile.Player WinningPlayer(int movesLeft) {
        if (blueTiles.Count == 0)
            return Tile.Player.Red;
        if (redTiles.Count == 0)
        {
            return Tile.Player.Blue;
        }

        for (int i = 0; i < blueTiles.Count; i++) {
            if (blueTiles[i] % height == height - 1)
            {
                return Tile.Player.Blue;
            }
        }
        if (movesLeft == 0)
            return Tile.Player.Red;
        for (int i = 0; i < redTiles.Count; i++)
        {
            if (redTiles[i] % height == 0)
                return Tile.Player.Red;
        }
        return Tile.Player.Neutral;
    }

}
