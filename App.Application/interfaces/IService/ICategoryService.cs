public interface ICategoryService
{
    Task<PagedResponse<GetCategoryResponse>> GetAllCategories(CancellationToken cancellationToken); 
    Task<GetCategoryResponse> GetCategoryById(Guid id, CancellationToken cancellationToken);
    Task<GetCategoryResponse> CreateCategory(CreateUpdateCategoryRequest request, CancellationToken cancellationToken);
    Task<GetCategoryResponse> UpdateCategory(Guid id, CreateUpdateCategoryRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteCategory(Guid id, CancellationToken cancellationToken);
}