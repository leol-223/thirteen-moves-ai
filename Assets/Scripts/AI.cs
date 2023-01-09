using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AI
{
    public Move bestMove;
    public TranspositionTable tt;
    public int nodes = 0;
    MoveOrdering moveOrdering = new MoveOrdering();

    private float StaticEvaluation(Board board, int movesLeft, int blueToMoveBonus) {
        int height = board.height;
        int blueCount = board.blueTiles.Count;
        int redCount = board.redTiles.Count;
        // bonus to prioritize losing while taking down as many ennemies as possible
        float bonus = (blueCount - redCount) * 0.01f;

        // if a player has no tiles left, they lose
        if (redCount == 0)
            return 100 + movesLeft + bonus;
        if (blueCount == 0)
            return -(100 + movesLeft - bonus);

        int maxBlueHeight = 0;
        int maxRedHeight = height-1;

        // "Passed" tiles cannot be stopped; they have a clear path to the end
        int maxPassedBlueHeight = 0;
        int maxPassedRedHeight = height-1;

        float bluePositionVal = 0f;
        float redPositionVal = 0f;

        float blueStd = 0;
        float blueAvg = board.BlueAverageX;
        for (int i = 0; i < blueCount; i++)
        {
            int tile = board.blueTiles[i];
            int blueHeight = tile % height;
            if (blueHeight == height - 1)
                return 100 + movesLeft + bonus;

            float dist = (tile / height) - blueAvg;
            blueStd += dist * dist;

            bluePositionVal += board.BluePositionVals[tile];
            maxBlueHeight = Mathf.Max(blueHeight, maxBlueHeight);
            if (board.RedCoveredSquare[tile] == 0)
                maxPassedBlueHeight = Mathf.Max(maxPassedBlueHeight, blueHeight);
        }

        // check if blue ran out of moves
        if (movesLeft == 0)
            return -(100 + movesLeft - bonus);

        float redStd = 0;
        float redAvg = board.RedAverageX;
        for (int i = 0; i < redCount; i++)
        {
            int tile = board.redTiles[i];
            int redHeight = tile % height;

            if (redHeight == 0)
                return -(100 + movesLeft - bonus);

            float dist = (tile / height) - redAvg;
            redStd += dist * dist;

            redPositionVal += board.RedPositionVals[tile];
            maxRedHeight = Mathf.Min(redHeight, maxRedHeight);
            if (board.BlueCoveredSquare[tile] == 0)
                maxPassedRedHeight = Mathf.Min(maxPassedRedHeight, redHeight);
        }

        // blue can get to the other side without being stopped
        int movesToWin = height - maxPassedBlueHeight - 1;
        if (movesToWin - blueToMoveBonus < maxRedHeight && movesToWin <= movesLeft)
        {
            return 100 + movesLeft - movesToWin + bonus;
        }

        // dead position, blue can't get to the other side
        int minMovesToFinish = height - maxBlueHeight - 1;
        if (minMovesToFinish > movesLeft && movesLeft < redCount)
            return -100+bonus;

        if (maxPassedRedHeight < minMovesToFinish + 1 - blueToMoveBonus)
            return Mathf.Min(-(100 + movesLeft - maxPassedRedHeight), -100)+bonus;

        /*
        float distance = 0;
        if (movesLeft < 5)
            distance = (blueAvg - redAvg) * (blueAvg - redAvg) * 0.2f;
        */
        return (blueCount - redCount) + (bluePositionVal - redPositionVal) + (redStd / redCount * 0.15f - blueStd / blueCount * 0.15f);// - distance;
    }

    public float Negamax(Board searchBoard, int movesLeft, int depth, int origDepth, float alpha, float beta, Tile.Player player) {
        nodes += 1;

        ulong key = searchBoard.zobristKey;
        bool originalDepth = depth == origDepth;
        TranspositionTable.Entry lookup = tt.Lookup(key);
        // -1 -> lookup failed
        if (lookup.depth == depth) {
            if (lookup.flag == TranspositionTable.Exact)
            {
                if (originalDepth)
                    bestMove = lookup.move;
                return lookup.value;
            }
            else if (lookup.flag == TranspositionTable.LowerBound)
            {
                alpha = Mathf.Max(alpha, lookup.value);
            }
            else if (lookup.flag == TranspositionTable.UpperBound)
            {
                beta = Mathf.Min(beta, lookup.value);
            }
            if (alpha >= beta) {
                if (originalDepth)
                    bestMove = lookup.move;
                return lookup.value;
            }
        }

        if (depth == 0)
        {
            if (player == Tile.Player.Blue)
                return StaticEvaluation(searchBoard, movesLeft, 1);
            else
                return -StaticEvaluation(searchBoard, movesLeft, 0);
        }

        Tile.Player winningPlayer = searchBoard.WinningPlayer(movesLeft);
        if (winningPlayer != Tile.Player.Neutral) {
            float bonus = (searchBoard.blueTiles.Count - searchBoard.redTiles.Count) * 0.01f;
            if (player == Tile.Player.Red)
                bonus *= -1;
            if (winningPlayer == player)
                return 100 + movesLeft + bonus;
            else
                return -(100 + movesLeft - bonus);
        }


        float alphaOrig = alpha + 0;
        float bestEval = -Mathf.Infinity;
        List<Move> validMoves = searchBoard.GetValidMoves(player);

        if (originalDepth)
        {
            bestMove = validMoves[0];
            moveOrdering.OrderMoves(searchBoard, validMoves, true);
        }
        else {
            moveOrdering.OrderMoves(searchBoard, validMoves, false);
        }
        Move bestMoveThisEval = validMoves[0];
        for (int i = 0; i < validMoves.Count; i++) {
            Move move = validMoves[i];
            searchBoard.MakeMove(move, player);
            float currentEval;
            if (player == Tile.Player.Blue)
                currentEval = -Negamax(searchBoard, movesLeft - 1, depth - 1, origDepth, -beta, -alpha, Tile.Player.Red);
            else
                currentEval = -Negamax(searchBoard, movesLeft, depth - 1, origDepth, -beta, -alpha, Tile.Player.Blue);
            searchBoard.UnmakeMove(move, player);

            if (currentEval > bestEval) {
                bestEval = currentEval;
                bestMoveThisEval = move;
            }
            if (currentEval > alpha) {
                alpha = currentEval;
                if (depth == origDepth)
                    bestMove = move;
            }
            if (alpha >= beta)
            {
                break;
            }
        }

        if (bestEval <= alphaOrig)
            tt.Store(key, depth, bestEval, TranspositionTable.UpperBound, bestMoveThisEval);
        else if (bestEval >= beta)
        {
            tt.Store(key, depth, bestEval, TranspositionTable.LowerBound, bestMoveThisEval);
        }
        else {
            tt.Store(key, depth, bestEval, TranspositionTable.Exact, bestMoveThisEval);
        }
        return bestEval;
    }
}
