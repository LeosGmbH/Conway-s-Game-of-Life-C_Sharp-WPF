

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GameOfLife
{
    public partial class MainWindow : Window
    {

        private HashSet<Point> liveCells = new HashSet<Point>();

        // Grid-Einstellungen
        private int GridWidth;
        private int GridHeight;
        private double cellSize = 20; // Default cell size
        private Point _centerCell;

        private bool isDarkMode = true;
        private readonly object _gridLock = new object();

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

                int startX = 0;
                int startY = 0;
                int endX = GridWidth - 1;
                int endY = GridHeight - 1;

                DrawGrid(startX, startY, endX, endY);
                DrawLiveCells(startX, startY, endX, endY);
                DrawCenterCellOutline(); // Draw the center cell highlight
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
            var liveCellBrush = (Brush)Resources["LiveCellColor"];

            foreach (var cell in liveCells)
            {
                int x = (int)cell.X;
                int y = (int)cell.Y;

                if (x >= startX && x <= endX && y >= startY && y <= endY)
                {
                    var rect = new Rectangle
                    {
                        Width = cellSize,
                        Height = cellSize,
                        Fill = liveCellBrush
                    };
                    Canvas.SetLeft(rect, x * cellSize);
                    Canvas.SetTop(rect, y * cellSize);
                    GameCanvas.Children.Add(rect);
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







        public MainWindow()
        {
            InitializeComponent();
            ApplyTheme();

            this.Loaded += MainWindow_Loaded;
            CanvasContainer.SizeChanged += CanvasContainer_SizeChanged;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
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
        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (GameCanvas == null) return; // Prevent crash on initialization

            lock (_gridLock)
            {
                Point oldCenter = _centerCell;

                // Invert the slider logic as requested: Left (min value) = Large Cells, Right (max value) = Small Cells
                cellSize = (ZoomSlider.Maximum + ZoomSlider.Minimum) - e.NewValue;
                UpdateGridDimensions(); // This now calculates the new center cell

                Point newCenter = _centerCell;
                double shiftX = newCenter.X - oldCenter.X;
                double shiftY = newCenter.Y - oldCenter.Y;

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
            Point mousePos = e.GetPosition(GameCanvas);
            int cellX = (int)(mousePos.X / cellSize);
            int cellY = (int)(mousePos.Y / cellSize);
            Point currentCell = new Point(cellX, cellY);

            // Wenn die Maus nicht über eine neue Zelle bewegt wird, nichts tun
            if (lastCell.HasValue && lastCell.Value == currentCell)
                return;

            if (e.LeftButton == MouseButtonState.Pressed)
                AddCellsBetween(lastCell, currentCell, true);
            else if (e.RightButton == MouseButtonState.Pressed)
                AddCellsBetween(lastCell, currentCell, false);

            lastCell = currentCell;
            DrawCells();
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
                UpdateCell(end, add);
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
                UpdateCell(new Point(x0, y0), add);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
            }
        }

        private void UpdateCell(Point cell, bool add)
        {
            if (add)
            {
                if (!liveCells.Contains(cell))
                    liveCells.Add(cell);
            }
            else
            {
                liveCells.Remove(cell);
            }
        }


        private void Canvas_LeftUp(object sender, MouseButtonEventArgs e) { }
        private void Canvas_RightUp(object sender, MouseButtonEventArgs e) { }
        
        private void Canvas_MouseLeave(object sender, MouseEventArgs e)
        {
            lastCell = null;
        }
        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Leere Methode, da kein Panning mehr benötigt wird
        }
    }
}
