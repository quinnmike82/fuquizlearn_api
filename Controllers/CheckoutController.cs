using fuquizlearn_api.Entities;
using fuquizlearn_api.Models.Transaction;
using fuquizlearn_api.Services;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using Stripe;
using Plan = fuquizlearn_api.Entities.Plan;
using fuquizlearn_api.Authorization;
using fuquizlearn_api.Helpers;

namespace fuquizlearn_api.Controllers
{
    [Authorize]
    [ApiController]
    public class CheckoutController : BaseController
    {
        private readonly IConfiguration _configuration;
        private readonly IHelperFrontEnd _frontEnd;
        private readonly ITransactionService _transactionService;
        private readonly IPlanService _planService;
        private readonly INotificationService _notificationService;
        public CheckoutController(IConfiguration configuration, IHelperFrontEnd frontEnd, ITransactionService transactionService, IPlanService planService, INotificationService notificationService)
        {
            _configuration = configuration;
            _frontEnd = frontEnd;
            _transactionService = transactionService;
            _planService = planService;
            _notificationService = notificationService;
        }

        [HttpPost]
        public async Task<ActionResult> CheckoutOrder(int planId)
        {
            var planCheck = await _planService.CheckCurrent(Account);
            if (planCheck != null)
                throw new AppException("Already Subscribe");
            var plan = await _planService.GetById(planId);
            if(plan == null)
                throw new KeyNotFoundException(nameof(plan));
            var productService = new ProductService();
            var prod = await productService.GetAsync(plan.Id.ToString());
            if (prod == null)
                throw new KeyNotFoundException();
            var priceService = new PriceService();
            var priceOptions = new PriceListOptions()
            {
                Product = prod.Id.ToString()
            };
            var prices = priceService.List(priceOptions);
            var lineItems = prices.Select(p => new SessionLineItemOptions
            {
                Price = p.Id,
                Quantity = 1
            }).ToList();
            var options = new SessionCreateOptions
            {
                // Stripe calls the URLs below when certain checkout events happen such as success and failure.
                PaymentMethodTypes = new List<string> // Only card available in test mode?
            {
                "card"
            },
                LineItems = lineItems,
                Mode = "subscription",
                UiMode = "embedded",
                RedirectOnCompletion = "never"
            };

            var service = new SessionService();
            Session session = await service.CreateAsync(options);

            var pubKey = _configuration["AppSettings:StripeKey:PublicKey"];
            var checkoutOrderResponse = new CheckoutOrderResponse()
            {
                ClientSecret = (string)session.RawJObject["client_secret"],
                SessionId = session.Id,
                PubKey = pubKey
            };
            return Ok(checkoutOrderResponse);
        }

        [HttpGet("success")]
        // Automatic query parameter handling from ASP.NET.
        // Example URL: https://localhost:7051/checkout/success?sessionId=si_123123123123
        public async Task<ActionResult> CheckoutSuccess(string sessionId)
        {
            var planCheck = await _planService.CheckCurrent(Account);
            if (planCheck != null)
                throw new AppException("Already Subscribe");
            var sessionService = new SessionService();
            var session = await sessionService.GetAsync(sessionId);
            var check = await _planService.CheckPurchase(session.SubscriptionId);
            if (check)
                return BadRequest("Session is used");
            var subscriptionService = new SubscriptionService();
            var sub = await subscriptionService.GetAsync(session.SubscriptionId);

            // Here you can save order and customer details to your database.
            if (session.CustomerDetails?.Email == null)
                return BadRequest();
            var total = session.AmountTotal.Value;
            var customerEmail = session.CustomerDetails.Email;
            var trans = new TransactionCreate
            {
                Amount = (int)total,
                Email= customerEmail,
                TransactionId = session.SubscriptionId,
                TransactionType = session.PaymentMethodCollection,
            };
            var plan = await _planService.GetById(int.Parse(sub.Items.Data[0].Plan.ProductId));

            await _transactionService.CreateTransaction(trans, Account);
            await _planService.RegisterPlan(plan.Id, trans.TransactionId, Account);
            await _notificationService.NotificationTrigger(new List<int> { Account.Id }, "Payment", "subcribe_plan", plan.Title);


            return Ok();
        }

        [HttpPut("cancel")]
        public async Task<ActionResult> CancelSubcribe()
        {
            var check = await _planService.CheckCurrent(Account);
            if(check != null)
            {
                await _planService.CancelledSubcribe(Account, check.Plan.Id);
                var options = new SubscriptionUpdateOptions { CancelAtPeriodEnd = true };
                var service = new SubscriptionService();
                await service.UpdateAsync(check.TransactionId, options);
                await _notificationService.NotificationTrigger(new List<int> { Account.Id }, "Payment", "cancel_plan", check.Plan.Title);
                return Ok();
            }
            else return BadRequest("No Plan currently");
        }
    }
}

