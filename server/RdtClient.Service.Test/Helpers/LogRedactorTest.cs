using Microsoft.Extensions.Primitives;
using RdtClient.Service.Helpers;

namespace RdtClient.Service.Test.Helpers;

public class LogRedactorTest
{
    [Fact]
    public void RedactQueryString_RedactsQBittorrentLoginPassword()
    {
        // The qBittorrent API accepts credentials on the query string of a GET, which request logging wrote to disk.
        var result = LogRedactor.RedactQueryString("?username=admin&password=hunter2");

        Assert.Equal("?username=admin&password=*****", result);
        Assert.DoesNotContain("hunter2", result);
    }

    [Fact]
    public void RedactQueryString_RedactsSabnzbdApiKey()
    {
        // SabNZBd encodes "username:password" into the apikey parameter.
        var result = LogRedactor.RedactQueryString("?mode=queue&apikey=admin:hunter2&output=json");

        Assert.Equal("?mode=queue&apikey=*****&output=json", result);
        Assert.DoesNotContain("hunter2", result);
    }

    [Fact]
    public void RedactQueryString_RedactsMaPassword()
    {
        var result = LogRedactor.RedactQueryString("?ma_username=admin&ma_password=hunter2");

        Assert.Equal("?ma_username=admin&ma_password=*****", result);
    }

    [Fact]
    public void RedactQueryString_RedactsFirstParameterWithoutLeadingQuestionMark()
    {
        Assert.Equal("password=*****", LogRedactor.RedactQueryString("password=hunter2"));
        Assert.Equal("?password=*****", LogRedactor.RedactQueryString("?password=hunter2"));
    }

    [Fact]
    public void RedactQueryString_IsCaseInsensitive()
    {
        Assert.Equal("?PassWord=*****", LogRedactor.RedactQueryString("?PassWord=hunter2"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void RedactQueryString_HandlesEmptyInput(String? queryString)
    {
        Assert.Equal("", LogRedactor.RedactQueryString(queryString));
    }

    [Fact]
    public void RedactQueryString_LeavesNonSensitiveParametersIntact()
    {
        const String queryString = "?mode=addurl&name=some.nzb&cat=tv&priority=1";

        Assert.Equal(queryString, LogRedactor.RedactQueryString(queryString));
    }

    [Fact]
    public void RedactFormValues_RedactsSensitiveKeysOnly()
    {
        var form = new List<KeyValuePair<String, StringValues>>
        {
            new("username", "admin"),
            new("password", "hunter2")
        };

        var result = LogRedactor.RedactFormValues(form);

        Assert.Equal("username: admin, password: *****", result);
        Assert.DoesNotContain("hunter2", result);
    }

    [Fact]
    public void RedactJson_RedactsPasswordProperty()
    {
        var result = LogRedactor.RedactJson("""{"userName":"admin","password":"hunter2"}""");

        Assert.Equal("""{"userName":"admin","password":"*****"}""", result);
        Assert.DoesNotContain("hunter2", result);
    }

    [Fact]
    public void RedactJson_HandlesWhitespaceAndEscapedQuotes()
    {
        var result = LogRedactor.RedactJson("""{ "password" : "hun\"ter2", "cat": "tv" }""");

        Assert.Equal("""{ "password" : "*****", "cat": "tv" }""", result);
        Assert.DoesNotContain("hun", result);
    }

    [Fact]
    public void RedactJson_LeavesNonSensitiveJsonIntact()
    {
        const String body = """{"magnetLink":"magnet:?xt=urn:btih:abc","category":"tv"}""";

        Assert.Equal(body, LogRedactor.RedactJson(body));
    }

    [Theory]
    [InlineData("password", true)]
    [InlineData("ApiKey", true)]
    [InlineData("ma_password", true)]
    [InlineData("username", false)]
    [InlineData("category", false)]
    [InlineData(null, false)]
    public void IsSensitiveKey_ClassifiesKeys(String? key, Boolean expected)
    {
        Assert.Equal(expected, LogRedactor.IsSensitiveKey(key));
    }
}
