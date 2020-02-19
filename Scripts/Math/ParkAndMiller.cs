using System.Collections.Generic;

public class ParkAndMiller
{
    private static List<int> cache;

    public static void Warmup()
    {
        cache = new List<int>();
        for (var i = cache.Count; i <= 10000; i++)
            cache.Add(i == 0 ? 1 : Next(cache[i - 1]));
    }
    
    
    public static float Get(int index)
    {
        return cache[index] % 10000 / 10000f;
    }

    private static int Next(int prev)
    {
        return (48271 * prev) % 2147483647;
    }
}