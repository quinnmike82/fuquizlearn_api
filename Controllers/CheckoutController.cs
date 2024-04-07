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
        public async Task<ActionResult> CheckoutOrder([FromBody] Plan product)
        {
                var session = await CheckOut(product);
                var pubKey = _configuration["AppSettings:StripeKey:PublicKey"];
            await Console.Out.WriteLineAsync(session);
            var checkoutOrderResponse = new CheckoutOrderResponse()
                {
                    ClientSecret = session,
                    PubKey = pubKey
                };
                return Ok(checkoutOrderResponse);
        }

        [NonAction]
        public async Task<string> CheckOut(Plan product)
        {
            // Create a payment flow from the items in the cart.
            // Gets sent to Stripe API.
            var productService = new ProductService();
            var prod = await productService.GetAsync(product.Id.ToString());
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
                ReturnUrl = _frontEnd.GetBaseUrl() + "/return?session_id={CHECKOUT_SESSION_ID}"

            };

            var service = new SessionService();
            Session session = await service.CreateAsync(options);

            await Console.Out.WriteLineAsync(session.ToString());

            return (string)session.RawJObject["client_secret"];
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

