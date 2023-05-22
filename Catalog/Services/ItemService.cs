using MongoDB.Driver;
using MongoDB.Bson;
using Catalog.Models;

namespace Catalog.Services;

public class ItemService
{
    private readonly ILogger _logger;
    private readonly IMongoCollection<Item> _collection;
    public ItemService(ILogger logger, IMongoCollection<Item> collection)
    {
        _logger = logger;
        _collection = collection;
    }

    public async Task<List<Item>> GetAllItemsAsync()
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

    public async Task<List<Item>> GetItemsInCategoryAsync(string categoryCode)
    {
        try
        {
            if (categoryCode.Length > 2)
            {
                categoryCode = categoryCode.Substring(0, 2).ToUpper();
            }

            var filter = Builders<Item>.Filter.Eq("Category", categoryCode.ToUpper());
            List<Item> itemsInCat = await _collection.Find(filter).ToListAsync();

            if (itemsInCat.Count < 1)
            {
                throw new Exception($"No items were found in category; {categoryCode}");
            }

            return itemsInCat;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            throw;
        }
    }


    public async Task<Item> GetItemByIdAsync(int itemId)
    {
        try
        {
            List<Item> itemList = await _collection.Find(Builders<Item>.Filter.Eq("ItemId", itemId)).ToListAsync();

            if (itemList.Count == 0)
            {
                throw new Exception($"No item with the the given id ({itemId}) was found");
            }

            Item item = itemList.FirstOrDefault()!;
            if (item.ImageDataList?.Count > 0)
            {
                // Håndtering af billeder fra binær data til læsbar fil
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


    public async Task<IResult> PostItemAsync(Item model)
    {
        try
        {
            if (model.ItemExists(model.ItemId, _logger, _collection) == true)
            {
                throw new Exception($"An item with the same ItemId ({model.ItemId}) already exists.");
            }

            if (model.ImageList?.Count > 0)
            {
                // Omdan billeder til binær data
                model.ImageDataList = await SaveImages(model.ImageList!);

                // Assign imageData til model
                model.ImageList = null; // Tømmer listen for IFormFiles
            }

            int highestId = ((int)_collection.CountDocuments(Builders<Item>.Filter.Empty)) + 1;
            while (_collection.Find(Builders<Item>.Filter.Eq("ItemId", highestId)).Any() == true)
            {
                highestId++;
            }
            model.ItemId = highestId;

            model.Category = model.Category?.Substring(0, 2).ToUpper();
            await _collection.InsertOneAsync(model);

            return Results.Ok($"A new item was appended and given ItemId: {model.ItemId}"); ;
        }
        catch (Exception ex)
        {
            // Håndter eventuelle fejl
            _logger.LogError(ex.Message);
            return Results.Problem($"ERROR: {ex.Message}", statusCode: 500);
        }
    }


    public async Task<IResult> UpdateItemAsync(Item model, int itemId)
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
                if (model.ImageList?.Count > 0)
                {
                    var update = Builders<Item>.Update
                        .Set(x => x.Category, model.Category.Substring(0, 2).ToUpper())
                        .Set(x => x.ItemDesc, model.ItemDesc.ToLower())
                        .Set(x => x.ImageDataList, await SaveImages(model.ImageList!));
                    await _collection.UpdateOneAsync(filter, update);
                }
                else
                {
                    var update = Builders<Item>.Update
                        .Set(x => x.Category, model.Category.ToLower())
                        .Set(x => x.ItemDesc, model.ItemDesc.ToLower())
                        .Set(x => x.ImageDataList, null);
                    await _collection.UpdateOneAsync(filter, update);
                }
                return Results.Ok($"An item with the ItemId of {model.ItemId} was updated.");
            }
        }
        catch (Exception ex)
        {
            // Håndter eventuelle fejl
            _logger.LogError(ex.Message);
            return Results.Problem($"ERROR: {ex.Message}", statusCode: 500);
        }
    }


    // DELETE
    public async Task<IResult> DeleteItemAsync(int itemId)
    {
        try
        {
            var filter = Builders<Item>.Filter.Eq("ItemId", itemId);
            var itemlist = await _collection.Find(filter).ToListAsync();
            Item item = itemlist.FirstOrDefault()!;
            if (item == null)
            {
                throw new Exception($"Item was not found. ItemId: {itemId}");
            }

            await _collection.DeleteOneAsync(filter);
            return Results.Ok($"An item with the ItemId of {itemId} was deleted.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return Results.Problem($"ERROR: {ex.Message}", statusCode: 500);
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
