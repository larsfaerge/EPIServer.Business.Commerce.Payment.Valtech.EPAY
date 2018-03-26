using System.Collections.Generic;
using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Text;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Order;
using EPiServer.Core;
using EPiServer.Framework.Localization;
using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;
using Mediachase.Commerce;
using Mediachase.Commerce.Catalog;

namespace EPiServer.Business.Commerce.Payment.Valtech.Epay.Helpers
{
    public class Utilities
    {
        private static Injected<UrlResolver> _urlResolver = default(Injected<UrlResolver>);
        private static Injected<LocalizationService> _localizationService = default(Injected<LocalizationService>);
        private static Injected<IContentRepository> _catalogContentLoader = default(Injected<IContentRepository>);
        private static Injected<ReferenceConverter> _referenceConverter = default(Injected<ReferenceConverter>);

        /// <summary>
        /// Gets display name with current language.
        /// </summary>
        /// <param name="lineItem">The line item of order.</param>
        /// <param name="maxSize">The number of character to get display name.</param>
        /// <returns>Display name with current language.</returns>
        public static string GetDisplayNameOfCurrentLanguage(ILineItem lineItem, int maxSize)
        {
            // if the entry is null (product is deleted), return item display name
            var entryContent = _catalogContentLoader.Service.Get<EntryContentBase>(_referenceConverter.Service.GetContentLink(lineItem.Code));
            var displayName = entryContent != null ? entryContent.DisplayName : lineItem.DisplayName;
            return StripPreviewText(displayName, maxSize <= 0 ? 100 : maxSize);
        }

        /// <summary>
        /// Updates display name with current language.
        /// </summary>
        /// <param name="purchaseOrder">The purchase order.</param>
        public static void UpdateDisplayNameWithCurrentLanguage(IPurchaseOrder purchaseOrder)
        {
            if (purchaseOrder != null)
            {
                foreach (ILineItem lineItem in purchaseOrder.GetAllLineItems())
                {
                    lineItem.DisplayName = GetDisplayNameOfCurrentLanguage(lineItem, 100);
                }
            }
        }

        /// <summary>
        /// Gets url from start page's reference property.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <returns>The friendly url.</returns>
        public static string GetUrlFromStartPageReferenceProperty(string propertyName)
        {
            var startPageData = DataFactory.Instance.GetPage(ContentReference.StartPage);
            if (startPageData == null)
            {
                return _urlResolver.Service.GetUrl(ContentReference.StartPage);
            }

            var property = startPageData.Property[propertyName];
            if (property != null && !property.IsNull && property.Value is ContentReference)
            {
                return _urlResolver.Service.GetUrl((ContentReference)property.Value);
            }
            return _urlResolver.Service.GetUrl(ContentReference.StartPage);
        }

        /// <summary>
        /// Calculates the amount, to return the smallest unit of an amount in the selected currency.
        /// </summary>
        /// <remarks>http://epay.bambora.com/en/payment-window-parameters</remarks>
        /// <param name="currency">Selected currency</param>
        /// <param name="amount">Amount in the selected currency</param>
        /// <returns>The string represents the smallest unit of an amount in the selected currency.</returns>
        public static string GetAmount(Currency currency, decimal amount)
        {
            var delta = currency.Equals(Currency.JPY) ? 1 : 100;
            return (amount * delta).ToString("#");
        }

        /// <summary>
        /// Gets the Md5 key refund.
        /// </summary>
        /// <param name="paymentConfiguration">The DIBS payment configuration.</param>
        /// <param name="merchant">The merchant.</param>
        /// <param name="orderId">The order id.</param>
        /// <param name="transact">The transact.</param>
        /// <param name="amount">The amount.</param>
        public static string GetMD5RefundKey(EpayConfiguration paymentConfiguration, string merchant, string orderId, string transact, string amount)
        {
            //todo: not implemented
            var hashString = $"merchant={merchant}&orderid={orderId}&transact={transact}&amount={amount}"; //todo: fix 
            return GetMD5Key(paymentConfiguration, hashString);
        }

