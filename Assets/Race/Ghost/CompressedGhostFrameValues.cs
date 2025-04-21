using System;
using UnityEngine;

[Serializable]
public struct CompressedGhostFrameValues
{
    public int[] pos;
    //public float[] scale;
    //public uint animID;
    public uint zRot;
}

public struct GhostFrameValues
{
    public Vector2 pos;
    //public Vector2 scale;
    public float zRot;
    //public uint animID;
}

public static class GhostFrameConversions
{
    public const float Z_ROT_ACCURACY = 1000;
    public const float POS_ACCURACY = 1000;

    public static GhostFrameValues ToUncompressed(CompressedGhostFrameValues compressed)
    {
        return new GhostFrameValues
        {
            zRot = (((float)compressed.zRot - 360) / Z_ROT_ACCURACY),
            pos = new(compressed.pos[0] / POS_ACCURACY, compressed.pos[1] / POS_ACCURACY)
        };
    }

    public static CompressedGhostFrameValues ToCompressed(GhostFrameValues unCompressed)
    {
        return new CompressedGhostFrameValues
        {
            zRot = (uint)(unCompressed.zRot * Z_ROT_ACCURACY + 360),
            pos = new int[] { (int)(unCompressed.pos.x * POS_ACCURACY), (int)(unCompressed.pos.y * POS_ACCURACY) },
        };
    }
}
