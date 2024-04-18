using fuquizlearn_api.Models.Transaction;
using fuquizlearn_api.Entities;
using fuquizlearn_api.Models.Response;
using fuquizlearn_api.Models.Request;
using fuquizlearn_api.Helpers;
using AutoMapper;
using fuquizlearn_api.Models.Classroom;
using Microsoft.EntityFrameworkCore;
using fuquizlearn_api.Extensions;
using System.Text;
using System.Web;

namespace fuquizlearn_api.Services
{
    public interface ITransactionService
    {
        Task<TransactionResponse> CreateTransaction(TransactionCreate transactionCreate, Account account);
        Task<PagedResponse<TransactionResponse>> GetCurrentTransaction(PagedRequest options, Account account);
        Task<PagedResponse<TransactionResponse>> GetAllTransaction(PagedRequest options,int month, Account account);
        Task<PagedResponse<TransactionResponse>> GetAllTransaction(PagedRequest options, Account account);
        Task<List<ChartTransaction>> GetByYear(int year, Account account);
    }
    public class TransactionService : ITransactionService
    {
        private readonly DataContext _context;
        private readonly IMapper _mapper;
        public TransactionService(DataContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        public async Task<TransactionResponse> CreateTransaction(TransactionCreate transactionCreate, Account account)
        {
            var trans = _mapper.Map<Transaction>(transactionCreate);
            trans.Account = account;
            _context.Transactions.Add(trans);
            await _context.SaveChangesAsync();
            var month = trans.Created.Month;
            var year = trans.Created.Year;
            var chart = await _context.ChartTransactions.FirstOrDefaultAsync(c => c.Month == month && c.Year == year);
            if(chart == null)
            {
                chart = new ChartTransaction
                {
                    Amount = trans.Amount,
                    Month = month,
                    Year = year
                };
                _context.ChartTransactions.Add(chart);
            }
            else
            {
                chart.Amount = chart.Amount + trans.Amount;
                _context.Update(chart);
            }
            await _context.SaveChangesAsync();
            return _mapper.Map<TransactionResponse>(trans);
        }

        public async Task<PagedResponse<TransactionResponse>> GetCurrentTransaction(PagedRequest options, Account account)
        {
            var trans = await _context.Transactions.Include(c => c.Account).Where(c => c.Account.Id == account.Id)
                                                     .ToPagedAsync(options,
                x => x.Email.ToLower().Contains(HttpUtility.UrlDecode(options.Search, Encoding.ASCII).ToLower()));

            return new PagedResponse<TransactionResponse>
            {
                Data = _mapper.Map<IEnumerable<TransactionResponse>>(trans.Data),
                Metadata = trans.Metadata
            };
        }

        public async Task<PagedResponse<TransactionResponse>> GetAllTransaction(PagedRequest options, int month, Account account)
        {
            if(account.Role != Role.Admin)
                throw new UnauthorizedAccessException("Plan.NotAdmin");
            var trans = await _context.Transactions.Include(c => c.Account).Where(c => c.Created.Month.Equals(month)).OrderByDescending(c => c.Id)
                                                     .ToPagedAsync(options,
                x => x.Email.ToLower().Contains(HttpUtility.UrlDecode(options.Search, Encoding.ASCII).ToLower()));

            return new PagedResponse<TransactionResponse>
            {
                Data = _mapper.Map<IEnumerable<TransactionResponse>>(trans.Data),
                Metadata = trans.Metadata
            };
        }
        
        public async Task<PagedResponse<TransactionResponse>> GetAllTransaction(PagedRequest options, Account account)
        {
            if(account.Role != Role.Admin)
                throw new UnauthorizedAccessException("Plan.NotAdmin");
            var trans = await _context.Transactions.Include(c => c.Account).OrderByDescending(c => c.Id)
                                                     .ToPagedAsync(options,
                x => x.Email.ToLower().Contains(HttpUtility.UrlDecode(options.Search, Encoding.ASCII).ToLower()));

            return new PagedResponse<TransactionResponse>
            {
                Data = _mapper.Map<IEnumerable<TransactionResponse>>(trans.Data),
                Metadata = trans.Metadata
            };
        }


        public async Task<List<ChartTransaction>> GetByYear(int year, Account account)
        {
            if (account.Role != Role.Admin)
                throw new UnauthorizedAccessException("Plan.NotAdmin");
            return await _context.ChartTransactions.Where(c => c.Year == year).OrderBy(c => c.Month).ToListAsync();
        }
    }
}
