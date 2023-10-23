using Microsoft.EntityFrameworkCore;
using ToriApp.Server.Data;
using ToriApp.Server.Interfaces;

namespace ToriApp.Server.Services
{
    public class ProductService : IProductService
    {
        private readonly DataContext _dataContext;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ProductService(DataContext dataContext, IHttpContextAccessor httpContextAccessor)
        {
            _dataContext = dataContext;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<ServiceResponse<Product>> CreateProduct(Product product)
        {
            foreach (var variant in product.Variants)
            {
                variant.ProductType = null;
            }
            _dataContext.Products.Add(product);
            await _dataContext.SaveChangesAsync();
            return new ServiceResponse<Product> { Data = product };
        }

        public async Task<ServiceResponse<bool>> DeleteProduct(int productId)
        {
            var dbProduct = await _dataContext.Products.FindAsync(productId);
            if (dbProduct == null)
            {
                return new ServiceResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = "Product not found."
                };
            }

            dbProduct.Deleted = true;

            await _dataContext.SaveChangesAsync();
            return new ServiceResponse<bool> { Data = true };
        }

        public async Task<ServiceResponse<List<Product>>> GetAdminProducts()
        {
            var response = new ServiceResponse<List<Product>>
            {
                Data = await _dataContext.Products.Where(p => !p.Deleted).Include(p => p.Variants.Where(v => !v.Deleted)).ThenInclude(v => v.ProductType).Include(p => p.Images).ToListAsync()
            };

            return response;
        }

        public async Task<ServiceResponse<List<Product>>> GetFeaturedProducts()
        {
            var response = new ServiceResponse<List<Product>>
            {
                Data = await _dataContext.Products
                .Where(p => p.Featured && p.Visible && !p.Deleted)
                .Include(p => p.Variants.Where(v => v.Visible && !v.Deleted))
                .Include(p => p.Images)
                .ToListAsync()
            };
            
            return response;
        }

        public async Task<ServiceResponse<List<Product>>> GetProductAsync()
        {
            var response = new ServiceResponse<List<Product>>
            {
                Data = await _dataContext.Products
                .Where(p => p.Visible && !p.Deleted)
                .Include(p => p.Variants.Where(v => v.Visible && !v.Deleted))
                .ToListAsync()
            };

            return response;
        }

        public async Task<ServiceResponse<Product>> GetProductAsync(int productId)
        {
            var response = new ServiceResponse<Product>();
            Product product = null;

            if (_httpContextAccessor.HttpContext.User.IsInRole("Admin"))
            {
                product = await _dataContext.Products
                    .Include(p => p.Variants
                    .Where(v => !v.Deleted))
                    .ThenInclude(v => v.ProductType)
                    .Include(p => p.Images)
                    .FirstOrDefaultAsync(p => p.Id == productId && !p.Deleted);
            }
            else
            {
                product = await _dataContext.Products
                    .Include(p => p.Variants.Where(v => v.Visible && !v.Deleted))
                    .ThenInclude(v => v.ProductType)
                    .Include(p => p.Images)
                    .FirstOrDefaultAsync(p => p.Id == productId && !p.Deleted && p.Visible);
            }

            if (product != null)
            {
                response.Data = product;
            }
            else
            {
                response.Success = false;
                response.Message = "This product does not exist.";
            }

            return response;
        }

        public async Task<ServiceResponse<List<Product>>> GetProductByCategory(string categoryUrl)
        {
            var response = new ServiceResponse<List<Product>> 
            {
                Data = await _dataContext.Products
                    .Where(p => p.Category.Url.ToLower().Equals(categoryUrl.ToLower()) && p.Visible && !p.Deleted)
                    .Include(p => p.Variants.Where(v => v.Visible && !v.Deleted))
                    .Include(p => p.Images)
                    .ToListAsync()
            };

            return response;
        }

        public async Task<ServiceResponse<List<string>>> GetProductSearchSuggestions(string searchText)
        {
            var products = await FindProductsBySearchText(searchText);
            List<string> result = new();

            foreach (var product in products)
            {
                if (product.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(product.Title);
                }

                if (product.Description != null)
                {
                    var punctuation = product.Description.Where(char.IsPunctuation).Distinct().ToArray();
                    var words = product.Description.Split().Select(s => s.Trim(punctuation));

                    foreach (var word in words)
                    {
                        if (word.Contains(searchText, StringComparison.OrdinalIgnoreCase) && !result.Contains(word))
                        {
                            result.Add(word);
                        }
                    }
                }
            }

            return new ServiceResponse<List<string>> { Data = result };
        }

        public async Task<ServiceResponse<ProductSearchResult>> SearchProducts(string searchText, int page)
        {
            var pageResults = 2f;
            var pageCount = Math.Ceiling((await FindProductsBySearchText(searchText)).Count / pageResults);
            var products = await _dataContext.Products
                .Where(p => p.Title.ToLower().Contains(searchText.ToLower()) || p.Description.ToLower().Contains(searchText.ToLower()) && p.Visible && !p.Deleted)
                .Include(p => p.Variants)
                .Include(p => p.Images)
                .Skip((page -1) * (int)pageResults)
                .Take((int)pageResults)
                .ToListAsync();

            var response = new ServiceResponse<ProductSearchResult>
            {
                Data = new ProductSearchResult
                {
                    Products = products,
                    CurrentPage = page,
                    Pages = (int)pageCount
                }
            };

            return response;
        }

        public async Task<ServiceResponse<Product>> UpdateProduct(Product product)
        {
            var dbProduct = await _dataContext.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == product.Id);

            if (dbProduct == null)
            {
                return new ServiceResponse<Product>
                {
                    Success = false,
                    Message = "Product not found."
                };
            }

            dbProduct.Title = product.Title;
            dbProduct.Description = product.Description;
            dbProduct.ImageUrl = product.ImageUrl;
            dbProduct.CategoryId = product.CategoryId;
            dbProduct.Visible = product.Visible;
            dbProduct.Featured = product.Featured;

            var productImages = dbProduct.Images;

            foreach (var variant in product.Variants)
            {
                var dbVariant = await _dataContext.ProductVariants.SingleOrDefaultAsync(v => v.ProductId == variant.ProductId && v.ProductTypeId == variant.ProductTypeId);
                if (dbVariant != null)
                {
                    dbVariant.ProductTypeId = variant.ProductTypeId;
                    dbVariant.Price = variant.Price;
                    dbVariant.OriginalPrice = variant.OriginalPrice;
                    dbVariant.Visible = variant.Visible;
                    dbVariant.Deleted = variant.Deleted;
                }
                else
                {
                    variant.ProductType = null;
                    _dataContext.ProductVariants.Add(variant);
                }
            }

            await _dataContext.SaveChangesAsync();
            return new ServiceResponse<Product> { Data = product };
        }

        private async Task<List<Product>> FindProductsBySearchText(string searchText)
        {
            return await _dataContext.Products
                .Where(p => p.Title.ToLower().Contains(searchText.ToLower()) ||
                p.Description.ToLower().Contains(searchText.ToLower()) && p.Visible && !p.Deleted)
                .Include(p => p.Variants)
                .ToListAsync();
        }
    }
}
