using System.Globalization;

namespace ParameterQc;

public enum SlotKind
{
    Filled,
    Empty,
    Excluded,
    Tbd
}

public static class ValueRules
{
    private const double ZeroTolerance = 1e-9;

    public static SlotKind ClassifyText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return SlotKind.Empty;

        string trimmed = text.Trim();
        if (IsNa(trimmed))
            return SlotKind.Excluded;
        if (IsTbd(trimmed))
            return SlotKind.Tbd;
        if (IsZero(trimmed))
            return SlotKind.Empty;

        return SlotKind.Filled;
    }

    public static SlotKind ClassifyInteger(int value, bool yesNo)
    {
        if (yesNo)
            return SlotKind.Filled;

        return value == 0 ? SlotKind.Empty : SlotKind.Filled;
    }

    public static SlotKind ClassifyDouble(double value, bool yesNo)
    {
        if (yesNo)
            return SlotKind.Filled;

        return Math.Abs(value) < ZeroTolerance ? SlotKind.Empty : SlotKind.Filled;
    }

    public static bool IsYes(string? text)
    {
        return text != null && text.Trim().Equals("YES", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsNa(string text)
    {
        return text.Equals("n/a", StringComparison.OrdinalIgnoreCase)
            || text.Equals("na", StringComparison.OrdinalIgnoreCase)
            || text.Equals("n.a.", StringComparison.OrdinalIgnoreCase)
            || text.Equals("n.a", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTbd(string text)
    {
        return text.Equals("tbd", StringComparison.OrdinalIgnoreCase)
            || text.Equals("t.b.d.", StringComparison.OrdinalIgnoreCase)
            || text.Equals("t.b.d", StringComparison.OrdinalIgnoreCase)
            || text.Equals("to be determined", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsZero(string text)
    {
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double invariant)
            && Math.Abs(invariant) < ZeroTolerance)
            return true;

        return double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out double local)
            && Math.Abs(local) < ZeroTolerance;
    }
}
