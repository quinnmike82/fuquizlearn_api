using System.ComponentModel.DataAnnotations;

namespace fuquizlearn_api.Models.Report;

public class ReportDelete
{
    [Required]
    [MinLength(1)]
    public List<int> ReportIds { get; set; }
}