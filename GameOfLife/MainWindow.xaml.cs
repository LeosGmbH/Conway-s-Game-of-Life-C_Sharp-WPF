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
                        Fill = Brushes.LightGreen
                    };
                    Canvas.SetLeft(rect, x * cellSize);
                    Canvas.SetTop(rect, y * cellSize);
                    GameCanvas.Children.Add(rect);
                }
            }
        }

        private void Canvas_LeftDown(object sender, MouseButtonEventArgs e)
        {
            Point mousePos = e.GetPosition(GameCanvas);
            int cellX = (int)(mousePos.X / cellSize);
            int cellY = (int)(mousePos.Y / cellSize);

            liveCells.Add(new Point(cellX, cellY));
            DrawCells();
        }



        private void Scroll_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            DrawCells();
        }



        public MainWindow()
        {
            InitializeComponent();
        }

        // Simulation Buttons
        private void Start_Click(object sender, RoutedEventArgs e) { }
        private void StopPause_Click(object sender, RoutedEventArgs e) { }
        private void Clear_Click(object sender, RoutedEventArgs e) { }

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
        private void Canvas_LeftUp(object sender, MouseButtonEventArgs e) { }
        private void Canvas_RightDown(object sender, MouseButtonEventArgs e) { }
        private void Canvas_RightUp(object sender, MouseButtonEventArgs e) { }
        private void Canvas_MouseMove(object sender, MouseEventArgs e) { }
        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e) { }
    }
}
