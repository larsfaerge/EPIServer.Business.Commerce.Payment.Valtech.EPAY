using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using EPiServer.Commerce.Order;
using EPiServer.ServiceLocation;
using Mediachase.Commerce;
using Mediachase.Commerce.Core;

namespace EPiServer.Business.Commerce.Payment.Valtech.Epay.Helpers
{
    public class EpayRequestHelper
    {
        //todo: create refund
        private const string RefundRequestUrl = "https://ssl.ditonlinebetalingssystem.dk/remote/payment/refund"; 
        
        public EpayConfiguration EpayConfiguration { get; private set; }
        private readonly IOrderNumberGenerator _orderNumberGenerator;
        private readonly SiteContext _siteContext;

        public EpayRequestHelper() : this(ServiceLocator.Current.GetInstance<IOrderNumberGenerator>(), ServiceLocator.Current.GetInstance<SiteContext>(), new EpayConfiguration())
        {
        }

        public EpayRequestHelper(IOrderNumberGenerator orderNumberGenerator, SiteContext siteContext, EpayConfiguration epayConfiguration)
        {
            _orderNumberGenerator = orderNumberGenerator;
            _siteContext = siteContext;
            EpayConfiguration = epayConfiguration;
        }

        /// <summary>
        /// Build key-value pair to epay
        /// </summary>
        /// <param name="payment"></param>
        /// <param name="currentCart"></param>
        /// <param name="notifyUrl"></param>
        /// <returns></returns>
        public Dictionary<string, object> CreateRequestPaymentData(IPayment payment, ICart currentCart, string notifyUrl)
        {
            var currency = currentCart.Currency;
            var orderNumber = _orderNumberGenerator.GenerateOrderNumber(currentCart);
            var amount = Utilities.GetAmount(currency, payment.Amount);
            var mechantId = EpayConfiguration.Merchant;

            // request subcription
            var subscription = 0; 
            if (currentCart.Properties != null && currentCart.Properties.ContainsKey("subscription"))
            {
                if (currentCart.Properties["subscription"].Equals("true")) {
                    subscription = 1;
                }
            }

            var requestPaymentData = new Dictionary<string, object>();
            //requestPaymentData.Add("paymentprovider", EpayConfiguration.EpaySystemName);
            requestPaymentData.Add("merchantnumber", mechantId);
            requestPaymentData.Add("amount", amount);
            requestPaymentData.Add("currency", currency.CurrencyCode); //todo: check if this match http://epay.bambora.com/en/currency-codes
            requestPaymentData.Add("orderid", orderNumber);
            requestPaymentData.Add("accepturl", notifyUrl); 
            requestPaymentData.Add("cancelurl", notifyUrl);
            if (!string.IsNullOrEmpty(EpayConfiguration.CallBackUrl)) { requestPaymentData.Add("callbackurl", EpayConfiguration.CallBackUrl); }
            requestPaymentData.Add("windowstate", "1");
            requestPaymentData.Add("mobile", "1");
            requestPaymentData.Add("language", EpayLanguages.GetCurrentEpaySupportedLanguage(_siteContext.LanguageName));
            requestPaymentData.Add("instantcallback", "1"); //test
            if (!string.IsNullOrEmpty(EpayConfiguration.Cssurl)){requestPaymentData.Add("cssurl", EpayConfiguration.Cssurl);}
            if (!string.IsNullOrEmpty(EpayConfiguration.MobileCssUrl)) { requestPaymentData.Add("mobilecssurl", EpayConfiguration.MobileCssUrl); }
            requestPaymentData.Add("ordertext", $"Payment for Order number {orderNumber}");
            requestPaymentData.Add("subscription", subscription);
            requestPaymentData.Add("hash", Utilities.GetMd5RequestKey(EpayConfiguration, requestPaymentData)); // mechantId, orderNumber, currency, amount));

            return requestPaymentData;
        }

