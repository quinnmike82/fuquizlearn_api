using fuquizlearn_api.Entities;
using fuquizlearn_api.Models.Transaction;
using fuquizlearn_api.Services;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
                var session = await CheckOut(product, thisApiUrl);
                var pubKey = _configuration["AppSettings:StripeKey:PublicKey"];
                var checkoutOrderResponse = new CheckoutOrderResponse()
                {
                    Session = session,
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
        public async Task<object> CheckOut(Plan product, string thisApiUrl)
        {
            // Create a payment flow from the items in the cart.
            // Gets sent to Stripe API.
            var productService = new ProductService();
            var prod = await productService.GetAsync(product.Id.ToString());
            if (prod == null)
                throw new KeyNotFoundException();
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
                        UnitAmount = product.Amount,
                        Product = prod.Id,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = product.Title,
                            Description = product.Description,
                        },
                    },
                    Quantity = 1,
                },
            },
                Mode = "subscription"
            };

            var service = new SessionService();
            Session session = service.Create(options);

            return (new { clientSecret = session.RawJObject["client_secret"] });
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

