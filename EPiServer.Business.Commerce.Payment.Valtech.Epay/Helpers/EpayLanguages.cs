using System.Collections.Generic;

namespace EPiServer.Business.Commerce.Payment.Valtech.Epay.Helpers
{
    /// <summary>
    /// Handles epay supported languages.
    /// </summary>
    /// <remarks>http://epay.bambora.com/en/specification</remarks>
    public class EpayLanguages
    {
        // Refer to lang parameter in http://epay.bambora.com/en/specification (
        static readonly IDictionary<string, string> _supportedLanguage = new Dictionary<string, string>
        {
            { "Danish", "1" }, { "English", "2" }, { "Swedish", "3" }, { "Norwegian", "4" },{ "Greenlandic", "5" },
            { "Icelandic", "6" },{ "German", "7" },{ "Finnish", "8" }, { "Spanish", "9" },{ "French", "10" },{ "Polish", "11" },
            { "Italian", "12" },{ "Dutch", "13" }, 
        };

        /// <summary>
        /// Converts the site language to the language which epay can support.
        /// </summary>
        /// <returns>The supported language. O is autodetect language in epay</returns>
        public static string GetCurrentEpaySupportedLanguage(string languageName)
        {
            return _supportedLanguage.TryGetValue(languageName, out var lang)? lang: "0";
        }
    }
}
