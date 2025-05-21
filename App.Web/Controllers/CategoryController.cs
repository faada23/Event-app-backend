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

    [HttpGet]
    [AllowAnonymous] 
    public async Task<ActionResult<List<GetCategoryResponse>>> GetAllCategories(CancellationToken cancellationToken)
    {
        var result = await _categoryService.GetAllCategories(cancellationToken);
        
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous] 
    public async Task<ActionResult<GetCategoryResponse>> GetCategoryById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _categoryService.GetCategoryById(id, cancellationToken);

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy ="AdminPolicy")]
    public async Task<ActionResult<GetCategoryResponse>> CreateCategory([FromBody] CreateUpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        var result = await _categoryService.CreateCategory(request, cancellationToken);

        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy ="AdminPolicy")]
    public async Task<ActionResult<GetCategoryResponse>> UpdateCategory(Guid id, [FromBody] CreateUpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        var result = await _categoryService.UpdateCategory(id, request, cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy ="AdminPolicy")]
    public async Task<IActionResult> DeleteCategory(Guid id, CancellationToken cancellationToken)
    {
        var result = await _categoryService.DeleteCategory(id, cancellationToken);

        return Ok(result);
    }
}