public class CategoryService : ICategoryService
{
    private readonly IRepository<Category> _categoryRepository;
    private readonly IDefaultMapper _mapper;

    public CategoryService(IRepository<Category> categoryRepository, IDefaultMapper mapper)
    {
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    private async Task<Category> GetCategoryByIdAsync(Guid id,CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetFirstOrDefault(
            filter: c => c.Id == id,
            cancellationToken: cancellationToken) 
            ?? throw new NotFoundException("Category", id);
            
        return category;
    }

    private async Task CategoryNameConflictCheck(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = await _categoryRepository.GetFirstOrDefault(
            filter: c =>c.Name == name && (!excludeId.HasValue || c.Id != excludeId),
            cancellationToken: cancellationToken);

        if (query != null)
            throw new AlreadyExistsException("Category", name);
    }

    public async Task<PagedResponse<GetCategoryResponse>> GetAllCategories(CancellationToken cancellationToken)
    {
        var categoriesResult = await _categoryRepository.GetAll(
            orderBy: q => q.OrderBy(c => c.Name),
            cancellationToken: cancellationToken);

        var pagedResponse = _mapper.Map<PagedList<Category>, PagedResponse<GetCategoryResponse>>(categoriesResult);
        return pagedResponse;
    }

    public async Task<GetCategoryResponse> GetCategoryById(Guid id, CancellationToken cancellationToken)
    {
        var categoryEntity = await GetCategoryByIdAsync(id, cancellationToken);

        return _mapper.Map<Category, GetCategoryResponse>(categoryEntity);
    }

    public async Task<GetCategoryResponse> CreateCategory(CreateUpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        await CategoryNameConflictCheck(
            name:request.Name,
            cancellationToken: cancellationToken);

        var newCategory = _mapper.Map<CreateUpdateCategoryRequest, Category>(request);

        _categoryRepository.Insert(newCategory);
        await _categoryRepository.SaveChangesAsync(cancellationToken);

        return _mapper.Map<Category, GetCategoryResponse>(newCategory);
    }

    public async Task<GetCategoryResponse> UpdateCategory(Guid id, CreateUpdateCategoryRequest request,CancellationToken cancellationToken)
    {
        var category = await GetCategoryByIdAsync(id, cancellationToken);

        if (category.Name == request.Name)
            return _mapper.Map<Category, GetCategoryResponse>(category);
        
        await CategoryNameConflictCheck(
            name:request.Name,
            excludeId: id,
            cancellationToken: cancellationToken);

        category.Name = request.Name;
        _categoryRepository.Update(category);
        await _categoryRepository.SaveChangesAsync(cancellationToken);

        return _mapper.Map<Category, GetCategoryResponse>(category);
    }

    public async Task<bool> DeleteCategory(Guid id, CancellationToken cancellationToken)
    {
        var category = await GetCategoryByIdAsync(id, cancellationToken);
    
        _categoryRepository.Delete(category);
        await _categoryRepository.SaveChangesAsync(cancellationToken);

        return true; 
    }
}