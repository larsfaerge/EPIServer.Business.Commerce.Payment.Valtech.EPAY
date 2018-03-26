using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using EPiServer.Business.Commerce.Payment.Valtech.Epay.Helpers;
using EPiServer.Commerce.Order;
using EPiServer.Commerce.Orders.Internal;
using EPiServer.Data.Dynamic;
using EPiServer.Framework.Localization;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Core.Features;
using Mediachase.Commerce.Customers;
using Mediachase.Commerce.Extensions;
using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Orders.Managers;
using Mediachase.Commerce.Plugins.Payment;
using Mediachase.Commerce.Security;
using Mediachase.Data.Provider;

namespace EPiServer.Business.Commerce.Payment.Valtech.Epay
{
    public class EpayPaymentGateway : AbstractPaymentGateway, IPaymentPlugin
    {
        private IFeatureSwitch _featureSwitch;
        private IInventoryProcessor _inventoryProcessor;
        private IOrderRepository _orderRepository;
        private LocalizationService _localizationService;
        private EpayRequestHelper _epayRequestHelper;
        //private string subscriptionkey = "subscriptionId";

        public EpayPaymentGateway():this(
            ServiceLocator.Current.GetInstance<IFeatureSwitch>(),
            ServiceLocator.Current.GetInstance<IInventoryProcessor>(),
            ServiceLocator.Current.GetInstance<IOrderRepository>(),
            ServiceLocator.Current.GetInstance<LocalizationService>())
        {
        }

        public EpayPaymentGateway(
            IFeatureSwitch featureSwitch,
            IInventoryProcessor inventoryProcessor,
            IOrderRepository orderRepository,
            LocalizationService localizationService)
        {
            _featureSwitch = featureSwitch;
            _inventoryProcessor = inventoryProcessor;
            _orderRepository = orderRepository;
            _localizationService = localizationService;

            _epayRequestHelper = new EpayRequestHelper();
        }

        /// <summary>
        /// Main entry point of ECF Payment Gateway.
        /// </summary>
        /// <param name="payment">The payment to process</param>
        /// <param name="message">The message ?</param>
        /// <returns>return false and set the message will make the WorkFlow activity raise PaymentExcetion(message)</returns>
        public override bool ProcessPayment(Mediachase.Commerce.Orders.Payment payment, ref string message)
        {
            var orderGroup = payment.Parent.Parent;
            var paymentProcessingResult = ProcessPayment(orderGroup, payment);

            if (!string.IsNullOrEmpty(paymentProcessingResult.RedirectUrl))
            {
                HttpContext.Current.Response.Redirect(paymentProcessingResult.RedirectUrl);
            }
            message = paymentProcessingResult.Message;
            return paymentProcessingResult.IsSuccessful;
        }

        /// <summary>
        /// Processes the payment.
        /// </summary>
        /// <param name="orderGroup">The order group.</param>
        /// <param name="payment">The payment.</param>
        public PaymentProcessingResult ProcessPayment(IOrderGroup orderGroup, IPayment payment)
        {
            // checks
            if (HttpContext.Current == null){
                return PaymentProcessingResult.CreateSuccessfulResult(Utilities.Translate("ProcessPaymentNullHttpContext"));
            }

            if (payment == null)
            {
                return PaymentProcessingResult.CreateUnsuccessfulResult(Utilities.Translate("PaymentNotSpecified"));
            }
          
            var orderForm = orderGroup.Forms.FirstOrDefault(f => f.Payments.Contains(payment));
            if (orderForm == null)
            {
                return PaymentProcessingResult.CreateUnsuccessfulResult(Utilities.Translate("PaymentNotAssociatedOrderForm"));
            }

            // todo: not needed...
            // bool isRegularTransaction = IsRegularTransaction(orderGroup);

            // its a purchase order
            var purchaseOrder = orderGroup as IPurchaseOrder;
            if (purchaseOrder != null)
            {
                if (payment.TransactionType == TransactionType.Capture.ToString())
                {
                    // return true meaning the capture request is done by epay
                    var result = _epayRequestHelper.PostCaptureRequest(payment, purchaseOrder);
                    if (result.Equals(false))
                    {
                        return PaymentProcessingResult.CreateUnsuccessfulResult("There was an error while capturing payment with Epay");
                    }

                    return PaymentProcessingResult.CreateSuccessfulResult(string.Empty);
                }

                //todo: add credit
                if (payment.TransactionType == TransactionType.Credit.ToString())
                {
                    //return PaymentProcessingResult.CreateUnsuccessfulResult("The current payment method credit does not support this order type.");

                    var transactionID = payment.TransactionID;
                    if (string.IsNullOrEmpty(transactionID) || transactionID.Equals("0"))
                    {
                        return PaymentProcessingResult.CreateUnsuccessfulResult("TransactionID is not valid or the current payment method does not support this order type.");
                    }

                    // The transact must be captured before refunding
                    string message = string.Empty;
                    if ( _epayRequestHelper.PostRefundRequest(payment, purchaseOrder, ref message))
                    {
                        return PaymentProcessingResult.CreateSuccessfulResult(string.Empty);
                    }
                    return PaymentProcessingResult.CreateUnsuccessfulResult($"There was an error while refunding with epay. {message}");
                }

                // right now we do not support processing the order which is created by Commerce Manager
                return PaymentProcessingResult.CreateUnsuccessfulResult("The current payment method does not support this order type.");
            }

            var cart = orderGroup as ICart;
            if (cart != null && cart.OrderStatus == OrderStatus.Completed)
            {
                return PaymentProcessingResult.CreateSuccessfulResult(string.Empty);
            }
            _orderRepository.Save(orderGroup);

            var redirectUrl = Utilities.GetUrlFromStartPageReferenceProperty("EpayPaymentPage");

            return PaymentProcessingResult.CreateSuccessfulResult(string.Empty, redirectUrl);
        }

