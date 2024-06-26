﻿using fuquizlearn_api.Entities;
using fuquizlearn_api.Models.Accounts;
using fuquizlearn_api.Models.Classroom;
using fuquizlearn_api.Models.QuizBank;

namespace fuquizlearn_api.Models.Posts
{
    public class PostResponse
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public ClassroomResponse Classroom { get; set; }
        public string Content { get; set; }
        public AccountResponse? Author { get; set; }
        public List<CommentResponse>? Comments { get; set; }
        public string? GameLink { get; set; }
        public string? BankLink { get; set; }
        public QuizBankResponse? QuizBank { get; set; }
        public GameResponse? Game { get; set; }
        public int View { get; set; }
        public DateTime Created { get; set; } 
        public DateTime? Updated { get; set; }
    }
}
