using System.Collections.Generic;
using Mediachase.Commerce;

namespace EPiServer.Business.Commerce.Payment.Valtech.Epay.Helpers
{
    ///  /// <summary>
    /// Transform currency code to integer
    /// </summary>
    public class EpayCurrencies
    {
        // Refer to: List is here http://epay.bambora.com/en/currency-codes
        static readonly IDictionary<string, string> _currencyCodes = new Dictionary<string, string>() {
            {"AFA", "4" }
, {"ALL", "8" }
, {"DZD", "12" }
, {"ADP", "20" }
, {"AZM", "31" }
, {"ARS", "32" }
, {"AUD", "36" }
, {"BSD", "44" }
, {"BHD", "48" }
, {"BDT", "50" }
, {"AMD", "51" }
, {"BBD", "52" }
, {"BMD", "60" }
, {"BTN", "64" }
, {"BOB", "68" }
, {"BWP", "72" }
, {"BZD", "84" }
, {"SBD", "90" }
, {"BND", "96" }
, {"BGL", "100" }
, {"MMK", "104" }
, {"BIF", "108" }
, {"KHR", "116" }
, {"CAD", "124" }
, {"CVE", "132" }
, {"KYD", "136" }
, {"LKR", "144" }
, {"CLP", "152" }
, {"CNY", "156" }
, {"COP", "170" }
, {"KMF", "174" }
, {"CRC", "188" }
, {"HRK", "191" }
, {"CUP", "192" }
, {"CYP", "196" }
, {"CZK", "203" }
, {"DKK", "208" }
, {"DOP", "214" }
, {"ECS", "218" }
, {"SVC", "222" }
, {"ETB", "230" }
, {"ERN", "232" }
, {"EEK", "233" }
, {"FKP", "238" }
, {"FJD", "242" }
, {"DJF", "262" }
, {"GMD", "270" }
, {"GHC", "288" }
, {"GIP", "292" }
, {"GTQ", "320" }
, {"GNF", "324" }
, {"GYD", "328" }
, {"HTG", "332" }
, {"HNL", "340" }
, {"HKD", "344" }
, {"HUF", "348" }
, {"ISK", "352" }
, {"INR", "356" }
, {"IDR", "360" }
, {"IRR", "364" }
, {"IQD", "368" }
, {"ILS", "376" }
, {"JMD", "388" }
, {"JPY", "392" }
, {"KZT", "398" }
, {"JOD", "400" }
, {"KES", "404" }
, {"KPW", "408" }
, {"KRW", "410" }
, {"KWD", "414" }
, {"KGS", "417" }
, {"LAK", "418" }
, {"LBP", "422" }
, {"LSL", "426" }
, {"LVL", "428" }
, {"LRD", "430" }
, {"LYD", "434" }
, {"LTL", "440" }
, {"MOP", "446" }
, {"MGF", "450" }
, {"MWK", "454" }
, {"MYR", "458" }
, {"MVR", "462" }
, {"MTL", "470" }
, {"MRO", "478" }
, {"MUR", "480" }
, {"MXN", "484" }
, {"MNT", "496" }
, {"MDL", "498" }
, {"MAD", "504" }
, {"MZM", "508" }
, {"OMR", "512" }
, {"NAD", "516" }
, {"NPR", "524" }
, {"ANG", "532" }
, {"AWG", "533" }
, {"VUV", "548" }
, {"NZD", "554" }
, {"NIO", "558" }
, {"NGN", "566" }
, {"NOK", "578" }
, {"PKR", "586" }
, {"PAB", "590" }
, {"PGK", "598" }
, {"PYG", "600" }
, {"PEN", "604" }
, {"PHP", "608" }
, {"GWP", "624" }
, {"TPE", "626" }
, {"QAR", "634" }
, {"ROL", "642" }
, {"RUB", "643" }
, {"RWF", "646" }
, {"SHP", "654" }
, {"STD", "678" }
, {"SAR", "682" }
, {"SCR", "690" }
, {"SLL", "694" }
, {"SGD", "702" }
, {"SKK", "703" }
, {"VND", "704" }
, {"SIT", "705" }
, {"SOS", "706" }
, {"ZAR", "710" }
, {"ZWD", "716" }
, {"SDD", "736" }
, {"SRG", "740" }
, {"SZL", "748" }
, {"SEK", "752" }
, {"CHF", "756" }
, {"SYP", "760" }
, {"THB", "764" }
, {"TOP", "776" }
, {"TTD", "780" }
, {"AED", "784" }
, {"TND", "788" }
, {"TRL", "792" }
, {"TMM", "795" }
, {"UGX", "800" }
, {"MKD", "807" }
, {"RUR", "810" }
, {"EGP", "818" }
, {"GBP", "826" }
, {"TZS", "834" }
, {"USD", "840" }
, {"UYU", "858" }
, {"UZS", "860" }
, {"VEB", "862" }
, {"YER", "886" }
, {"YUM", "891" }
, {"ZMK", "894" }
, {"TWD", "901" }
, {"TRY", "949" }
, {"XAF", "950" }
, {"XCD", "951" }
, {"XOF", "952" }
, {"XPF", "953" }
, {"TJS", "972" }
, {"AOA", "973" }
, {"BYR", "974" }
, {"BGN", "975" }
, {"CDF", "976" }
, {"BAM", "977" }
, {"EUR", "978" }
, {"MXV", "979" }
, {"UAH", "980" }
, {"GEL", "981" }
, {"ECV", "983" }
, {"BOV", "984" }
, {"PLN", "985" }
, {"BRL", "986" }
, {"CLF", "990" }


};
        
        /// <summary>
        /// Converts the currency code of the site to
        /// the  number for that currency for epay to understand.
        /// </summary>
        /// <param name="currency">The currency.</param>
        /// <returns>The currency code.</returns>
        public static int GetCurrencyCode(Currency currency)
        {
            _currencyCodes.TryGetValue(currency.CurrencyCode, out string code);
            return int.TryParse(code, out int codeResult) ? codeResult : -1;
        }
    }
}
