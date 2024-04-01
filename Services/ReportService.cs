using AutoMapper;
using fuquizlearn_api.Entities;
using fuquizlearn_api.Extensions;
using fuquizlearn_api.Helpers;
using fuquizlearn_api.Models.Classroom;
using fuquizlearn_api.Models.Report;
using fuquizlearn_api.Models.Request;
using fuquizlearn_api.Models.Response;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Web;

namespace fuquizlearn_api.Services
{
    public interface IReportService
    {
        Task<ReportResponse> AddReport(ReportCreate report, Account account);
        Task<PagedResponse<ReportResponse>> GetAllReport(PagedRequest options, Account account);
        Task VerifyReport(int reportId, Account account);
    }
    public class ReportService : IReportService
    {
        private readonly DataContext _context;
        private readonly IMapper _mapper;
        private readonly IAccountService _accountService;
        private readonly INotificationService _notificationService;
        public ReportService(DataContext context,IMapper mapper, IAccountService accountService, INotificationService notificationService)
        {
            _context = context;
            _mapper = mapper;
            _accountService = accountService;
            _notificationService = notificationService;
        }
        public async Task<ReportResponse> AddReport(ReportCreate report, Account account)
        {
            Report newReport = new Report
            {
                Owner = account,
                Reason = report.Reason
            };
            if(report.QuizBankId == null && report.AccountId == null) {
                throw new AppException("Fields missing");
            }
            int id;
            if(int.TryParse(report.QuizBankId, out id)) {
                var bank = await _context.QuizBanks.Include(c => c.Author).FirstOrDefaultAsync(i => i.Id == id);
                newReport.QuizBank = bank;
                await _notificationService.NotificationTrigger(new List<int> { bank.Author.Id }, "Warning", "reported_quizbank", bank.BankName);
            }
            if (int.TryParse(report.AccountId, out id)) {
                var ac = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == id);
                newReport.Account = ac;
                await _notificationService.NotificationTrigger(new List<int> { ac.Id }, "Warning", "warning", string.Empty);
            }
            _context.Reports.Add(newReport);
            await _context.SaveChangesAsync();
            return _mapper.Map<ReportResponse>(newReport);
        }

        public async Task<PagedResponse<ReportResponse>> GetAllReport(PagedRequest options, Account account)
        {
            if (account.Role != Role.Admin)
                throw new UnauthorizedAccessException("Not Admin");
            var reports = await _context.Reports.Include(c => c.Owner).Include(c => c.QuizBank).Include(c => c.Account).ToPagedAsync(options,
   q => q.Reason.ToLower().Contains(HttpUtility.UrlDecode(options.Search, Encoding.ASCII).ToLower()));
            return new PagedResponse<ReportResponse>
            {
                Data = _mapper.Map<IEnumerable<ReportResponse>>(reports.Data),
                Metadata = reports.Metadata
            };
        }

        public async Task VerifyReport(int reportId, Account account)
        {
            if (account.Role != Role.Admin)
                throw new UnauthorizedAccessException("Not Admin");
            var report = await _context.Reports.Include(c => c.Account).FirstOrDefaultAsync(c => c.Id == reportId);
            if(report == null)
            {
                throw new KeyNotFoundException(nameof(report));
            }
            if (report.Account == null)
            {
                var quizbank = await _context.QuizBanks.Include(c =>c.Author).FirstOrDefaultAsync(c => c.Id == report.QuizBank.Id);
                _context.QuizBanks.Remove(quizbank);
                await _context.SaveChangesAsync();
                await _notificationService.NotificationTrigger(new List<int> { quizbank.Author.Id }, "Warning" ,"deleted_quizbank", quizbank.BankName);
                _accountService.WarningAccount(quizbank.Author.Id, string.Empty);
            }
            else
            {
                await _notificationService.NotificationTrigger(new List<int> { report.Account.Id }, "Warning", "reported", string.Empty);
                _accountService.WarningAccount(report.Account.Id, string.Empty);
            }
        }
    }
}
