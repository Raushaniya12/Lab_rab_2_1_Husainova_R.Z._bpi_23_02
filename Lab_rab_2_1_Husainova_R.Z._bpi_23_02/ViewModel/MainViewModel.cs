using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lab_rab_2_1_Husainova_R.Z._bpi_23_02.ViewModel
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly ArraySorter _sorter;
        private int[] _originalArray;
        // Наблюдаемые свойства
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
        private double _bubbleSortProgress;
        [ObservableProperty]
        private double _quickSortProgress;
        [ObservableProperty]
        private double _insertionSortProgress;
        [ObservableProperty]
        private double _shakerSortProgress;
        [ObservableProperty]
        private string _bubbleSortProgressText = "";
        [ObservableProperty]
        private string _quickSortProgressText = "";
        [ObservableProperty]
        private string _insertionSortProgressText = "";
        [ObservableProperty]
        private string _shakerSortProgressText = "";

        // Команды
        public IAsyncRelayCommand GenerateArrayCommand { get; }
        public IAsyncRelayCommand BubbleSortCommand { get; }
        public IAsyncRelayCommand QuickSortCommand { get; }
        public IAsyncRelayCommand InsertionSortCommand { get; }
        public IAsyncRelayCommand ShakerSortCommand { get; }
        public MainViewModel()
        {
            _sorter = new ArraySorter();
            // Инициализация команд
            GenerateArrayCommand = new AsyncRelayCommand(GenerateArrayAsync, CanGenerateArray);
            BubbleSortCommand = new AsyncRelayCommand(BubbleSortAsync, CanSortBubble);
            QuickSortCommand = new AsyncRelayCommand(QuickSortAsync, CanSortQuick);
            InsertionSortCommand = new AsyncRelayCommand(InsertionSortAsync, CanSortInsertion);
            ShakerSortCommand = new AsyncRelayCommand(ShakerSortAsync, CanSortShaker);
        }
        // Условия выполнения команд
        private bool CanGenerateArray() => !GenerateArrayCommand.IsRunning;
        private bool CanSortBubble() => _originalArray != null && !BubbleSortCommand.IsRunning;
        private bool CanSortQuick() => _originalArray != null && !QuickSortCommand.IsRunning;
        private bool CanSortInsertion() => _originalArray != null && !InsertionSortCommand.IsRunning;
        private bool CanSortShaker() => _originalArray != null && !ShakerSortCommand.IsRunning;
        // Асинхронные методы команд
        private async Task GenerateArrayAsync()
        {
            // Имитация небольшой задержки (можно убрать)
            await Task.Delay(100);
            _originalArray = _sorter.GenerateRandomArray(ArraySize);
            OriginalArrayString = "Исходный массив: " + string.Join(", ", _originalArray, 0, Math.Min(20, _originalArray.Length)) + (ArraySize > 20 ? "..." : "");
            // Сброс результатов
            BubbleSortResult = QuickSortResult = InsertionSortResult = ShakerSortResult = null;
            TotalComparisons = "Общее число сравнений: 0";

            // Сброс прогресса
            BubbleSortProgress = QuickSortProgress = InsertionSortProgress = ShakerSortProgress = 0;
            BubbleSortProgressText = QuickSortProgressText = InsertionSortProgressText = ShakerSortProgressText = "";

            // Обновляем состояние других команд
            BubbleSortCommand.NotifyCanExecuteChanged();
            QuickSortCommand.NotifyCanExecuteChanged();
            InsertionSortCommand.NotifyCanExecuteChanged();
            ShakerSortCommand.NotifyCanExecuteChanged();
        }
        private async Task BubbleSortAsync()
        {
            BubbleSortResult = "Сортируется...";
            BubbleSortProgress = 0;

            var progress = new Progress<double>(value =>
            {
                BubbleSortProgress = Math.Min(100, value);
                BubbleSortProgressText = $"{Math.Min(100, value):F1}%";
            });

            var result = await _sorter.BubbleSortAsync(_originalArray, progress);
            BubbleSortProgress = 100;
            BubbleSortProgressText = "100%";
            BubbleSortResult = $"Пузырьковая: {FormatArray(result.SortedArray)}, время: {result.ElapsedMilliseconds:F2} мс, сравнений: {result.Comparisons}";
            UpdateTotalComparisons();
        }
        private async Task QuickSortAsync()
        {
            QuickSortResult = "Сортируется...";
            QuickSortProgress = 0;

            var progress = new Progress<double>(value =>
            {
                QuickSortProgress = value;
                QuickSortProgressText = $"{value:F1}%";
            });

            var result = await _sorter.QuickSortAsync(_originalArray, progress);
            QuickSortProgress = 100;
            QuickSortProgressText = "100%";
            QuickSortResult = $"Быстрая: {FormatArray(result.SortedArray)}, время: {result.ElapsedMilliseconds:F2} мс, сравнений: {result.Comparisons}";
            UpdateTotalComparisons();
        }
        private async Task InsertionSortAsync()
        {
            InsertionSortResult = "Сортируется...";
            InsertionSortProgress = 0;

            var progress = new Progress<double>(value =>
            {
                InsertionSortProgress = value;
                InsertionSortProgressText = $"{value:F1}%";
            });

            var result = await _sorter.InsertionSortAsync(_originalArray, progress);
            InsertionSortProgress = 100;
            InsertionSortProgressText = "100%";
            InsertionSortResult = $"Вставками: {FormatArray(result.SortedArray)}, время: {result.ElapsedMilliseconds:F2} мс, сравнений: {result.Comparisons}";
            UpdateTotalComparisons();
        }
        private async Task ShakerSortAsync()
        {
            if (_originalArray == null) return;

            ShakerSortResult = "Сортируется...";
            ShakerSortProgress = 0;

            var progress = new Progress<double>(value =>
            {
                ShakerSortProgress = value;
                ShakerSortProgressText = $"{value:F1}%";
            });

            var result = await _sorter.ShakerSortAsync(_originalArray, progress);
            ShakerSortProgress = 100;
            ShakerSortProgressText = "100%";
            ShakerSortResult = $"Шейкерная: {FormatArray(result.SortedArray)}, время: {result.ElapsedMilliseconds:F2} мс, сравнений: {result.Comparisons}";
            UpdateTotalComparisons();
        }
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
    }
}
