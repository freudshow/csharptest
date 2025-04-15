using System;
using System.Collections.Generic;
using System.Linq;

internal class SortingAlgorithms
{
    // 冒泡排序
    public static void BubbleSort(int[] arr)
    {
        int n = arr.Length;
        for (int i = 0; i < n - 1; i++)
        {
            for (int j = 0; j < n - i - 1; j++)
            {
                if (arr[j] > arr[j + 1])
                {
                    int temp = arr[j];
                    arr[j] = arr[j + 1];
                    arr[j + 1] = temp;
                }
            }
        }
    }

    // 选择排序
    public static void SelectionSort(int[] arr)
    {
        int n = arr.Length;
        for (int i = 0; i < n - 1; i++)
        {
            int minIndex = i;
            for (int j = i + 1; j < n; j++)
            {
                if (arr[j] < arr[minIndex])
                {
                    minIndex = j;
                }
            }
            int temp = arr[minIndex];
            arr[minIndex] = arr[i];
            arr[i] = temp;
        }
    }

    // 插入排序
    public static void InsertionSort(int[] arr)
    {
        int n = arr.Length;
        for (int i = 1; i < n; i++)
        {
            int key = arr[i];
            int j = i - 1;
            while (j >= 0 && arr[j] > key)
            {
                arr[j + 1] = arr[j];
                j--;
            }
            arr[j + 1] = key;
        }
    }

    // 归并排序
    public static void MergeSort(int[] arr)
    {
        if (arr.Length <= 1) return;

        int mid = arr.Length / 2;
        int[] left = arr.Take(mid).ToArray();
        int[] right = arr.Skip(mid).ToArray();

        MergeSort(left);
        MergeSort(right);
        Merge(arr, left, right);
    }

    private static void Merge(int[] arr, int[] left, int[] right)
    {
        int i = 0, j = 0, k = 0;

        while (i < left.Length && j < right.Length)
        {
            if (left[i] <= right[j])
            {
                arr[k++] = left[i++];
            }
            else
            {
                arr[k++] = right[j++];
            }
        }

        while (i < left.Length)
        {
            arr[k++] = left[i++];
        }

        while (j < right.Length)
        {
            arr[k++] = right[j++];
        }
    }

    // 快速排序
    public static void QuickSort(int[] arr, int low, int high)
    {
        if (low < high)
        {
            int pi = Partition(arr, low, high);
            QuickSort(arr, low, pi - 1);
            QuickSort(arr, pi + 1, high);
        }
    }

    private static int Partition(int[] arr, int low, int high)
    {
        int pivot = arr[high];
        int i = low - 1;

        for (int j = low; j < high; j++)
        {
            if (arr[j] < pivot)
            {
                i++;
                int temp = arr[i];
                arr[i] = arr[j];
                arr[j] = temp;
            }
        }

        int temp1 = arr[i + 1];
        arr[i + 1] = arr[high];
        arr[high] = temp1;
        return i + 1;
    }

    // 堆排序
    public static void HeapSort(int[] arr)
    {
        int n = arr.Length;

        for (int i = n / 2 - 1; i >= 0; i--)
        {
            Heapify(arr, n, i);
        }

        for (int i = n - 1; i > 0; i--)
        {
            int temp = arr[0];
            arr[0] = arr[i];
            arr[i] = temp;
            Heapify(arr, i, 0);
        }
    }

    private static void Heapify(int[] arr, int n, int i)
    {
        int largest = i;
        int left = 2 * i + 1;
        int right = 2 * i + 2;

        if (left < n && arr[left] > arr[largest])
        {
            largest = left;
        }

        if (right < n && arr[right] > arr[largest])
        {
            largest = right;
        }

        if (largest != i)
        {
            int swap = arr[i];
            arr[i] = arr[largest];
            arr[largest] = swap;
            Heapify(arr, n, largest);
        }
    }

