using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")] 
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
    }

    private ActionResult ResultIdIsNull(){
        var badRequest = Result<object>.Failure("Event ID cannot be empty.", ErrorType.InvalidInput);
        return badRequest.ToActionResult();
    }

    [HttpGet]
    [AllowAnonymous] 
    public async Task<ActionResult<List<GetCategoryResponse>>> GetAllCategories()
    {
        var result = await _categoryService.GetAllCategories();
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous] 
    public async Task<ActionResult<GetCategoryResponse>> GetCategoryById(Guid id)
    {
        if (id == Guid.Empty)
            return ResultIdIsNull();

        var result = await _categoryService.GetCategoryById(id);
        return result.ToActionResult();
    }

    [HttpPost]
    [Authorize(Policy ="AdminPolicy")]
    public async Task<ActionResult<GetCategoryResponse>> CreateCategory([FromBody] CreateUpdateCategoryRequest request)
    {
        var result = await _categoryService.CreateCategory(request);
        return result.ToActionResult();
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy ="AdminPolicy")]
    public async Task<ActionResult<GetCategoryResponse>> UpdateCategory(Guid id, [FromBody] CreateUpdateCategoryRequest request)
    {
        if (id == Guid.Empty)
            return ResultIdIsNull();

        var result = await _categoryService.UpdateCategory(id, request);
        return result.ToActionResult();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy ="AdminPolicy")]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        if (id == Guid.Empty)
            return ResultIdIsNull();

        var result = await _categoryService.DeleteCategory(id);

        if (result.IsSuccess)
            return NoContent(); 
        
        var errorResult = Result<object>.Failure(result.Message!, result.ErrorType!.Value);
        return errorResult.ToActionResult();
    }
}