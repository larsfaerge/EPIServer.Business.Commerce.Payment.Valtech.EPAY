using System;
using EPiServer.Business.Commerce.Payment.Valtech.Epay.Helpers;
using Mediachase.Commerce;
using Xunit;

namespace EPIServer.Business.Commerce.Payment.Valtech.EPAY.Tests
{

    public class EpayCurrenciesTest
    {
        [InlineData("USD", "840")]
        [InlineData("XX", "")]
        [Theory]
        public void GetCurrencyCode_TestCode_ReturnsExpectedResult(string currencycode, string expected)
        {
            var result = EpayCurrencies.GetCurrencyCode(new Currency(currencycode));

            Assert.Equal(expected, result);
        }
      
    }
}
