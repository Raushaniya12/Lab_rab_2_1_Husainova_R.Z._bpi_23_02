using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lab_rab_2_1_Husainova_R.Z._bpi_23_02
{
    public class SortResult
    {
        public int[] SortedArray { get; set; }
        public long Comparisons { get; set; }
        public double ElapsedMilliseconds { get; set; }
    }
    public class ArraySorter
    {
        // Общий счётчик сравнений (разделяемый ресурс)
        private long _totalComparisons;
        private readonly object _locker = new object();
        // Делегаты и события для уведомления о завершении сортировки
        public delegate void SortCompletedHandler(int[] sortedArray, long comparisons, double elapsedMilliseconds);
        public event SortCompletedHandler BubbleSortCompleted;
        public event SortCompletedHandler QuickSortCompleted;
        public event SortCompletedHandler InsertionSortCompleted;
        public event SortCompletedHandler ShakerSortCompleted;
        // Свойство для доступа к общему счётчику
        public long TotalComparisons => _totalComparisons;
        // Генерация случайного массива заданного размера
        public int[] GenerateRandomArray(int size)
        {
            Random rand = new Random();
            int[] array = new int[size];
            for (int i = 0; i < size; i++)
                array[i] = rand.Next(1000); // числа от 0 до 999
            return array;
        }
        // Копирование массива (чтобы каждый поток работал со своей копией)
        private int[] CopyArray(int[] source)
        {
            int[] copy = new int[source.Length];
            Array.Copy(source, copy, source.Length);
            return copy;
        }
        // Метод для пузырьковой сортировки (запускается в потоке)
        public void BubbleSort(int[] originalArray, IProgress<double> progress = null)
        {
            if (originalArray == null) throw new ArgumentNullException(nameof(originalArray));

            int[] array = CopyArray(originalArray);
            long comparisons = 0;
            var watch = Stopwatch.StartNew();
            int n = array.Length;
            int totalOps = n * (n - 1) / 2;
            int currentOps = 0;
            int lastProgress = -1;

            for (int i = 0; i < n - 1; i++)
            {
                for (int j = 0; j < n - 1 - i; j++)
                {
                    comparisons++;
                    if (array[j] > array[j + 1])
                    {
                        int temp = array[j];
                        array[j] = array[j + 1];
                        array[j + 1] = temp;
                    }

                    currentOps++;
                    int percent = Math.Min(100, (int)((double)currentOps / totalOps * 100));
                    if (percent > lastProgress && percent <= 100)  
                    {
                        progress?.Report(percent);
                        lastProgress = percent;
                    }
                }
            }

            watch.Stop();
            lock (_locker) { _totalComparisons += comparisons; }
            progress?.Report(100);
            BubbleSortCompleted?.Invoke(array, comparisons, watch.Elapsed.TotalMilliseconds);
        }
        // Метод для быстрой сортировки (обёртка)
        public void QuickSort(int[] originalArray, IProgress<double> progress = null)
        {
            if (originalArray == null) throw new ArgumentNullException(nameof(originalArray));

            int[] array = CopyArray(originalArray);
            long comparisons = 0;
            var watch = Stopwatch.StartNew();
            int lastProgress = -1;

            QuickSortRecursive(array, 0, array.Length - 1, ref comparisons, array.Length, progress, ref lastProgress);

            watch.Stop();
            lock (_locker) { _totalComparisons += comparisons; }
            progress?.Report(100);
            QuickSortCompleted?.Invoke(array, comparisons, watch.Elapsed.TotalMilliseconds);
        }

        private void QuickSortRecursive(int[] arr, int left, int right, ref long comparisons,
            int totalLen, IProgress<double> progress, ref int lastProgress)
        {
            if (left < right)
            {
                int pivotIndex = Partition(arr, left, right, ref comparisons);

                int percent = Math.Min(100, (int)((double)(totalLen - (right - left)) / totalLen * 100));
                if (percent > lastProgress && percent <= 100)
                {
                    progress?.Report(percent);
                    lastProgress = percent;
                }

                QuickSortRecursive(arr, left, pivotIndex - 1, ref comparisons, totalLen, progress, ref lastProgress);
                QuickSortRecursive(arr, pivotIndex + 1, right, ref comparisons, totalLen, progress, ref lastProgress);
            }
        }
        private int Partition(int[] arr, int left, int right, ref long comparisons)
        {
            int pivot = arr[right];
            int i = left - 1;
            for (int j = left; j < right; j++)
            {
                comparisons++;
                if (arr[j] < pivot)
                {
                    i++;
                    int temp = arr[i];
                    arr[i] = arr[j];
                    arr[j] = temp;
                }
            }
            int temp1 = arr[i + 1];
            arr[i + 1] = arr[right];
            arr[right] = temp1;
            return i + 1;
        }
        // Метод для сортировки вставками
        public void InsertionSort(int[] originalArray, IProgress<double> progress = null)
        {
            if (originalArray == null) throw new ArgumentNullException(nameof(originalArray));

            int[] array = CopyArray(originalArray);
            long comparisons = 0;
            var watch = Stopwatch.StartNew();
            int n = array.Length;
            int lastProgress = -1;

            for (int i = 1; i < n; i++)
            {
                int key = array[i];
                int j = i - 1;
                while (j >= 0 && array[j] > key)
                {
                    comparisons++;
                    array[j + 1] = array[j];
                    j--;
                }
                if (j >= 0) comparisons++;
                array[j + 1] = key;

                if (i % 10 == 0)
                {
                    int percent = (int)((double)i / n * 100);
                    if (percent > lastProgress)
                    {
                        progress?.Report(percent);
                        lastProgress = percent;
                    }
                }
            }

            watch.Stop();
            lock (_locker) { _totalComparisons += comparisons; }
            progress?.Report(100);
            InsertionSortCompleted?.Invoke(array, comparisons, watch.Elapsed.TotalMilliseconds);
        }
        // Метод для шейкерной сортировки 
        public void ShakerSort(int[] originalArray, IProgress<double> progress = null)
        {
            if (originalArray == null) throw new ArgumentNullException(nameof(originalArray));

            int[] array = CopyArray(originalArray);
            long comparisons = 0;
            var watch = Stopwatch.StartNew();

            bool swapped = true;
            int start = 0;
            int end = array.Length - 1;
            int n = array.Length;
            int lastProgress = -1;
            int totalPasses = 0;
            int maxPasses = n / 2 + 1;

            while (swapped)
            {
                swapped = false;
                for (int i = start; i < end; i++)
                {
                    comparisons++;
                    if (array[i] > array[i + 1])
                    {
                        int temp = array[i];
                        array[i] = array[i + 1];
                        array[i + 1] = temp;
                        swapped = true;
                    }
                }
                if (!swapped) break;
                end--;

                swapped = false;
                for (int i = end - 1; i >= start; i--)
                {
                    comparisons++;
                    if (array[i] > array[i + 1])
                    {
                        int temp = array[i];
                        array[i] = array[i + 1];
                        array[i + 1] = temp;
                        swapped = true;
                    }
                }
                start++;
                totalPasses++;

                int percent = Math.Min(100, (int)((double)totalPasses / maxPasses * 100));
                if (percent > lastProgress && percent <= 100)
                {
                    progress?.Report(percent);
                    lastProgress = percent;
                }
            }

            watch.Stop();
            lock (_locker) { _totalComparisons += comparisons; }
            progress?.Report(100);
            ShakerSortCompleted?.Invoke(array, comparisons, watch.Elapsed.TotalMilliseconds);
        }

        // Асинхронные методы-обертки 
        public async Task<SortResult> BubbleSortAsync(int[] originalArray, IProgress<double> progress = null)
        {
            var tcs = new TaskCompletionSource<SortResult>();
            SortCompletedHandler handler = null;

            handler = (sortedArray, comparisons, elapsedMs) =>
            {
                BubbleSortCompleted -= handler;
                tcs.SetResult(new SortResult
                {
                    SortedArray = sortedArray,
                    Comparisons = comparisons,
                    ElapsedMilliseconds = elapsedMs
                });
            };

            BubbleSortCompleted += handler;
            await Task.Run(() => BubbleSort(originalArray, progress));
            return await tcs.Task;
        }

        public async Task<SortResult> QuickSortAsync(int[] originalArray, IProgress<double> progress = null)
        {
            var tcs = new TaskCompletionSource<SortResult>();
            SortCompletedHandler handler = null;

            handler = (sortedArray, comparisons, elapsedMs) =>
            {
                QuickSortCompleted -= handler;
                tcs.SetResult(new SortResult
                {
                    SortedArray = sortedArray,
                    Comparisons = comparisons,
                    ElapsedMilliseconds = elapsedMs
                });
            };

            QuickSortCompleted += handler;
            await Task.Run(() => QuickSort(originalArray, progress));
            return await tcs.Task;
        }

        public async Task<SortResult> InsertionSortAsync(int[] originalArray, IProgress<double> progress = null)
        {
            var tcs = new TaskCompletionSource<SortResult>();
            SortCompletedHandler handler = null;

            handler = (sortedArray, comparisons, elapsedMs) =>
            {
                InsertionSortCompleted -= handler;
                tcs.SetResult(new SortResult
                {
                    SortedArray = sortedArray,
                    Comparisons = comparisons,
                    ElapsedMilliseconds = elapsedMs
                });
            };

            InsertionSortCompleted += handler;
            await Task.Run(() => InsertionSort(originalArray, progress));
            return await tcs.Task;
        }

        public async Task<SortResult> ShakerSortAsync(int[] originalArray, IProgress<double> progress = null)
        {
            var tcs = new TaskCompletionSource<SortResult>();
            SortCompletedHandler handler = null;

            handler = (sortedArray, comparisons, elapsedMs) =>
            {
                ShakerSortCompleted -= handler;
                tcs.SetResult(new SortResult
                {
                    SortedArray = sortedArray,
                    Comparisons = comparisons,
                    ElapsedMilliseconds = elapsedMs
                });
            };

            ShakerSortCompleted += handler;
            await Task.Run(() => ShakerSort(originalArray, progress));
            return await tcs.Task;
        }

        // Сброс общего счётчика сравнений
        public void ResetComparisons()
        {
            lock (_locker)
            {
                _totalComparisons = 0;
            }
        }
    }
}