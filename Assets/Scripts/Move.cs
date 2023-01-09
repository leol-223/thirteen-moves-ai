using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Move
{
    readonly ushort moveValue;

    const ushort startSquareMask = 0b0000000011111111;
    const ushort targetSquareMask = 0b1111111100000000;

    public Move(int startSquare, int targetSquare) {
        moveValue = (ushort)(startSquare | targetSquare << 8);
    }

    public int StartSquare
    {
        get
        {
            return moveValue & startSquareMask;
        }
    }

    public int TargetSquare
    {
        get
        {
            return (moveValue & targetSquareMask) >> 8;
        }
    }

    public ushort Value
    {
        get
        {
            return moveValue;
        }
    }
}
