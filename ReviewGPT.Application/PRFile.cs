namespace ReviewGPT.Application;

public class PRFile
{
    public string Name { get; set; }
    public string Diff { get; set; }
    public string Summary { get; set; }
    public string Review { get; set; }
}


public class PR
{
    public string Title { get; set; }
    public string Description { get; set; }
}

public class UrlParseResult
{
    public string Owner { get; set; }
    public string Repo { get; set; }
    public string PrNumber { get; set; }
}
