using UrlShortener.Core.Services;

namespace UrlShortener.Tests;

public class Base62ConverterTests
{
    // ── Encode ────────────────────────────────────────────────────────────────

    [Fact]
    public void Encode_Zero_ReturnsAllZeroes()
    {
        Assert.Equal("0000000", Base62Converter.Encode(0));
    }

    [Fact]
    public void Encode_SequenceStartValue_ReturnsKnownCode()
    {
        // Identity sequence starts at 1,000,000,000 — we verified this
        // in production ("015ftgG"), so it's a good concrete anchor
        Assert.Equal("015ftgG", Base62Converter.Encode(1_000_000_000));
    }

    [Fact]
    public void Encode_MaxSupportedValue_ReturnsAllZ()
    {
        // 62^7 - 1: every digit is 61 = 'z' in the alphabet
        Assert.Equal("zzzzzzz", Base62Converter.Encode(3_521_614_606_207L));
    }

    [Fact]
    public void Encode_NegativeNumber_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Base62Converter.Encode(-1));
    }

    [Fact]
    public void Encode_ExceedsMaxSupportedValue_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Base62Converter.Encode(3_521_614_606_208L));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(61)]
    [InlineData(62)]
    [InlineData(1_000_000_000)]
    [InlineData(3_521_614_606_207L)]
    public void Encode_ReturnsExactlySevenCharacters(long input)
    {
        Assert.Equal(7, Base62Converter.Encode(input).Length);
    }

    // ── Decode ────────────────────────────────────────────────────────────────

    [Fact]
    public void Decode_AllZeroes_ReturnsZero()
    {
        Assert.Equal(0L, Base62Converter.Decode("0000000"));
    }

    [Fact]
    public void Decode_AllZ_ReturnsMaxSupportedValue()
    {
        Assert.Equal(3_521_614_606_207L, Base62Converter.Decode("zzzzzzz"));
    }

    [Fact]
    public void Decode_EmptyString_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Base62Converter.Decode(""));
    }

    [Fact]
    public void Decode_WrongLength_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Base62Converter.Decode("abc"));
    }

    [Fact]
    public void Decode_InvalidAsciiCharacter_ThrowsArgumentException()
    {
        // '-' is valid ASCII but not in the base62 alphabet
        Assert.Throws<ArgumentException>(() => Base62Converter.Decode("abc-def"));
    }

    [Fact]
    public void Decode_NonAsciiCharacter_ThrowsArgumentException()
    {
        // 'é' is > 127 — exercises the c >= 128 guard
        Assert.Throws<ArgumentException>(() => Base62Converter.Decode("abcédef"));
    }

    // ── TryDecode ─────────────────────────────────────────────────────────────

    [Fact]
    public void TryDecode_ValidCode_ReturnsTrueAndCorrectValue()
    {
        var success = Base62Converter.TryDecode("015ftgG", out var result);
        Assert.True(success);
        Assert.Equal(1_000_000_000L, result);
    }

    [Fact]
    public void TryDecode_AllZeroes_ReturnsTrueAndZero()
    {
        var success = Base62Converter.TryDecode("0000000", out var result);
        Assert.True(success);
        Assert.Equal(0L, result);
    }

    [Fact]
    public void TryDecode_EmptyString_ReturnsFalse()
    {
        Assert.False(Base62Converter.TryDecode("", out _));
    }

    [Fact]
    public void TryDecode_NullString_ReturnsFalse()
    {
        Assert.False(Base62Converter.TryDecode(null!, out _));
    }

    [Fact]
    public void TryDecode_WrongLength_ReturnsFalse()
    {
        Assert.False(Base62Converter.TryDecode("abcdef", out _)); // 6 chars
    }

    [Fact]
    public void TryDecode_InvalidAsciiCharacter_ReturnsFalse()
    {
        Assert.False(Base62Converter.TryDecode("abc-def", out _));
    }

    [Fact]
    public void TryDecode_NonAsciiCharacter_ReturnsFalse()
    {
        Assert.False(Base62Converter.TryDecode("abcédef", out _));
    }

    [Fact]
    public void TryDecode_InvalidChar_SetsResultToZero()
    {
        Base62Converter.TryDecode("abc!def", out var result);
        Assert.Equal(0L, result);
    }

    // ── Round-trip ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(61)]
    [InlineData(62)]
    [InlineData(1_000_000_000)]
    [InlineData(1_000_000_001)]
    [InlineData(3_521_614_606_207L)]
    public void Encode_ThenDecode_RoundTrips(long original)
    {
        var encoded = Base62Converter.Encode(original);
        var decoded = Base62Converter.Decode(encoded);
        Assert.Equal(original, decoded);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1_000_000_000)]
    [InlineData(3_521_614_606_207L)]
    public void Encode_ThenTryDecode_RoundTrips(long original)
    {
        var encoded = Base62Converter.Encode(original);
        var success = Base62Converter.TryDecode(encoded, out var decoded);
        Assert.True(success);
        Assert.Equal(original, decoded);
    }
}