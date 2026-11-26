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

        // Копирование массива (чтобы каждый поток работал со своей копией)
        private int[] CopyArray(int[] source)
        {
            int[] copy = new int[source.Length];
            Array.Copy(source, copy, source.Length);
            return copy;
        }

        // Метод для пузырьковой сортировки
        public void BubbleSort(int[] originalArray, Action<double> progressCallback)
        {
            int[] array = CopyArray(originalArray);
            long comparisons = 0;
            var watch = System.Diagnostics.Stopwatch.StartNew();

            for (int i = 0; i < array.Length - 1; i++)
            {
                for (int j = 0; j < array.Length - 1 - i; j++)
                {
                    comparisons++;
                    if (array[j] > array[j + 1])
                    {
                        int temp = array[j];
                        array[j] = array[j + 1];
                        array[j + 1] = temp;
                    }
                }

                // Обновление прогресса
                if (i % (array.Length / 10 + 1) == 0 && progressCallback != null)
                {
                    progressCallback((double)i / (array.Length - 1) * 100);
                }
            }

            watch.Stop();
            progressCallback?.Invoke(100);
            lock (_locker)
            {
                _totalComparisons += comparisons;
            }
            BubbleSortCompleted?.Invoke(array, comparisons, watch.Elapsed.TotalMilliseconds);
        }

        // Метод для быстрой сортировки
        public void QuickSort(int[] originalArray, Action<double> progressCallback)
        {
            int[] array = CopyArray(originalArray);
            long comparisons = 0;
            var watch = System.Diagnostics.Stopwatch.StartNew();

            QuickSortRecursive(array, 0, array.Length - 1, ref comparisons, progressCallback);

            watch.Stop();
            progressCallback?.Invoke(100);
            lock (_locker)
            {
                _totalComparisons += comparisons;
            }
            QuickSortCompleted?.Invoke(array, comparisons, watch.Elapsed.TotalMilliseconds);
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
            int[] array = CopyArray(originalArray);
            long comparisons = 0;
            var watch = System.Diagnostics.Stopwatch.StartNew();

            for (int i = 1; i < array.Length; i++)
            {
                int key = array[i];
                int j = i - 1;
                while (j >= 0 && array[j] > key)
                {
                    comparisons++;
                    array[j + 1] = array[j];
                    j--;
                }
                comparisons++;
                array[j + 1] = key;

                // Обновление прогресса
                if (i % (array.Length / 10 + 1) == 0 && progressCallback != null)
                {
                    progressCallback((double)i / array.Length * 100);
                }
            }

            watch.Stop();
            progressCallback?.Invoke(100);
            lock (_locker)
            {
                _totalComparisons += comparisons;
            }
            InsertionSortCompleted?.Invoke(array, comparisons, watch.Elapsed.TotalMilliseconds);
        }

        // Метод для шейкерной сортировки
        public void ShakerSort(int[] originalArray, Action<double> progressCallback)
        {
            int[] array = CopyArray(originalArray);
            long comparisons = 0;
            var watch = System.Diagnostics.Stopwatch.StartNew();

            int left = 0;
            int right = array.Length - 1;

            while (left < right)
            {
                // Проход слева направо
                for (int i = left; i < right; i++)
                {
                    comparisons++;
                    if (array[i] > array[i + 1])
                    {
                        int temp = array[i];
                        array[i] = array[i + 1];
                        array[i + 1] = temp;
                    }
                }

                right--;

                // Проход справа налево
                for (int i = right; i > left; i--)
                {
                    comparisons++;
                    if (array[i - 1] > array[i])
                    {
                        int temp = array[i - 1];
                        array[i - 1] = array[i];
                        array[i] = temp;
                    }
                }

                left++;

                // Обновление прогресса
                if (left % (array.Length / 10 + 1) == 0 && progressCallback != null)
                {
                    progressCallback((double)left / array.Length * 100);
                }
            }

            watch.Stop();
            progressCallback?.Invoke(100);
            lock (_locker)
            {
                _totalComparisons += comparisons;
            }
            ShakerSortCompleted?.Invoke(array, comparisons, watch.Elapsed.TotalMilliseconds);
        }
    }
}