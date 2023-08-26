namespace ReviewGPT.Application;

public static class GitHubUrlParse
{
    public static UrlParseResult Parse(string url)
    {
        var parts = url.Split('/');
        return new UrlParseResult
        {
            Owner = parts[3],
            Repo = parts[4],
            PrNumber = parts[6],
        };
    }
}
