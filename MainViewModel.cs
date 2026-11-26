using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace Latypova_Alina_BAI_24_LR4
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly ArraySorter _sorter;
        private readonly SynchronizationContext _uiContext;
        private int[] _originalArray;
        private SemaphoreSlim _semaphore;
        private Stopwatch _batchStopwatch = new Stopwatch();

        // Токен для прерывания потоков
        private CancellationTokenSource _cts;

        // Счетчики для отслеживания завершения всех запущенных сортировок
        private int _expectedSortsToComplete = 0;
        private int _completedSorts = 0;

        // Наблюдаемые свойства
        [ObservableProperty]
        private int _arraySize = 1000;

        [ObservableProperty]
        private int _threadCount = 4;

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

        [ObservableProperty]
        private string _totalElapsedTime = "Общее время выполнения: 0 мс";

        [ObservableProperty]
        private bool _canGenerate = true;

        [ObservableProperty]
        private string _shakerSortResult;

        // Флаг для переключения между общим массивом и копиями
        [ObservableProperty]
        private bool _useSharedArray = false;

        // Свойства для работы ProgressBar
        [ObservableProperty] private double _bubbleProgress;
        [ObservableProperty] private double _quickProgress;
        [ObservableProperty] private double _insertionProgress;
        [ObservableProperty] private double _shakerProgress;

        public MainViewModel()
        {
            _sorter = new ArraySorter();
            _semaphore = new SemaphoreSlim(ThreadCount);
            _cts = new CancellationTokenSource();

            // Получаем контекст синхронизации текущего потока (UI-потока), 
            // чтобы потом безопасно обновлять интерфейс из фоновых потоков.
            _uiContext = SynchronizationContext.Current ?? new SynchronizationContext();

            // Подписка на события завершения сортировки
            _sorter.BubbleSortCompleted += OnBubbleSortCompleted;
            _sorter.QuickSortCompleted += OnQuickSortCompleted;
            _sorter.InsertionSortCompleted += OnInsertionSortCompleted;
            _sorter.ShakerSortCompleted += OnShakerSortCompleted;
        }

        // Команда генерации массива
        [RelayCommand(CanExecute = nameof(CanGenerateArray))]
        private void GenerateArray()
        {
            _originalArray = _sorter.GenerateRandomArray(ArraySize);

            // Отображаем первые 20 элементов
            OriginalArrayString = "Исходный массив: " + string.Join(", ", _originalArray.Take(20)) + (ArraySize > 20 ? "..." : "");

            // Сбрасываем предыдущие результаты
            BubbleSortResult = QuickSortResult = InsertionSortResult = ShakerSortResult = null;
            TotalComparisons = "Общее число сравнений: 0";

            // Сбрасываем общее время и счётчик завершённых сортировок
            TotalElapsedTime = "Общее время выполнения: 0 мс";
            _completedSorts = 0;
            _expectedSortsToComplete = 0;
            _batchStopwatch.Reset();

            // Сбрасываем прогресс-бары при генерации нового массива
            BubbleProgress = QuickProgress = InsertionProgress = ShakerProgress = 0;

            // Пересоздаем токен отмены для новой партии сортировок
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            // Обновляем состояние команд сортировок
            BubbleSortCommand.NotifyCanExecuteChanged();
            QuickSortCommand.NotifyCanExecuteChanged();
            InsertionSortCommand.NotifyCanExecuteChanged();
            ShakerSortCommand.NotifyCanExecuteChanged();
        }

        private bool CanGenerateArray() => CanGenerate;

        // Изменение количества одновременно выполняемых сортировок
        partial void OnThreadCountChanged(int value)
        {
            _semaphore?.Dispose();
            _semaphore = new SemaphoreSlim(value);
        }

        // Вспомогательный метод для обновления прогресс-баров в UI-потоке
        private void UpdateProgress(Action updateAction)
        {
            _uiContext.Post(_ => updateAction(), null);
        }

        // Пузырьковая сортировка
        private bool CanSortBubble() => _originalArray != null && BubbleSortResult != "Сортируется...";

        [RelayCommand(CanExecute = nameof(CanSortBubble))]
        private void BubbleSort()
        {
            BubbleSortResult = "Сортируется...";
            BubbleProgress = 0;
            BubbleSortCommand.NotifyCanExecuteChanged();

            StartSort(() => _sorter.BubbleSort(_originalArray, p => UpdateProgress(() => BubbleProgress = p), _cts.Token, UseSharedArray));
        }

        // Быстрая сортировка
        private bool CanSortQuick() => _originalArray != null && QuickSortResult != "Сортируется...";

        [RelayCommand(CanExecute = nameof(CanSortQuick))]
        private void QuickSort()
        {
            QuickSortResult = "Сортируется...";
            QuickProgress = 0;
            QuickSortCommand.NotifyCanExecuteChanged();

            StartSort(() => _sorter.QuickSort(_originalArray, p => UpdateProgress(() => QuickProgress = p), _cts.Token, UseSharedArray));
        }

        // Сортировка вставками
        private bool CanSortInsertion() => _originalArray != null && InsertionSortResult != "Сортируется...";

        [RelayCommand(CanExecute = nameof(CanSortInsertion))]
        private void InsertionSort()
        {
            InsertionSortResult = "Сортируется...";
            InsertionProgress = 0;
            InsertionSortCommand.NotifyCanExecuteChanged();

            StartSort(() => _sorter.InsertionSort(_originalArray, p => UpdateProgress(() => InsertionProgress = p), _cts.Token, UseSharedArray));
        }

        // Шейкерная сортировка
        private bool CanSortShaker() => _originalArray != null && ShakerSortResult != "Сортируется...";

        [RelayCommand(CanExecute = nameof(CanSortShaker))]
        private void ShakerSort()
        {
            ShakerSortResult = "Сортируется...";
            ShakerProgress = 0;
            ShakerSortCommand.NotifyCanExecuteChanged();

            StartSort(() => _sorter.ShakerSort(_originalArray, p => UpdateProgress(() => ShakerProgress = p), _cts.Token, UseSharedArray));
        }

        // Команда для кнопки Прервать все
        [RelayCommand]
        private void CancelAll()
        {
            _cts?.Cancel();
        }

        // Обработчики событий (вызываются из фоновых потоков)
        private void OnBubbleSortCompleted(int[] sortedArray, long comparisons, double elapsedMs)
        {
            // Используем _uiContext.Post, чтобы обновить UI в главном потоке
            _uiContext.Post(_ =>
            {
                BubbleSortResult = $"Пузырьковая: {FormatArray(sortedArray)}, время: {elapsedMs:F2} мс, сравнений: {comparisons}";
                UpdateTotalComparisons();
                BubbleSortCommand.NotifyCanExecuteChanged();
                CheckBatchCompletion();
            }, null);
        }

        private void OnQuickSortCompleted(int[] sortedArray, long comparisons, double elapsedMs)
        {
            _uiContext.Post(_ =>
            {
                QuickSortResult = $"Быстрая: {FormatArray(sortedArray)}, время: {elapsedMs:F2} мс, сравнений: {comparisons}";
                UpdateTotalComparisons();
                QuickSortCommand.NotifyCanExecuteChanged();
                CheckBatchCompletion();
            }, null);
        }

        private void OnInsertionSortCompleted(int[] sortedArray, long comparisons, double elapsedMs)
        {
            _uiContext.Post(_ =>
            {
                InsertionSortResult = $"Вставками: {FormatArray(sortedArray)}, время: {elapsedMs:F2} мс, сравнений: {comparisons}";
                UpdateTotalComparisons();
                InsertionSortCommand.NotifyCanExecuteChanged();
                CheckBatchCompletion();
            }, null);
        }

        private void OnShakerSortCompleted(int[] sortedArray, long comparisons, double elapsedMs)
        {
            _uiContext.Post(_ =>
            {
                ShakerSortResult = $"Шейкерная: {FormatArray(sortedArray)}, время: {elapsedMs:F2} мс, сравнений: {comparisons}";
                UpdateTotalComparisons();
                ShakerSortCommand.NotifyCanExecuteChanged();
                CheckBatchCompletion();
            }, null);
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
                return string.Join(", ", arr.Take(5)) + "...";
        }

        private void StartSort(Action sortAction)
        {
            // Запускаем таймер, если это первая запущенная сортировка
            if (!_batchStopwatch.IsRunning)
            {
                _batchStopwatch.Start();
            }
            // Увеличиваем ожидаемое количество завершений
            _expectedSortsToComplete++;

            Thread thread = new Thread(() =>
            {
                _semaphore.Wait();

                try
                {
                    sortAction();
                }
                finally
                {
                    _semaphore.Release();
                }
            });

            thread.Start();
        }

        // Проверка, все ли запущенные потоки завершили работу
        private void CheckBatchCompletion()
        {
            _completedSorts++;
            if (_completedSorts == _expectedSortsToComplete)
            {
                _batchStopwatch.Stop();
                TotalElapsedTime = $"Общее время выполнения: {_batchStopwatch.Elapsed.TotalMilliseconds:F2} мс";
            }
        }
    }
}