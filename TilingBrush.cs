using UnityEngine;
using System;

#if UNITY_EDITOR
namespace UnityEditor.Tilemaps
{
    /// <summary>
    /// Helper class to pass state between TilingBrush and TilingBrushEditor.
    /// </summary>
    static class TilingBrushState
    {

        public static bool isExecuting = false;
        public static bool isHeld = false;
        public static bool isMoving = false;
        public static BoundsInt palettePosition;
        public static Vector3Int tileStartPosition;

    }

    /// <summary>
    /// Brush that extends the normal brush behavior to avoid drawing overlapping tiles. Instead, it tiles
    /// a group of tiles across grid cells.
    /// </summary>
    [CustomGridBrush(true, false, false, "Tiling Brush")]
    public class TilingBrush : GridBrush
    {
        /// <summary>
        /// Tiles selected tiles onto the selected layers without overlapping any tiles.
        /// </summary>
        /// <param name="gridLayout">Grid used for layout.</param>
        /// <param name="brushTarget">Target of the paint operation. By default the currently selected GameObject.</param>
        /// <param name="position">The coordinates of the cell to paint data to.</param>
        public override void Paint(GridLayout grid, GameObject brushTarget, Vector3Int position)
        {
            // Execute normal behavior if moving tiles
            if (TilingBrushState.isMoving)
            {
                base.Paint(grid, brushTarget, position);
                return;
            }

            // Mark the start position (if mouse was just clicked)
            if (!TilingBrushState.isHeld) TilingBrushState.tileStartPosition = position;

            // Get difference between current position and the first tile laid down
            var diff = position - TilingBrushState.tileStartPosition;

            // Compute possible nearest coordinates that tile well with start position
            var offsetX1 = diff.x % TilingBrushState.palettePosition.size.x;
            var offsetX2 = offsetX1 + (diff.x < 0 ? 1 : -1) * TilingBrushState.palettePosition.size.x;
            var offsetY1 = diff.y % TilingBrushState.palettePosition.size.y;
            var offsetY2 = offsetY1 + (diff.y < 0 ? 1 : -1) * TilingBrushState.palettePosition.size.y;

            //  Find the mimium of coordinates (of x and y)
            var offsetX = Math.Abs(offsetX1) <= Math.Abs(offsetX2) ? offsetX1 : offsetX2;
            var offsetY = Math.Abs(offsetY1) <= Math.Abs(offsetY2) ? offsetY1 : offsetY2;


            // Only draw if current position is 1/2 of the tiles size of the dimension is greater than 2, or exact size otherwise
            var threshX = TilingBrushState.palettePosition.size.x > 2 ? TilingBrushState.palettePosition.size.x / 2 : 0;
            var threshY = TilingBrushState.palettePosition.size.y > 2 ? TilingBrushState.palettePosition.size.y / 2 : 0;
            if (Math.Abs(offsetX) <= threshX && Math.Abs(offsetY) <= threshY)
            {
                // Update position
                position.x -= offsetX;
                position.y -= offsetY;
                base.Paint(grid, brushTarget, position);
            }
        }

        /// <summary>
        /// Picks tiles from selected Tilemaps and child GameObjects, given the coordinates of the cells.
        /// The TilingBrush records the current bounds for use when painting.
        /// </summary>
        /// <param name="grid">Grid to pick data from.</param>
        /// <param name="brushTarget">Target of the picking operation. By default the currently selected GameObject.</param>
        /// <param name="position">The coordinates of the cells to paint data from.</param>
        /// <param name="pickStart">Pivot of the picking brush.</param>
        public override void Pick(GridLayout grid, GameObject brushTarget, BoundsInt position, Vector3Int pickStart)
        {
            // Execute normal behavior
            base.Pick(grid, brushTarget, position, pickStart);

            // Record bounds (size of selection)
            TilingBrushState.palettePosition = position;
        }

        /// <summary>
        /// Starts the movement of tiles and GameObjects from a given position within the selected layers.
        /// </summary>
        /// <param name="grid">Grid used for layout.</param>
        /// <param name="brushTarget">Target of the Move operation. By default the currently selected GameObject.</param>
        /// <param name="position">The coordinates of the cell to move data from.</param>
        public override void MoveStart(GridLayout grid, GameObject brushTarget, BoundsInt position)
        {
            // Execute normal behavior
            base.MoveStart(grid, brushTarget, position);

            // detect movement
            TilingBrushState.isMoving = true;
        }

        /// <summary>
        /// Ends the movement of tiles and GameObjects to a given position within the selected layers.
        /// </summary>
        /// <param name="grid">Grid used for layout.</param>
        /// <param name="brushTarget">Target of the Move operation. By default the currently selected GameObject.</param>
        /// <param name="position">The coordinates of the cell to move data to.</param>
        public override void MoveEnd(GridLayout grid, GameObject brushTarget, BoundsInt position)
        {
            // Execute normal behavior
            base.MoveEnd(grid, brushTarget, position);

            // detect movement
            TilingBrushState.isMoving = false;
        }
    }

    /// <summary>
    /// The Brush Editor for a TilingBrush.
    /// </summary>
    [CustomEditor(typeof(TilingBrush))]
    public class TilingBrushEditor : GridBrushEditor
    {
        /// <summary>
        /// Callback for painting the GUI for the GridBrush in the Scene View.
        /// The TilingBrushEditor draws the current coordinates of the brush to
        /// the screen and detects whether the brush is currently held or just
        /// started to execute.
        /// </summary>
        /// <param name="grid">Grid that the brush is being used on.</param>
        /// <param name="brushTarget">Target of the GridBrushBase::ref::Tool operation. By default the currently selected GameObject.</param>
        /// <param name="position">Current selected location of the brush.</param>
        /// <param name="tool">Current GridBrushBase::ref::Tool selected.</param>
        /// <param name="executing">Whether brush is being used.</param>
        public override void OnPaintSceneGUI(GridLayout grid, GameObject brushTarget, BoundsInt position, GridBrushBase.Tool tool, bool executing)
        {
            // Execute normal behavior
            base.OnPaintSceneGUI(grid, brushTarget, position, tool, executing);

            // Draw coordinates
            var labelText = "Pos: " + position.position;
            if (position.size.x > 1 || position.size.y > 1) labelText += " Size: " + position.size;
            Handles.Label(grid.CellToWorld(position.position), labelText);

            // Record the execution state of the brush (executing == currently drawing to tilemap)
            // held iif previously executing and currently executing
            TilingBrushState.isHeld = executing && TilingBrushState.isExecuting;
            TilingBrushState.isExecuting = executing;
        }
    }
}
#endif
