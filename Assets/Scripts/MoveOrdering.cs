using System.Collections.Generic;
using System.Linq;
using System;

public class MoveOrdering
{
    public float[] moveScores;
    int maxMoveCount = BoardUI.width*3;

	public MoveOrdering()
	{
		moveScores = new float[maxMoveCount];
	}

	public void OrderMoves(Board board, List<Move> moves, bool includeRandom)
	{
		if (includeRandom)
		{
			Random rand = new Random();
			for (int i = 0; i < moves.Count; i++)
			{
				// add randomness so AI doesn't always play the same moves in the same positions
				float score = rand.Next(0, 9999);
				int toSquare = moves[i].TargetSquare;
				int capturePieceType = board.Square[toSquare];
				int startPieceType = board.Square[moves[i].StartSquare];

				if (capturePieceType != 0)
				{
					// Order moves to try capturing the most valuable opponent piece with least valuable of own pieces first
					// The capturedPieceValueMultiplier is used to make even 'bad' captures like QxP rank above non-captures
					score += 10000;
				}
				if (startPieceType == 1)
				{
					if (board.RedCoveredSquare[toSquare] == 0)
						score += 20000;
				}
				else
				{
					if (board.BlueCoveredSquare[toSquare] == 0)
						score += 20000;
				}


				moveScores[i] = score;
			}
		}
		else {
			for (int i = 0; i < moves.Count; i++)
			{
				float score = 0;
				int startSquare = moves[i].StartSquare;
				int toSquare = moves[i].TargetSquare;
				int capturePieceType = board.Square[toSquare];
				int startPieceType = board.Square[startSquare];

				if (capturePieceType != 0)
				{
					score = 1;
				}
				if (startPieceType == 1)
				{
					/*
					float startDist1 = (startSquare / board.height) - board.BlueAverageX;
					float startDist2 = startDist1 * startDist1;

					float toDist1 = (toSquare / board.height) - board.BlueAverageX;
					float toDist2 = toDist1 * toDist1;
					float distDif = (toDist2 - startDist2) * 0.01f;
					score += distDif;
					*/
					if (board.RedCoveredSquare[toSquare] == 0)
						score += 2;
					score -= board.BluePositionVals[startSquare];
					score += board.BluePositionVals[toSquare];
				}
				else {
					/*
					float startDist1 = (startSquare / board.height) - board.RedAverageX;
					float startDist2 = startDist1 * startDist1;

					float toDist1 = (toSquare / board.height) - board.RedAverageX;
					float toDist2 = toDist1 * toDist1;
					float distDif = (toDist2 - startDist2) * 0.01f;
					score += distDif;
					*/
					if (board.BlueCoveredSquare[toSquare] == 0)
						score += 2;
					score -= board.RedPositionVals[startSquare];
					score += board.RedPositionVals[toSquare];
				}


				moveScores[i] = score;
			}
		}

		Sort(moves);
	}

	public void Sort(List<Move> moves)
	{
		// Sort the moves list based on scores
		for (int i = 0; i < moves.Count - 1; i++)
		{
			for (int j = i + 1; j > 0; j--)
			{
				int swapIndex = j - 1;
				if (moveScores[swapIndex] < moveScores[j])
				{
					(moves[j], moves[swapIndex]) = (moves[swapIndex], moves[j]);
					(moveScores[j], moveScores[swapIndex]) = (moveScores[swapIndex], moveScores[j]);
				}
			}
		}
	}
}
