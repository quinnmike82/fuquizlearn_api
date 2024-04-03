﻿namespace fuquizlearn_api.Entities
{
    public class Report
    {
        public int Id { get; set; }
        public Account? Account { get; set; }
        public QuizBank? QuizBank  { get; set; }
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public Account? Owner { get; set; }
        public string Reason { get; set; }
    }
}