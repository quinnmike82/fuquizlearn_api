using System.Text.Json;
using fuquizlearn_api.Enum;

namespace fuquizlearn_api.Models.Request;

public class PagedRequest
{
    public int Take { get; set; } = 10;
    public int Skip { get; set; } = 0;

    public string SortBy { get; set; } = "";
    public SortDirectionEnum SortDirection { get; set; } = SortDirectionEnum.Asc;

    public string Search { get; set; } = "";
    public bool IsGetAll { get; set; } = false;

    public override string ToString()
    {
        return JsonSerializer.Serialize(this);
    }
}