        /// <summary>
        /// Gets the MD5 key used to send to epay in authorization step.
        /// </summary>
        /// <param name="paymentConfiguration">The epay payment configuration.</param>
        /// <param name="requestPaymentData">The request parameters.</param>
        /// <remarks>http://epay.bambora.com/en/hash-md5-check</remarks>
        /// <returns>MD5Key</returns>
        public static string GetMd5RequestKey(EpayConfiguration paymentConfiguration, Dictionary<string, object> requestPaymentData)
        {
            var hashString = "";
            foreach (var data in requestPaymentData)
            {
                if (data.Key != "md5key")
                {
                    hashString += data.Value;
                }
            }
            
            return GetMD5Key(paymentConfiguration, hashString);
        }

        /// <summary>
        /// Gets the key used to verify response from epay when payment is approved.
        /// </summary>
        /// <param name="paymentConfiguration">The epay payment configuration.</param>
        /// <param name="requestForm">The NameValueCollection from epay</param>
        public static string GetMd5ResponseKey(EpayConfiguration paymentConfiguration, NameValueCollection requestForm)
        {
            var hashString = "";
            for (int i = 0; i < requestForm.Count; i++)
            {
                if (requestForm.GetKey(i) != "hash")
                {
                    hashString += requestForm[i]; 
                }
            }
            return GetMD5Key(paymentConfiguration, hashString);
        }
        
        /// <summary>
        /// Translate with languageKey under /Commerce/Checkout/DIBS/ in lang.xml
        /// </summary>
        /// <param name="languageKey">The language key.</param>
        public static string Translate(string languageKey)
        {
            //todo: change files to epay
            return _localizationService.Service.GetString("/Commerce/Checkout/DIBS/" + languageKey);
        }

        /// <summary>
        /// http://epay.bambora.com/en/hash-md5-check
        /// The hash you send to (and receive from) Bambora must be the value of all parameters in the order they are sent + the MD5 key.
        /// </summary>
        /// <param name="paymentConfiguration"></param>
        /// <param name="hashString"></param>
        /// <returns></returns>
        private static string GetMD5Key(EpayConfiguration paymentConfiguration, string hashString)
        {
            return GetMD5value(hashString + paymentConfiguration.MD5Key);
        }

        /// <summary>
        /// Get MD5 value
        /// </summary>
        /// <param name="inputStr"></param>
        /// <returns></returns>
        private static string GetMD5value(string inputStr)
        {
            byte[] textBytes = Encoding.Default.GetBytes(inputStr);
            try
            {
                MD5CryptoServiceProvider cryptHandler = new MD5CryptoServiceProvider();
                byte[] hash = cryptHandler.ComputeHash(textBytes);
                string ret = "";
                foreach (byte a in hash)
                {
                    if (a < 16)
                        ret += "0" + a.ToString("x");
                    else
                        ret += a.ToString("x");
                }
                return ret;
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Strips a text to a given length without splitting the last word.
        /// </summary>
        /// <param name="source">The source string.</param>
        /// <param name="maxLength">Max length of the text</param>
        /// <returns>A shortened version of the given string</returns>
        /// <remarks>Will return empty string if input is null or empty</remarks>
        private static string StripPreviewText(string source, int maxLength)
        {
            if (string.IsNullOrEmpty(source))
            {
                return string.Empty;
            }

            if (source.Length <= maxLength)
            {
                return source;
            }

            source = source.Substring(0, maxLength);
            // The maximum number of characters to cut from the end of the string.
            var maxCharCut = (source.Length > 15 ? 15 : source.Length - 1);
            var previousWord = source.LastIndexOfAny(new char[] { ' ', '.', ',', '!', '?' }, source.Length - 1, maxCharCut);
            if (previousWord >= 0)
            {
                source = source.Substring(0, previousWord);
            }

            return source + " ...";
        }

       
    }
}