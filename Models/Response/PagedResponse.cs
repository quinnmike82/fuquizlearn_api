namespace fuquizlearn_api.Models.Response;

public class PagedMetadata
{
    public PagedMetadata(int skip, int take, int totalPages)
    {
        Skip = skip;
        Take = take;
        TotalPages = totalPages;
    }

    public int Skip { get; set; }
    public int Take { get; set; }
    public int TotalPages { get; set; }
}

public class PagedResponse<T>
{
    public required PagedMetadata Metadata { get; set; }

    public required IEnumerable<T> Data { get; set; }
}