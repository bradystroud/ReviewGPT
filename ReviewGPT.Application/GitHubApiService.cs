namespace ReviewGPT.Application;

public class GitHubApiService
{
    private readonly HttpClient _httpClient;

    public GitHubApiService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "YourAppName");
    }

    public async Task<string> GetStringAsync(string url)
    {
        try
        {
            return await _httpClient.GetStringAsync(url);
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    public void SetGitHubDiffAcceptHeader()
    {
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.VERSION.diff");
    }

    public void RemoveGitHubDiffAcceptHeader()
    {
        _httpClient.DefaultRequestHeaders.Remove("Accept");
    }
}