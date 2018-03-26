using System.Collections.Specialized;
using EPiServer.Business.Commerce.Payment.Valtech.Epay.Helpers;
using Mediachase.Commerce;
using Mediachase.Commerce.Orders.Managers;

namespace EPiServer.Business.Commerce.Payment.Valtech.Epay.Controllers
{
    class TransactionRequest
    {
        private EpayConfiguration _epayConfiguration;
        private NameValueCollection _requestForm;
        //private string _authKey;
        private string _md5Key;
        private string _merchant;

        public string Transact { get; }
        public string OrderId { get; }
        public string Currency { get; }
        public string Amount { get; }
        public string SubscriptionId { get; }

        public TransactionRequest(NameValueCollection requestForm, EpayConfiguration epayConfiguration)
        {
            _epayConfiguration = epayConfiguration;
            _requestForm = requestForm;

            // setup reponse variables
            Transact = requestForm["txnid"];
            OrderId = requestForm["orderid"];
            Currency = requestForm["currency"];
            Amount = requestForm["amount"];
            SubscriptionId = requestForm["subscriptionid"];
            
            _md5Key = requestForm["hash"];
            _merchant = requestForm["merchant"];
        }

        /// <summary>
        /// The transaction can be processed
        /// </summary>
        /// <returns></returns>
        public bool IsProcessable()
        {
            return !string.IsNullOrEmpty(OrderId) && !string.IsNullOrEmpty(Currency) && !string.IsNullOrEmpty(Amount);
        }

        public bool IsSuccessful()
        {
            if (string.IsNullOrEmpty(_md5Key) || string.IsNullOrEmpty(Transact))
            {
                return false;
            }

            var hashKey = Utilities.GetMd5ResponseKey(_epayConfiguration, _requestForm);
            return hashKey.Equals(_md5Key);
        }

        public bool IsUnsuccessful()
        {
            if (string.IsNullOrEmpty(_merchant) || string.IsNullOrEmpty(_md5Key))
            {
                return false;
            }

            return true; //todo: check callback request 
            //var hashKey = Utilities.GetMD5RequestKey(_epayConfiguration, _merchant, OrderId, new Currency(Currency), Amount);
            //return hashKey.Equals(_md5Key);
        }
    }
}
