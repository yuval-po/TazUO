using System;
using System.Linq;

namespace ClassicUO.Common.Enums;

public static class EnumUtils
{
    public static TEnum AllBits<TEnum>() where TEnum : struct, Enum =>
        Enum.GetValues<TEnum>().Aggregate(default(TEnum), AddFlag);

    public static bool HasFlag<TEnum>(TEnum value, TEnum flag) where TEnum : struct, Enum
    {
        ulong valueBits = Convert.ToUInt64(value);
        ulong flagBits = Convert.ToUInt64(flag);

        return (valueBits & flagBits) == flagBits;
    }

    public static TEnum AddFlag<TEnum>(TEnum value, TEnum flag) where TEnum : struct, Enum
    {
        ulong valueBits = Convert.ToUInt64(value);
        ulong flagBits = Convert.ToUInt64(flag);

        return (TEnum)Enum.ToObject(typeof(TEnum), valueBits | flagBits);
    }

    public static TEnum RemoveFlag<TEnum>(TEnum value, TEnum flag) where TEnum : struct, Enum
    {
        ulong valueBits = Convert.ToUInt64(value);
        ulong flagBits = Convert.ToUInt64(flag);

        return (TEnum)Enum.ToObject(typeof(TEnum), valueBits & ~flagBits);
    }
}
