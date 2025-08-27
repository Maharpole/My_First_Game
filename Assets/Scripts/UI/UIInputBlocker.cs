using UnityEngine;

/// Global flag to indicate when UI is actively capturing pointer for dragging/interaction
public static class UIInputBlocker
{
    private static int _blockCount;
    public static bool IsPointerBlocking => _blockCount > 0;

    public static void PushBlock()
    {
        _blockCount++;
    }

    public static void PopBlock()
    {
        _blockCount = Mathf.Max(0, _blockCount - 1);
    }

    public static void Clear()
    {
        _blockCount = 0;
    }
}


