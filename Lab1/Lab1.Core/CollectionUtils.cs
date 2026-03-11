namespace Lab1.Core;

public class CollectionUtils
{
    // Обчислює середнє значення чисел. Викидає InvalidOperationException для порожньої колекції
    public double Average(IEnumerable<double> numbers)
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
    public T Max<T>(IEnumerable<T> items) where T : IComparable<T>
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

        return list.Aggregate((max, item) => item.CompareTo(max) > 0 ? item : max);
    }

    // Повертає унікальні елементи зі збереженням порядку
    public IEnumerable<T> Distinct<T>(IEnumerable<T> items)
    {
        if (items == null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        return items.Distinct();
    }

    // Розбиває колекцію на частини заданого розміру. Викидає ArgumentOutOfRangeException для size <= 0
    public IEnumerable<IEnumerable<T>> Chunk<T>(IEnumerable<T> items, int size)
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
        return Enumerable.Range(0, (list.Count + size - 1) / size)
            .Select(i => list.Skip(i * size).Take(size));
    }
}
