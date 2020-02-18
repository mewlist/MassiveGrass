using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class ParkAndMiller
{
    private static Mutex objMutex = new Mutex();

    private static List<int> cache;
    public static float Get(int index)
    {
        objMutex.WaitOne();
        if (cache == null) cache = new List<int>();
        if (cache.Count <= index)
        {
            for (var i = cache.Count; i <= index; i++)
            {
                cache.Add(i == 0 ? 1 : Next(cache[i - 1]));
            }
        }
        objMutex.ReleaseMutex();
        return cache[index] % 10000 / 10000f;
    }

    public static void Clear()
    {
        objMutex.WaitOne();
        cache = null;
        objMutex.ReleaseMutex();
    }

    private static int Next(int prev)
    {
        return (48271 * prev) % 2147483647;
    }
}
