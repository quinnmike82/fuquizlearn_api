namespace fuquizlearn_api.Models.Gemeni
{
    public class EmbedContentRequest
    {
        public string TaskType { get; set; }
        public string? Title { get; set; }
        public Content Content { get; set; }    
    }

    public static class TaskType
    {
        // Specifies the given text will be used for Semantic Textual Similarity (STS).
        public static readonly string SEMANTIC_SIMILARITY = "SEMANTIC_SIMILARITY";

        // Specifies that the embeddings will be used for classification.
        public static readonly string CLASSIFICATION = "CLASSIFICATION"; // Specifies that the embeddings will be used for clustering.
        public static readonly string CLUSTERING = "CLUSTERING";
    }

    public class Part
    {
        public string Text { get; set; }
    }

    public class Content
    {
        public Part[] Parts { get; set; }
        public string? Role { get; set; }   
    }
}