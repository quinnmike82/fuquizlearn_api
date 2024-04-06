using fuquizlearn_api.Entities;
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
            return await _transactionService.GetAllTransaction(options, month, Account);
        }
        [HttpGet("{month:int}/{year:int}")]
        public async Task<ActionResult<ChartTransaction>> GetChart(int month, int year) { 
            return await _transactionService.GetByMonth(month, year, Account);
        }
        [HttpGet("current")]
        public async Task<ActionResult<PagedResponse<TransactionResponse>>> GetAllCurrent([FromQuery] PagedRequest options)
        {
            return await _transactionService.GetCurrentTransaction(options, Account);
        }
        [HttpPost]
        public async Task<ActionResult<TransactionResponse>> Create([FromBody] TransactionCreate trans)
        {
            return await _transactionService.CreateTransaction(trans, Account);
        }
    }
}
