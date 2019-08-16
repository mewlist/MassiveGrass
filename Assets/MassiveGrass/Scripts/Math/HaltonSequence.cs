public static class HaltonSequence
{
    public static float[] result2;
    public static float[] result3;
    public static float Base2(int index)
    {
        if (result2 == null)
        {
            if (result2 == null) result2 = new float[100000];
            for (uint i = 0; i < 100000; i++)
            {
                uint a = FlipBits32(i) >> 1;
                float max = int.MaxValue;
                result2[i] = (float)a / max;
            }
        }
        return result2[index];
    }

    public static float Base3(int index)
    {
        if (result3 == null)
        {
            if (result3 == null) result3 = new float[100000];
            for (uint i = 0; i < 100000; i++)
            {
                float r = 0;
                uint x = i;
                float b = 3;
                while (x > 0)
                {
                    var mod = x % 3;
                    x /= 3;
                    r += mod / b;
                    b *= 3;
                }
                result3[i] = r;
            }
        }
        return result3[index];
    }

    // ビット反転
    public static uint FlipBits32(uint a)
    {
        uint b = a;
        a = a & 0x55555555;
        b = b ^ a;
        a = (a << 1) | (b >> 1);

        b = a;
        a = a & 0x33333333;
        b = b ^ a;
        a = (a << 2) | (b >> 2);

        b = a;
        a = a & 0x0f0f0f0f;
        b = b ^ a;
        a = (a << 4) | (b >> 4);

        b = a;
        a = a & 0x00ff00ff;
        b = b ^ a;
        a = (a << 8) | (b >> 8);

        b = a;
        a = a & 0x0000ffff;
        b = b ^ a;
        return (a << 16) | (b >> 16);
    }
}
