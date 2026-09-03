namespace Deployment.CLI.Localization;

public enum Language
{
    English,
    TraditionalChinese
}

public static class LanguageExtensions
{
    public static string ToCode(this Language lang) => lang switch
    {
        Language.TraditionalChinese => "zh-TW",
        _ => "en"
    };

    public static Language FromCode(string? code) => code switch
    {
        "zh-TW" => Language.TraditionalChinese,
        _ => Language.English
    };

    public static string DisplayName(this Language lang) => lang switch
    {
        Language.TraditionalChinese => "繁體中文",
        _ => "English"
    };
}