        /// <summary>
        /// Check if normal order (not recurring)
        /// </summary>
        /// <param name="orderGroup"></param>
        /// <returns>True if not paymentplan</returns>
        private bool IsRegularTransaction(IOrderGroup orderGroup)
        {
            bool flag = true;
            if ((orderGroup is IPurchaseOrder)) //todo:check && (string.Compare(this.GetSetting(RecurringMethodParameterName), AuthorizeRecurringMethodParameterValue, StringComparison.OrdinalIgnoreCase) == 0))
            {
                PurchaseOrder _tmp = (PurchaseOrder)orderGroup;
                if (_tmp.ParentOrderGroupId > 0)
                {
                    flag = false;
                }
            }
            return flag;
        }


        /// <summary>
        /// Processes the unsuccessful transaction.
        /// </summary>
        /// <param name="cancelUrl">The cancel url.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>The url redirection after process.</returns>
        public string ProcessUnsuccessfulTransaction(string cancelUrl, string errorMessage)
        {
            return UriSupport.AddQueryString(cancelUrl, "message", errorMessage);
        }

        /// <summary>
        /// Processes the successful transaction, will be called when epay server processes 
        /// the payment successfully and redirect back.
        /// </summary>
        /// <param name="cart">The cart that was processed.</param>
        /// <param name="payment">The order payment.</param>
        /// <param name="transactionId">The transaction id.</param>
        /// <param name="subscriptionId">The subscription id in case returned</param>
        /// <param name="orderNumber">The order number.</param>
        /// <param name="acceptUrl">The redirect url when finished.</param>
        /// <param name="cancelUrl">The redirect url when error happens.</param>
        /// <returns>The redirection url after processing.</returns>
        public string ProcessSuccessfulTransaction(ICart cart, IPayment payment, string transactionId, string subscriptionId, string orderNumber, string acceptUrl, string cancelUrl)
        {
            if (cart == null)
            {
                return cancelUrl;
            }

            var redirectionUrl = string.Empty;
            using (TransactionScope scope = new TransactionScope())
            {
                // Change status of payments to processed.
                // It must be done before execute workflow to ensure payments which should mark as processed.
                // To avoid get errors when executed workflow.
                PaymentStatusManager.ProcessPayment(payment);

                var errorMessages = new List<string>();
                var cartCompleted = DoCompletingCart(cart, errorMessages);

                if (!cartCompleted)
                {
                    return UriSupport.AddQueryString(cancelUrl, "message", string.Join(";", errorMessages.Distinct().ToArray()));
                }

                // Save the transact from epay to payment.
                payment.TransactionID = transactionId;
                

                // Save subscription id in the payment properties.
                if (!string.IsNullOrEmpty(subscriptionId))
                {
                    payment.AuthorizationCode = subscriptionId; //properties wasnt saved
                }
                
                // Create a purchase order
                var purchaseOrder = MakePurchaseOrder(cart, orderNumber);

                // Create a subscription plan
                var supscriptionPlan = MakeSubscription(cart, payment);
                
                // Commit changes
                scope.Complete();

                if (HttpContext.Current.Session != null){
                    HttpContext.Current.Session.Remove("LastCouponCode"); //todo: is this needed?
                }

                //todo: change in case of subscription or?
                redirectionUrl = UpdateAcceptUrl(purchaseOrder, payment, acceptUrl);
            }

            return redirectionUrl;
        }

