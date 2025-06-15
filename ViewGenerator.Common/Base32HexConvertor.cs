namespace ViewGenerator.Common;

public class Base32HexConverter
{
    private readonly char[] _base32HexAlphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUV".ToCharArray();

    public string ConvertToBase32Hex(int number)
    {
        if (number < 0)
        {
            throw new ArgumentException("Number must be non-negative.", nameof(number));
        }

        if (number == 0)
        {
            return "0";
        }

        var result = new System.Text.StringBuilder();
        while (number > 0)
        {
            int remainder = number % 32;
            result.Insert(0, _base32HexAlphabet[remainder]);
            number /= 32;
        }

        return result.ToString();
    }
}
