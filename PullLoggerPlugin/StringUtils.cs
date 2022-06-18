using System.Text;

namespace PullLogger;

// https://stackoverflow.com/a/25486
public static class StringUtils
{
    /// <summary>
    /// Produces optional, URL-friendly version of a title, "like-this-one". 
    /// hand-tuned for speed, reflects performance refactoring contributed
    /// by John Gietzen (user otac0n) 
    /// </summary>
    public static string Slugify(this string str)
    {
        const int maxLen = 80;
        var len = str.Length;
        var prevDash = false;
        var sb = new StringBuilder(len);
        for (var i = 0; i < len; i++)
        {
            var c = str[i];
            switch (c)
            {
                case >= 'a' and <= 'z':
                case >= '0' and <= '9':
                    sb.Append(c);
                    prevDash = false;
                    break;
                case >= 'A' and <= 'Z':
                    // tricky way to convert to lowercase
                    sb.Append((char)(c | 32));
                    prevDash = false;
                    break;
                default:
                    switch (c)
                    {
                        case ' ':
                        case ',':
                        case '.':
                        case '/':
                        case '\\':
                        case '-':
                        case '_':
                        case '=':
                        {
                            if (!prevDash && sb.Length > 0)
                            {
                                sb.Append('-');
                                prevDash = true;
                            }
                            break;
                        }
                        default:
                        {
                            if ((int)c >= 128)
                            {
                                var prevLen = sb.Length;
                                sb.Append(RemapInternationalCharToAscii(c));
                                if (prevLen != sb.Length) prevDash = false;
                            }
                            break;
                        }
                    }
                    break;
            }
            if (i == maxLen) break;
        }
        return prevDash ? sb.ToString()[..(sb.Length - 1)] : sb.ToString();
    }

    // https://meta.stackexchange.com/a/7696
    private static string RemapInternationalCharToAscii(char c)
    {
        var s = c.ToString().ToLowerInvariant();
        if ("àåáâäãåą".Contains(s)) return "a";
        if ("èéêëę".Contains(s)) return "e";
        if ("ìíîïı".Contains(s)) return "i";
        if ("òóôõöøőð".Contains(s)) return "o";
        if ("ùúûüŭů".Contains(s)) return "u";
        if ("çćčĉ".Contains(s)) return "c";
        if ("żźž".Contains(s)) return "z";
        if ("śşšŝ".Contains(s)) return "s";
        if ("ñń".Contains(s)) return "n";
        if ("ýÿ".Contains(s)) return "y";
        if ("ğĝ".Contains(s)) return "g";
        return c switch
        {
            'ř' => "r",
            'ł' => "l",
            'đ' => "d",
            'ß' => "ss",
            'Þ' => "th",
            'ĥ' => "h",
            'ĵ' => "j",
            _ => ""
        };
    }
}