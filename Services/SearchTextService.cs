﻿using AutoMapper;
using fuquizlearn_api.Entities;
using fuquizlearn_api.Helpers;
using fuquizlearn_api.Models.Classroom;
using fuquizlearn_api.Models.Posts;
using fuquizlearn_api.Models.Quiz;
using fuquizlearn_api.Models.QuizBank;
using fuquizlearn_api.Models.Request;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace fuquizlearn_api.Services
{
    public interface ISearchTextService
    {
        Task<Dictionary<string, List<object>>> Search(PagedRequest options);
    }

    public class SearchTextService : ISearchTextService
    {
        private readonly DataContext _context;
        private IMapper _mapper;

        public SearchTextService(DataContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<Dictionary<string, List<object>>> Search(PagedRequest options )
        {
            var results = new Dictionary<string, List<object>>();

            var entitiesToSearch = new Dictionary<Type, string[]>
            {
                { typeof(Quiz), new[] { "\"Question\"", "\"Answer\"" } },
                { typeof(QuizBank), new[] { "\"BankName\"", "\"Description\"", "array_to_string(\"Tags\", ' ')" } },
                { typeof(Post), new[] { "\"Title\"", "\"Content\"" } },
                { typeof(Classroom), new[] { "\"Classname\"", "\"Description\"" } },
            };

            foreach (var (entityType, propertyNames) in entitiesToSearch)
            {
                var tableName = _context.Model.FindEntityType(entityType).GetTableName();

                // Construct the search expression
                var pgSearchExpression = string.Join(" || ' ' || ", propertyNames.Select(p => $"{p}"));
                var searchTermTokensAnd = options.Search.Split(' ').Select(token => token.Trim());
                var searchTermTokensOr = options.Search.Split(' ').Select(token => token.Trim());

                var searchTermAndQuery = string.Join(" & ", searchTermTokensAnd.Select(token => $"{token}"));
                var searchTermOrQuery = string.Join(" | ", searchTermTokensOr.Select(token => $"{token}"));

                var query = $@"
                    SELECT ""Id""
                    FROM ""{tableName}""
                    WHERE to_tsvector({pgSearchExpression}) @@ to_tsquery('{searchTermAndQuery}') OR
                          to_tsvector({pgSearchExpression}) @@ to_tsquery('{searchTermOrQuery}')
                    OFFSET {options.Skip}
                    LIMIT {options.Take};
                ";

                try
                {
                    var ids = await GetIdsFromQuery(query);
                    var objects = await GetObjectsByIds(entityType, ids);
                    results.Add(objects.Keys.First(),objects.Values.First());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during search for {entityType}: {ex.Message}");
                    // Handle or log the exception as needed
                }
            }

            return results;
        }

        private async Task<List<int>> GetIdsFromQuery(string query)
        {
            var ids = new List<int>();

            using (var command = _context.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = query;
                await _context.Database.OpenConnectionAsync();

                using (var result = await command.ExecuteReaderAsync())
                {
                    while (result.Read())
                    {
                        ids.Add(result.GetInt32(0));
                    }
                }
            }

            return ids;
        }

        private async Task<Dictionary<string, List<object>>> GetObjectsByIds(Type entityType, List<int> ids)
        {
            var objects = new Dictionary<string, List<object>>();

            if (entityType == typeof(Quiz))
            {
                var quizes = await _context.Quizes.Where(q => ids.Contains(q.Id)).ToListAsync();
                var quizResponses = _mapper.Map<List<QuizResponse>>(quizes);
                objects.Add("Quizes", quizResponses.Cast<object>().ToList());
            }
            else if (entityType == typeof(QuizBank))
            {
                var quizBanks = await _context.QuizBanks.Where(q => ids.Contains(q.Id)).ToListAsync();
                var quizBankResponses = _mapper.Map<List<QuizBankResponse>>(quizBanks);
                objects.Add("QuizBanks", quizBankResponses.Cast<object>().ToList());
            }
            else if (entityType == typeof(Post))
            {
                var posts = await _context.Posts.Where(q => ids.Contains(q.Id)).ToListAsync();
                var postResponses = _mapper.Map<List<PostResponse>>(posts);
                objects.Add("Posts", postResponses.Cast<object>().ToList());
            }
            else if (entityType == typeof(Classroom))
            {
                var classrooms = await _context.Classrooms.Where(q => ids.Contains(q.Id)).ToListAsync();
                var classroomResponses = _mapper.Map<List<ClassroomResponse>>(classrooms);
                objects.Add("Classrooms", classroomResponses.Cast<object>().ToList());
            }

            return objects;
        }

    }
}