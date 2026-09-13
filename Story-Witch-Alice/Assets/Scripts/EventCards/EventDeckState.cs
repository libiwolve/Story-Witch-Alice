using System;
using System.Collections.Generic;

// Pure runtime state: the generated queue is never changed during a round.
public sealed class EventDeckState
{
    public const int DeckSize = 20;
    public const int RoundLength = 12;
    public const int DebugOwnedCopies = 99;
    private readonly List<EventCardKind> queue = new List<EventCardKind>();
    private int[] counts = { 8, 4, 8 };
    public int Round { get; private set; }
    public int Tick { get; private set; }
    public int TotalTicks { get; private set; }
    public bool HasPendingCard { get; private set; }
    public bool NeedsDeck => Round == 0 || (Tick == RoundLength && !HasPendingCard);
    public IReadOnlyList<EventCardKind> Queue => queue.AsReadOnly();
    public int[] CopyCounts() => (int[])counts.Clone();

    public static bool IsValidDeck(int[] candidate)
    {
        if (candidate == null || candidate.Length != 3) return false;
        int total = 0;
        foreach (int count in candidate)
        {
            if (count < 0 || count > DebugOwnedCopies) return false;
            total += count;
        }
        return total == DeckSize;
    }

    public bool BeginRound(int[] candidate, Random random)
    {
        if (!NeedsDeck || !IsValidDeck(candidate) || random == null) return false;
        var pool = new List<EventCardKind>(DeckSize);
        for (int kind = 0; kind < candidate.Length; kind++)
            for (int copy = 0; copy < candidate[kind]; copy++) pool.Add((EventCardKind)kind);
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            EventCardKind value = pool[i]; pool[i] = pool[j]; pool[j] = value;
        }
        counts = (int[])candidate.Clone();
        queue.Clear();
        queue.AddRange(pool.GetRange(0, RoundLength));
        Tick = 0;
        Round++;
        return true;
    }

    public bool TryAdvance(out EventCardKind card)
    {
        card = default;
        if (Round == 0 || Tick >= RoundLength || HasPendingCard) return false;
        card = queue[Tick++];
        TotalTicks++;
        HasPendingCard = true;
        return true;
    }

    public bool Resolve()
    {
        if (!HasPendingCard) return false;
        HasPendingCard = false;
        return true;
    }
}
