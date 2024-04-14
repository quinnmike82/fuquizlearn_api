using fuquizlearn_api.Entities;
using fuquizlearn_api.Models.Accounts;
using fuquizlearn_api.Models.QuizBank;
using System.ComponentModel.DataAnnotations;

namespace fuquizlearn_api.Models.Report
{
    public class ReportResponse
    {
        public int Id { get; set; }
        public AccountResponse? Account { get; set; }
        public QuizBankResponse? QuizBank { get; set; }
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public AccountResponse? Owner { get; set; }
        public string Reason { get; set; }
        public bool IsActive { get; set; }
    }
}
