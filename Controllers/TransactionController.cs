﻿using fuquizlearn_api.Entities;
using fuquizlearn_api.Models.Request;
using fuquizlearn_api.Models.Response;
using fuquizlearn_api.Models.Transaction;
using fuquizlearn_api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace fuquizlearn_api.Controllers
{
    [ApiController]
    public class TransactionController : BaseController
    {
        private readonly ITransactionService _transactionService;
        public TransactionController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }
        [HttpGet("{month:int}")]
        public async Task<ActionResult<PagedResponse<TransactionResponse>>> GetAll(int month, [FromQuery] PagedRequest options)
        {
            var result = await _transactionService.GetAllTransaction(options, month, Account);
            return Ok(result);
        }
        [HttpGet]
        public async Task<ActionResult<PagedResponse<TransactionResponse>>> GetAll( [FromQuery] PagedRequest options)
        {
            var result = await _transactionService.GetAllTransaction(options, Account);
            return Ok(result);
        }
        [HttpGet("getbyyear/{year:int}")]
        public async Task<ActionResult<List<ChartTransaction>>> GetChart(int year) { 
            var result = await _transactionService.GetByYear(year, Account);
            return Ok(result);
        }
        [HttpGet("current")]
        public async Task<ActionResult<PagedResponse<TransactionResponse>>> GetAllCurrent([FromQuery] PagedRequest options)
        {
            var result = await _transactionService.GetCurrentTransaction(options, Account);
            return Ok(result);
        }
        [HttpPost]
        public async Task<ActionResult<TransactionResponse>> Create([FromBody] TransactionCreate trans)
        {
            var result = await _transactionService.CreateTransaction(trans, Account);
            return Ok(result);
        }
    }
}
