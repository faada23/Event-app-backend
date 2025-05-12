public class CategoryService : ICategoryService
{
    private readonly IRepository<Category> _categoryRepository;
    private readonly IDefaultMapper _mapper;

    public CategoryService(IRepository<Category> categoryRepository, IDefaultMapper mapper)
    {
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    private async Task<Result<Category>> GetCategoryEntityByIdAsync(Guid id)
    {
        var categoryResult = await _categoryRepository.GetFirstOrDefault(c => c.Id == id);

        if (!categoryResult.IsSuccess)
        {
            return categoryResult!;
        }
        if (categoryResult.Value == null)
        {
            return Result<Category>.Failure($"Category with id '{id}' was not found.", ErrorType.RecordNotFound);
        }
        return categoryResult!;
    }

    private async Task<Result<bool>> NoCategoryNameConflictCheck(string name, Guid? excludeId = null)
    {
        var queryResult = await _categoryRepository.GetFirstOrDefault(c =>
            c.Name == name && (!excludeId.HasValue || c.Id != excludeId));

        if (!queryResult.IsSuccess)
        {
            return Result<bool>.Failure(queryResult.Message!, queryResult.ErrorType!.Value);
        }

        if (queryResult.Value != null)
        {
            return Result<bool>.Failure($"Category with name '{name}' already exists.", ErrorType.AlreadyExists);
        }
        return Result<bool>.Success(true);
    }

    public async Task<Result<PagedResponse<GetCategoryResponse>>> GetAllCategories()
    {
        var categoriesResult = await _categoryRepository.GetAll(
            orderBy: q => q.OrderBy(c => c.Name)
        );

        if (!categoriesResult.IsSuccess)
        {
            return Result<PagedResponse<GetCategoryResponse>>.Failure(
                categoriesResult.Message!,
                categoriesResult.ErrorType!.Value);
        }

        var pagedResponse = _mapper.Map<PagedList<Category>, PagedResponse<GetCategoryResponse>>(categoriesResult.Value!);

        return Result<PagedResponse<GetCategoryResponse>>.Success(pagedResponse);
    }

    public async Task<Result<GetCategoryResponse>> GetCategoryById(Guid id)
    {
        var categoryEntityResult = await GetCategoryEntityByIdAsync(id);

        if (!categoryEntityResult.IsSuccess)
        {
            return Result<GetCategoryResponse>.Failure(
                categoryEntityResult.Message!,
                categoryEntityResult.ErrorType!.Value);
        }

        return Result<GetCategoryResponse>.Success(_mapper.Map<Category, GetCategoryResponse>(categoryEntityResult.Value!));
    }

    public async Task<Result<GetCategoryResponse>> CreateCategory(CreateUpdateCategoryRequest request)
    {
        var nameConflictResult = await NoCategoryNameConflictCheck(request.Name);
        if (!nameConflictResult.IsSuccess)
        {
            return Result<GetCategoryResponse>.Failure(nameConflictResult.Message!, nameConflictResult.ErrorType!.Value);
        }

        var newCategory = _mapper.Map<CreateUpdateCategoryRequest, Category>(request);

        _categoryRepository.Insert(newCategory);
        var saveResult = await _categoryRepository.SaveChangesAsync();

        if (!saveResult.IsSuccess)
        {
            return Result<GetCategoryResponse>.Failure(saveResult.Message!, saveResult.ErrorType!.Value);
        }

        return Result<GetCategoryResponse>.Success(_mapper.Map<Category, GetCategoryResponse>(newCategory));
    }

    public async Task<Result<GetCategoryResponse>> UpdateCategory(Guid id, CreateUpdateCategoryRequest request)
    {
        var categoryEntityResult = await GetCategoryEntityByIdAsync(id);
        if (!categoryEntityResult.IsSuccess)
        {
            return Result<GetCategoryResponse>.Failure(
                categoryEntityResult.Message!,
                categoryEntityResult.ErrorType!.Value);
        }

        var categoryToUpdate = categoryEntityResult.Value!;

        if (categoryToUpdate.Name == request.Name)
        {
            return Result<GetCategoryResponse>.Success(_mapper.Map<Category, GetCategoryResponse>(categoryToUpdate));
        }

        var nameConflictResult = await NoCategoryNameConflictCheck(request.Name, id);
        if (!nameConflictResult.IsSuccess)
        {
            return Result<GetCategoryResponse>.Failure(nameConflictResult.Message!, nameConflictResult.ErrorType!.Value);
        }

        categoryToUpdate.Name = request.Name;
        _categoryRepository.Update(categoryToUpdate);
        var saveResult = await _categoryRepository.SaveChangesAsync();

        if (!saveResult.IsSuccess)
        {
            return Result<GetCategoryResponse>.Failure(saveResult.Message!, saveResult.ErrorType!.Value);
        }

        return Result<GetCategoryResponse>.Success(_mapper.Map<Category, GetCategoryResponse>(categoryToUpdate));
    }

    public async Task<Result<bool>> DeleteCategory(Guid id)
    {
        var categoryEntityResult = await GetCategoryEntityByIdAsync(id);
        if (!categoryEntityResult.IsSuccess)
        {
            return Result<bool>.Failure(
                categoryEntityResult.Message!,
                categoryEntityResult.ErrorType!.Value);
        }

        _categoryRepository.Delete(categoryEntityResult.Value!);
        var saveResult = await _categoryRepository.SaveChangesAsync();

        if (!saveResult.IsSuccess)
        {
            return Result<bool>.Failure(saveResult.Message!, saveResult.ErrorType!.Value);
        }

        return Result<bool>.Success(true); 
    }
}