public class CategoryService : ICategoryService
{
    private readonly IRepository<Category> _categoryRepository;
    private readonly IDefaultMapper _mapper;

    public CategoryService(IRepository<Category> categoryRepository, IDefaultMapper mapper)
    {
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    private async Task<Category> GetCategoryByIdAsync(Guid id)
    {
        var category = await _categoryRepository.GetFirstOrDefault(c => c.Id == id) 
            ?? throw new NotFoundException("Category", id);
            
        return category;
    }

    private async Task CategoryNameConflictCheck(string name, Guid? excludeId = null)
    {
        var query = await _categoryRepository.GetFirstOrDefault(c =>
            c.Name == name && (!excludeId.HasValue || c.Id != excludeId));

        if (query != null)
            throw new AlreadyExistsException("Category", name);
    }

    public async Task<PagedResponse<GetCategoryResponse>> GetAllCategories()
    {
        var categoriesResult = await _categoryRepository.GetAll(
            orderBy: q => q.OrderBy(c => c.Name)
        );

        var pagedResponse = _mapper.Map<PagedList<Category>, PagedResponse<GetCategoryResponse>>(categoriesResult);
        return pagedResponse;
    }

    public async Task<GetCategoryResponse> GetCategoryById(Guid id)
    {
        var categoryEntity = await GetCategoryByIdAsync(id);

        return _mapper.Map<Category, GetCategoryResponse>(categoryEntity);
    }

    public async Task<GetCategoryResponse> CreateCategory(CreateUpdateCategoryRequest request)
    {
        await CategoryNameConflictCheck(request.Name);

        var newCategory = _mapper.Map<CreateUpdateCategoryRequest, Category>(request);

        _categoryRepository.Insert(newCategory);
        await _categoryRepository.SaveChangesAsync();

        return _mapper.Map<Category, GetCategoryResponse>(newCategory);
    }

    public async Task<GetCategoryResponse> UpdateCategory(Guid id, CreateUpdateCategoryRequest request)
    {
        var category = await GetCategoryByIdAsync(id);

        if (category.Name == request.Name)
            return _mapper.Map<Category, GetCategoryResponse>(category);
        
        await CategoryNameConflictCheck(request.Name, id);

        category.Name = request.Name;
        _categoryRepository.Update(category);
        await _categoryRepository.SaveChangesAsync();

        return _mapper.Map<Category, GetCategoryResponse>(category);
    }

    public async Task<bool> DeleteCategory(Guid id)
    {
        var category = await GetCategoryByIdAsync(id);
    
        _categoryRepository.Delete(category);
        await _categoryRepository.SaveChangesAsync();

        return true; 
    }
}