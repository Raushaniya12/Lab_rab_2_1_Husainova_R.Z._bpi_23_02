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
        private string _totalComparisons = "Общее число сравнений: 0";
        // Команды
        public IAsyncRelayCommand GenerateArrayCommand { get; }
        public IAsyncRelayCommand BubbleSortCommand { get; }
        public IAsyncRelayCommand QuickSortCommand { get; }
        public IAsyncRelayCommand InsertionSortCommand { get; }
        public MainViewModel()
        {
            _sorter = new ArraySorter();
            // Инициализация команд
            GenerateArrayCommand = new AsyncRelayCommand(GenerateArrayAsync, CanGenerateArray);
            BubbleSortCommand = new AsyncRelayCommand(BubbleSortAsync, CanSortBubble);
            QuickSortCommand = new AsyncRelayCommand(QuickSortAsync, CanSortQuick);
            InsertionSortCommand = new AsyncRelayCommand(InsertionSortAsync, CanSortInsertion);
        }
        // Условия выполнения команд
        private bool CanGenerateArray() => !GenerateArrayCommand.IsRunning;
        private bool CanSortBubble() => _originalArray != null && !BubbleSortCommand.IsRunning;
        private bool CanSortQuick() => _originalArray != null && !QuickSortCommand.IsRunning;
        private bool CanSortInsertion() => _originalArray != null && !InsertionSortCommand.IsRunning;
        // Асинхронные методы команд
        private async Task GenerateArrayAsync()
        {
            // Имитация небольшой задержки (можно убрать)
            await Task.Delay(100);
            _originalArray = _sorter.GenerateRandomArray(ArraySize);
            OriginalArrayString = "Исходный массив: " + string.Join(", ", _originalArray, 0, Math.Min(20,
           _originalArray.Length)) + (ArraySize > 20 ? "..." : "");
            // Сброс результатов
            BubbleSortResult = QuickSortResult = InsertionSortResult = null;
            TotalComparisons = "Общее число сравнений: 0";
            // Обновляем состояние других команд
            BubbleSortCommand.NotifyCanExecuteChanged();
            QuickSortCommand.NotifyCanExecuteChanged();
            InsertionSortCommand.NotifyCanExecuteChanged();
        }
        private async Task BubbleSortAsync()
        {
            BubbleSortResult = "Сортируется...";
            // Запускаем асинхронную сортировку и ждём результат
            var result = await _sorter.BubbleSortAsync(_originalArray);
            BubbleSortResult = $"Пузырьковая: {FormatArray(result.SortedArray)}, время:{ result.ElapsedMilliseconds:F2}мс, сравнений: { result.Comparisons}";
        UpdateTotalComparisons();
        }
        private async Task QuickSortAsync()
        {
            QuickSortResult = "Сортируется...";
            var result = await _sorter.QuickSortAsync(_originalArray);
            QuickSortResult = $"Быстрая: {FormatArray(result.SortedArray)}, время: {result.ElapsedMilliseconds:F2}мс, сравнений: { result.Comparisons}";
        UpdateTotalComparisons();
        }
        private async Task InsertionSortAsync()
        {
            InsertionSortResult = "Сортируется...";
            var result = await _sorter.InsertionSortAsync(_originalArray);
            InsertionSortResult = $"Вставками: {FormatArray(result.SortedArray)}, время:{ result.ElapsedMilliseconds:F2}мс, сравнений: { result.Comparisons}";
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
