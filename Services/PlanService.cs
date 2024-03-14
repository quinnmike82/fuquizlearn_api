using AutoMapper;
using fuquizlearn_api.Entities;
using fuquizlearn_api.Helpers;
using fuquizlearn_api.Models.Plan;
using Microsoft.EntityFrameworkCore;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace fuquizlearn_api.Services
{
    public interface IPlanService
    {
        Task<PlanResponse> CreatePlan(PlanCreate planCreate, Account account);
        Task<PlanResponse> UpdatePlan(PlanUpdate planUpdate, Account account);
        Task<PlanResponse> UnReleasePlan(int id, Account account);
        Task RemovePlan(int id, Account account);
        Task<List<PlanResponse>> GetAllPlan();
        Task<PlanAccount> RegisterPlan(int id,Account account);
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
        public async Task<PlanResponse> CreatePlan(PlanCreate planCreate, Account account)
        {
            if (account.Role != Role.Admin)
                throw new UnauthorizedAccessException("Not Admin");
            var plan = _mapper.Map<Plan>(planCreate);
            _context.Plans.Add(plan);
            await _context.SaveChangesAsync();
            return _mapper.Map<PlanResponse>(plan);
        }

        public async Task<List<PlanResponse>> GetAllPlan()
        {
            var plan = await _context.Plans.ToListAsync();
            return _mapper.Map<List<PlanResponse>>(plan);
        }

        public async Task<PlanAccount> RegisterPlan(int id, Account account)
        {
            var plan = await _context.Plans.FirstOrDefaultAsync(plan => plan.Id == id);
            if (plan == null)
                throw new KeyNotFoundException("Not Found Plan");
            var newRegist = new PlanAccount
            {
                Account = account,
                Amount = plan.Amount,
                Duration = plan.Duration,
                Plan = plan,
            };
            _context.PlanAccounts.Add(newRegist);   
            await _context.SaveChangesAsync();
            return newRegist;
        }

        public async Task RemovePlan(int id, Account account)
        {
            if (account.Role != Role.Admin)
                throw new UnauthorizedAccessException("Not Admin");
            var plan = await _context.Plans.FirstOrDefaultAsync(plan => plan.Id == id);
            if (plan == null) throw new KeyNotFoundException("Not Found Plan");
            plan.Deleted = DateTime.UtcNow;
            _context.Plans.Update(plan);
            await _context.SaveChangesAsync();
        }

        public async Task<PlanResponse> UnReleasePlan(int id, Account account)
        {
            if (account.Role != Role.Admin)
                throw new UnauthorizedAccessException("Not Admin");
            var plan = await _context.Plans.FirstOrDefaultAsync(plan => plan.Id == id);
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
            var plan = await _context.Plans.FirstOrDefaultAsync(plan => plan.Id == planUpdate.Id);
            if (plan == null) throw new KeyNotFoundException("Not Found Plan");
            plan = _mapper.Map<Plan>(planUpdate);
            _context.Plans.Update(plan);
            await _context.SaveChangesAsync();
            return _mapper.Map<PlanResponse>(plan);
        }
    }
}
