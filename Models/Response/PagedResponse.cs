namespace fuquizlearn_api.Models.Response;

public class PagedMetadata
{
    public PagedMetadata(int skip, int take, int totals, bool hasMore)
    {
        Skip = skip;
        Take = take;
        Totals = totals;
        HasMore = hasMore;
    }

    public int Skip { get; set; }
    public int Take { get; set; }
    public int Totals { get; set; }
    public bool HasMore { get; set; }
}

public class PagedResponse<T>
{
    public required PagedMetadata Metadata { get; set; }

    public required IEnumerable<T> Data { get; set; }
}