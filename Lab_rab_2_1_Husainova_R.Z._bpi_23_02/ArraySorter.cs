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
        public bool WasCancelled { get; set; }
    }
    public class ArraySorter
    {
        // Общий счётчик сравнений (разделяемый ресурс)
        private long _totalComparisons;
        private readonly object _locker = new();
        public bool UseSharedArray { get; set; } = false;
        private readonly object _arrayAccessLock = new object();

        // Делегаты и события для уведомления о завершении сортировки
        public delegate void SortCompletedHandler(int[] sortedArray, long comparisons, double elapsedMilliseconds, bool wasCancelled);
        public event SortCompletedHandler BubbleSortCompleted;
        public event SortCompletedHandler QuickSortCompleted;
        public event SortCompletedHandler InsertionSortCompleted;
        public event SortCompletedHandler ShakerSortCompleted;

        // Свойство для доступа к общему счётчику
        private int _maxThreads = 1;
        private int _activeThreads;
        private readonly object _threadCounterLock = new object();
        private SemaphoreSlim _threadSemaphore;

        public long TotalComparisons => _totalComparisons;

        public int MaxDegreeOfParallelism
        {
            get => _maxThreads;
            set => SetMaxThreads(value);
        }
        public ArraySorter()
        {
            _threadSemaphore = new SemaphoreSlim(_maxThreads, _maxThreads);
        }

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
        private bool CompareAndSwap(int[] array, int i, int j, ref long comparisons, CancellationToken cancellationToken)
        {
            lock (_arrayAccessLock)
            {
                cancellationToken.ThrowIfCancellationRequested();
                comparisons++;
                if (array[i] > array[j])
                {
                    int temp = array[i];
                    array[i] = array[j];
                    array[j] = temp;
                    return true;
                }
                return false;
            }
        }
        private int SafeRead(int[] array, int index)
        {
            lock (_arrayAccessLock)
            {
                return array[index];
            }
        }

        // Метод для пузырьковой сортировки (запускается в потоке)
        public async Task<SortResult> BubbleSortAsync(int[] originalArray, CancellationToken cancellationToken, IProgress<int> progress = null)
        {
            await _threadSemaphore.WaitAsync(cancellationToken);
            try
            {
                return await Task.Run(() =>
                {
                    int[] array = UseSharedArray ? originalArray : CopyArray(originalArray);
                    long comparisons = 0;
                    var watch = Stopwatch.StartNew();
                    int n = array.Length;
                    long totalOperations = (long)n * (n - 1) / 2;
                    long currentOperation = 0;
                    int lastReportedPercent = -1;
                    bool wasCancelled = false;

                    try
                    {
                        for (int i = 0; i < array.Length - 1; i++)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            for (int j = 0; j < array.Length - 1 - i; j++)
                            {
                                cancellationToken.ThrowIfCancellationRequested();

                                if (UseSharedArray)
                                {
                                    CompareAndSwap(array, j, j + 1, ref comparisons, cancellationToken);
                                }
                                else
                                {
                                    comparisons++;
                                    if (array[j] > array[j + 1])
                                    {
                                        int temp = array[j];
                                        array[j] = array[j + 1];
                                        array[j + 1] = temp;
                                    }
                                }

                                currentOperation++;
                                int percent = (int)((currentOperation * 100.0) / totalOperations);

                                if (percent != lastReportedPercent)
                                {
                                    progress?.Report(percent);
                                    lastReportedPercent = percent;
                                }
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        wasCancelled = true;
                    }

                    watch.Stop();
                    lock (_locker)
                    {
                        _totalComparisons += comparisons;
                    }

                    return new SortResult
                    {
                        SortedArray = (int[])array.Clone(),
                        Comparisons = comparisons,
                        ElapsedMilliseconds = watch.Elapsed.TotalMilliseconds,
                        WasCancelled = wasCancelled
                    };
                }, cancellationToken);
            }
            finally
            {
                _threadSemaphore.Release();
            }
        }

        // Метод для быстрой сортировки (обёртка)
        public async Task<SortResult> QuickSortAsync(int[] originalArray, CancellationToken cancellationToken, IProgress<int> progress = null)
        {
            await _threadSemaphore.WaitAsync(cancellationToken);
            try
            {
                return await Task.Run(async () =>
                {
                    int[] array = UseSharedArray ? originalArray : CopyArray(originalArray);
                    var watch = Stopwatch.StartNew();
                    bool wasCancelled = false;
                    long comparisons = 0;

                    try
                    {
                        comparisons = await QuickSortParallelAsync(array, 0, array.Length - 1, cancellationToken, 0);
                    }
                    catch (OperationCanceledException)
                    {
                        wasCancelled = true;
                    }

                    watch.Stop();
                    if (!wasCancelled)
                    {
                        lock (_locker)
                        {
                            _totalComparisons += comparisons;
                        }
                    }

                    return new SortResult
                    {
                        SortedArray = (int[])array.Clone(),
                        Comparisons = comparisons,
                        ElapsedMilliseconds = watch.Elapsed.TotalMilliseconds,
                        WasCancelled = wasCancelled
                    };
                }, cancellationToken);
            }
            finally
            {
                _threadSemaphore.Release();
            }
        }

        private async Task<long> QuickSortParallelAsync(int[] arr, int left, int right,
            CancellationToken cancellationToken,
            int recursionDepth)
        {
            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException();

            if (left >= right) return 0;

            long comparisons = 0;
            bool canParallel = recursionDepth < 2 &&
                             (right - left + 1) > 500 &&
                             _maxThreads > 1;

            if (canParallel)
            {
                int pivotIndex = Partition(arr, left, right, ref comparisons, cancellationToken);
                var leftTask = QuickSortParallelAsync(arr, left, pivotIndex - 1, cancellationToken, recursionDepth + 1);
                var rightTask = QuickSortParallelAsync(arr, pivotIndex + 1, right, cancellationToken, recursionDepth + 1);

                await Task.WhenAll(leftTask, rightTask);
                comparisons += await leftTask + await rightTask;
            }
            else
            {
                int pivotIndex = Partition(arr, left, right, ref comparisons, cancellationToken);
                comparisons += await QuickSortParallelAsync(arr, left, pivotIndex - 1, cancellationToken, recursionDepth + 1);
                comparisons += await QuickSortParallelAsync(arr, pivotIndex + 1, right, cancellationToken, recursionDepth + 1);
            }

            return comparisons;
        }

        private int Partition(int[] arr, int left, int right,
            ref long comparisons, CancellationToken cancellationToken)
        {
            int pivot = arr[right];
            int i = left - 1;

            for (int j = left; j < right; j++)
            {
                if (cancellationToken.IsCancellationRequested)
                    throw new OperationCanceledException();

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
        public async Task<SortResult> InsertionSortAsync(int[] originalArray, CancellationToken cancellationToken, IProgress<int> progress = null)
        {
            await _threadSemaphore.WaitAsync(cancellationToken);
            try
            {
                return await Task.Run(() =>
                {
                    int[] array = UseSharedArray ? originalArray : CopyArray(originalArray);
                    long comparisons = 0;
                    var watch = Stopwatch.StartNew();
                    int n = array.Length;
                    int lastReportedPercent = -1;
                    bool wasCancelled = false;

                    try
                    {
                        for (int i = 1; i < array.Length; i++)
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                wasCancelled = true;
                                break;
                            }

                            int key = UseSharedArray ? SafeRead(array, i) : array[i];
                            int j = i - 1;

                            while (j >= 0)
                            {
                                if (cancellationToken.IsCancellationRequested)
                                {
                                    wasCancelled = true;
                                    break;
                                }

                                int currentVal = UseSharedArray ? SafeRead(array, j) : array[j];
                                if (currentVal > key)
                                {
                                    comparisons++;
                                    if (UseSharedArray)
                                    {
                                        lock (_arrayAccessLock)
                                        {
                                            array[j + 1] = array[j];
                                        }
                                    }
                                    else
                                    {
                                        array[j + 1] = array[j];
                                    }
                                    j--;
                                }
                                else
                                {
                                    comparisons++;
                                    break;
                                }
                            }

                            if (wasCancelled) break;

                            if (UseSharedArray)
                            {
                                lock (_arrayAccessLock)
                                {
                                    array[j + 1] = key;
                                }
                            }
                            else
                            {
                                array[j + 1] = key;
                            }

                            int percent = (int)((i * 100.0) / n);
                            if (percent != lastReportedPercent)
                            {
                                progress?.Report(percent);
                                lastReportedPercent = percent;
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        wasCancelled = true;
                    }

                    watch.Stop();
                    if (!wasCancelled)
                    {
                        lock (_locker)
                        {
                            _totalComparisons += comparisons;
                        }
                    }

                    return new SortResult
                    {
                        SortedArray = (int[])array.Clone(),
                        Comparisons = comparisons,
                        ElapsedMilliseconds = watch.Elapsed.TotalMilliseconds,
                        WasCancelled = wasCancelled
                    };
                }, cancellationToken);
            }
            finally
            {
                _threadSemaphore.Release();
            }
        }

        // Метод для шейкерной сортировки 
        public async Task<SortResult> ShakerSortAsync(int[] originalArray, CancellationToken cancellationToken, IProgress<int> progress = null)
        {
            await _threadSemaphore.WaitAsync(cancellationToken);
            try
            {
                return await Task.Run(() =>
                {
                    int[] array = UseSharedArray ? originalArray : CopyArray(originalArray);
                    long comparisons = 0;
                    var watch = Stopwatch.StartNew();
                    int n = array.Length;
                    int lastReportedPercent = -1;
                    long totalPasses = n;
                    int currentPass = 0;
                    bool wasCancelled = false;
                    int start = 0;
                    int end = n - 1;
                    bool swapped = true;

                    try
                    {
                        while (swapped)
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                wasCancelled = true;
                                break;
                            }

                            swapped = false;
                            // Прямой проход
                            for (int i = start; i < end; i++)
                            {
                                if (cancellationToken.IsCancellationRequested)
                                {
                                    wasCancelled = true;
                                    break;
                                }

                                if (UseSharedArray)
                                {
                                    CompareAndSwap(array, i, i + 1, ref comparisons, cancellationToken);
                                    swapped = true;
                                }
                                else
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
                            }

                            if (wasCancelled) break;
                            if (!swapped) break;

                            currentPass++;
                            int percent = Math.Min(100, (int)((currentPass * 100.0) / totalPasses));
                            if (percent != lastReportedPercent)
                            {
                                progress?.Report(percent);
                                lastReportedPercent = percent;
                            }

                            swapped = false;
                            end--;

                            // Обратный проход
                            for (int i = end; i > start; i--)
                            {
                                if (cancellationToken.IsCancellationRequested)
                                {
                                    wasCancelled = true;
                                    break;
                                }

                                if (UseSharedArray)
                                {
                                    CompareAndSwap(array, i - 1, i, ref comparisons, cancellationToken);
                                    swapped = true;
                                }
                                else
                                {
                                    comparisons++;
                                    if (array[i] < array[i - 1])
                                    {
                                        int temp = array[i];
                                        array[i] = array[i - 1];
                                        array[i - 1] = temp;
                                        swapped = true;
                                    }
                                }
                            }

                            if (wasCancelled) break;

                            currentPass++;
                            percent = Math.Min(100, (int)((currentPass * 100.0) / totalPasses));
                            if (percent != lastReportedPercent)
                            {
                                progress?.Report(percent);
                                lastReportedPercent = percent;
                            }

                            start++;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        wasCancelled = true;
                    }

                    watch.Stop();
                    if (!wasCancelled)
                    {
                        lock (_locker)
                        {
                            _totalComparisons += comparisons;
                        }
                    }

                    return new SortResult
                    {
                        SortedArray = (int[])array.Clone(),
                        Comparisons = comparisons,
                        ElapsedMilliseconds = watch.Elapsed.TotalMilliseconds,
                        WasCancelled = wasCancelled
                    };
                }, cancellationToken);
            }
            finally
            {
                _threadSemaphore.Release();
            }
        }
        public void SetMaxThreads(int maxThreads)
        {
            if (maxThreads < 1) maxThreads = 1;
            lock (_locker)
            {
                _maxThreads = maxThreads;
                _threadSemaphore?.Dispose();
                _threadSemaphore = new SemaphoreSlim(_maxThreads, _maxThreads);
            }
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
