using AutoMapper;
using AutoMapper.Features;
using fuquizlearn_api.Entities;
using fuquizlearn_api.Extensions;
using fuquizlearn_api.Helpers;
using fuquizlearn_api.Models.Plan;
using fuquizlearn_api.Models.QuizBank;
using fuquizlearn_api.Models.Request;
using fuquizlearn_api.Models.Response;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Web;
using Plan = fuquizlearn_api.Entities.Plan;
using Account = fuquizlearn_api.Entities.Account;

namespace fuquizlearn_api.Services
{
    public interface IPlanService
    {
        Task<PlanResponse> CreatePlan(PlanCreate planCreate, Account account);
        Task<PlanResponse> UpdatePlan(PlanUpdate planUpdate, Account account);
        Task<PlanResponse> UnReleasePlan(int id, Account account);
        Task RemovePlan(int id, Account account);
        Task<PagedResponse<PlanResponse>> GetAllPlan(PagedRequest options);
        Task<PlanAccount> RegisterPlan(int id, string transactionId, Account account);
        Task<PlanAccount> CheckCurrent(Account account);
        Task CancelledSubcribe(Account account);
        Task<bool> CheckAICount(Account account);
    }
    public class PlanService : IPlanService
    {
        private readonly DataContext _context;
        private readonly IMapper _mapper;
        public PlanService(DataContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task CancelledSubcribe(Account account)
        {
            var result = await _context.PlanAccounts.FirstOrDefaultAsync(c => c.Account.Id == account.Id);
            if(result == null)
            {
                throw new AppException("Not have plan");
            }
            result.Cancelled = DateTime.UtcNow;
            _context.PlanAccounts.Update(result);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> CheckAICount(Account account)
        {
            var plan = await _context.PlanAccounts.Include(c => c.Account).Include(c => c.Plan).Where(c => c.Account.Id == account.Id).OrderByDescending(c => c.Plan.useAICount).Select(c => c.Plan).ToListAsync();
            if (plan.Count > 0)
            {
                if (plan[0].useAICount > account.useAICount)
                {
                    account.useAICount++;
                    _context.Accounts.Update(account);
                    await _context.SaveChangesAsync();
                    return true;
                }
                else
                    return false;
            }

            if (10 > account.useAICount)
            {
                account.useAICount++;
                _context.Accounts.Update(account);
                await _context.SaveChangesAsync();
                return true;
            }
            else return false;
        }

        public async Task<PlanAccount> CheckCurrent(Account account)
        {
            return await _context.PlanAccounts.Include(c => c.Plan).Include(c => c.Account).FirstOrDefaultAsync(c => c.Account.Id == account.Id);
        }

        public async Task<PlanResponse> CreatePlan(PlanCreate planCreate, Account account)
        {
            if (account.Role != Role.Admin)
                throw new UnauthorizedAccessException("Not Admin");
            var plan = _mapper.Map<Plan>(planCreate);
            _context.Plans.Add(plan);
            await _context.SaveChangesAsync();
            ProductService productsService = new ProductService();
            var prodCreateOptions = new ProductCreateOptions
            {
                Id = plan.Id.ToString(),
                Name = plan.Title,
                Description = plan.Description,
                UnitLabel = "month"
            };

            var newHubProduct = await productsService.CreateAsync(prodCreateOptions);
            PriceService priceService = new PriceService();

            // Create flat price in product
            var priceCreateOptions = new PriceCreateOptions
            {
                Product = newHubProduct.Id,
                Nickname = newHubProduct.Name,
                Currency = "usd",
                UnitAmount = plan.Amount,
                Recurring =
                   new PriceRecurringOptions { Interval = "month", UsageType = "licensed", IntervalCount = plan.Duration }
            };

            var newProductPrice = await priceService.CreateAsync(priceCreateOptions);

            // Update default price
            await productsService.UpdateAsync(newHubProduct.Id, new ProductUpdateOptions
            {
                DefaultPrice = newProductPrice.Id
            });
            return _mapper.Map<PlanResponse>(plan);
        }

        public async Task<PagedResponse<PlanResponse>> GetAllPlan(PagedRequest options)
        {
            var plan = await _context.Plans.ToPagedAsync(options,
            x => x.Title.ToLower().Contains(HttpUtility.UrlDecode(options.Search, Encoding.ASCII).ToLower()) || x.Description.ToLower().Contains(HttpUtility.UrlDecode(options.Search, Encoding.ASCII).ToLower()));
            return new PagedResponse<PlanResponse>
            {
                Data = _mapper.Map<IEnumerable<PlanResponse>>(plan.Data),
                Metadata = plan.Metadata
            };
        }

        public async Task<PlanAccount> RegisterPlan(int id, string transactionId, Account account)
        {
            var plan = await _context.Plans.FirstOrDefaultAsync(plan => plan.Id.Equals(id));
            if (plan == null)
                throw new KeyNotFoundException("Not Found Plan");
            var newRegist = new PlanAccount
            {
                Account = account,
                Amount = plan.Amount,
                Duration = plan.Duration,
                Plan = plan,
                TransactionId = transactionId
            };
            _context.PlanAccounts.Add(newRegist);   
            await _context.SaveChangesAsync();
            return newRegist;
        }

        public async Task RemovePlan(int id, Account account)
        {
            if (account.Role != Role.Admin)
                throw new UnauthorizedAccessException("Not Admin");
            var plan = await _context.Plans.FirstOrDefaultAsync(plan => plan.Id.Equals(id));
            if (plan == null) throw new KeyNotFoundException("Not Found Plan");
            plan.Deleted = DateTime.UtcNow;
            _context.Plans.Update(plan);
            await _context.SaveChangesAsync();
        }

        public async Task<PlanResponse> UnReleasePlan(int id, Account account)
        {
            if (account.Role != Role.Admin)
                throw new UnauthorizedAccessException("Not Admin");
            var plan = await _context.Plans.FirstOrDefaultAsync(plan => plan.Id.Equals(id));
            if (plan == null) throw new KeyNotFoundException("Not Found Plan");
            plan.IsRelease = false;
            _context.Plans.Update(plan);
            await _context.SaveChangesAsync();
            return _mapper.Map<PlanResponse>(plan);
        }

        public async Task<PlanResponse> UpdatePlan(PlanUpdate planUpdate, Account account)
        {
            if (account.Role != Role.Admin)
                throw new UnauthorizedAccessException("Not Admin");
            var plan = await _context.Plans.FirstOrDefaultAsync(plan => plan.Id.Equals(planUpdate.Id));
            if (plan == null) throw new KeyNotFoundException("Not Found Plan");
            _mapper.Map(planUpdate,plan);
            _context.Plans.Update(plan);
            await _context.SaveChangesAsync();
            return _mapper.Map<PlanResponse>(plan);
        }
    }
}
