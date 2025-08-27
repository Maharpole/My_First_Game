using UnityEngine;
using UnityEngine.UI;

/// Resizes GridLayoutGroup cellSize so slots remain square and fit the parent window.
/// Attach to the RectTransform that has the GridLayoutGroup (your inventory grid).
public class ResponsiveSquareGrid : MonoBehaviour
{
    public GridLayoutGroup grid;
    [Tooltip("Number of columns to keep; rows flow automatically")] public int columns = 5;
    [Tooltip("Optional fixed rows (0 = auto)")] public int rows = 0;
    [Tooltip("Minimum cell size in pixels")] public float minCell = 24f;
    [Tooltip("Maximum cell size in pixels (0 = unlimited)")] public float maxCell = 0f;
    [Tooltip("Recalculate each frame (on if your window frequently resizes)")] public bool continuous = true;

    RectTransform _rt;
    Vector2 _lastSize;

    void Reset()
    {
        grid = GetComponent<GridLayoutGroup>();
    }

    void Awake()
    {
        if (grid == null) grid = GetComponent<GridLayoutGroup>();
        _rt = GetComponent<RectTransform>();
        Apply();
    }

    void OnEnable()
    {
        Apply();
    }

    void Update()
    {
        if (!continuous) return;
        if (_rt == null) return;
        var s = _rt.rect.size;
        if (s != _lastSize) Apply();
    }

    void OnRectTransformDimensionsChange()
    {
        if (!isActiveAndEnabled) return;
        Apply();
    }

    public void Apply()
    {
        if (grid == null || _rt == null) return;
        _lastSize = _rt.rect.size;

        // Configure constraint by columns
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = Mathf.Max(1, columns);

        // Available area after padding
        var pad = grid.padding;
        float availW = Mathf.Max(1f, _rt.rect.width - pad.left - pad.right);
        float availH = Mathf.Max(1f, _rt.rect.height - pad.top - pad.bottom);

        int cols = Mathf.Max(1, columns);
        float spacingX = grid.spacing.x;
        float spacingY = grid.spacing.y;

        // Compute width-limited cell
        float cellW = (availW - spacingX * (cols - 1)) / cols;

        // If rows specified, also constrain by height; else just make square using width
        float cell = cellW;
        if (rows > 0)
        {
            int r = Mathf.Max(1, rows);
            float cellH = (availH - spacingY * (r - 1)) / r;
            cell = Mathf.Min(cellW, cellH);
        }

        if (maxCell > 0f) cell = Mathf.Min(cell, maxCell);
        cell = Mathf.Max(minCell, cell);

        grid.cellSize = new Vector2(cell, cell);
    }
}


