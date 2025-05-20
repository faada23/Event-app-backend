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
    public async Task<ActionResult<List<GetCategoryResponse>>> GetAllCategories()
    {
        var result = await _categoryService.GetAllCategories();
        
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous] 
    public async Task<ActionResult<GetCategoryResponse>> GetCategoryById(Guid id)
    {
        var result = await _categoryService.GetCategoryById(id);

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy ="AdminPolicy")]
    public async Task<ActionResult<GetCategoryResponse>> CreateCategory([FromBody] CreateUpdateCategoryRequest request)
    {
        var result = await _categoryService.CreateCategory(request);

        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy ="AdminPolicy")]
    public async Task<ActionResult<GetCategoryResponse>> UpdateCategory(Guid id, [FromBody] CreateUpdateCategoryRequest request)
    {
        var result = await _categoryService.UpdateCategory(id, request);

        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy ="AdminPolicy")]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        var result = await _categoryService.DeleteCategory(id);

        return Ok(result);
    }
}