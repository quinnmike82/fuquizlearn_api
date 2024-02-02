namespace fuquizlearn_api.Models.Response;

public class Candidate
{
    public Content content { get; set; }
    public string finishReason { get; set; }
    public int index { get; set; }
    public List<SafetyRating> safetyRatings { get; set; }
}

public class Content
{
    public List<Part> parts { get; set; }
}

public class Part
{
    public string text { get; set; }
}

public class PromptFeedback
{
    public List<SafetyRating> safetyRatings { get; set; }
}

public class GeminiAiResponse
{
    public List<Candidate> candidates { get; set; }
    public PromptFeedback promptFeedback { get; set; }
}

public class SafetyRating
{
    public string category { get; set; }
    public string probability { get; set; }
}

