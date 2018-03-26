﻿using System;
using System.Linq;
using EPiServer.Business.Commerce.Payment.Valtech.Epay.Helpers;
using EPiServer.Commerce.Order;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Orders.Dto;

namespace EPiServer.Business.Commerce.Payment.Valtech.Epay
{
    [ServiceConfiguration(typeof(IPaymentOption))]
    public class EpayPaymentOption : IPaymentOption
    {
        private readonly IOrderGroupFactory _orderGroupFactory;
        private readonly PaymentMethodDto.PaymentMethodRow _paymentMethod;

        public Guid PaymentMethodId { get; }
        public string SystemKeyword { get; }
        public string Name { get; }
        public string Description { get; }

        public EpayPaymentOption() : this(ServiceLocator.Current.GetInstance<IOrderGroupFactory>())
        {
        }

        public EpayPaymentOption(IOrderGroupFactory orderGroupFactory)
        {
            _orderGroupFactory = orderGroupFactory;
            _paymentMethod = EpayConfiguration.GetEpayPaymentMethod()?.PaymentMethod?.FirstOrDefault();

            if (_paymentMethod == null)
            {
                return;
            }

            PaymentMethodId = _paymentMethod.PaymentMethodId;
            SystemKeyword = _paymentMethod.SystemKeyword;
            Name = _paymentMethod.Name;
            Description = _paymentMethod.Description;
        }

        public IPayment CreatePayment(decimal amount, IOrderGroup orderGroup)
        {
            var type = Type.GetType(_paymentMethod.PaymentImplementationClassName);
            var payment = type == null ? orderGroup.CreatePayment(_orderGroupFactory) : orderGroup.CreatePayment(_orderGroupFactory, type);

            payment.PaymentMethodId = _paymentMethod.PaymentMethodId;
            payment.PaymentMethodName = _paymentMethod.Name;
            payment.Amount = amount;
            payment.Status = PaymentStatus.Pending.ToString();

            payment.TransactionType = TransactionType.Authorization.ToString();

            return payment;
        }
        
        public bool ValidateData()
        {
            return true;
        }
    }
}