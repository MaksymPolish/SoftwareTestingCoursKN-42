namespace Lab1.Core;

public class Calculator
{
    // Складає два числа
    public double Add(double a, double b)
    {
        return a + b;
    }

    // Віднімає друге число від першого
    public double Subtract(double a, double b)
    {
        return a - b;
    }

    // Множить два числа
    public double Multiply(double a, double b)
    {
        return a * b;
    }

    // Ділить перше число на друге. Викидає DivideByZeroException, якщо дільник = 0
    public double Divide(double a, double b)
    {
        if (b == 0)
        {
            throw new DivideByZeroException("Cannot divide by zero.");
        }

        return a / b;
    }
}
