using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Lab_rab_2_1_Husainova_R.Z._bpi_23_02.ViewModel
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly ArraySorter _sorter;
        private int[] _originalArray;

        // Управление отменой
        private CancellationTokenSource _bubbleSortCts;
        private CancellationTokenSource _quickSortCts;
        private CancellationTokenSource _insertionSortCts;
        private CancellationTokenSource _shakerSortCts;

        [ObservableProperty]
        private int _arraySize = 1000;

        [ObservableProperty]
        private string _originalArrayString;

        [ObservableProperty]
        private string _bubbleSortResult;
        [ObservableProperty]
        private string _quickSortResult;
        [ObservableProperty]
        private string _insertionSortResult;
        [ObservableProperty]
        private string _shakerSortResult;

        [ObservableProperty]
        private string _totalComparisons = "Общее число сравнений: 0";

        [ObservableProperty]
        private bool _canGenerate = true;

        [ObservableProperty]
        private double _bubbleSortProgress;
        [ObservableProperty]
        private double _quickSortProgress;
        [ObservableProperty]
        private double _insertionSortProgress;
        [ObservableProperty]
        private double _shakerSortProgress;

        [ObservableProperty]
        private string _bubbleSortProgressText = "0%";
        [ObservableProperty]
        private string _quickSortProgressText = "0%";
        [ObservableProperty]
        private string _insertionSortProgressText = "0%";
        [ObservableProperty]
        private string _shakerSortProgressText = "0%";

        [ObservableProperty]
        private ObservableCollection<int> _availableThreadCounts =
            new ObservableCollection<int> { 1, 2, 4, 8, Environment.ProcessorCount };

        [ObservableProperty]
        private int _threadCount = 1;

        [ObservableProperty]
        private string _totalExecutionTime = "Общее время: 0 мс";

        private readonly Dictionary<string, double> _sortTimings = new();

        [ObservableProperty]
        private bool _useSharedArray = false;
        [ObservableProperty]
        private string _performanceNote = "";

        public IAsyncRelayCommand GenerateArrayCommand { get; }
        public IAsyncRelayCommand BubbleSortCommand { get; }
        public IAsyncRelayCommand QuickSortCommand { get; }
        public IAsyncRelayCommand InsertionSortCommand { get; }
        public IAsyncRelayCommand ShakerSortCommand { get; }
        public IRelayCommand RunAllSortsCommand { get; }
        public IRelayCommand CancelAllCommand { get; }

        public MainViewModel()
        {
            _sorter = new ArraySorter();

            var threadOptions = new HashSet<int> { 1, 2, 4, 8, Environment.ProcessorCount };
            AvailableThreadCounts = new ObservableCollection<int>(threadOptions.OrderBy(x => x));

            // Инициализация команд
            GenerateArrayCommand = new AsyncRelayCommand(GenerateArrayAsync, CanGenerateArray);
            BubbleSortCommand = new AsyncRelayCommand(BubbleSortAsync, CanSortBubble);
            QuickSortCommand = new AsyncRelayCommand(QuickSortAsync, CanSortQuick);
            InsertionSortCommand = new AsyncRelayCommand(InsertionSortAsync, CanSortInsertion);
            ShakerSortCommand = new AsyncRelayCommand(ShakerSortAsync, CanSortShaker);
            RunAllSortsCommand = new RelayCommand(RunAllSorts);
            CancelAllCommand = new RelayCommand(CancelAll);
        }

        // Условия выполнения команд
        private bool CanGenerateArray() => !GenerateArrayCommand.IsRunning;
        private bool CanSortBubble() => _originalArray != null && !BubbleSortCommand.IsRunning;
        private bool CanSortQuick() => _originalArray != null && !QuickSortCommand.IsRunning;
        private bool CanSortInsertion() => _originalArray != null && !InsertionSortCommand.IsRunning;
        private bool CanSortShaker() => _originalArray != null && !ShakerSortCommand.IsRunning;

        // Команда генерации массива
        private async Task GenerateArrayAsync()
        {
            await Task.Delay(100);
            CancelAll();
            _sorter.ResetComparisons();
            _sortTimings.Clear();
            _originalArray = _sorter.GenerateRandomArray(ArraySize);
            OriginalArrayString = "Исходный массив: " + string.Join(", ", _originalArray, 0, Math.Min(20, _originalArray.Length)) + (ArraySize > 20 ? "..." : "");

            BubbleSortResult = QuickSortResult = InsertionSortResult = ShakerSortResult = null;
            TotalComparisons = "Общее число сравнений: 0";
            PerformanceNote = "";

            BubbleSortProgress = QuickSortProgress = InsertionSortProgress = ShakerSortProgress = 0;
            BubbleSortProgressText = QuickSortProgressText = InsertionSortProgressText = ShakerSortProgressText = "0%";

            BubbleSortCommand.NotifyCanExecuteChanged();
            QuickSortCommand.NotifyCanExecuteChanged();
            InsertionSortCommand.NotifyCanExecuteChanged();
            ShakerSortCommand.NotifyCanExecuteChanged();
        }

        // Пузырьковая сортировка
        private async Task BubbleSortAsync()
        {
            _bubbleSortCts?.Cancel();
            _bubbleSortCts?.Dispose();
            _bubbleSortCts = new CancellationTokenSource();

            _sorter.UseSharedArray = UseSharedArray;
            BubbleSortResult = "Сортируется...";
            BubbleSortProgress = 0;
            BubbleSortProgressText = "0%";
            BubbleSortCommand.NotifyCanExecuteChanged();

            var progress = new Progress<int>(value =>
            {
                BubbleSortProgress = value;
                BubbleSortProgressText = $"{value}%";
            });

            try
            {
                var result = await _sorter.BubbleSortAsync(_originalArray, _bubbleSortCts.Token, progress);
                if (result.WasCancelled)
                {
                    BubbleSortResult = "Пузырьковая: отменена";
                    _sortTimings.Remove("Bubble");
                }
                else
                {
                    BubbleSortResult = $"Пузырьковая: {FormatArray(result.SortedArray)}, время: {result.ElapsedMilliseconds:F2} мс, сравнений: {result.Comparisons}";
                    BubbleSortProgress = 100;
                    BubbleSortProgressText = "100%";
                    _sortTimings["Bubble"] = result.ElapsedMilliseconds;
                    UpdateTotalComparisons();
                    UpdateTotalExecutionTime();

                    if (UseSharedArray)
                    {
                        PerformanceNote = "возможны задержки";
                    }
                }
            }
            catch (OperationCanceledException)
            {
                BubbleSortResult = "Пузырьковая: отменена";
            }
            finally
            {
                BubbleSortCommand.NotifyCanExecuteChanged();
            }
        }

        // Быстрая сортировка
        private async Task QuickSortAsync()
        {
            _quickSortCts?.Cancel();
            _quickSortCts?.Dispose();
            _quickSortCts = new CancellationTokenSource();

            _sorter.UseSharedArray = UseSharedArray;
            QuickSortResult = "Сортируется...";
            QuickSortProgress = 0;
            QuickSortProgressText = "0%";
            QuickSortCommand.NotifyCanExecuteChanged();

            var progress = new Progress<int>(value =>
            {
                QuickSortProgress = value;
                QuickSortProgressText = $"{value}%";
            });

            try
            {
                var result = await _sorter.QuickSortAsync(_originalArray, _quickSortCts.Token, progress);
                if (result.WasCancelled)
                {
                    QuickSortResult = "Быстрая: отменена";
                    _sortTimings.Remove("Quick");
                }
                else
                {
                    QuickSortResult = $"Быстрая: {FormatArray(result.SortedArray)}, время: {result.ElapsedMilliseconds:F2} мс, сравнений: {result.Comparisons}";
                    QuickSortProgress = 100;
                    QuickSortProgressText = "100%";
                    _sortTimings["Quick"] = result.ElapsedMilliseconds;
                    UpdateTotalComparisons();
                    UpdateTotalExecutionTime();

                    if (UseSharedArray)
                    {
                        PerformanceNote = "возможны задержки";
                    }
                }
            }
            catch (OperationCanceledException)
            {
                QuickSortResult = "Быстрая: отменена";
            }
            finally
            {
                QuickSortCommand.NotifyCanExecuteChanged();
            }
        }

        // Сортировка вставками
        private async Task InsertionSortAsync()
        {
            _insertionSortCts?.Cancel();
            _insertionSortCts?.Dispose();
            _insertionSortCts = new CancellationTokenSource();

            _sorter.UseSharedArray = UseSharedArray;
            InsertionSortResult = "Сортируется...";
            InsertionSortProgress = 0;
            InsertionSortProgressText = "0%";
            InsertionSortCommand.NotifyCanExecuteChanged();

            var progress = new Progress<int>(value =>
            {
                InsertionSortProgress = value;
                InsertionSortProgressText = $"{value}%";
            });

            try
            {
                var result = await _sorter.InsertionSortAsync(_originalArray, _insertionSortCts.Token, progress);
                if (result.WasCancelled)
                {
                    InsertionSortResult = "Вставками: отменена";
                    _sortTimings.Remove("Insertion");
                }
                else
                {
                    InsertionSortResult = $"Вставками: {FormatArray(result.SortedArray)}, время: {result.ElapsedMilliseconds:F2} мс, сравнений: {result.Comparisons}";
                    InsertionSortProgress = 100;
                    InsertionSortProgressText = "100%";
                    _sortTimings["Insertion"] = result.ElapsedMilliseconds;
                    UpdateTotalComparisons();
                    UpdateTotalExecutionTime();

                    if (UseSharedArray)
                    {
                        PerformanceNote = "возможны задержки";
                    }
                }
            }
            catch (OperationCanceledException)
            {
                InsertionSortResult = "Вставками: отменена";
            }
            finally
            {
                InsertionSortCommand.NotifyCanExecuteChanged();
            }
        }

        // Шейкерная сортировка
        private async Task ShakerSortAsync()
        {
            _shakerSortCts?.Cancel();
            _shakerSortCts?.Dispose();
            _shakerSortCts = new CancellationTokenSource();

            _sorter.UseSharedArray = UseSharedArray;
            ShakerSortResult = "Сортируется...";
            ShakerSortProgress = 0;
            ShakerSortProgressText = "0%";
            ShakerSortCommand.NotifyCanExecuteChanged();

            var progress = new Progress<int>(value =>
            {
                ShakerSortProgress = value;
                ShakerSortProgressText = $"{value}%";
            });

            try
            {
                var result = await _sorter.ShakerSortAsync(_originalArray, _shakerSortCts.Token, progress);
                if (result.WasCancelled)
                {
                    ShakerSortResult = "Шейкерная: отменена";
                    _sortTimings.Remove("Shaker");
                }
                else
                {
                    ShakerSortResult = $"Шейкерная: {FormatArray(result.SortedArray)}, время: {result.ElapsedMilliseconds:F2} мс, сравнений: {result.Comparisons}";
                    ShakerSortProgress = 100;
                    ShakerSortProgressText = "100%";
                    _sortTimings["Shaker"] = result.ElapsedMilliseconds;
                    UpdateTotalComparisons();
                    UpdateTotalExecutionTime();

                    if (UseSharedArray)
                    {
                        PerformanceNote = "возможны задержки";
                    }
                }
            }
            catch (OperationCanceledException)
            {
                ShakerSortResult = "Шейкерная: отменена";
            }
            finally
            {
                ShakerSortCommand.NotifyCanExecuteChanged();
            }
        }

        // Запуск всех сортировок
        private async void RunAllSorts() 
        {
            if (_originalArray == null)
            {
                MessageBox.Show("Сначала сгенерируйте массив!", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _sorter.MaxDegreeOfParallelism = Math.Min(4, Environment.ProcessorCount);

            UseSharedArray = false;

            _ = BubbleSortAsync();
            _ = QuickSortAsync();
            _ = InsertionSortAsync();
            _ = ShakerSortAsync();

            PerformanceNote = "Все сортировки запущены параллельно";
        }

        // Команда отмены всех потоков
        private void CancelAll()
        {
            _bubbleSortCts?.Cancel();
            _quickSortCts?.Cancel();
            _insertionSortCts?.Cancel();
            _shakerSortCts?.Cancel();

            _bubbleSortCts?.Dispose();
            _quickSortCts?.Dispose();
            _insertionSortCts?.Dispose();
            _shakerSortCts?.Dispose();

            _bubbleSortCts = null;
            _quickSortCts = null;
            _insertionSortCts = null;
            _shakerSortCts = null;
        }

        // Вспомогательные методы
        private void UpdateTotalComparisons()
        {
            TotalComparisons = $"Общее число сравнений: {_sorter.TotalComparisons}";
        }

        private string FormatArray(int[] arr)
        {
            if (arr.Length <= 10)
                return string.Join(", ", arr);
            else
                return string.Join(", ", arr, 0, 5) + "...";
        }

        partial void OnThreadCountChanged(int value)
        {
            _sorter.MaxDegreeOfParallelism = value;
            System.Diagnostics.Debug.WriteLine($"Потоков: {value}");
        }

        private void UpdateTotalExecutionTime()
        {
            double totalMs = _sortTimings.Values.Sum();
            TotalExecutionTime = $"Общее время выполнения: {totalMs:F2} мс";
        }
    }
}