    // 基数排序
    public static void RadixSort(int[] arr)
    {
        int max = arr.Max();
        for (int exp = 1; max / exp > 0; exp *= 10)
        {
            CountingSort(arr, exp);
        }
    }

    private static void CountingSort(int[] arr, int exp)
    {
        int n = arr.Length;
        int[] output = new int[n];
        int[] count = new int[10];

        for (int i = 0; i < n; i++)
        {
            count[(arr[i] / exp) % 10]++;
        }

        for (int i = 1; i < 10; i++)
        {
            count[i] += count[i - 1];
        }

        for (int i = n - 1; i >= 0; i--)
        {
            output[count[(arr[i] / exp) % 10] - 1] = arr[i];
            count[(arr[i] / exp) % 10]--;
        }

        for (int i = 0; i < n; i++)
        {
            arr[i] = output[i];
        }
    }

    // 桶排序
    public static void BucketSort(float[] arr)
    {
        int n = arr.Length;
        if (n <= 0) return;

        List<float>[] buckets = new List<float>[n];
        for (int i = 0; i < n; i++)
        {
            buckets[i] = new List<float>();
        }

        for (int i = 0; i < n; i++)
        {
            int bucketIndex = (int)(n * arr[i]);
            if (bucketIndex >= n) bucketIndex = n - 1;
            buckets[bucketIndex].Add(arr[i]);
        }

        for (int i = 0; i < n; i++)
        {
            buckets[i].Sort();
        }

        int index = 0;
        for (int i = 0; i < n; i++)
        {
            foreach (float value in buckets[i])
            {
                arr[index++] = value;
            }
        }
    }

    // 测试函数
    private static void SortMain(string[] args)
    {
        // 各个数组大小和数值不同
        int[] arr1 = { 64, 34, 25, 12, 22, 11, 90 };
        Console.WriteLine("原始数组 1: " + string.Join(", ", arr1));
        BubbleSort(arr1);
        Console.WriteLine("冒泡排序: " + string.Join(", ", arr1));

        int[] arr2 = { 29, 10, 14, 37, 13 };
        Console.WriteLine("原始数组 2: " + string.Join(", ", arr2));
        SelectionSort(arr2);
        Console.WriteLine("选择排序: " + string.Join(", ", arr2));

        int[] arr3 = { 5, 2, 9, 1, 5, 6 };
        Console.WriteLine("原始数组 3: " + string.Join(", ", arr3));
        InsertionSort(arr3);
        Console.WriteLine("插入排序: " + string.Join(", ", arr3));

        int[] arr4 = { 38, 27, 43, 3, 9, 82, 10 };
        Console.WriteLine("原始数组 4: " + string.Join(", ", arr4));
        MergeSort(arr4);
        Console.WriteLine("归并排序: " + string.Join(", ", arr4));

        int[] arr5 = { 8, 7, 6, 1, 0, 9, 2 };
        Console.WriteLine("原始数组 5: " + string.Join(", ", arr5));
        QuickSort(arr5, 0, arr5.Length - 1);
        Console.WriteLine("快速排序: " + string.Join(", ", arr5));

        int[] arr6 = { 3, 5, 1, 10, 2, 7, 6 };
        Console.WriteLine("原始数组 6: " + string.Join(", ", arr6));
        HeapSort(arr6);
        Console.WriteLine("堆排序: " + string.Join(", ", arr6));

        int[] radixArr = { 170, 45, 75, 90, 802, 24, 2, 66 };
        Console.WriteLine("原始数组 7: " + string.Join(", ", radixArr));
        RadixSort(radixArr);
        Console.WriteLine("基数排序: " + string.Join(", ", radixArr));

        float[] bucketArr = { 0.78f, 0.17f, 0.39f, 0.26f, 0.72f, 0.94f, 0.21f };
        Console.WriteLine("原始数组 8: " + string.Join(", ", bucketArr));
        BucketSort(bucketArr);
        Console.WriteLine("桶排序: " + string.Join(", ", bucketArr));

        Console.ReadLine();
    }
}