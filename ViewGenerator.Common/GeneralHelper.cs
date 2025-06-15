using System.Xml;
using ViewGenerator.Common.Models;

namespace ViewGenerator.Common;

public static class GeneralHelper
{
    public static string GetLineInfoRepresentation(IXmlLineInfo lineInfo) => $"{lineInfo.LineNumber}:{lineInfo.LinePosition}";

    public static void ThrowOrInvokeErrorAction(Exception exception, ErrorHandler errorAction, bool abortOnError)
    {
        if (abortOnError)
        {
            throw exception;
        }

        errorAction(exception);
    }

    //public static Dictionary<TKey, List<TVal>> MergeDictionaries<TKey, TVal>(IEnumerable<KeyValuePair<TKey, TVal>> dictionaries) 
    //    where TKey : notnull
    //{
    //    var result = new Dictionary<TKey, List<TVal>>();

    //    foreach (var dict in dictionaries)
    //    {
    //        foreach (var kv in dict)
    //        {
    //            if (result.TryGetValue(kv.Key, out var existingValues))
    //            {
    //                existingValues.Add(kv.Value);
    //            }
    //            else
    //            {
    //                result[kv.Key] = [kv.Value];
    //            }
    //        }
    //    }

    //    return result;
    //}

    public static Dictionary<TKey, List<TVal>> MergeDictionaries<TKey, TVal>(IEnumerable<KeyValuePair<TKey, TVal>> dictionaries)
    where TKey : notnull
    {
        var result = new Dictionary<TKey, List<TVal>>();

        foreach (var dict in dictionaries)
        {
            if (result.TryGetValue(dict.Key, out var existingValues))
            {
                existingValues.Add(dict.Value);
            }
            else
            {
                result[dict.Key] = [dict.Value];
            }
        }

        return result;
    }

    public static Dictionary<string, TEnum> GetEnumKeysToValueMap<TEnum>() where TEnum : struct, Enum
        => Enum.GetValues<TEnum>().ToDictionary(x => x.ToString(), x => x);

    public static Dictionary<int, TEnum> GetEnumValueToKeysMap<TEnum>() where TEnum : struct, Enum
        => Enum.GetValues<TEnum>().ToDictionary(x => (int)(object)x, x => x);
}
