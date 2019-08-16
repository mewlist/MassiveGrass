using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParkAndMiller
{
    private static List<int> cache;
    public static float Get(int index)
    {
        if (cache == null) cache = new List<int>();
        if (cache.Count <= index)
        {
            for (var i = cache.Count; i <= index; i++)
            {
                cache.Add(i == 0 ? 1 : Next(cache[i - 1]));
            }
        }
        return cache[index] % 10000 / 10000f;
    }

    private static int Next(int prev)
    {
        return (48271 * prev) % 2147483647;
    }
}
