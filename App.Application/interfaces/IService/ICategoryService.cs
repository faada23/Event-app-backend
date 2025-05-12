public interface ICategoryService
{
    Task<Result<PagedResponse<GetCategoryResponse>>> GetAllCategories(); 
    Task<Result<GetCategoryResponse>> GetCategoryById(Guid id);
    Task<Result<GetCategoryResponse>> CreateCategory(CreateUpdateCategoryRequest request);
    Task<Result<GetCategoryResponse>> UpdateCategory(Guid id, CreateUpdateCategoryRequest request);
    Task<Result<bool>> DeleteCategory(Guid id);
}