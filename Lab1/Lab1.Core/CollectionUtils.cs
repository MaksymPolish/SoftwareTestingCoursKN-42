namespace Lab1.Core;

public static class CollectionUtils
{
    // Обчислює середнє значення чисел. Викидає InvalidOperationException для порожньої колекції
    public static double Average(IEnumerable<double> numbers)
    {
        if (numbers == null)
        {
            throw new ArgumentNullException(nameof(numbers));
        }

        var list = numbers.ToList();
        if (list.Count == 0)
        {
            throw new InvalidOperationException("Cannot calculate average of an empty collection.");
        }

        return list.Sum() / list.Count;
    }

    // Повертає максимальний елемент. Викидає InvalidOperationException для порожньої колекції
    public static T Max<T>(IEnumerable<T> items) where T : IComparable<T>
    {
        if (items == null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        var list = items.ToList();
        if (list.Count == 0)
        {
            throw new InvalidOperationException("Cannot find max of an empty collection.");
        }

        T max = list[0];
        foreach (var item in list.Skip(1))
        {
            if (item.CompareTo(max) > 0)
            {
                max = item;
            }
        }

        return max;
    }

    // Повертає унікальні елементи зі збереженням порядку
    public static IEnumerable<T> Distinct<T>(IEnumerable<T> items)
    {
        if (items == null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        var seen = new HashSet<T>();
        foreach (var item in items)
        {
            if (seen.Add(item))
            {
                yield return item;
            }
        }
    }

    // Розбиває колекцію на частини заданого розміру. Викидає ArgumentOutOfRangeException для size <= 0
    public static IEnumerable<IEnumerable<T>> Chunk<T>(IEnumerable<T> items, int size)
    {
        if (items == null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        if (size <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "Chunk size must be greater than 0.");
        }

        var list = items.ToList();
        for (int i = 0; i < list.Count; i += size)
        {
            yield return list.Skip(i).Take(size);
        }
    }
}
