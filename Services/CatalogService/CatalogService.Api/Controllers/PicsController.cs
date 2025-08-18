using CatalogService.Api.Infrastructure.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PicsController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly CatalogContext _catalogContext;

    public PicsController(IWebHostEnvironment env, CatalogContext catalogContext)
    {
        _env = env;
        _catalogContext = catalogContext;
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok("App and Running");
    }

    [HttpGet("catalog/items/{catalogItemId:int}/pic")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetImageAsync(int catalogItemId)
    {
        if (catalogItemId <= 0)
        {
            return BadRequest();
        }

        var item = await _catalogContext.CatalogItems
            .SingleOrDefaultAsync(ci => ci.Id == catalogItemId);

        if (item == null)
        {
            return NotFound();
        }

        var webRoot = _env.WebRootPath;
        var path = Path.Combine(webRoot, item.PictureFileName);

        if (!System.IO.File.Exists(path))
        {
            return NotFound();
        }

        string extension = Path.GetExtension(item.PictureFileName);
        string mimeType = GetImageMimeTypeFromImageFileExtension(extension);

        var buffer = await System.IO.File.ReadAllBytesAsync(path);
        return File(buffer, mimeType);
    }

    private string GetImageMimeTypeFromImageFileExtension(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            _ => "application/octet-stream"
        };
    }
}
