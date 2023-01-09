using System.Collections.Generic;

public class TranspositionTable
{
	public const int Exact = 0;
	public const int LowerBound = 1;
	public const int UpperBound = 2;

	public Dictionary<ulong, Entry> entries;
	public bool enabled = true;
	public Entry lookupFailed = new Entry(-1, -1, 1, new Move(0, 0));

	public TranspositionTable()
	{
		entries = new Dictionary<ulong, Entry>();
	}

	public Entry Lookup(ulong key)
	{
		if (!enabled)
			return lookupFailed;
		if (entries.ContainsKey(key))
			return entries[key];
		return lookupFailed;
	}

	public void Store(ulong key, int depth, float eval, int flag, Move move)
	{
		if (!enabled)
		{
			return;
		}
		entries[key] = new Entry(eval, depth, (byte)flag, move);
	}
	public struct Entry
	{

		public readonly float value;
		public readonly Move move;
		public readonly float depth;
		public readonly byte flag;

		//	public readonly byte gamePly;

		public Entry(float value, float depth, byte flag, Move move)
		{
			this.value = value;
			this.depth = depth;
			this.flag = flag;
			this.move = move;
		}
	}
}