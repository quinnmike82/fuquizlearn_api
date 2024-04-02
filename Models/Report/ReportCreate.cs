using System.ComponentModel.DataAnnotations;

namespace fuquizlearn_api.Models.Report
{
    public class ReportCreate
    {
        public string? AccountId { get; set; }
        public string? QuizBankId { get; set; }
        [Required]
        public string Reason { get; set;}
    }
}
