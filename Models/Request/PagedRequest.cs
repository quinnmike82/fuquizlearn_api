using System.Text.Json;
using fuquizlearn_api.Enum;

namespace fuquizlearn_api.Models.Request;

public class PagedRequest
{
    public int Take { get; set; } = 1;
    public int Limit { get; set; } = 10;

    public string SortBy { get; set; } = "";
    public string SortDirection { get; set; } = SortDirectionEnum.Ascending.ToString();

    public string Search { get; set; } = "";

    public override string ToString()
    {
        return JsonSerializer.Serialize(this);
    }
}