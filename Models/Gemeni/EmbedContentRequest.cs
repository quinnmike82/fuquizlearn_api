namespace fuquizlearn_api.Models.Gemeni
{
    public class EmbedContentRequest
    {
        public TaskType TaskType { get; set; }
        public string? Title { get; set; }
        public Content Content { get; set; }    
    }

    public enum TaskType
    {
        // Specifies the given text is a query in a search/retrieval setting.
        RETRIEVAL_QUERY,

        // Specifies the given text is a document in a search/retrieval setting.
        RETRIEVAL_DOCUMENT,

        // Specifies the given text will be used for Semantic Textual Similarity (STS).
        SEMANTIC_SIMILARITY,

        // Specifies that the embeddings will be used for classification.
        CLASSIFICATION,

        // Specifies that the embeddings will be used for clustering.
        CLUSTERING,
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