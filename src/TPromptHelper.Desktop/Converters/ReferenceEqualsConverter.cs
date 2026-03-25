using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

namespace TPromptHelper.Desktop.Converters;

/// <summary>
/// MultiBinding 转换器：values[0] 与 values[1] 是同一对象引用时返回 true
/// </summary>
public class ReferenceEqualsConverter : IMultiValueConverter
{
    public static ReferenceEqualsConverter Instance { get; } = new();
    public static ReferenceEqualsConverter NotInstance { get; } = new() { Negate = true };

    public bool Negate { get; init; }

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        var result = values.Count == 2 && ReferenceEquals(values[0], values[1]);
        return Negate ? !result : result;
    }
}
