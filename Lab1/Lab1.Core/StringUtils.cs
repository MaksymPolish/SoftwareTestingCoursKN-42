namespace Lab1.Core;

public static class StringUtils
{
    // Робить першу літеру кожного слова великою
    public static string Capitalize(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        var words = input.Split(' ');
        var capitalizedWords = words.Select(word =>
        {
            if (word.Length == 0)
            {
                return word;
            }

            return char.ToUpper(word[0]) + word.Substring(1).ToLower();
        });

        return string.Join(" ", capitalizedWords);
    }

    // Переворотить рядок навпаки
    public static string Reverse(string input)
    {
        if (input == null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        var charArray = input.ToCharArray();
        Array.Reverse(charArray);
        return new string(charArray);
    }

    // Перевіряє, чи є рядок паліндромом (без урахування регістру)
    public static bool IsPalindrome(string input)
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
    public static string Truncate(string input, int maxLength)
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

        if (maxLength < 3)
        {
            return input.Substring(0, maxLength);
        }

        return input.Substring(0, maxLength - 3) + "...";
    }
}