        /// <summary>
        /// Posts the capture request to Epay API.
        /// http://epay.bambora.com/en/payment-web-service#553
        /// https://ssl.ditonlinebetalingssystem.dk/remote/payment.asmx
        /// Returns true if payment is captured
        /// </summary>
        /// <param name="payment">The payment.</param>
        /// <param name="purchaseOrder">The purchase order.</param>
        public bool PostCaptureRequest(IPayment payment, IPurchaseOrder purchaseOrder)
        {
            
            // Check for subscription and then use auth and capture with that instead
            if (!(string.IsNullOrEmpty(payment.AuthorizationCode)))
            {
                return PostSubscriptionAuthorize(payment, purchaseOrder);
            }

            // initital
            var currencyCode = purchaseOrder.Currency;

            // parameters
            int amount = 0;
            int.TryParse(Utilities.GetAmount(new Currency(currencyCode), payment.Amount), out amount);
            int merchantnNumber = int.Parse(EpayConfiguration.Merchant);
            long transactionId = long.Parse(payment.TransactionID);

            // response parameters
            int pbsresponse = -1;
            int epayrespone = -1;
           
            // build body
            epayPaymentService.PaymentSoapClient client = new epayPaymentService.PaymentSoapClient("PaymentSoap");
            bool isCaptured = client.capture(
                merchantnumber: merchantnNumber, 
                transactionid: transactionId, 
                amount: amount,
                group: "", 
                invoice:"",
                pwd:"",
                orderid:"", 
                pbsResponse: ref pbsresponse, 
                epayresponse: ref epayrespone
                );

            //todo: some bug issue handling


            return isCaptured; 

        }

        /// <summary>
        /// Autorize a subscription
        /// </summary>
        /// <param name="payment"></param>
        /// <param name="purchaseOrder"></param>
        /// <returns></returns>
        public bool PostSubscriptionAuthorize(IPayment payment, IPurchaseOrder purchaseOrder)
        {
            // initital
            var currencyCode = purchaseOrder.Currency;

            // parameters
            int amount = 0;
            int.TryParse(Utilities.GetAmount(new Currency(currencyCode), payment.Amount), out amount);
            int merchantnNumber = int.Parse(EpayConfiguration.Merchant);
            long transactionId = long.Parse(payment.TransactionID);

            // response parameters
            long transactionid = -1;
            int pbsresponse = -1;
            int epayrespone = -1;
            int fraud = -1;
            int currency = 0;

            // Only convertion known episerver currencies
            currency = EpayCurrencies.GetCurrencyCode(currencyCode);

            // Build Soap request //http://epay.bambora.com/en/subscription-web-service
            epaySubscriptionService.SubscriptionSoapClient client = new epaySubscriptionService.SubscriptionSoapClient("SubscriptionSoap");

            bool isAuthorize = client.authorize(
                merchantnumber: merchantnNumber,
                subscriptionid: long.Parse(payment.AuthorizationCode),
                amount: amount,
                currency: currency,
                orderid: purchaseOrder.OrderNumber,
                instantcapture: 1,
                group: "",
                description: "",
                email: "",
                sms: "",
                ipaddress: "",
                pwd: "",
                textonstatement: "",
                customerkey: "",
                fraud: ref fraud,
                transactionid: ref transactionid,
                epayresponse: ref epayrespone,
                pbsresponse: ref pbsresponse
            );
            
            return isAuthorize;
        }

        /// <summary>
        /// Posts the refund request to Epay API.
        /// </summary>
        /// <param name="payment"></param>
        /// <param name="purchaseOrder"></param>
        /// <param name="message">return message</param>
        public bool PostRefundRequest(IPayment payment, IPurchaseOrder purchaseOrder, ref string message)
        {
            return PostRequest(payment, purchaseOrder, ref message);
        }

        /// <summary>
        /// Posts the request to Epay API.
        /// </summary>
        /// <param name="payment">The payment.</param>
        /// <param name="purchaseOrder">The purchase order.</param>
        /// <param name="message">Return message</param>
        /// <remarks>Info http://epay.bambora.com/en/payment-web-service and http://epay.bambora.com/en/payment-web-service</remarks>
        /// <returns>A string contains result from epay API</returns>
        private bool PostRequest(IPayment payment, IPurchaseOrder purchaseOrder, ref string message)
        {
            // parameters
            int amount = int.Parse(Utilities.GetAmount(new Currency(purchaseOrder.Currency), payment.Amount));
            int merchantnNumber = int.Parse(EpayConfiguration.Merchant);
            long transactionId = long.Parse(payment.TransactionID);
            
            // response parameters
            int pbsresponse = -1;
            int epayrespone = -1;

            // Build Soap request //http://epay.bambora.com/en/subscription-web-service
            epayPaymentService.PaymentSoapClient client = new epayPaymentService.PaymentSoapClient("PaymentSoap");

            bool isAuthorize = client.credit(
                amount: amount,
                merchantnumber: merchantnNumber,
                orderid: purchaseOrder.OrderNumber,
                group: "",
                pwd: "",
                transactionid: transactionId,
                epayresponse: ref epayrespone,
                pbsresponse: ref pbsresponse,
                invoice:""
            );

            message = "Credit has been done";
            if (isAuthorize.Equals(false)){
                message = $"Credit failed. Epay Response Code: {epayrespone}. PBS Response Code: {pbsresponse} ";
            }

            return isAuthorize;
        }
    }
}
