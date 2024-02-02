namespace fuquizlearn_api.Models.Request;

public class Content
{
    public List<Part> parts { get; set; }
}

public class Part
{
    public string text { get; set; }
    public InlineData inline_data { get; set; }
}

public class InlineData
{
    public string mime_type { get; set; }
    public byte[] data { get; set; }
}

public class GeminiAiRequest
{
    public List<Content> contents { get; set; }
}