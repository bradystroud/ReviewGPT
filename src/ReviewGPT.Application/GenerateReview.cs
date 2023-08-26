using Microsoft.Extensions.Configuration;
using System.Text.Json;
using OpenAI;
using OpenAI.Managers;
using OpenAI.ObjectModels;
using OpenAI.ObjectModels.RequestModels;

namespace ReviewGPT.Application;

public class GenerateReviewService
{
    private readonly string _aiModel;
    private readonly OpenAIService _openAiService;

    public GenerateReviewService()
    {
        _aiModel = Models.Gpt_3_5_Turbo;
        var config = InitConfig();
        var openAiApiKey = GetOpenAiApiKey(config);
        
        _openAiService = InitOpenAiService(openAiApiKey);
    }
    
    public async Task<string> GenerateReview(string url)
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "ReviewGPT");

        var gitHubApiService = new GitHubApiService();

        var urlParseResult = GitHubURLParse.Parse(url);

        var prDetails = await FetchPrDetails(urlParseResult, gitHubApiService);
        var prFiles = await FetchPrFilesChanged(gitHubApiService, urlParseResult, prDetails);

        var highLevelReviewPrompt = Prompts.GetHighLevelReviewPrompt(prDetails.Title, prDetails.Description);

        var changeSummaries = JsonSerializer.Serialize(prFiles.Select(f => f.Summary));

        var completionResult = await _openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
        {
            Messages = new List<ChatMessage>
            {
                ChatMessage.FromSystem(highLevelReviewPrompt),
                ChatMessage.FromUser(changeSummaries),
            },
            Model = _aiModel
        });

        var reviewResponse = completionResult.Choices.First().Message.Content;

        if (!completionResult.Successful) return "";
        
        Console.WriteLine(reviewResponse);
        foreach (var prFile in prFiles)
        {
            Console.WriteLine(prFile.Review);
        }

        return $"{reviewResponse} {string.Join(", ", prFiles.Select(p => p.Review))}";
    }

    private static IConfiguration InitConfig() => new ConfigurationBuilder().AddUserSecrets<GenerateReviewService>().Build();
    private static string GetOpenAiApiKey(IConfiguration c) => c.GetSection("OpenAI")["ApiKey"];
    private static OpenAIService InitOpenAiService(string apiKey) => new OpenAIService(new OpenAiOptions { ApiKey = apiKey });

    private static async Task<PR> FetchPrDetails(UrlParseResult urlParseResult, GitHubApiService gitHubApiService1)
    {
        var apiUrl = $"https://api.github.com/repos/{urlParseResult.Owner}/{urlParseResult.Repo}/pulls/{urlParseResult.PrNumber}";
        var response = await gitHubApiService1.GetStringAsync(apiUrl);
        var prObject = JsonSerializer.Deserialize<JsonElement>(response);

        return new PR
        {
            Title = prObject.GetProperty("title").GetString(),
            Description = prObject.GetProperty("body").GetString()
        };
    }

    private async Task<List<PRFile>> FetchPrFilesChanged(GitHubApiService gitHubApiService2, UrlParseResult urlParseResult, PR prDetails)
    {
        gitHubApiService2.SetGitHubDiffAcceptHeader();
        var filesChangedApiUrl = $"https://api.github.com/repos/{urlParseResult.Owner}/{urlParseResult.Repo}/pulls/{urlParseResult.PrNumber}/files";
        var filesChangedResponse = await gitHubApiService2.GetStringAsync(filesChangedApiUrl);
        gitHubApiService2.RemoveGitHubDiffAcceptHeader();

        var jsonElement = JsonSerializer.Deserialize<JsonElement>(filesChangedResponse);
        var list = new List<PRFile>();

        foreach (var file in jsonElement.EnumerateArray())
        {
            var filename = file.GetProperty("filename").GetString();
            if (filename.Equals("yarn.lock")) continue;

            try
            {
                var patch = file.GetProperty("patch").GetString();

                list.Add(new PRFile
                {
                    Name = filename,
                    Diff = patch,
                    Summary = await GetSummary(filename, patch, prDetails.Title, prDetails.Description),
                    Review = await GetLowLevelReview(filename, patch, prDetails.Title, prDetails.Description)
                });
            }
            catch (KeyNotFoundException)
            {
                Console.WriteLine($"Skipping {filename}, no patch found");
            }
        }

        return list;
    }

    private async Task<string> GetSummary(string filename, string diff, string title, string description)
    {
        // Prepare the input for GPT
        var summaryPrompt = Prompts.GetSummaryPrompt(title, description);


        var completionResult = await _openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
        {
            Messages = new List<ChatMessage>
            {
                ChatMessage.FromSystem(summaryPrompt),
                ChatMessage.FromUser("Filename: " + filename),
                ChatMessage.FromUser("Diff: " + diff)
            },
            Model = _aiModel
        });

        var messageContent = completionResult.Choices.First().Message.Content;

        if (completionResult.Successful)
        {
            Console.WriteLine(messageContent);
        }

        return messageContent;
    }

    private async Task<string> GetLowLevelReview(string filename, string diff, string title, string description)
    {
        var reviewPrompt = Prompts.GetReviewPrompt(title, description);

        var completionResult = await _openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
        {
            Messages = new List<ChatMessage>
            {
                ChatMessage.FromSystem(reviewPrompt),
                ChatMessage.FromUser("Filename: " + filename),
                ChatMessage.FromUser("Diff: " + diff)
            },
            Model = _aiModel
        });

        var messageContent = completionResult.Choices.First().Message.Content;

        if (completionResult.Successful) return messageContent;

        Console.WriteLine("Error getting low level review");
        Console.WriteLine(completionResult.Error.Message);

        throw new Exception(completionResult.Error.Message);
    }
}
