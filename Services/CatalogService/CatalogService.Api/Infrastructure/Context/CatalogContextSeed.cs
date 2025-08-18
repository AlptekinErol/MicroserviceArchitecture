using System.Data.SqlClient;
using System.Globalization;
using System.IO.Compression;
using CatalogService.Api.Core.Domain;
using Polly;

namespace CatalogService.Api.Infrastructure.Context;

public class CatalogContextSeed
{
    /// <summary>
    /// Executes the database seeding process with a retry policy. 
    /// Handles transient SQL exceptions by retrying the operation up to 3 times. 
    /// </summary>
    public async Task SeedAsync(CatalogContext context, IWebHostEnvironment env, ILogger<CatalogContextSeed> logger)
    {
        var policy = Policy.Handle<SqlException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retry => TimeSpan.FromSeconds(5),
                onRetry: (exception, timeSpan, retry, ctx) =>
                {
                    logger.LogWarning(exception, "[{prefix}] Exception {ExceptionType} with message {Message} detected on attempt {retry} of {retryCount}",
                        nameof(CatalogContextSeed), exception.GetType().Name, exception.Message, retry, 3);
                }
            );

        var setupDirPath = Path.Combine(env.ContentRootPath, "Infrastructure", "Setup", "SeedFiles");
        var picturePath = "Pics";

