using System.Text.RegularExpressions;
using Microsoft.Extensions.Primitives;

namespace RdtClient.Service.Helpers;

/// <summary>
///     Removes credentials from request data before it reaches the log file.
///     The qBittorrent API accepts <c>GET /api/v2/auth/login?username=..&amp;password=..</c> and the SabNZBd API accepts
///     <c>?apikey=user:password</c>, so request logging writes plaintext passwords to disk unless they are redacted here.
/// </summary>
public static class LogRedactor
{
    public const String Placeholder = "*****";

    /// <summary>
    ///     Parameter names whose value is a credential. <c>apikey</c> is included because the SabNZBd integration encodes
    ///     "username:password" into it.
    /// </summary>
    private static readonly HashSet<String> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "newpassword",
        "confirmpassword",
        "ma_password",
        "apikey",
        "api_key",
        "token",
        "secret"
    };

    private static readonly String SensitiveKeyPattern = String.Join('|', SensitiveKeys.Select(Regex.Escape));

    // The leading separator is captured rather than looked behind so a query string is handled with or without its "?".
    private static readonly Regex QueryStringRegex = new($@"(^|[?&])({SensitiveKeyPattern})=[^&]*",
                                                         RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex JsonRegex = new($@"(""({SensitiveKeyPattern})""\s*:\s*)""(?:[^""\\]|\\.)*""",
                                                  RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static Boolean IsSensitiveKey(String? key)
    {
        return !String.IsNullOrWhiteSpace(key) && SensitiveKeys.Contains(key.Trim());
    }

    /// <summary>
    ///     Redacts sensitive parameters in a raw query string, leaving the rest of it intact so logs stay useful.
    /// </summary>
    public static String RedactQueryString(String? queryString)
    {
        if (String.IsNullOrWhiteSpace(queryString))
        {
            return queryString ?? "";
        }

        return QueryStringRegex.Replace(queryString, match => $"{match.Groups[1].Value}{match.Groups[2].Value}={Placeholder}");
    }

    /// <summary>
    ///     Formats a form collection as "key: value" pairs, replacing the values of sensitive keys.
    /// </summary>
    public static String RedactFormValues(IEnumerable<KeyValuePair<String, StringValues>> form)
    {
        return String.Join(", ", form.Select(f => $"{f.Key}: {(IsSensitiveKey(f.Key) ? Placeholder : f.Value.ToString())}"));
    }

    /// <summary>
    ///     Redacts sensitive string properties in a JSON body. Non-JSON or malformed input is returned unchanged apart from
    ///     any matches, so this never throws on a logging path.
    /// </summary>
    public static String RedactJson(String? body)
    {
        if (String.IsNullOrWhiteSpace(body))
        {
            return body ?? "";
        }

        return JsonRegex.Replace(body, match => $"{match.Groups[1].Value}\"{Placeholder}\"");
    }
}
