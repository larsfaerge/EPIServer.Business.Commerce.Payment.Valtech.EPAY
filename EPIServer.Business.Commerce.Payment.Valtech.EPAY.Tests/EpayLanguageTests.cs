using System;
using EPiServer.Business.Commerce.Payment.Valtech.Epay.Helpers;
using Xunit;

namespace EPIServer.Business.Commerce.Payment.Valtech.EPAY.Tests
{
    public class EpayLanguageTests
    {
        [InlineData("Swedish", "3")]
        [InlineData("XX", "0")]
        [Theory]
        public void etCurrentEpaySupportedLanguage_TestLanguage_ReturnsExpectedResult(string languageName, string expected)
        {
            var result = EpayLanguages.GetCurrentEpaySupportedLanguage(languageName);
            Assert.Equal(expected, result);
        }
    }
}