        await policy.ExecuteAsync(() => ProcessSeeding(context, setupDirPath, picturePath, logger));
    }

    /// <summary>
    /// Seeds the database with catalog brands, catalog types, and catalog items 
    /// if they do not already exist. Also ensures catalog item pictures are copied.
    /// </summary>
    private async Task ProcessSeeding(CatalogContext context, string setupDirPath, string picturePath, ILogger logger)
    {
        if (!context.CatalogBrands.Any())
        {
            await context.CatalogBrands.AddRangeAsync(GetCatalogBrandsFromFile(setupDirPath));
            await context.SaveChangesAsync();
        }

        if (!context.CatalogTypes.Any())
        {
            await context.CatalogTypes.AddRangeAsync(GetCatalogTypesFromFile(setupDirPath));
            await context.SaveChangesAsync();
        }

        if (!context.CatalogItems.Any())
        {
            await context.CatalogItems.AddRangeAsync(GetCatalogItemsFromFile(setupDirPath, context));
            await context.SaveChangesAsync();

            GetCatalogItemPictures(setupDirPath, picturePath);
        }
    }

    /// <summary>
    /// Reads catalog brands from a seed file. 
    /// If the file does not exist, returns preconfigured catalog brands.
    /// </summary>
    private IEnumerable<CatalogBrand> GetCatalogBrandsFromFile(string contentPath)
    {
        /// <summary>
        /// Provides a set of preconfigured catalog brands 
        /// (e.g., Azure, .NET, Visual Studio, SQL Server, Other).
        /// </summary>
        IEnumerable<CatalogBrand> GetPreconfiguredCatalogBrands()
        {
            return new List<CatalogBrand>()
        {
            new CatalogBrand() { Brand = "Azure" },
            new CatalogBrand() { Brand = ".NET" },
            new CatalogBrand() { Brand = "Visual Studio" },
            new CatalogBrand() { Brand = "SQL Server" },
            new CatalogBrand() { Brand = "Other" }
        };
        }

        string fileName = Path.Combine(contentPath, "BrandsTextFile.txt");

        if (!File.Exists(fileName))
        {
            return GetPreconfiguredCatalogBrands();
        }

        var fileContent = File.ReadAllLines(fileName);

        var list = fileContent
            .Select(i => new CatalogBrand()
            {
                Brand = i.Trim('"')
            })
            .Where(i => i != null);

        return list ?? GetPreconfiguredCatalogBrands();
    }

    /// <summary>
    /// Reads catalog types from a seed file. 
    /// If the file does not exist, returns preconfigured catalog types.
    /// </summary>
    private IEnumerable<CatalogType> GetCatalogTypesFromFile(string contentPath)
    {
        /// <summary>
        /// Provides a set of preconfigured catalog types 
        /// (e.g., Mug, T-Shirt, Sheet, USB Memory Stick).
        /// </summary>
        IEnumerable<CatalogType> GetPreconfiguredCatalogTypes()
        {
            return new List<CatalogType>()
        {
            new CatalogType() { Type = "Mug" },
            new CatalogType() { Type = "T-Shirt" },
            new CatalogType() { Type = "Sheet" },
            new CatalogType() { Type = "USB Memory Stick" }
        };
        }

        string fileName = Path.Combine(contentPath, "CatalogTypes.txt");

        if (!File.Exists(fileName))
        {
            return GetPreconfiguredCatalogTypes();
        }

        var fileContent = File.ReadAllLines(fileName);

        var list = fileContent
            .Select(i => new CatalogType()
            {
                Type = i.Trim('"')
            })
            .Where(i => i != null);

        return list ?? GetPreconfiguredCatalogTypes();
    }

    /// <summary>
    /// Reads catalog items from a seed file. 
    /// If the file does not exist, returns preconfigured catalog items.
    /// Uses provided context to map CatalogType and CatalogBrand names to their IDs.
    /// </summary>
    private IEnumerable<CatalogItem> GetCatalogItemsFromFile(string contentPath, CatalogContext context)
    {
        /// <summary>
        /// Provides a set of preconfigured catalog items to seed the database 
        /// when no external seed file is found.
        /// </summary>
        IEnumerable<CatalogItem> GetPreconfiguredItems()
        {
            return new List<CatalogItem>
            {
                new CatalogItem { CatalogTypeId = 1, CatalogBrandId = 1, Description = ".NET Bot Black Hoodie, and more", Name = ".NET Bot Black Hoodie", Price = 19.5M, PictureFileName = "1.png", AvailableStock = 100, OnReorder = false },
                new CatalogItem { CatalogTypeId = 2, CatalogBrandId = 1, Description = ".NET Black & White Mug", Name = ".NET Black & White Mug", Price = 8.50M, PictureFileName = "2.png", AvailableStock = 89, OnReorder = true },
                new CatalogItem { CatalogTypeId = 1, CatalogBrandId = 2, Description = "Prism White T-Shirt", Name = "Prism White T-Shirt", Price = 12M, PictureFileName = "3.png", AvailableStock = 56, OnReorder = false },
                new CatalogItem { CatalogTypeId = 1, CatalogBrandId = 1, Description = ".NET Foundation T-shirt", Name = ".NET Foundation T-shirt", Price = 12M, PictureFileName = "4.png", AvailableStock = 120, OnReorder = false },
                new CatalogItem { CatalogTypeId = 3, CatalogBrandId = 2, Description = "Roslyn Red Sheet", Name = "Roslyn Red Sheet", Price = 8.5M, PictureFileName = "5.png", AvailableStock = 55, OnReorder = false },
                new CatalogItem { CatalogTypeId = 1, CatalogBrandId = 1, Description = ".NET Blue Hoodie", Name = ".NET Blue Hoodie", Price = 12M, PictureFileName = "6.png", AvailableStock = 17, OnReorder = false },
                new CatalogItem { CatalogTypeId = 1, CatalogBrandId = 2, Description = "Roslyn Red T-Shirt", Name = "Roslyn Red T-Shirt", Price = 12M, PictureFileName = "7.png", AvailableStock = 8, OnReorder = false },
                new CatalogItem { CatalogTypeId = 1, CatalogBrandId = 2, Description = "Kudu Purple Hoodie", Name = "Kudu Purple Hoodie", Price = 8.5M, PictureFileName = "8.png", AvailableStock = 34, OnReorder = false },
                new CatalogItem { CatalogTypeId = 2, CatalogBrandId = 2, Description = "Cup<T> White Mug", Name = "Cup<T> White Mug", Price = 12M, PictureFileName = "9.png", AvailableStock = 76, OnReorder = false },
                new CatalogItem { CatalogTypeId = 3, CatalogBrandId = 1, Description = ".NET Foundation Sheet", Name = ".NET Foundation Sheet", Price = 12M, PictureFileName = "10.png", AvailableStock = 11, OnReorder = false },
                new CatalogItem { CatalogTypeId = 3, CatalogBrandId = 2, Description = "Cup<T> Sheet", Name = "Cup<T> Sheet", Price = 8.5M, PictureFileName = "11.png", AvailableStock = 3, OnReorder = false },
                new CatalogItem { CatalogTypeId = 1, CatalogBrandId = 2, Description = "Prism White TShirt", Name = "Prism White TShirt", Price = 12M, PictureFileName = "12.png", AvailableStock = 0, OnReorder = false },
                new CatalogItem { CatalogTypeId = 2, CatalogBrandId = 2, Description = "De los Palotes", Name = "pepito", Price = 12M, PictureFileName = "12.png", AvailableStock = 0, OnReorder = false }
            };
        }

        string fileName = Path.Combine(contentPath, "CatalogItems.txt");

        if (!File.Exists(fileName))
        {
            return GetPreconfiguredItems();
        }

        var catalogTypeIdLookup = context.CatalogTypes.ToDictionary(ct => ct.Type, ct => ct.Id);
        var catalogBrandIdLookup = context.CatalogBrands.ToDictionary(ct => ct.Brand, ct => ct.Id);

        var fileContent = File.ReadAllLines(fileName)
            .Skip(1) // skip header row
            .Select(i => i.Split(','))
            .Select(i => new CatalogItem
            {
                CatalogTypeId = catalogTypeIdLookup[i[0]],
                CatalogBrandId = catalogBrandIdLookup[i[1]],
                Description = i[2].Trim('"').Trim(),
                Name = i[3].Trim('"').Trim(),
                Price = Decimal.Parse(i[4].Trim('"').Trim(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture),
                PictureFileName = i[5].Trim('"').Trim(),
                AvailableStock = string.IsNullOrEmpty(i[6]) ? 0 : int.Parse(i[6]),
                OnReorder = Convert.ToBoolean(i[7])
            });

        return fileContent;
    }

    /// <summary>
    /// Extracts catalog item pictures from a zip archive into the specified picture directory.
    /// If the directory already contains files, they are deleted before extraction.
    /// </summary>
    /// <param name="contentPath">The root path where the catalog seed files are located.</param>
    /// <param name="picturePath">The destination folder where pictures will be extracted. Defaults to "pics" if null.</param>
    private void GetCatalogItemPictures(string contentPath, string picturePath)
    {
        picturePath ??= "pics";

        if (picturePath != null)
        {
            DirectoryInfo directory = new DirectoryInfo(picturePath); 

            foreach (FileInfo file in directory.GetFiles())
            {
                file.Delete();
            }

            string zipFileCatalogItemPicture = Path.Combine(contentPath, "CatalogItems.zip");
            ZipFile.ExtractToDirectory(zipFileCatalogItemPicture, picturePath);
        }
    }
}