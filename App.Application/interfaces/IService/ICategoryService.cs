public interface ICategoryService
{
    Task<PagedResponse<GetCategoryResponse>> GetAllCategories(); 
    Task<GetCategoryResponse?> GetCategoryById(Guid id);
    Task<GetCategoryResponse> CreateCategory(CreateUpdateCategoryRequest request);
    Task<GetCategoryResponse> UpdateCategory(Guid id, CreateUpdateCategoryRequest request);
    Task<bool> DeleteCategory(Guid id);
}