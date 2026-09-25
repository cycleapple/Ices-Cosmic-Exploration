using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICE.Utilities;

public class RandomUtil
{
    private static Random random = new Random();

    // Constructor with seed for reproducible results
    public static void RandomPointGenerator(int seed)
    {
        random = new Random(seed);
    }

    public static Vector3 GetRandomPointInBounds(Vector3 corner1, Vector3 corner2, Vector3 corner3, Vector3 corner4, float fixedY)
    {
        // Generate two random values between 0 and 1
        float u = (float)random.NextDouble();
        float v = (float)random.NextDouble();

        Vector3 topEdge = Vector3.Lerp(corner1, corner2, u);
        Vector3 bottomEdge = Vector3.Lerp(corner4, corner3, u);
        Vector3 result = Vector3.Lerp(topEdge, bottomEdge, v);

        // Override Y with fixed value
        result.Y = fixedY;

        return result;
    }
}
