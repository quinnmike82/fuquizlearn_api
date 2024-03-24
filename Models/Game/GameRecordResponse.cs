using fuquizlearn_api.Entities;
using fuquizlearn_api.Models.Accounts;
using fuquizlearn_api.Models.Quiz;
using System.ComponentModel.DataAnnotations;

namespace fuquizlearn_api.Models.Classroom
{
    public class GameRecordResponse
    {
        public int Id { get; set; }
        public int GameId { get; set; }
        public GameResponse Game { get; set; }
        public int AccountId { get; set; }
        public AccountResponse Account { get; set; }
        public int TotalMark {  get; set; }
        public DateTime Created { get; set; }
        public DateTime? Updated { get; set; }
    }
}
