using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AiAnalysis
{

    public static void DebugArray(float[] arr) {
        string s = "{";
        for (int i = 0; i < arr.Length; i++) {
            s += (arr[i].ToString("0.00") + "f");
            if (i < arr.Length - 1)
                s += ", ";
        }
        s += "}";
        Debug.Log(s);
    }

    public AIGame[] Games(int num, int blueDepth, int redDepth) {
        AIGame[] games = new AIGame[num];
        for (int i = 0; i < num; i++)
        {
            games[i] = Game(blueDepth, redDepth);
        }
        return games;
    }

    public AIGame Game(int blueDepth, int redDepth) {
        int movesLeft = 13;
        Board board = new Board();

        board.Initialize(5, 6);
        board.BluePositionVals = board.Zeros(30);
        board.RedPositionVals = board.Zeros(30);

        List<Move> MoveHistory = new List<Move>();
        Tile.Player currentPlayer = Tile.Player.Blue;

        while (board.WinningPlayer(movesLeft) == Tile.Player.Neutral) {
            Move move; 
            if (currentPlayer == Tile.Player.Blue)
            {
                move = MakeAIMove(board, currentPlayer, movesLeft, blueDepth);
                board.MakeMove(move, currentPlayer);
                movesLeft -= 1;
                currentPlayer = Tile.Player.Red;
            }
            else {
                move = MakeAIMove(board, currentPlayer, movesLeft, redDepth);
                board.MakeMove(move, currentPlayer);
                currentPlayer = Tile.Player.Blue;
            }
            MoveHistory.Add(move);
        }

        return new AIGame(board.WinningPlayer(movesLeft), MoveHistory);
    }

    public Move MakeAIMove(Board board, Tile.Player player, int movesLeft, int depth) {
        AI ai = new AI();
        ai.tt = new TranspositionTable();
        ai.tt.enabled = true;
        float score = ai.Negamax(board, movesLeft, depth, depth, -Mathf.Infinity, Mathf.Infinity, player);
        return ai.bestMove;
    }
}

public struct AIGame
{
    public Tile.Player Outcome;
    public List<Move> MoveHistory;

    public AIGame(Tile.Player Outcome, List<Move> MoveHistory) {
        this.Outcome = Outcome;
        this.MoveHistory = MoveHistory;
    }
}