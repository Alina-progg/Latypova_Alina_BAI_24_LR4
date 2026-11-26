using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Latypova_Alina_BAI_24_LR4
{
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
                array[i] = rand.Next(1000);
            return array;
        }

        // Метод для пузырьковой сортировки
        public void BubbleSort(int[] originalArray, Action<double> progressCallback)
        {
            long comparisons = 0;
            var watch = System.Diagnostics.Stopwatch.StartNew();

            // Весь процесс сортировки заблокирован, так как работаем с общим массивом
            lock (_locker)
            {
                for (int i = 0; i < originalArray.Length - 1; i++)
                {
                    for (int j = 0; j < originalArray.Length - 1 - i; j++)
                    {
                        comparisons++;
                        if (originalArray[j] > originalArray[j + 1])
                        {
                            int temp = originalArray[j];
                            originalArray[j] = originalArray[j + 1];
                            originalArray[j + 1] = temp;
                        }
                    }

                    // Обновление прогресса
                    if (i % (originalArray.Length / 10 + 1) == 0 && progressCallback != null)
                    {
                        progressCallback((double)i / (originalArray.Length - 1) * 100);
                    }
                }
            }

            watch.Stop();
            progressCallback?.Invoke(100);
            _totalComparisons += comparisons;
            BubbleSortCompleted?.Invoke(originalArray, comparisons, watch.Elapsed.TotalMilliseconds);
        }

        // Метод для быстрой сортировки
        public void QuickSort(int[] originalArray, Action<double> progressCallback)
        {
            long comparisons = 0;
            var watch = System.Diagnostics.Stopwatch.StartNew();

            lock (_locker)
            {
                QuickSortRecursive(originalArray, 0, originalArray.Length - 1, ref comparisons, progressCallback);
            }

            watch.Stop();
            progressCallback?.Invoke(100);
            _totalComparisons += comparisons;
            QuickSortCompleted?.Invoke(originalArray, comparisons, watch.Elapsed.TotalMilliseconds);
        }

        private void QuickSortRecursive(int[] arr, int left, int right, ref long comparisons, Action<double> progressCallback)
        {
            if (left < right)
            {
                int pivotIndex = Partition(arr, left, right, ref comparisons);

                // Обновление прогресса (приблизительно)
                if (progressCallback != null)
                {
                    double progress = (double)(arr.Length - (right - left)) / arr.Length * 100;
                    progressCallback(progress);
                }

                QuickSortRecursive(arr, left, pivotIndex - 1, ref comparisons, progressCallback);
                QuickSortRecursive(arr, pivotIndex + 1, right, ref comparisons, progressCallback);
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
        public void InsertionSort(int[] originalArray, Action<double> progressCallback)
        {
            long comparisons = 0;
            var watch = System.Diagnostics.Stopwatch.StartNew();

            lock (_locker)
            {
                for (int i = 1; i < originalArray.Length; i++)
                {
                    int key = originalArray[i];
                    int j = i - 1;
                    while (j >= 0 && originalArray[j] > key)
                    {
                        comparisons++;
                        originalArray[j + 1] = originalArray[j];
                        j--;
                    }
                    comparisons++;
                    originalArray[j + 1] = key;

                    // Обновление прогресса
                    if (i % (originalArray.Length / 10 + 1) == 0 && progressCallback != null)
                    {
                        progressCallback((double)i / originalArray.Length * 100);
                    }
                }
            }

            watch.Stop();
            progressCallback?.Invoke(100);
            _totalComparisons += comparisons;
            InsertionSortCompleted?.Invoke(originalArray, comparisons, watch.Elapsed.TotalMilliseconds);
        }

        // Метод для шейкерной сортировки
        public void ShakerSort(int[] originalArray, Action<double> progressCallback)
        {
            long comparisons = 0;
            var watch = System.Diagnostics.Stopwatch.StartNew();

            lock (_locker)
            {
                int left = 0;
                int right = originalArray.Length - 1;

                while (left < right)
                {
                    // Проход слева направо
                    for (int i = left; i < right; i++)
                    {
                        comparisons++;
                        if (originalArray[i] > originalArray[i + 1])
                        {
                            int temp = originalArray[i];
                            originalArray[i] = originalArray[i + 1];
                            originalArray[i + 1] = temp;
                        }
                    }

                    right--;

                    // Проход справа налево
                    for (int i = right; i > left; i--)
                    {
                        comparisons++;
                        if (originalArray[i - 1] > originalArray[i])
                        {
                            int temp = originalArray[i - 1];
                            originalArray[i - 1] = originalArray[i];
                            originalArray[i] = temp;
                        }
                    }

                    left++;

                    // Обновление прогресса
                    if (left % (originalArray.Length / 10 + 1) == 0 && progressCallback != null)
                    {
                        progressCallback((double)left / originalArray.Length * 100);
                    }
                }
            }

            watch.Stop();
            progressCallback?.Invoke(100);
            _totalComparisons += comparisons;
            ShakerSortCompleted?.Invoke(originalArray, comparisons, watch.Elapsed.TotalMilliseconds);
        }
    }
}