using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using GameOfLife.Converters;

namespace GameOfLife
{
    public partial class PrefabPreviewWindow : Window
    {
        public class PatternViewModel : ViewModelBase
        {
            private const double CellPixelSize = 18.0;

            public string Name { get; set; } = string.Empty;
            public string Group { get; set; } = string.Empty;
            public bool[,] CellStates { get; set; } = new bool[0, 0];
            public int Width => CellStates?.GetLength(0) ?? 0;
            public int Height => CellStates?.GetLength(1) ?? 0;

            private double _canvasWidth;
            public double CanvasWidth
            {
                get => _canvasWidth;
                private set => SetProperty(ref _canvasWidth, value);
            }

            private double _canvasHeight;
            public double CanvasHeight
            {
                get => _canvasHeight;
                private set => SetProperty(ref _canvasHeight, value);
            }

            public ObservableCollection<CellViewModel> Cells { get; } = new ObservableCollection<CellViewModel>();
            public ObservableCollection<GridLineViewModel> GridLines { get; } = new ObservableCollection<GridLineViewModel>();

            public void InitializeCells()
            {
                Cells.Clear();
                GridLines.Clear();

                if (CellStates == null || Width == 0 || Height == 0)
                {
                    CanvasWidth = 0;
                    CanvasHeight = 0;
                    return;
                }

                CanvasWidth = Width * CellPixelSize;
                CanvasHeight = Height * CellPixelSize;

                // Grid lines
                for (int x = 0; x <= Width; x++)
                {
                    double offset = x * CellPixelSize;
                    GridLines.Add(new GridLineViewModel
                    {
                        X1 = offset,
                        Y1 = 0,
                        X2 = offset,
                        Y2 = CanvasHeight
                    });
                }

                for (int y = 0; y <= Height; y++)
                {
                    double offset = y * CellPixelSize;
                    GridLines.Add(new GridLineViewModel
                    {
                        X1 = 0,
                        Y1 = offset,
                        X2 = CanvasWidth,
                        Y2 = offset
                    });
                }

                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        Cells.Add(new CellViewModel
                        {
                            IsAlive = CellStates[x, y],
                            Left = x * CellPixelSize,
                            Top = y * CellPixelSize,
                            Size = CellPixelSize
                        });
                    }
                }
            }
        }

        public class CellViewModel : ViewModelBase
        {
            private bool _isAlive;
            public bool IsAlive
            {
                get => _isAlive;
                set => SetProperty(ref _isAlive, value);
            }

            private double _left;
            public double Left
            {
                get => _left;
                set => SetProperty(ref _left, value);
            }

            private double _top;
            public double Top
            {
                get => _top;
                set => SetProperty(ref _top, value);
            }

            private double _size;
            public double Size
            {
                get => _size;
                set => SetProperty(ref _size, value);
            }
        }

        public class GridLineViewModel : ViewModelBase
        {
            private double _x1;
            public double X1
            {
                get => _x1;
                set => SetProperty(ref _x1, value);
            }

            private double _y1;
            public double Y1
            {
                get => _y1;
                set => SetProperty(ref _y1, value);
            }

            private double _x2;
            public double X2
            {
                get => _x2;
                set => SetProperty(ref _x2, value);
            }

            private double _y2;
            public double Y2
            {
                get => _y2;
                set => SetProperty(ref _y2, value);
            }
        }

        public ObservableCollection<PatternViewModel> Patterns { get; } = new ObservableCollection<PatternViewModel>();
        public ICommand SelectPatternCommand { get; }

        public PatternViewModel? SelectedPattern { get; private set; }

        public PrefabPreviewWindow()
        {
            InitializeComponent();
            DataContext = this;
            
            // Load patterns
            foreach (var pattern in PrefabsData.Patterns)
            {
                var vm = new PatternViewModel
                {
                    Name = pattern.Name,
                    Group = pattern.Group,
                    CellStates = pattern.Cells
                };
                vm.InitializeCells();
                Patterns.Add(vm);
            }

            SelectPatternCommand = new RelayCommand<PatternViewModel>(SelectPattern);
        }

        private void SelectPattern(PatternViewModel pattern)
        {
            if (pattern != null)
            {
                SelectedPattern = pattern;
                DialogResult = true;
                Close();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    public class ViewModelBase : System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Predicate<T>? _canExecute;

        public RelayCommand(Action<T> execute, Predicate<T>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke((T)parameter!) ?? true;

        public void Execute(object? parameter) => _execute((T)parameter!);

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}
