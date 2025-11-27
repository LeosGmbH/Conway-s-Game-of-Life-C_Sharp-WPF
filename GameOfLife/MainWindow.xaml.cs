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
        private readonly Path liveCellPath = new Path { IsHitTestVisible = false };
        private readonly Path liveCellOutlinePath = new Path { IsHitTestVisible = false };
        private readonly Path gridPath = new Path { IsHitTestVisible = false };
        private StreamGeometry liveCellsGeometry = new StreamGeometry();
        private StreamGeometry gridGeometry = new StreamGeometry();
        private bool gridNeedsRedraw = true;
        private bool liveCellsDirty = true;
        private readonly DispatcherTimer zoomRedrawTimer;
        private readonly DispatcherTimer simulationTimer;
        private bool zoomRedrawPending = false;
        private const string DefaultPrefabButtonContent = "Select Prefab";
        private string? _selectedPrefabName;
        private bool[,]? _selectedPrefabCells;
        private int _selectedPrefabWidth;
        private int _selectedPrefabHeight;
        private readonly List<(int X, int Y)> _selectedPrefabOffsets = new();

        private bool IsPrefabPlacementMode => DrawModeSelector != null && DrawModeSelector.SelectedIndex == 1 && _selectedPrefabOffsets.Count > 0;
        private bool simulationRunning = false;
        private TimeSpan simulationInterval = TimeSpan.FromMilliseconds(200);
        private static readonly TimeSpan[] SimulationSpeedSteps = new[]
        {
            TimeSpan.FromMilliseconds(600),
            TimeSpan.FromMilliseconds(400),
            TimeSpan.FromMilliseconds(200),
            TimeSpan.FromMilliseconds(120),
            TimeSpan.FromMilliseconds(60)
        };

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
                liveCellPath.Stroke = Brushes.Transparent;
                liveCellPath.StrokeThickness = 0;
                hoverVisuals.Clear();

                int startX = 0;
                int startY = 0;
                int endX = GridWidth - 1;
                int endY = GridHeight - 1;

                if (gridNeedsRedraw)
                {
                    RebuildGridGeometry(startX, startY, endX, endY);
                }

                gridPath.Stroke = (Brush)Resources["GridLineColor"];
                gridPath.Fill = Brushes.Transparent;
                gridPath.Data = gridGeometry;

                RenderLiveCellsGeometry();

                liveCellOutlinePath.Stroke = (Brush)Resources["DeadCellColor"];
                liveCellOutlinePath.Data = liveCellsGeometry;

                GameCanvas.Children.Add(liveCellPath);
                GameCanvas.Children.Add(gridPath);
                GameCanvas.Children.Add(liveCellOutlinePath);
                DrawCenterCellOutline();
                DrawHoverPreview();
            }
        }

        private void RebuildGridGeometry(int startX, int startY, int endX, int endY)
        {
            var geometry = new StreamGeometry();

            using (var ctx = geometry.Open())
            {
                double width = GameCanvas.Width;
                double height = GameCanvas.Height;

                for (int x = startX; x <= endX + 1; x++)
                {
                    double xCoord = x * cellSize;
                    ctx.BeginFigure(new Point(xCoord, 0), false, false);
                    ctx.LineTo(new Point(xCoord, height), true, false);
                }

                for (int y = startY; y <= endY + 1; y++)
                {
                    double yCoord = y * cellSize;
                    ctx.BeginFigure(new Point(0, yCoord), false, false);
                    ctx.LineTo(new Point(width, yCoord), true, false);
                }
            }

            geometry.Freeze();
            gridGeometry = geometry;
            gridNeedsRedraw = false;
        }

        private void RenderLiveCellsGeometry()
        {
            if (!liveCellsDirty)
            {
                liveCellPath.Data = liveCellsGeometry;
                liveCellOutlinePath.Data = liveCellsGeometry;
                return;
            }

            var geometry = new StreamGeometry();

            using (var ctx = geometry.Open())
            {
                foreach (var cell in liveCells)
                {
                    double x = cell.X * cellSize;
                    double y = cell.Y * cellSize;

                    ctx.BeginFigure(new Point(x, y), true, true);
                    ctx.LineTo(new Point(x + cellSize, y), true, false);
                    ctx.LineTo(new Point(x + cellSize, y + cellSize), true, false);
                    ctx.LineTo(new Point(x, y + cellSize), true, false);
                }
            }

            geometry.Freeze();
            liveCellsGeometry = geometry;
            liveCellPath.Data = liveCellsGeometry;
            liveCellOutlinePath.Data = liveCellsGeometry;
            liveCellsDirty = false;
        }

        private void RefreshLiveCellsVisual()
        {
            liveCellsDirty = true;
            RenderLiveCellsGeometry();
            liveCellPath.InvalidateVisual();
            liveCellOutlinePath.InvalidateVisual();
        }

        private void DrawCenterCellOutline()
        {
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

            foreach (var cell in EnumeratePlacementCells(hoverCenterCell.Value))
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

        private IEnumerable<Point> EnumeratePlacementCells(Point anchor)
        {
            if (IsPrefabPlacementMode && _selectedPrefabCells != null && _selectedPrefabOffsets.Count > 0)
            {
                int baseX = (int)anchor.X;
                int baseY = (int)anchor.Y;

                foreach (var (offsetX, offsetY) in _selectedPrefabOffsets)
                {
                    int x = baseX + offsetX;
                    int y = baseY + offsetY;
                    if (IsCellWithinGrid(x, y))
                    {
                        yield return new Point(x, y);
                    }
                }
                yield break;
            }

            foreach (var cell in EnumerateBrushCells(anchor))
            {
                yield return cell;
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

        private bool ApplyBrush(Point center, bool add)
        {
            bool changed = false;
            foreach (var cell in EnumeratePlacementCells(center))
            {
                if (UpdateCell(cell, add))
                {
                    changed = true;
                }
            }
            return changed;
        }

        public MainWindow()
        {
            InitializeComponent();
            ApplyTheme();

            liveCellPath.StrokeThickness = 0.5;
            gridPath.StrokeThickness = 0.5;
            liveCellOutlinePath.StrokeThickness = 0.5;
            liveCellOutlinePath.Fill = Brushes.Transparent;

        
            zoomRedrawTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            zoomRedrawTimer.Tick += ZoomRedrawTimer_Tick;

            simulationTimer = new DispatcherTimer
            {
                Interval = simulationInterval
            };
            simulationTimer.Tick += SimulationTimer_Tick;

            ApplySimulationSpeedFromSlider();
            UpdateSimulationButton();

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
            ApplySimulationSpeedFromSlider();
            UpdateSimulationButton();
            UpdateGridDimensions();
            liveCellsDirty = true;
            gridNeedsRedraw = true;
            DrawCells();
        }

        private void CanvasContainer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            lock (_gridLock)
            {
                Point oldCenter = _centerCell;

                UpdateGridDimensions(); // This calculates the new center
                liveCellsDirty = true;
                gridNeedsRedraw = true;

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
        private void ToggleSimulationButton_Click(object sender, RoutedEventArgs e)
        {
            if (simulationRunning)
            {
                StopSimulation();
            }
            else
            {
                StartSimulation();
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            StopSimulation();

            if (liveCells.Count > 0)
            {
                liveCells.Clear();
                liveCellsDirty = true;
            }
            DrawCells();

        }

        private void StartSimulation()
        {
            if (simulationRunning)
            {
                UpdateSimulationButton();
                return;
            }

            simulationRunning = true;
            simulationTimer.Interval = simulationInterval;
            simulationTimer.Start();
            UpdateSimulationButton();
        }

        private void StopSimulation()
        {
            if (!simulationRunning)
            {
                UpdateSimulationButton();
                return;
            }

            simulationRunning = false;
            simulationTimer.Stop();
            UpdateSimulationButton();
        }

        private void SimulationTimer_Tick(object? sender, EventArgs e)
        {
            AdvanceSimulation();
        }

        private void AdvanceSimulation()
        {
            lock (_gridLock)
            {
                if (GridWidth <= 0 || GridHeight <= 0)
                {
                    return;
                }

                var neighborCounts = new Dictionary<Point, int>();

                foreach (var cell in liveCells)
                {
                    int cellX = (int)cell.X;
                    int cellY = (int)cell.Y;

                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            if (dx == 0 && dy == 0)
                            {
                                continue;
                            }

                            int neighborX = cellX + dx;
                            int neighborY = cellY + dy;

                            if (!IsCellWithinGrid(neighborX, neighborY))
                            {
                                continue;
                            }

                            var neighbor = new Point(neighborX, neighborY);

                            if (neighborCounts.TryGetValue(neighbor, out int count))
                            {
                                neighborCounts[neighbor] = count + 1;
                            }
                            else
                            {
                                neighborCounts[neighbor] = 1;
                            }
                        }
                    }
                }

                var nextGeneration = new HashSet<Point>();

                foreach (var cell in liveCells)
                {
                    int liveNeighbors = neighborCounts.TryGetValue(cell, out int count) ? count : 0;
                    if (liveNeighbors == 2 || liveNeighbors == 3)
                    {
                        nextGeneration.Add(cell);
                    }
                }

                foreach (var kvp in neighborCounts)
                {
                    if (!liveCells.Contains(kvp.Key) && kvp.Value == 3)
                    {
                        nextGeneration.Add(kvp.Key);
                    }
                }

                if (!liveCells.SetEquals(nextGeneration))
                {
                    liveCells = nextGeneration;
                    liveCellsDirty = true;
                    DrawCells();
                }
                else if (simulationRunning && liveCells.Count == 0)
                {
                    StopSimulation();
                }
            }
        }

        private void UpdateSimulationButton()
        {
            if (ToggleSimulationButton == null)
            {
                return;
            }

            if (simulationRunning)
            {
                ToggleSimulationButton.Content = "Stop";
                ToggleSimulationButton.Background = Brushes.Red;
            }
            else
            {
                ToggleSimulationButton.Content = "Start";
                ToggleSimulationButton.Background = Brushes.Green;
            }

            ToggleSimulationButton.Foreground = Brushes.White;
            ToggleSimulationButton.BorderBrush = Brushes.Transparent;
        }

        private void SpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ApplySimulationSpeedFromSlider();
        }

        private void ApplySimulationSpeedFromSlider()
        {
            if (SpeedSlider == null)
            {
                return;
            }

            int index = (int)Math.Round(SpeedSlider.Value);
            if (index < 0)
            {
                index = 0;
            }
            else if (index >= SimulationSpeedSteps.Length)
            {
                index = SimulationSpeedSteps.Length - 1;
            }

            simulationInterval = SimulationSpeedSteps[index];
            if (simulationRunning)
            {
                simulationTimer.Interval = simulationInterval;
            }
        }

        // DrawMode
        private void DrawModeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PrefabSelectorButton == null) return;  // <- schützt vor NullReferenceException
            PrefabSelectorButton.IsEnabled = DrawModeSelector.SelectedIndex == 1;
            ClearHoverPreview();
        }


        private void PrefabSelectorButton_Click(object sender, RoutedEventArgs e)
        {
            var previewWindow = new PrefabPreviewWindow
            {
                Owner = this
            };

            var result = previewWindow.ShowDialog();
            if (result == true && previewWindow.SelectedPattern != null)
            {
                _selectedPrefabName = previewWindow.SelectedPattern.Name;
                StoreSelectedPrefab(previewWindow.SelectedPattern.CellStates);
                UpdatePrefabButtonContent();
                ClearHoverPreview();
            }
        }

        private void StoreSelectedPrefab(bool[,]? cells)
        {
            if (cells == null)
            {
                _selectedPrefabCells = null;
                _selectedPrefabWidth = 0;
                _selectedPrefabHeight = 0;
                _selectedPrefabOffsets.Clear();
                return;
            }

            int width = cells.GetLength(0);
            int height = cells.GetLength(1);

            var clone = new bool[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    clone[x, y] = cells[x, y];
                }
            }

            _selectedPrefabCells = clone;
            _selectedPrefabWidth = width;
            _selectedPrefabHeight = height;

            _selectedPrefabOffsets.Clear();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (clone[x, y])
                    {
                        _selectedPrefabOffsets.Add((x, y));
                    }
                }
            }
        }

        private void UpdatePrefabButtonContent()
        {
            if (PrefabSelectorButton == null)
            {
                return;
            }

            PrefabSelectorButton.Content = string.IsNullOrWhiteSpace(_selectedPrefabName)
                ? DefaultPrefabButtonContent
                : _selectedPrefabName;
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
                liveCellsDirty = true;
            gridNeedsRedraw = true;

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
            liveCellsDirty = true;
            gridNeedsRedraw = true;

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

            bool changed = false;

            if (leftPressed)
            {
                changed = AddCellsBetween(lastCell, currentCell, true);
            }
            else if (rightPressed)
            {
                changed = AddCellsBetween(lastCell, currentCell, false);
            }

            if (changed)
            {
                RefreshLiveCellsVisual();
            }

            DrawHoverPreview();
            lastCell = currentCell;
        }

        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            lastCell = null;
        }

        // Funktion, um alle Zellen zwischen zwei Punkten zu bearbeiten (Bresenham-Linie)
        private bool AddCellsBetween(Point? start, Point end, bool add)
        {
            bool anyChange = false;
            if (start == null)
            {
                return ApplyBrush(end, add);
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
                if (ApplyBrush(new Point(x0, y0), add))
                {
                    anyChange = true;
                }

                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
            }
            return anyChange;
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
                    liveCellsDirty = true;
                    return true;
                }
            }
            else
            {
                if (liveCells.Remove(cell))
                {
                    liveCellsDirty = true;
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

            bool changed = false;
            if (e.ChangedButton == MouseButton.Left)
            {
                changed = ApplyBrush(cell, true);
            }
            else if (e.ChangedButton == MouseButton.Right)
            {
                changed = ApplyBrush(cell, false);
            }
            else
            {
                return;
            }

            lastCell = cell;
            UpdateHoverCells(cell);
            DrawHoverPreview();

            if (changed)
            {
                RefreshLiveCellsVisual();
            }
        }
    }
}
