namespace ToriApp.Client.Services.CategoryService
{
    public interface ICategoryService
    {
        event Action OnChanged;
        List<Category> Categories { get; set; }
        List<Category> AdminCategories { get; set; }
        Task GetCategories();
        Task GetAdminCategories();
        Task AddCategory(Category category);
        Category CreateNewCategory();
        Task DeleteCategory(int categoryId);
        Task UpdateCategory(Category category);
    }
}
