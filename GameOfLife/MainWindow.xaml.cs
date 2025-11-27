using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace GameOfLife
{
    public partial class MainWindow : Window
    {

        private HashSet<Point> liveCells = new HashSet<Point>();

        // Grid-Einstellungen
        private int GridWidth;
        private int GridHeight;
        private double cellSize; 
        private Point _centerCell;

        private bool isDarkMode = true;
        private readonly object _gridLock = new object();
        private int brushRadius = 0;
        private readonly List<Point> hoverCells = new List<Point>();
        private Point? hoverCenterCell = null;
        private readonly List<Rectangle> hoverVisuals = new List<Rectangle>();
        private readonly GeometryGroup liveCellGeometryGroup = new GeometryGroup();
        private readonly Dictionary<Point, RectangleGeometry> liveCellGeometries = new Dictionary<Point, RectangleGeometry>();
        private readonly Path liveCellPath = new Path { IsHitTestVisible = false };
        private readonly DispatcherTimer zoomRedrawTimer;
        private bool zoomRedrawPending = false;

        private void ApplyTheme()
        {
            if (isDarkMode)
            {
                Resources["WindowBackgroundColor"] = Resources["DarkWindowBackgroundColor"];
                Resources["GridLineColor"] = Resources["DarkGridLineColor"];
                Resources["DeadCellColor"] = Resources["DarkDeadCellColor"];
                Resources["LiveCellColor"] = Resources["DarkLiveCellColor"];
                Resources["ToolbarColor"] = Resources["DarkToolbarColor"];
            }
            else
            {
                Resources["WindowBackgroundColor"] = Resources["LightWindowBackgroundColor"];
                Resources["GridLineColor"] = Resources["LightGridLineColor"];
                Resources["DeadCellColor"] = Resources["LightDeadCellColor"];
                Resources["LiveCellColor"] = Resources["LightLiveCellColor"];
                Resources["ToolbarColor"] = Resources["LightToolbarColor"];
            }

            // Update existing UI elements
            this.Background = (Brush)Resources["WindowBackgroundColor"];
            GameCanvas.Background = (Brush)Resources["DeadCellColor"];
            DrawCells();
        }

        private void ToggleTheme_Click(object sender, RoutedEventArgs e)
        {
            isDarkMode = !isDarkMode;
            ApplyTheme();
        }

        private void DrawCells()
        {
            lock (_gridLock)
            {
                if (double.IsNaN(GameCanvas.Width) || double.IsNaN(GameCanvas.Height) || GridWidth <= 0 || GridHeight <= 0)
                {
                    return;
                }

                GameCanvas.Children.Clear();
                liveCellPath.Fill = (Brush)Resources["LiveCellColor"];
                liveCellGeometryGroup.Children.Clear();
                liveCellGeometries.Clear();
                hoverVisuals.Clear();

                int startX = 0;
                int startY = 0;
                int endX = GridWidth - 1;
                int endY = GridHeight - 1;

                DrawGrid(startX, startY, endX, endY);
                liveCellPath.Data = liveCellGeometryGroup;
                GameCanvas.Children.Add(liveCellPath);
                DrawLiveCells(startX, startY, endX, endY);
                DrawCenterCellOutline(); // Draw the center cell highlight
                DrawHoverPreview();
            }
        }

        private void DrawGrid(int startX, int startY, int endX, int endY)
        {
            var gridBrush = (Brush)Resources["GridLineColor"];

            // Draw vertical lines
            for (int x = startX; x <= endX + 1; x++)
            {
                var line = new Line
                {
                    X1 = x * cellSize,
                    Y1 = 0,
                    X2 = x * cellSize,
                    Y2 = GameCanvas.Height,
                    Stroke = gridBrush,
                    StrokeThickness = 0.5
                };
                GameCanvas.Children.Add(line);
            }

            // Draw horizontal lines
            for (int y = startY; y <= endY + 1; y++)
            {
                var line = new Line
                {
                    X1 = 0,
                    Y1 = y * cellSize,
                    X2 = GameCanvas.Width,
                    Y2 = y * cellSize,
                    Stroke = gridBrush,
                    StrokeThickness = 0.5
                };
                GameCanvas.Children.Add(line);
            }
        }

        private void DrawLiveCells(int startX, int startY, int endX, int endY)
        {
            foreach (var cell in liveCells)
            {
                int x = (int)cell.X;
                int y = (int)cell.Y;

                if (x >= startX && x <= endX && y >= startY && y <= endY)
                {
                    AddLiveCellVisual(new Point(x, y));
                }
            }
        }

        private void DrawCenterCellOutline()
        {
            if (_centerCell == null) return;

            var outline = new Rectangle
            {
                Width = cellSize,
                Height = cellSize,
                Stroke = Brushes.Red,
                StrokeThickness = 1,
                Fill = Brushes.Transparent
            };

            Canvas.SetLeft(outline, _centerCell.X * cellSize);
            Canvas.SetTop(outline, _centerCell.Y * cellSize);
            GameCanvas.Children.Add(outline);
        }

        private void DrawHoverPreview()
        {
            int required = hoverCells.Count;

            for (int i = 0; i < required; i++)
            {
                Rectangle outline;
                if (i < hoverVisuals.Count)
                {
                    outline = hoverVisuals[i];
                }
                else
                {
                    outline = new Rectangle
                    {
                        Stroke = Brushes.Red,
                        StrokeThickness = 1,
                        Fill = Brushes.Transparent,
                        IsHitTestVisible = false
                    };
                    hoverVisuals.Add(outline);
                    GameCanvas.Children.Add(outline);
                }

                outline.Width = cellSize;
                outline.Height = cellSize;
                outline.Visibility = Visibility.Visible;

                var cell = hoverCells[i];
                Canvas.SetLeft(outline, cell.X * cellSize);
                Canvas.SetTop(outline, cell.Y * cellSize);
            }

            for (int i = required; i < hoverVisuals.Count; i++)
            {
                hoverVisuals[i].Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateHoverCells(Point center)
        {
            hoverCells.Clear();

            int centerX = (int)center.X;
            int centerY = (int)center.Y;

            if (!IsCellWithinGrid(centerX, centerY))
            {
                hoverCenterCell = null;
                return;
            }

            hoverCenterCell = new Point(centerX, centerY);

            foreach (var cell in EnumerateBrushCells(hoverCenterCell.Value))
            {
                hoverCells.Add(cell);
            }
        }

        private void ClearHoverPreview()
        {
            if (hoverCells.Count == 0 && !hoverCenterCell.HasValue && hoverVisuals.Count == 0)
            {
                return;
            }

            hoverCells.Clear();
            hoverCenterCell = null;
            foreach (var outline in hoverVisuals)
            {
                outline.Visibility = Visibility.Collapsed;
            }
        }

        private IEnumerable<Point> EnumerateBrushCells(Point center)
        {
            int baseX = (int)center.X;
            int baseY = (int)center.Y;

            for (int dx = -brushRadius; dx <= brushRadius; dx++)
            {
                for (int dy = -brushRadius; dy <= brushRadius; dy++)
                {
                    int x = baseX + dx;
                    int y = baseY + dy;

                    if (IsCellWithinGrid(x, y))
                    {
                        yield return new Point(x, y);
                    }
                }
            }
        }

        private bool TryGetCellFromMouse(Point mousePosition, out Point cell)
        {
            cell = new Point(-1, -1);

            if (GridWidth <= 0 || GridHeight <= 0 || cellSize <= 0)
            {
                return false;
            }

            if (double.IsNaN(mousePosition.X) || double.IsNaN(mousePosition.Y))
            {
                return false;
            }

            if (mousePosition.X < 0 || mousePosition.Y < 0)
            {
                return false;
            }

            int cellX = (int)(mousePosition.X / cellSize);
            int cellY = (int)(mousePosition.Y / cellSize);

            if (!IsCellWithinGrid(cellX, cellY))
            {
                return false;
            }

            cell = new Point(cellX, cellY);
            return true;
        }

        private bool IsCellWithinGrid(int x, int y)
        {
            return x >= 0 && x < GridWidth && y >= 0 && y < GridHeight;
        }

        private void ApplyBrush(Point center, bool add)
        {
            foreach (var cell in EnumerateBrushCells(center))
            {
                UpdateCell(cell, add);
            }
        }

        private void AddLiveCellVisual(Point cell)
        {
            if (liveCellGeometries.ContainsKey(cell))
            {
                return;
            }

            var rectGeometry = new RectangleGeometry(new Rect(cell.X * cellSize, cell.Y * cellSize, cellSize, cellSize));
            liveCellGeometries[cell] = rectGeometry;
            liveCellGeometryGroup.Children.Add(rectGeometry);
        }

        private void RemoveLiveCellVisual(Point cell)
        {
            if (liveCellGeometries.TryGetValue(cell, out var geometry))
            {
                liveCellGeometryGroup.Children.Remove(geometry);
                liveCellGeometries.Remove(cell);
            }
        }







        public MainWindow()
        {
            InitializeComponent();
            ApplyTheme();

            zoomRedrawTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            zoomRedrawTimer.Tick += ZoomRedrawTimer_Tick;

            this.Loaded += MainWindow_Loaded;
            CanvasContainer.SizeChanged += CanvasContainer_SizeChanged;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateCellSizeFromSlider();
            if (BrushSizeSlider != null)
            {
                brushRadius = (int)Math.Round(BrushSizeSlider.Value);
            }
            UpdateGridDimensions();
            DrawCells();
        }

        private void CanvasContainer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            lock (_gridLock)
            {
                Point oldCenter = _centerCell;

                UpdateGridDimensions(); // This calculates the new center

                Point newCenter = _centerCell;
                double shiftX = newCenter.X - oldCenter.X;
                double shiftY = newCenter.Y - oldCenter.Y;

                // No need to shift if the center hasn't moved
                if (shiftX == 0 && shiftY == 0)
                {
                    DrawCells();
                    return;
                }

                var newLiveCells = new HashSet<Point>();
                foreach (var cell in liveCells)
                {
                    var newPoint = new Point(cell.X + shiftX, cell.Y + shiftY);
                    if (newPoint.X >= 0 && newPoint.X < GridWidth && newPoint.Y >= 0 && newPoint.Y < GridHeight)
                    {
                        newLiveCells.Add(newPoint);
                    }
                }
                liveCells = newLiveCells;

                DrawCells();
            }
        }

        // Simulation Buttons
        private void Start_Click(object sender, RoutedEventArgs e) { }
        private void StopPause_Click(object sender, RoutedEventArgs e) { }
        private void Clear_Click(object sender, RoutedEventArgs e) { 
        foreach (var cell in liveCells.ToList())
            {
                liveCells.Remove(cell);
            }
            DrawCells();

        }

        // DrawMode
        private void DrawModeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PrefabPicker == null) return;  // <- schützt vor NullReferenceException
            PrefabPicker.IsEnabled = DrawModeSelector.SelectedIndex == 1;
        }


        // Prefab Auswahl
        private void PrefabPicker_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

        // Preview
        private void Preview_Click(object sender, RoutedEventArgs e)
        {
            PrefabPreviewWindow preview = new PrefabPreviewWindow();
            preview.Show();
        }

        // Zoom
        private void UpdateCellSizeFromSlider()
        {
            if (ZoomSlider == null) return;
            cellSize = (ZoomSlider.Maximum + ZoomSlider.Minimum) - ZoomSlider.Value;
        }

        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (GameCanvas == null) return; // Prevent crash on initialization

            UpdateCellSizeFromSlider();

            zoomRedrawPending = true;
            zoomRedrawTimer.Stop();
            zoomRedrawTimer.Start();
        }

        private void ZoomRedrawTimer_Tick(object? sender, EventArgs e)
        {
            zoomRedrawTimer.Stop();

            if (!zoomRedrawPending)
            {
                return;
            }

            zoomRedrawPending = false;

            lock (_gridLock)
            {
                Point oldCenter = _centerCell;

                UpdateGridDimensions(); // This now calculates the new center cell

                Point newCenter = _centerCell;
                double shiftX = newCenter.X - oldCenter.X;
                double shiftY = newCenter.Y - oldCenter.Y;

                if (shiftX != 0 || shiftY != 0)
                {
                    var newLiveCells = new HashSet<Point>();
                    foreach (var cell in liveCells)
                    {
                        var newPoint = new Point(cell.X + shiftX, cell.Y + shiftY);
                        if (newPoint.X >= 0 && newPoint.X < GridWidth && newPoint.Y >= 0 && newPoint.Y < GridHeight)
                        {
                            newLiveCells.Add(newPoint);
                        }
                    }
                    liveCells = newLiveCells;
                }

                DrawCells();
            }
        }

        private void BrushSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            brushRadius = (int)Math.Round(e.NewValue);
            if (hoverCenterCell.HasValue)
            {
                UpdateHoverCells(hoverCenterCell.Value);
            }
            DrawHoverPreview();
        }


        private void UpdateGridDimensions()
        {
            if (CanvasContainer == null || GameCanvas == null || CanvasContainer.ActualWidth <= 0 || CanvasContainer.ActualHeight <= 0) return;
            if (cellSize <= 0) return;

            // Calculate how many cells fit and ensure the number is odd
            GridWidth = (int)Math.Floor(CanvasContainer.ActualWidth / cellSize);
            if (GridWidth % 2 == 0) GridWidth--; // Make it odd

            GridHeight = (int)Math.Floor(CanvasContainer.ActualHeight / cellSize);
            if (GridHeight % 2 == 0) GridHeight--; // Make it odd

            // Update the center cell coordinate
            _centerCell = new Point(GridWidth / 2, GridHeight / 2);

            // Set the canvas size to be a multiple of the cell size
            GameCanvas.Width = GridWidth * cellSize;
            GameCanvas.Height = GridHeight * cellSize;

            // Center the canvas within the container
            double offsetX = (CanvasContainer.ActualWidth - GameCanvas.Width) / 2;
            double offsetY = (CanvasContainer.ActualHeight - GameCanvas.Height) / 2;
            GameCanvas.Margin = new Thickness(offsetX, offsetY, 0, 0);
        }

        // Canvas Interaktion
        private Point? lastCell = null;


        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (GameCanvas == null || cellSize <= 0) return;

            Point mousePos = e.GetPosition(GameCanvas);
            if (!TryGetCellFromMouse(mousePos, out Point currentCell))
            {
                ClearHoverPreview();
                lastCell = null;
                return;
            }

            UpdateHoverCells(currentCell);

            bool leftPressed = e.LeftButton == MouseButtonState.Pressed;
            bool rightPressed = e.RightButton == MouseButtonState.Pressed;

            if (leftPressed)
            {
                AddCellsBetween(lastCell, currentCell, true);
            }
            else if (rightPressed)
            {
                AddCellsBetween(lastCell, currentCell, false);
            }

            DrawHoverPreview();
            lastCell = currentCell;
        }

        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            lastCell = null;
        }

        // Funktion, um alle Zellen zwischen zwei Punkten zu bearbeiten (Bresenham-Linie)
        private void AddCellsBetween(Point? start, Point end, bool add)
        {
            if (start == null)
            {
                ApplyBrush(end, add);
                return;
            }

            int x0 = (int)start.Value.X;
            int y0 = (int)start.Value.Y;
            int x1 = (int)end.X;
            int y1 = (int)end.Y;

            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                ApplyBrush(new Point(x0, y0), add);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
            }
        }

        private bool UpdateCell(Point cell, bool add)
        {
            int cellX = (int)cell.X;
            int cellY = (int)cell.Y;

            if (!IsCellWithinGrid(cellX, cellY))
            {
                return false;
            }

            cell = new Point(cellX, cellY);

            if (add)
            {
                if (liveCells.Add(cell))
                {
                    AddLiveCellVisual(cell);
                    return true;
                }
            }
            else
            {
                if (liveCells.Remove(cell))
                {
                    RemoveLiveCellVisual(cell);
                    return true;
                }
            }

            return false;
        }


        private void Canvas_LeftUp(object sender, MouseButtonEventArgs e) { }
        private void Canvas_RightUp(object sender, MouseButtonEventArgs e) { }
        
        private void Canvas_MouseLeave(object sender, MouseEventArgs e)
        {
            ClearHoverPreview();
            lastCell = null;
        }
        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (GameCanvas == null || cellSize <= 0) return;

            Point mousePos = e.GetPosition(GameCanvas);
            if (!TryGetCellFromMouse(mousePos, out Point cell))
            {
                return;
            }

            if (e.ChangedButton == MouseButton.Left)
            {
                ApplyBrush(cell, true);
            }
            else if (e.ChangedButton == MouseButton.Right)
            {
                ApplyBrush(cell, false);
            }
            else
            {
                return;
            }

            lastCell = cell;
            UpdateHoverCells(cell);
            DrawHoverPreview();
        }
    }
}
