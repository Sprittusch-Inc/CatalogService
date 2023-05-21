using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Catalog.Models;
using MongoDB.Bson;
using MongoDB.Driver.GridFS;
using System.Text.Json;

namespace Catalog.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CatalogController : ControllerBase
{
    private readonly ILogger<CatalogController> _logger;
    private readonly IConfiguration _config;
    private readonly IMongoCollection<Item> _collection;

    public CatalogController(ILogger<CatalogController> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;

        // Henter connectionstring fra appsettings.json
        string connectionString = config.GetConnectionString("MongoDB")!;

        // Opret forbindelse til mongoDB
        var client = new MongoClient(connectionString);

        // Hent DB
        var database = client.GetDatabase("CatalogDB");
        _collection = database.GetCollection<Item>("Items");

    }

    // GET
    [HttpGet("items")]
    public async Task<List<Item>> GetItems()
    {
        try
        {
            List<Item> itemList = await _collection.Find(Builders<Item>.Filter.Empty).ToListAsync();

            if (itemList.Count == 0)
            {
                throw new Exception("No items were found");
            }
            else
            {
                return itemList;
            }
        }
        catch (Exception ex)
        {
            // Logging af eventuelle fejl
            _logger.LogError(ex.Message);
            throw;
        }
    }

    [HttpGet("items/{itemId}")]
    public async Task<Item> GetItemById(int itemId)
    {
        try
        {
            List<Item> itemList = await _collection.Find(Builders<Item>.Filter.Eq("ItemId", itemId)).ToListAsync();

            if (itemList.Count == 0)
            {
                throw new Exception($"No item with the the given id ({itemId}) were found");
            }

            Item item = itemList.FirstOrDefault()!;
            int imageId = 0; // Bruges til at give billeder et ID i filnavnet

            foreach (var imageData in item.ImageDataList!)
            {
                byte[] imgdata = imageData.Data!;
                string imageType = imageData.Type!;

                // Ekstrahér fil-typen fra strengen: imageType
                string fileExtension = imageType.Split('/')[1];

                // Generér unikt filnavn for filen, med den rigtige fil-type
                string fileName = $"image_{++imageId}.{fileExtension}";

                // Tjek om folder for billeder af en item med itemId findes
                // Hvis folderen ikke findes, bliver den oprettet.
                string itemPath = $"./items/item-{item.ItemId}/"; // KAN SÆTTES SOM SECRET!
                if (!Directory.Exists(itemPath)) Directory.CreateDirectory(itemPath);

                // Gem billede i en fil
                using (var fs = new FileStream(Path.Combine(itemPath, fileName), FileMode.Create))
                {
                    await fs.WriteAsync(imgdata, 0, imgdata.Length); // 0 er offset - Den læser fra starten af den binære streng
                }
            }

            return item;
        }
        catch (Exception ex)
        {
            // Logging af eventuelle fejl
            _logger.LogError(ex.Message);
            throw;
        }
    }

    // CREATE
    [HttpPost("items")]
    public async Task<IActionResult> PostItem([FromForm] Item model)
    {
        try
        {
            if (model.ItemExists(model.ItemId, _logger, _collection) == true)
            {
                throw new Exception($"An item with the same ItemId ({model.ItemId}) already exists.");
            }

            // Omdan billeder til binær data
            model.ImageDataList = await SaveImages(model.ImageList!);

            // Assign imageData til model
            model.ImageList = null; // Tømmer listen for IFormFiles

            await _collection.InsertOneAsync(model);

            return Ok($"A new item was appended and given ItemId: {model.ItemId}");
        }
        catch (Exception ex)
        {
            // Håndter eventuelle fejl
            return StatusCode(500, $"Fejl: {ex.Message}");
        }
    }


    // UPDATE
    [HttpPut("items/{itemId}")]
    public async Task<IActionResult> PutItem([FromForm] Item model, int itemId)
    {
        try
        {
            if (model.ItemExists(itemId, _logger, _collection) == false)
            {
                throw new Exception($"No item with the given ItemId ({itemId}) was found");
            }
            if (model.ItemDesc == null || model.Category == null)
            {
                throw new Exception("ItemDesc and Category are required.");
            }

            else
            {
                var filter = Builders<Item>.Filter.Eq("ItemId", itemId);
                var update = Builders<Item>.Update
                    .Set(x => x.Category, model.Category.ToLower())
                    .Set(x => x.ItemDesc, model.ItemDesc.ToLower())
                    .Set(x => x.ImageDataList, await SaveImages(model.ImageList!));

                _collection.UpdateOne(filter, update);
                return Ok($"An item with the ItemId of {model.ItemId} was updated.");
            }
        }
        catch (Exception ex)
        {
            // Håndter eventuelle fejl
            return StatusCode(500, $"Fejl: {ex.Message}");
        }
    }

    // Metode som omdanner liste af billeder til binær data
    public async Task<List<ImageData>> SaveImages(List<IFormFile> imageList)
    {
        try
        {
            List<ImageData> imageDataList = new List<ImageData>();

            foreach (var image in imageList)
            {
                byte[] imgData;

                using (var memoryStream = new MemoryStream())
                {
                    await image.CopyToAsync(memoryStream);
                    imgData = memoryStream.ToArray();
                }

                if (image.Length > 0)
                {
                    var imageData = new ImageData { Type = image.ContentType.ToString(), Data = imgData };
                    imageDataList.Add(imageData);
                }
                else
                {
                    throw new Exception("ImageData was <= 0");
                }
            }

            return imageDataList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            throw;
        }
    }
}



