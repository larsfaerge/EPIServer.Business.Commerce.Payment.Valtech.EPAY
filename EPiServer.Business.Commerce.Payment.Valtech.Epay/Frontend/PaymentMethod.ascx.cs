using System.Web.UI;
using EPiServer.Business.Commerce.Payment.Valtech.Epay.Helpers;
using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Website;

namespace EPiServer.Business.Commerce.Payment.Valtech.Epay.Frontend
{
    /// <summary>
    ///	Implements User interface for generic gateway
    /// </summary>
    public partial class PaymentMethod : UserControl, IPaymentOption
    {
        /// <summary>
        /// Handles the Load event of the Page control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void Page_Load(object sender, System.EventArgs e)
        {
            if (Request.Form["paymentprovider"] != null && Request.Form["paymentprovider"].Equals(EpayConfiguration.EpaySystemName))
            {
                ErrorManager.GenerateError(Utilities.Translate("CancelMessage"));
            }

            if (!IsPostBack)
            {
                var epayConfiguration = new EpayConfiguration();
                if (string.IsNullOrEmpty(epayConfiguration.ProcessingUrl) || string.IsNullOrEmpty(epayConfiguration.MD5Key))
                {
                    ConfigMessage.Text = Utilities.Translate("EpaySettingsError");
                }
            }
        }
        
        /// <summary>
        /// Validates input data for the control. In case of Credit card pre authentication will be the best way to
        /// validate. The error message if any should be displayed within a control itself.
        /// </summary>
        /// <returns>Returns false if validation is failed.</returns>
        public bool ValidateData()
        {
            return true;
        }

        /// <summary>
        /// This method is called before the order is completed. This method should check all the parameters
        /// and validate the credit card or other parameters accepted.
        /// </summary>
        /// <param name="orderForm">The order form.</param>
        public Mediachase.Commerce.Orders.Payment PreProcess(OrderForm orderForm)
        {
            var otherPayment = new OtherPayment { TransactionType = TransactionType.Authorization.ToString() };
            return otherPayment as Mediachase.Commerce.Orders.Payment;
        }

        /// <summary>
        /// This method is called after the order is placed. This method should be used by the gateways that want to
        /// redirect customer to their site.
        /// </summary>
        /// <param name="orderForm">The order form.</param>
        public bool PostProcess(OrderForm orderForm)
        {
            return true;
        }
    }
}