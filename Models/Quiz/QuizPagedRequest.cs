using System.Text.Json;
using fuquizlearn_api.Enum;

namespace fuquizlearn_api.Models.Request;

public class QuizPagedRequest: PagedRequest
{
    public bool IsGetAll { get; set; } = false;
}