        /// <summary>
        /// Create subscription plan
        /// </summary>
        /// <param name="cart">The abstract cart</param>
        /// <param name="payment">The payment</param>
        /// <returns>IPaymentPlan in case its created</returns>
        private IPaymentPlan MakeSubscription(ICart cart, IPayment payment)
        {
            // Verify that the cart has a subscription
            if (cart == null){return null;}
            if (!(cart.Properties.ContainsKey("subscription") && cart.Properties["subscription"].Equals("true")))
            {
                return null;
            }

            // Convert cart to payment plan
            var paymentPlanOrderLink = _orderRepository.SaveAsPaymentPlan(cart); //todo: verify that cart hasnt been removed 
            
            // Add properties to payment plan
            var paymentPlan = _orderRepository.Load<IPaymentPlan>(paymentPlanOrderLink.OrderGroupId);
            paymentPlan.CycleLength = 1;
            paymentPlan.CycleMode = PaymentPlanCycle.Days;
            paymentPlan.CompletedCyclesCount = 0; //todo: should it be one or 0.
            paymentPlan.IsActive = true;
            paymentPlan.StartDate = DateTime.UtcNow;

            _orderRepository.Save(paymentPlan); //todo: could be saved elsewhere 

            return paymentPlan;

            // advanced - in case we need to break into more than one subscription
            // cart
            // cart.line 1 : normal order
            // cart.line 2 : subscription 1. same startdate, same frequence
            // cart.line 3 : subscription 1. same startdate, same frequence
            // cart.line 4 : subscription 2. different startedate or frequence, different or same frequence

            //var cartx = _orderRepository.Load<Cart>(cart.OrderLink);
            //Cart cartx = Cart

            ////https://world.episerver.com/documentation/class-library/?product=commerce&version=11
            //List<IPaymentPlan> listPaymentPlans = new List<IPaymentPlan>();

            //// Loop cart and create subscriptions
            //foreach (var line in cart.GetAllLineItems())
            //{
            //    // todo: some business logic
            //    if (line.Properties.ContainsKey("subscription") && line.Properties["subscription"].Equals("true"))
            //    {
            //        // create new payment plan or use existing
            //        DateTime startDate = DateTime.Parse(DateTime.UtcNow.ToShortDateString());
            //        PaymentPlanCycle planCycle = PaymentPlanCycle.Days;
            //        int cycleLength = 1;

            //        // Create or reference paymentplan
            //        IPaymentPlan paymentplan = CreatePaymentPlan(cart, planCycle, cycleLength, ref listPaymentPlans);
            //        paymentplan.AddLineItem(line, new OrderGroupFactory()); 



            //        //Cart parent = (Cart)cart.Clone();
            //        //IPaymentPlan testpaymentPlan = _orderRepository.Create<IPaymentPlan>(cart.CustomerId, "Default");
            //        //testpaymentPlan.CopyFrom(cart.orde);
            //        //Cart parent = (Cart)base.Clone();
            //        //ICart cart3 = new Cart("sub", cart.CustomerId);
            //        ////cart3 = cart.p
            //        //cart.DeepClone();

            //        //listSubscriptionCarts.Add();        
            //    }

            //}
            
        }

        private IPaymentPlan CreatePaymentPlan(ICart cart, PaymentPlanCycle cycleMode, int cycleLength, ref List<IPaymentPlan> listPaymentPlans)
        {
            if (listPaymentPlans == null){
                listPaymentPlans = new List<IPaymentPlan>();
            }

            // use existing plan
            if (listPaymentPlans.Any(row => row.CycleLength == cycleLength && row.CycleMode == cycleMode))
            {
                return listPaymentPlans.FirstOrDefault(row =>
                    row.CycleLength == cycleLength && row.CycleMode == cycleMode);
            }

            // create a new plan
            var paymentPlan = _orderRepository.Create<IPaymentPlan>(cart.CustomerId, cart.Name);
            paymentPlan.CycleLength = cycleLength;
            paymentPlan.CycleMode = cycleMode;
            paymentPlan.CompletedCyclesCount = 0; //todo: should it be one or 0.
            paymentPlan.IsActive = true;
            paymentPlan.StartDate = DateTime.UtcNow;

            // add the details

            listPaymentPlans.Add(paymentPlan);

            return paymentPlan;
        }

        private IPurchaseOrder MakePurchaseOrder(ICart cart, string orderNumber)
        {
            // Save changes
            // this might cause problem when checkout using multiple shipping address because ECF workflow does not handle it. Modify the workflow instead of modify in this payment
            var purchaseOrderLink = _orderRepository.SaveAsPurchaseOrder(cart);
            var purchaseOrder = _orderRepository.Load<IPurchaseOrder>(purchaseOrderLink.OrderGroupId);

            UpdateOrder(purchaseOrder, orderNumber);
            UpdateLastOrderOfCurrentContact(CustomerContext.Current.CurrentContact, purchaseOrder.Created);
            
            AddNoteToPurchaseOrder($"New order placed by {PrincipalInfo.CurrentPrincipal.Identity.Name} in Public site", purchaseOrder);

            // Save purchase order and remove cart
            _orderRepository.Save(purchaseOrder);
            _orderRepository.Delete(cart.OrderLink);

            return purchaseOrder;
        }

