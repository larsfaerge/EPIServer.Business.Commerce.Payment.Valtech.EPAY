using System;
using System.Collections.Generic;
using System.Linq;
using Mediachase.Commerce.Core;
using Mediachase.Commerce.Orders.Dto;
using Mediachase.Commerce.Orders.Managers;

namespace EPiServer.Business.Commerce.Payment.Valtech.Epay.Helpers
{
    /// <summary>
    /// Represents DIBS configuration data.
    /// </summary>
    public class EpayConfiguration
    {

        #region Properties

        public const string UserParameter = "MerchantNumber";
        public const string PasswordParameter = "Password";
        public const string ProcessingUrlParamter = "ProcessingUrl";
        public const string AcceptUrlParamter = "AcceptUrl";
        public const string CancelUrlParamter = "CancelUrl";
        public const string CssurlParamter = "Cssurl";
        public const string MobileCssUrlParamter = "MobileCssUrl";
        public const string Md5Key1Parameter = "MD5Key1";
        public const string EpaySystemName = "EPAY";

        private PaymentMethodDto _paymentMethodDto;
        private IDictionary<string, string> _settings;

        /// <summary>
        /// Gets the payment method ID.
        /// </summary>
        public Guid PaymentMethodId { get; protected set; }

        /// <summary>
        /// Gets the merchant number.
        /// </summary>
        public string Merchant { get; protected set; }

        /// <summary>
        /// Gets the password.
        /// </summary>
        public string Password { get; protected set; }

        /// <summary>
        /// Gets the progressing Url.
        /// </summary>
        public string ProcessingUrl { get; protected set; }

        /// <summary>
        /// Gets the CallBackURL Url.
        /// </summary>
        public string CallBackUrl { get; protected set; }

        /// <summary>
        /// Gets the accepturl Url.
        /// </summary>
        public string AcceptUrl { get; protected set; }

        /// <summary>
        /// Gets the cancel Url.
        /// </summary>
        public string CancelUrl { get; protected set; }
        
        /// <summary>
        /// Gets the css url (optional)
        /// </summary>
        public string Cssurl { get; protected set; }

        /// <summary>
        /// Gets the css url (optional)
        /// </summary>
        public string MobileCssUrl { get; protected set; }

        /// <summary>
        /// Gets the MD5 key setting.
        /// </summary>
        public string MD5Key { get; protected set; }
        
        #endregion

        /// <summary>
        /// Initializes a new instance of <see cref="EpayConfiguration"/>.
        /// </summary>
        public EpayConfiguration():this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="EpayConfiguration"/> with specific settings.
        /// </summary>
        /// <param name="settings">The specific settings.</param>
        public EpayConfiguration(IDictionary<string, string> settings)
        {
            Initialize(settings);
        }

        /// <summary>
        /// Gets the PaymentMethodDto's parameter (setting in CommerceManager of epay) by name.
        /// </summary>
        /// <param name="paymentMethodDto">The payment method dto.</param>
        /// <param name="parameterName">The parameter name.</param>
        /// <returns>The parameter row.</returns>
        public static PaymentMethodDto.PaymentMethodParameterRow GetParameterByName(PaymentMethodDto paymentMethodDto, string parameterName)
        {
            var rowArray = (PaymentMethodDto.PaymentMethodParameterRow[])paymentMethodDto.PaymentMethodParameter.Select($"Parameter = '{parameterName}'");
            return rowArray.Length > 0 ? rowArray[0] : null;
        }

        /// <summary>
        /// Gets the PaymentMethodDto of Epay.
        /// </summary>
        /// <returns>The Epay payment method.</returns>
        public static PaymentMethodDto GetEpayPaymentMethod()
        {
            return PaymentManager.GetPaymentMethodBySystemName(EpaySystemName, SiteContext.Current.LanguageName);
        }

        protected virtual void Initialize(IDictionary<string, string> settings)
        {
            _paymentMethodDto = GetEpayPaymentMethod();
            PaymentMethodId = GetPaymentMethodId();

            _settings = settings ?? GetSettings();
            GetParametersValues();
        }

        private IDictionary<string, string> GetSettings()
        {
            return _paymentMethodDto.PaymentMethod
                                    .FirstOrDefault()
                                   ?.GetPaymentMethodParameterRows()
                                   ?.ToDictionary(row => row.Parameter, row => row.Value);
        }

        private void GetParametersValues()
        {
            if (_settings != null)
            {
                Merchant = GetParameterValue(UserParameter);
                Password = GetParameterValue(PasswordParameter);
                ProcessingUrl = GetParameterValue(ProcessingUrlParamter);
                AcceptUrl = GetParameterValue(AcceptUrlParamter);
                CancelUrl = GetParameterValue(CancelUrlParamter);
                Cssurl = GetParameterValue(CssurlParamter);
                MobileCssUrl = GetParameterValue(MobileCssUrlParamter);
                MD5Key = GetParameterValue(Md5Key1Parameter);
            }
        }
        private string GetParameterValue(string parameterName)
        {
            string parameterValue;
            return _settings.TryGetValue(parameterName, out parameterValue) ? parameterValue : string.Empty;
        }

        private Guid GetPaymentMethodId()
        {
            var ePayPaymentMethodRow = _paymentMethodDto.PaymentMethod.Rows[0] as PaymentMethodDto.PaymentMethodRow;
            var paymentMethodId = ePayPaymentMethodRow != null ? ePayPaymentMethodRow.PaymentMethodId : Guid.Empty;
            return paymentMethodId;
        }
    }
}
