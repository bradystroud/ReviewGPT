namespace ReviewGPT.WebAPI;

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