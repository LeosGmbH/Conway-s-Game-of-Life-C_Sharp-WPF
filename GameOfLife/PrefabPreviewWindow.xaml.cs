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
            public string Name { get; set; } = string.Empty;
            public bool[,] CellStates { get; set; } = new bool[0, 0];
            public int Width => CellStates?.GetLength(0) ?? 0;
            public int Height => CellStates?.GetLength(1) ?? 0;
            
            public ObservableCollection<CellViewModel> Cells { get; } = new ObservableCollection<CellViewModel>();

            public void InitializeCells()
            {
                Cells.Clear();
                if (CellStates == null) return;
                
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        Cells.Add(new CellViewModel { IsAlive = CellStates[x, y] });
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
        }

        public ObservableCollection<PatternViewModel> Patterns { get; } = new ObservableCollection<PatternViewModel>();
        public ICommand SelectPatternCommand { get; }

        public PatternViewModel? SelectedPattern { get; private set; }

        public PrefabPreviewWindow()
        {
            InitializeComponent();
            DataContext = this;
            
            // Register the converter as a resource
            Resources.Add("BoolToColorConverter", new BoolToColorConverter());
            
            // Load patterns
            foreach (var pattern in PrefabsData.Patterns)
            {
                var vm = new PatternViewModel
                {
                    Name = pattern.Name,
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
