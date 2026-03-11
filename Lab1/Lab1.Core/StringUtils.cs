namespace Lab1.Core;

public class StringUtils
{
    // Робить першу літеру кожного слова великою
    public string Capitalize(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        return string.Join(" ", input.Split(' ')
            .Select(word => word.Length == 0 ? word : char.ToUpper(word[0]) + word.Substring(1).ToLower()));
    }

    // Переворотить рядок навпаки
    public string Reverse(string input)
    {
        if (input == null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        return new string(input.Reverse().ToArray());
    }

    // Перевіряє, чи є рядок паліндромом (без урахування регістру)
    public bool IsPalindrome(string input)
    {
        if (input == null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        var normalized = input.ToLower();
        var reversed = Reverse(normalized);
        return normalized == reversed;
    }

    // Обрізає рядок до максимальної довжини та додає "..." якщо потрібно
    public string Truncate(string input, int maxLength)
    {
        if (input == null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        if (maxLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxLength), "Max length cannot be negative.");
        }

        if (input.Length <= maxLength)
        {
            return input;
        }

        return maxLength < 3 ? input.Substring(0, maxLength) : input.Substring(0, maxLength - 3) + "...";
    }
}
