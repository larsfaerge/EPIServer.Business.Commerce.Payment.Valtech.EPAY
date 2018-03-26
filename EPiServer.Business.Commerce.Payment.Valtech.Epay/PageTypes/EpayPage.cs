using EPiServer.Core;
using EPiServer.DataAnnotations;

namespace EPiServer.Business.Commerce.Payment.Valtech.Epay.PageTypes
{
    [ContentType(GUID = "adc6231d-3f48-4a1c-bfc6-353604a829e3",
        DisplayName = "Epay Payment Page",
        Description = "Epay pagettype",
        GroupName = "Payment",
        Order = 100)]
    [ImageUrl("~/styles/images/epay-bambora-logo.png")]
    public class EpayPage : PageData
    {
    }
}