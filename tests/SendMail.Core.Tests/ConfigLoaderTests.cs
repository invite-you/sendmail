using SendMail.Core.Config;

namespace SendMail.Core.Tests;

public class ConfigLoaderTests
{
    [Fact]
    public void LoadFromJson_ParsesEmailColumnAndSubject()
    {
        const string json = """
        {
          "Excel": { "EmailColumn": "이메일" },
          "Mail": { "Subject": "[{round}회차] 고위험자 안내" }
        }
        """;

        var (config, error) = ConfigLoader.LoadFromJson(json);

        Assert.Null(error);
        Assert.NotNull(config);
        Assert.Equal("이메일", config!.Excel.EmailColumn);
        Assert.Contains("{round}", config.Mail.Subject);
    }

    [Fact]
    public void LoadFromJson_InvalidJson_ReturnsError()
    {
        const string json = "{ \"Excel\": ";

        var (config, error) = ConfigLoader.LoadFromJson(json);

        Assert.Null(config);
        Assert.NotNull(error);
    }
}
