using fuquizlearn_api.Entities;
using fuquizlearn_api.Models.Transaction;
using fuquizlearn_api.Services;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using Stripe;
using Plan = fuquizlearn_api.Entities.Plan;
using fuquizlearn_api.Authorization;

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
        public CheckoutController(IConfiguration configuration, IHelperFrontEnd frontEnd, ITransactionService transactionService, IPlanService planService)
        {
            _configuration = configuration;
            _frontEnd = frontEnd;
            _transactionService = transactionService;
            _planService = planService;
        }

        [HttpPost]
        public async Task<ActionResult> CheckoutOrder(int planId)
        {
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
                "card", "paypal", "wechatPay" 
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
            var sessionService = new SessionService();
            var session = sessionService.Get(sessionId);

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

            await _transactionService.CreateTransaction(trans, Account);
            await _planService.RegisterPlan(int.Parse(session.LineItems.Data[0].Price.Product.Id), trans.TransactionId, Account);
            
            return Ok();
        }

        [HttpPut("cancel")]
        public async Task<ActionResult> CancelSubcribe()
        {
            var check = await _planService.CheckCurrent(Account);
            if(check != null)
            {
                await _planService.CancelledSubcribe(Account);
                var options = new SubscriptionUpdateOptions { CancelAtPeriodEnd = true };
                var service = new SubscriptionService();
                await service.UpdateAsync(check.Plan.Id.ToString(), options);
                return Ok();
            }
            else return BadRequest();
        }
    }
}

