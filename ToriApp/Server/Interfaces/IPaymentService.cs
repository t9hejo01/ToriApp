using Stripe.Checkout;
using ToriApp.Shared;

namespace ToriApp.Server.Interfaces
{
    public interface IPaymentService
    {
        Task<Session> CreateCheckoutSession();
        Task<ServiceResponse<bool>> FulfillOrder(HttpRequest request);
    }
}