        private void UpdateOrder(IPurchaseOrder purchaseOrder, string orderNumber)
        {
            purchaseOrder.OrderStatus = OrderStatus.InProgress;
            purchaseOrder.OrderNumber = orderNumber;

            // Update display name of product by current language
            Utilities.UpdateDisplayNameWithCurrentLanguage(purchaseOrder);
        }

        /// <summary>
        /// Update last order time stamp which current user completed.
        /// </summary>
        /// <param name="contact">The customer contact.</param>
        /// <param name="datetime">The order time.</param>
        private void UpdateLastOrderOfCurrentContact(CustomerContact contact, DateTime datetime)
        {
            if (contact != null)
            {
                contact.LastOrder = datetime;
                contact.SaveChanges();
            }
        }

        private string UpdateAcceptUrl(IPurchaseOrder purchaseOrder, IPayment payment, string acceptUrl)
        {
            var redirectionUrl = UriSupport.AddQueryString(acceptUrl, "success", "true");
            redirectionUrl = UriSupport.AddQueryString(redirectionUrl, "contactId", purchaseOrder.CustomerId.ToString());
            redirectionUrl = UriSupport.AddQueryString(redirectionUrl, "orderNumber", purchaseOrder.OrderLink.OrderGroupId.ToString());
            redirectionUrl = UriSupport.AddQueryString(redirectionUrl, "notificationMessage", string.Format(_localizationService.GetString("/OrderConfirmationMail/ErrorMessages/SmtpFailure"), payment.BillingAddress.Email));
            redirectionUrl = UriSupport.AddQueryString(redirectionUrl, "email", payment.BillingAddress.Email);
            return redirectionUrl;
        }

        /// <summary>
        /// Validates and completes a cart.
        /// </summary>
        /// <param name="cart">The cart.</param>
        /// <param name="errorMessages">The error messages.</param>
        private bool DoCompletingCart(ICart cart, IList<string> errorMessages)
        {
            var isSuccess = true;

            if (_featureSwitch.IsSerializedCartsEnabled())
            {
                var validationIssues = new Dictionary<ILineItem, IList<ValidationIssue>>();
                cart.AdjustInventoryOrRemoveLineItems((item, issue) => AddValidationIssues(validationIssues, item, issue), _inventoryProcessor);

                isSuccess = !validationIssues.Any();

                foreach (var issue in validationIssues.Values.SelectMany(x => x).Distinct())
                {
                    if (issue == ValidationIssue.RejectedInventoryRequestDueToInsufficientQuantity)
                    {
                        errorMessages.Add(Utilities.Translate("NotEnoughStockWarning"));
                    }
                    else
                    {
                        errorMessages.Add(Utilities.Translate("CartValidationWarning"));
                    }
                }

                return isSuccess;
            }

            // Execute CheckOutWorkflow with parameter to ignore running process payment activity again.
            var isIgnoreProcessPayment = new Dictionary<string, object> { { "PreventProcessPayment", true } };
            var workflowResults = OrderGroupWorkflowManager.RunWorkflow((OrderGroup)cart, OrderGroupWorkflowManager.CartCheckOutWorkflowName, true, isIgnoreProcessPayment);

            var warnings = workflowResults.OutputParameters["Warnings"] as StringDictionary;
            isSuccess = warnings.Count == 0;

            foreach (string message in warnings.Values)
            {
                errorMessages.Add(message);
            }

            return isSuccess;
        }

        /// <summary>
        /// Adds the note to purchase order.
        /// </summary>
        /// <param name="note">The note detail.</param>
        /// <param name="purchaseOrder">The purchase order.</param>
        private void AddNoteToPurchaseOrder(string note, IPurchaseOrder purchaseOrder)
        {
            var orderNote = purchaseOrder.CreateOrderNote();
            orderNote.Type = OrderNoteTypes.System.ToString();
            orderNote.CustomerId = PrincipalInfo.CurrentPrincipal.GetContactId();
            orderNote.Title = note.Substring(0, Math.Min(note.Length, 24)) + "...";
            orderNote.Detail = note;
            orderNote.Created = DateTime.UtcNow;
            purchaseOrder.Notes.Add(orderNote);
        }

        private void AddValidationIssues(IDictionary<ILineItem, IList<ValidationIssue>> issues, ILineItem lineItem, ValidationIssue issue)
        {
            if (!issues.ContainsKey(lineItem))
            {
                issues.Add(lineItem, new List<ValidationIssue>());
            }

            if (!issues[lineItem].Contains(issue))
            {
                issues[lineItem].Add(issue);
            }
        }
    }
}