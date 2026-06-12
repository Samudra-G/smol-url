namespace UrlShortener.Core.Services;
public static class Base62Converter
{
    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    private const int TargetLength = 7;
    
    // 62^7 - 1 = 3,521,614,606,207
    private const long MaxSupportedValue = 3521614606207L;

    // O(1) ASCII lookup table
    private static readonly int[] s_charToValueMap = CreateLookupTable();

    private static int[] CreateLookupTable()
    {
        var map = new int[128];
        Array.Fill(map, -1);
        for(int i = 0; i < Alphabet.Length; i++)
        {
            map[Alphabet[i]] = i;
        }

        return map;
    }

    /// <summary>
    /// Encodes a long integer into a 7-char long Base62 string
    /// </summary>
    public static string Encode(long number)
    {
        if(number < 0) 
            throw new ArgumentOutOfRangeException(nameof(number), "Negative number not allowed");
        if(number > MaxSupportedValue)
            throw new ArgumentOutOfRangeException(nameof(number), $"Value exceeds the 7-character Base62 maximum limit of {MaxSupportedValue}.");


        Span<char> buffer = stackalloc char[7];

        for(int i = TargetLength-1; i >=0; i--)
        {
            buffer[i] = Alphabet[(int)(number % 62)];
            number /= 62;
        }

        return new string(buffer);
    }

    /// <summary>
    /// Decodes a 7-char Base62 string back into a long integer.
    /// </summary>
    public static long Decode(string base62String)
    {
        if(string.IsNullOrEmpty(base62String) || base62String.Length != TargetLength)
            throw new ArgumentException($"Input must be exactly {TargetLength} long");

        long result = 0;
        foreach(char c in base62String)
        {
            if(c >= 128) 
                throw new ArgumentException($"Non-ASCII character encountered: {c}");
            
            int val = s_charToValueMap[c];
            if(val == -1)
                throw new ArgumentException($"Invalid Base 62 character encountered: {c}");

            result = (result * 62) + val;
        }

        return result;
    }

    /// <summary>
    /// Safely attempts to decode a 7-char Base62 string.
    /// Highly optimized for public, high-throughput redirect endpoints (10k+ RPS).
    /// </summary>
    public static bool TryDecode(string base62String, out long result)
    {
        result = 0;
        if(string.IsNullOrEmpty(base62String) || base62String.Length != TargetLength)
            return false;

        long accumulated = 0;
        foreach(char c in base62String)
        {
            if(c >= 128) return false;
            int val = s_charToValueMap[c];
            if(val == -1) return false;

            accumulated = (accumulated * 62) + val;
        }

        if(accumulated > MaxSupportedValue)
            return false;

        result = accumulated;
        return true;
    }
}   