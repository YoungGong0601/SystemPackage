using System.Collections.Generic;

public static class RandomSeed
{
    public static int Seed {get; private set;}= 0;

    public static void SetSeed(int seed)
    {
        Seed = seed;
    }

    public static int Random(int min, int max, int? seed = null)
    {
        System.Random rand = new System.Random(seed ?? Seed);
        return rand.Next(min, max);
    }
    public static List<T> Shuffle<T>(List<T> list, int? seed = null)
    {
        System.Random rand = new System.Random(seed ?? Seed);

        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rand.Next(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
        return list;
    }
}