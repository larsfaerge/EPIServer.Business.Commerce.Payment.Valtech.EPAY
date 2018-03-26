using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using EPiServer.Business.Commerce.Payment.Valtech.Epay.Helpers;
using EPiServer.Business.Commerce.Payment.Valtech.Epay.PageTypes;
using EPiServer.Commerce.Order;
using EPiServer.Editor;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using EPiServer.Web.Mvc;
using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Orders.Exceptions;
using Mediachase.Commerce.Security;

namespace EPiServer.Business.Commerce.Payment.Valtech.Epay.Controllers
{
    public class EpayPaymentController : PageController<EpayPage>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly EpayRequestHelper _epayRequestHelper;

        public EpayPaymentController() : this (ServiceLocator.Current.GetInstance<IOrderRepository>())
        { }

        public EpayPaymentController(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
            _epayRequestHelper = new EpayRequestHelper();
        }

        public ActionResult Index()
        {
            if (PageEditing.PageIsInEditMode)
            {
                return new EmptyResult();
            }

            // verify that we have a basket with payment
            var currentCart = _orderRepository.LoadCart<ICart>(PrincipalInfo.CurrentPrincipal.GetContactId(), Cart.DefaultName);
            if (!currentCart.Forms.Any() || !currentCart.GetFirstForm().Payments.Any())
            {
                throw new PaymentException(PaymentException.ErrorType.ProviderError, "", Utilities.Translate("GenericError"));
            }

            // verify payment is epay
            var payment = currentCart.Forms.SelectMany(f => f.Payments).FirstOrDefault(c => c.PaymentMethodId.Equals(_epayRequestHelper.EpayConfiguration.PaymentMethodId));
            if (payment == null)
            {
                throw new PaymentException(PaymentException.ErrorType.ProviderError, "", Utilities.Translate("PaymentNotSpecified"));
            }

            // Clear cache
            InitializeReponse();

            // Post-Call Epay. 
            var actionResult = PostCheckAndRedirect(currentCart, payment); //Test and redirect if callback from Epay
            if (actionResult != null){return actionResult;}

            // Pre-Call Epay
            var notifyUrl = UriSupport.AbsoluteUrlBySettings(Utilities.GetUrlFromStartPageReferenceProperty("EpayPaymentPage"));
            var requestPaymentData = _epayRequestHelper.CreateRequestPaymentData(payment, currentCart, notifyUrl);

            return new RedirectAndPostActionResult(_epayRequestHelper.EpayConfiguration.ProcessingUrl, requestPaymentData);
        }

        private ActionResult PostCheckAndRedirect(ICart currentCart, IPayment payment)
        {
            var transactionRequest = new TransactionRequest(Request.QueryString, _epayRequestHelper.EpayConfiguration);
            if (transactionRequest.IsProcessable())
            {
                var cancelUrl = Utilities.GetUrlFromStartPageReferenceProperty("CheckoutPage"); // get link to Checkout page
                cancelUrl = UriSupport.AddQueryString(cancelUrl, "success", "false");
                cancelUrl = UriSupport.AddQueryString(cancelUrl, "paymentmethod", "epay");

                var redirectUrl = cancelUrl; //default
                var gateway = new EpayPaymentGateway();

                // Process successful transaction                        
                if (transactionRequest.IsSuccessful())
                {
                    var acceptUrl = Utilities.GetUrlFromStartPageReferenceProperty("EpayPaymentLandingPage");
                    redirectUrl = gateway.ProcessSuccessfulTransaction(currentCart, payment, transactionRequest.Transact, transactionRequest.SubscriptionId, transactionRequest.OrderId, acceptUrl, cancelUrl);
                }
                // Process unsuccessful transaction
                else if (transactionRequest.IsUnsuccessful())
                {
                    TempData["Message"] = Utilities.Translate("CancelMessage");
                    redirectUrl = gateway.ProcessUnsuccessfulTransaction(cancelUrl, Utilities.Translate("CancelMessage"));
                }

                return Redirect(redirectUrl);
            }

            return null;
        }

        private void InitializeReponse()
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetExpires(DateTime.Now.AddSeconds(-1));
            Response.Cache.SetNoStore();
            Response.AppendHeader("Pragma", "no-cache");
        }

    }
}