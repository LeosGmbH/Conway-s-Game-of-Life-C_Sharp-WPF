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

        // Zellen-Größe in Pixel
        private double cellSize = 20;

        private bool isDarkMode = true;

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
            GameCanvas.Children.Clear();

            double offsetX = Scroll.HorizontalOffset;
            double offsetY = Scroll.VerticalOffset;
            double viewWidth = Scroll.ViewportWidth;
            double viewHeight = Scroll.ViewportHeight;

            int startX = (int)(offsetX / cellSize);
            int startY = (int)(offsetY / cellSize);
            int endX = (int)((offsetX + viewWidth) / cellSize);
            int endY = (int)((offsetY + viewHeight) / cellSize);

            // Draw grid lines
            var gridBrush = (Brush)Resources["GridLineColor"];
            for (int x = startX; x <= endX; x++)
            {
                var line = new Line
                {
                    X1 = x * cellSize,
                    Y1 = startY * cellSize,
                    X2 = x * cellSize,
                    Y2 = (endY + 1) * cellSize,
                    Stroke = gridBrush,
                    StrokeThickness = 1
                };
                GameCanvas.Children.Add(line);
            }
            for (int y = startY; y <= endY; y++)
            {
                var line = new Line
                {
                    X1 = startX * cellSize,
                    Y1 = y * cellSize,
                    X2 = (endX + 1) * cellSize,
                    Y2 = y * cellSize,
                    Stroke = gridBrush,
                    StrokeThickness = 1
                };
                GameCanvas.Children.Add(line);
            }

            // Draw live cells
            var liveCellBrush = (Brush)Resources["LiveCellColor"];
            foreach (var cell in liveCells)
            {
                int x = (int)cell.X;
                int y = (int)cell.Y;

                if (x >= startX && x <= endX && y >= startY && y <= endY)
                {
                    Rectangle rect = new Rectangle
                    {
                        Width = cellSize - 1,
                        Height = cellSize - 1,
                        Fill = liveCellBrush
                    };
                    Canvas.SetLeft(rect, x * cellSize);
                    Canvas.SetTop(rect, y * cellSize);
                    GameCanvas.Children.Add(rect);
                }
            }
        }





        private void Scroll_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            DrawCells();
        }



        public MainWindow()
        {
            InitializeComponent();
            ApplyTheme();
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
        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) { }
        private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e) { }

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
        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e) { }
    }
}
