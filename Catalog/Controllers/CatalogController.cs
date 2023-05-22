using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MongoDB.Bson;
using Catalog.Models;
using Catalog.Services;

namespace Catalog.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CatalogController : ControllerBase
{
    private readonly ILogger<CatalogController> _logger;
    private readonly IConfiguration _config;
    private readonly IMongoCollection<Item> _collection;
    private readonly ItemService _service;

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

        _service = new ItemService(_logger, _collection);
    }

    // GET
    [HttpGet("items")]
    public async Task<List<Item>> GetItems()
    {
        return await _service.GetAllItemsAsync();
    }

    [HttpGet("category/{categoryCode}")]
    public async Task<List<Item>> GetItemsInCategory(string categoryCode)
    {
        return await _service.GetItemsInCategoryAsync(categoryCode);
    }

    [HttpGet("items/{itemId}")]
    public async Task<Item> GetItemById(int itemId)
    {
        return await _service.GetItemByIdAsync(itemId);
    }

    // CREATE
    [HttpPost("items")]
    public async Task<IResult> PostItem([FromForm] Item model)
    {
        return await _service.PostItemAsync(model);
    }


    // UPDATE
    [HttpPut("items/{itemId}")]
    public async Task<IResult> PutItem([FromForm] Item model, int itemId)
    {
        return await _service.UpdateItemAsync(model, itemId);
    }

    [HttpDelete("items/{itemId}")]
    public async Task<IResult> DeleteItemAsync(int itemId){
        return await _service.DeleteItemAsync(itemId);
    }
}



