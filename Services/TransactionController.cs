using fuquizlearn_api.Controllers;
using fuquizlearn_api.Models.Request;
using fuquizlearn_api.Models.Response;
using fuquizlearn_api.Models.Transaction;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace fuquizlearn_api.Services
{
    [ApiController]
    public class TransactionController : BaseController
    {
        private readonly ITransactionService _transactionService;
        public TransactionController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }
        [HttpGet]
        public async Task<ActionResult<PagedResponse<TransactionResponse>>> GetAll([FromQuery] PagedRequest options) 
        {
            return await _transactionService.GetAllTransaction(options, Account);
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
