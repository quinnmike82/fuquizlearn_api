namespace fuquizlearn_api.Models.Response;

public class PagedMetadata
{
    public PagedMetadata(int limit, int take, int totalPages, string nextPage = "", string previousPage = "")
    {
        Limit = limit;
        Take = take;
        TotalPages = totalPages;
    }

    public int Limit { get; set; }
    public int Take { get; set; }
    public int TotalPages { get; set; }
}

public class PagedResponse<T>
{
    public required PagedMetadata Metadata { get; set; }

    public required IEnumerable<T> Data { get; set; }
}