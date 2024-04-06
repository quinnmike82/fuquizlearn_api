using fuquizlearn_api.Entities;
using fuquizlearn_api.Models.Transaction;
using fuquizlearn_api.Services;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
namespace fuquizlearn_api.Controllers
{
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
        public async Task<ActionResult> CheckoutOrder([FromBody] Plan product, [FromServices] IServiceProvider sp)
        {
            var referer = Request.Headers.Referer;

            // Build the URL to which the customer will be redirected after paying.
            var server = sp.GetRequiredService<IServer>();

            var serverAddressesFeature = server.Features.Get<IServerAddressesFeature>();

            string? thisApiUrl = null;

            if (serverAddressesFeature is not null)
            {
                thisApiUrl = serverAddressesFeature.Addresses.FirstOrDefault();
            }

            if (thisApiUrl is not null)
            {
                var sessionId = await CheckOut(product, thisApiUrl);
                var pubKey = _configuration["AppSettings:StripeKey:PublicKey"];

                var checkoutOrderResponse = new CheckoutOrderResponse()
                {
                    SessionId = sessionId,
                    PubKey = pubKey
                };

                return Ok(checkoutOrderResponse);
            }
            else
            {
                return StatusCode(500);
            }
        }

        [NonAction]
        public async Task<string> CheckOut(Plan product, string thisApiUrl)
        {
            // Create a payment flow from the items in the cart.
            // Gets sent to Stripe API.
            var options = new SessionCreateOptions
            {
                // Stripe calls the URLs below when certain checkout events happen such as success and failure.
                SuccessUrl = $"{_frontEnd.GetBaseUrl()}/checkout/success?sessionId=" + "{CHECKOUT_SESSION_ID}", // Customer paid.
                CancelUrl = _frontEnd.GetBaseUrl() + "/failed",  // Checkout cancelled.
                PaymentMethodTypes = new List<string> // Only card available in test mode?
            {
                "card"
            },
                LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = product.Amount, // Price is in USD cents.
                        Currency = "USD",
                        Product = product.Id.ToString(),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = product.Title,
                            Description = product.Description,
                        },
                    },
                    Quantity = 1,
                },
            },
                Mode = "payment" // One-time payment. Stripe supports recurring 'subscription' payments.
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            return session.Id;
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
                TransactionId = sessionId,
                TransactionType = session.PaymentMethodCollection,
            };

            await _transactionService.CreateTransaction(trans, Account);
            await _planService.RegisterPlan(int.Parse(session.LineItems.Data[0].Id), Account);

            return Ok();
        }
    }
}

