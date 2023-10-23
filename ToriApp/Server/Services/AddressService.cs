using Microsoft.EntityFrameworkCore;
using ToriApp.Server.Data;
using ToriApp.Server.Interfaces;

namespace ToriApp.Server.Services
{
    public class AddressService : IAddressService
    {
        private readonly IAuthService _authService;
        private readonly DataContext _dataContext;

        public AddressService(IAuthService authService, DataContext dataContext)
        {
            _authService = authService;
            _dataContext = dataContext;
        }
        public async Task<ServiceResponse<Address>> AddOrUpdateAddress(Address address)
        {
            var response = new ServiceResponse<Address>();
            var dbAdress = (await GetAddress()).Data;
            if (dbAdress != null)
            {
                dbAdress.FirstName = address.FirstName;
                dbAdress.LastName = address.LastName;
                dbAdress.Street = address.Street;
                dbAdress.City = address.City;
                dbAdress.PostalCode = address.PostalCode;
            } 
            else
            {
                address.UserId = _authService.GetUserId();
                _dataContext.Addresses.Add(address);
                response.Data = dbAdress;
            }

            await _dataContext.SaveChangesAsync();

            return response;
        }

        public async Task<ServiceResponse<Address>> GetAddress()
        {
            int userId = _authService.GetUserId();
            var address = await _dataContext.Addresses.FirstOrDefaultAsync(a => a.UserId == userId);
            return new ServiceResponse<Address> { Data = address };
        }
    }
}
