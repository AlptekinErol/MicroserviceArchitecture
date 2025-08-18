using CatalogService.Api.Bootstrappers;
using CatalogService.Api.Core.Application;
using CatalogService.Api.Core.Domain;
using CatalogService.Api.Infrastructure.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CatalogService.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CatalogsController : ControllerBase
    {
        private readonly CatalogContext catalogContext;
        private readonly CatalogSettingsBootstrapper settings;

        public CatalogsController(CatalogContext catalogContext, IOptionsSnapshot<CatalogSettingsBootstrapper> settings)
        {
            this.catalogContext = catalogContext ?? throw new ArgumentNullException(nameof(catalogContext));
            this.settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        }

        // GET api/catalogs/items?pageSize=10&pageIndex=0&ids=1,2,3
        [HttpGet("items")]
        [ProducesResponseType(typeof(PaginatedItemsViewModel<CatalogItem>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ItemsAsync([FromQuery] int pageSize = 10, [FromQuery] int pageIndex = 0, string? ids = null)
        {
            if (!string.IsNullOrEmpty(ids))
            {
                var items = await GetItemsByIdAsync(ids);

                if (!items.Any())
                {
                    return BadRequest("ids value invalid. Must be comma-separated list of numbers");
                }

                return Ok(items);
            }

            var totalItems = await catalogContext.CatalogItems.LongCountAsync();

            var itemsOnPage = await catalogContext.CatalogItems.OrderBy(c => c.Name).Skip(pageSize * pageIndex).Take(pageSize).ToListAsync();

            itemsOnPage = ChangeUriPlaceholder(itemsOnPage);

            var model = new PaginatedItemsViewModel<CatalogItem>(pageIndex, pageSize, totalItems, itemsOnPage);

            return Ok(model);
        }

        private async Task<List<CatalogItem>> GetItemsByIdAsync(string ids)
        {
            var numdIds = ids.Split(',').Select(id => (Ok: int.TryParse(id, out int x), Value: x));

            if (!numdIds.All(nid => nid.Ok))
            {
                return new List<CatalogItem>();
            }

            var idsToSelect = numdIds.Select(id => id.Value);

            var items = await catalogContext.CatalogItems.Where(ci => idsToSelect.Contains(ci.Id)).ToListAsync();

            return ChangeUriPlaceholder(items);
        }

        private List<CatalogItem> ChangeUriPlaceholder(List<CatalogItem> items)
        {
            var baseUri = settings.PicBaseUrl?.TrimEnd('/') + "/";

            items.ForEach(item =>
            {
                if (!string.IsNullOrWhiteSpace(item?.PictureFileName))
                {
                    item.PictureUrl = baseUri + item.PictureFileName;
                }
            });

            return items;
        }

        // GET api/catalogs/items/{id}
        [HttpGet("items/{id:int}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(CatalogItem), StatusCodes.Status200OK)]
        public async Task<ActionResult<CatalogItem>> ItemByIdAsync(int id)
        {
            if (id <= 0)
            {
                return BadRequest();
            }

            var item = await catalogContext.CatalogItems.SingleOrDefaultAsync(ci => ci.Id == id);

            if (item is null)
            {
                return NotFound();
            }

            var baseUri = settings.PicBaseUrl?.TrimEnd('/') + "/";

            if (!string.IsNullOrWhiteSpace(item.PictureFileName))
            {
                item.PictureUrl = baseUri + item.PictureFileName;
            }

            return item;
        }

        // GET api/catalogs/items/withname/{name}
        [HttpGet("items/withname/{name:minlength(1)}")]
        [ProducesResponseType(typeof(PaginatedItemsViewModel<CatalogItem>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedItemsViewModel<CatalogItem>>> ItemsWithNameAsync(
            string name, [FromQuery] int pageSize = 10, [FromQuery] int pageIndex = 0)
        {
            var totalItems = await catalogContext.CatalogItems.Where(c => c.Name.StartsWith(name)).LongCountAsync();

            var itemsOnPage = await catalogContext.CatalogItems.Where(c => c.Name.StartsWith(name)).OrderBy(c => c.Name).Skip(pageSize * pageIndex).Take(pageSize).ToListAsync();

            itemsOnPage = ChangeUriPlaceholder(itemsOnPage);

            return new PaginatedItemsViewModel<CatalogItem>(pageIndex, pageSize, totalItems, itemsOnPage);
        }

        // GET api/catalogs/items/type/all/brand/{catalogBrandId?}
        [HttpGet("items/type/all/brand/{catalogBrandId:int?}")]
        [ProducesResponseType(typeof(PaginatedItemsViewModel<CatalogItem>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedItemsViewModel<CatalogItem>>> ItemsByBrandIdAsync(int? catalogBrandId, [FromQuery] int pageSize = 10, [FromQuery] int pageIndex = 0)
        {
            IQueryable<CatalogItem> root = catalogContext.CatalogItems;

            if (catalogBrandId.HasValue)
            {
                root = root.Where(ci => ci.CatalogBrandId == catalogBrandId);
            }

            var totalItems = await root.LongCountAsync();

            var itemsOnPage = await root.OrderBy(ci => ci.Name).Skip(pageSize * pageIndex).Take(pageSize).ToListAsync();

            itemsOnPage = ChangeUriPlaceholder(itemsOnPage);

            return new PaginatedItemsViewModel<CatalogItem>(pageIndex, pageSize, totalItems, itemsOnPage);
        }

        // GET api/catalogs/catalogtypes
        [HttpGet("catalogtypes")]
        [ProducesResponseType(typeof(List<CatalogType>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<CatalogType>>> CatalogTypesAsync()
        {
            return await catalogContext.CatalogTypes.ToListAsync();
        }

        // GET api/catalogs/catalogbrands
        [HttpGet("catalogbrands")]
        [ProducesResponseType(typeof(List<CatalogBrand>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<CatalogBrand>>> CatalogBrandsAsync()
        {
            return await catalogContext.CatalogBrands.ToListAsync();
        }

        // PUT api/catalogs/items
        [HttpPut("items")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<ActionResult> UpdateProductAsync([FromBody] CatalogItem productToUpdate)
        {
            var catalogItem = await catalogContext.CatalogItems.SingleOrDefaultAsync(i => i.Id == productToUpdate.Id);

            if (catalogItem == null)
            {
                return NotFound(new { Message = $"Item with id {productToUpdate.Id} not found." });
            }

            var oldPrice = catalogItem.Price;
            var raiseProductPriceChangedEvent = oldPrice != productToUpdate.Price;

            catalogItem = productToUpdate;
            catalogContext.CatalogItems.Update(catalogItem);

            await catalogContext.SaveChangesAsync();

            // TODO: Event publish edilecekse buraya eklenebilir

            return CreatedAtAction(nameof(ItemByIdAsync), new { id = productToUpdate.Id }, null);
        }

        // POST api/catalogs/items
        [HttpPost("items")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<ActionResult> CreateProductAsync([FromBody] CatalogItem product)
        {
            var item = new CatalogItem
            {
                CatalogBrandId = product.CatalogBrandId,
                CatalogTypeId = product.CatalogTypeId,
                Description = product.Description,
                Name = product.Name,
                PictureFileName = product.PictureFileName,
                Price = product.Price
            };

            catalogContext.CatalogItems.Add(item);
            await catalogContext.SaveChangesAsync();

            return CreatedAtAction(nameof(ItemByIdAsync), new { id = item.Id }, null);
        }

        // DELETE api/catalogs/{id}
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DeleteProductAsync(int id)
        {
            var product = await catalogContext.CatalogItems.SingleOrDefaultAsync(x => x.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            catalogContext.CatalogItems.Remove(product);
            await catalogContext.SaveChangesAsync();

            return NoContent();
        }
    }
}