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
            // Finder alle items ved et tomt filter
            _logger.LogInformation("Fetching all items in database...");
            List<Item> itemList = await _collection.Find(Builders<Item>.Filter.Empty).ToListAsync();

            // Hvis ingen items findes, kastes en exception som informerer om dette.
            if (itemList.Count == 0)
            {
                throw new Exception("No items were found");
            }
            else
            {
                _logger.LogInformation($"Successfully fetched {itemList.Count} items from database.");
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
            // Hvis kategoriens længde et større end 2, bliver de første 2 bogstaver substringet og gjort til blokbogstaver
            if (categoryCode.Length > 2)
            {
                _logger.LogInformation("Processing category information...");
                categoryCode = categoryCode.Substring(0, 2).ToUpper();
            }

            // Opretter et filter med den substringede kategori og smider items hvor kategorien matcher ind i en liste
            _logger.LogInformation("Looking up items in category...");
            var filter = Builders<Item>.Filter.Eq("Category", categoryCode.ToUpper());
            List<Item> itemsInCat = await _collection.Find(filter).ToListAsync();

            // Hvis listen er tom, kastes en exception om at der ikke blev fundet items i kategorien
            if (itemsInCat.Count < 1)
            {
                throw new Exception($"No items were found in category; {categoryCode}");
            }

            _logger.LogInformation($"Successfully found {itemsInCat.Count} items in category {categoryCode}.");
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
            // Tjekker om der findes items med ItemId
            _logger.LogInformation("Looking up item by id...");
            List<Item> itemList = await _collection.Find(Builders<Item>.Filter.Eq("ItemId", itemId)).ToListAsync();

            if (itemList.Count == 0)
            {
                throw new Exception($"No item with the the given id ({itemId}) was found");
            }

            // Item hentes fra resultatet
            Item item = itemList.FirstOrDefault()!;
            _logger.LogInformation($"Item with ItemId {itemId} found.");

            // Hvis der er billeder tilknyttet til item, bliver de hentet ned i egen folder
            if (item.ImageDataList?.Count > 0)
            {
                _logger.LogInformation("Processing item images...");
                
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
                    _logger.LogInformation($"Attempting to write images into directory: {itemPath}");
                    using (var fs = new FileStream(Path.Combine(itemPath, fileName), FileMode.Create))
                    {
                        await fs.WriteAsync(imgdata, 0, imgdata.Length); // 0 er offset - Den læser fra starten af den binære streng
                    }
                }
            }

            _logger.LogInformation($"Successfully fetched and processed item with ItemId {itemId}");
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
            _logger.LogInformation("Checking if ItemId already exists...");
            if (model.ItemExists(model.ItemId, _logger, _collection) == true)
            {
                throw new Exception($"An item with the same ItemId ({model.ItemId}) already exists.");
            }

            _logger.LogInformation("Checking if item contains images...");
            if (model.ImageList?.Count > 0)
            {
                // Omdan billeder til binær data
                _logger.LogInformation("Images detected. Attempting to convert to binary data...");
                model.ImageDataList = await SaveImages(model.ImageList!);

                // Assign imageData til model
                _logger.LogInformation("Emptying list of images for processing...");
                model.ImageList = null; // Tømmer listen for IFormFiles
            }

            // Så længe highestId forekommer i databasen, bliver det plusset med 1
            _logger.LogInformation("Finding available ItemId...");
            int highestId = ((int)_collection.CountDocuments(Builders<Item>.Filter.Empty)) + 1;
            while (_collection.Find(Builders<Item>.Filter.Eq("ItemId", highestId)).Any() == true)
            {
                highestId++;
            }
            model.ItemId = highestId;
            _logger.LogInformation($"Assigned item the ItemId {model.ItemId}");

            model.Category = model.Category?.Substring(0, 2).ToUpper();
            _logger.LogInformation($"Assigned item the category {model.Category}");

            _logger.LogInformation("Saving item in database...");
            await _collection.InsertOneAsync(model);

            _logger.LogInformation($"Successfully saved item in database with ItemId {model.ItemId}");
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
            _logger.LogInformation("Checking if item exists in database...");
            if (model.ItemExists(itemId, _logger, _collection) == false)
            {
                throw new Exception($"No item with the given ItemId ({itemId}) was found");
            }

            _logger.LogInformation("Validating information...");
            if (model.ItemDesc == null || model.Category == null)
            {
                throw new Exception("ItemDesc and Category are required.");
            }
            else
            {
                _logger.LogInformation("ItemDesc and Category are valid.");
                var filter = Builders<Item>.Filter.Eq("ItemId", itemId);
                if (model.ImageList?.Count > 0)
                {
                    _logger.LogInformation("Updated item includes images.");
                    var update = Builders<Item>.Update
                        .Set(x => x.Category, model.Category.Substring(0, 2).ToUpper())
                        .Set(x => x.ItemDesc, model.ItemDesc.ToLower())
                        .Set(x => x.ImageDataList, await SaveImages(model.ImageList!));
                    _logger.LogInformation("Updating item in database...");
                    await _collection.UpdateOneAsync(filter, update);
                }
                else
                {
                    _logger.LogInformation("Updated item does not include images.");
                    var update = Builders<Item>.Update
                        .Set(x => x.Category, model.Category.ToLower())
                        .Set(x => x.ItemDesc, model.ItemDesc)
                        .Set(x => x.ImageDataList, null);
                    _logger.LogInformation("Updating item in database...");
                    await _collection.UpdateOneAsync(filter, update);
                }
                _logger.LogInformation($"Successfully updated item with ItemId {model.ItemId}.");
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
            // Tjekker om Item med ItemId findes i databasen
            _logger.LogInformation($"Checking if item with ItemId {itemId} exists...");
            var filter = Builders<Item>.Filter.Eq("ItemId", itemId);
            var itemlist = await _collection.Find(filter).ToListAsync();
            Item item = itemlist.FirstOrDefault()!;
            
            if (item == null)
            {
                throw new Exception($"Item was not found. ItemId: {itemId}");
            }

            // Hvis Item med ItemId findes, bliver den slettet.
            _logger.LogInformation($"Deleting item with ItemId {itemId}...");
            await _collection.DeleteOneAsync(filter);

            _logger.LogInformation($"Successfully deleted item with ItemId {itemId}.");
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
            // Opretter en liste af ImageData. Bruges til at gemme bearbejdede billeder.
            _logger.LogInformation("Attempting to process images...");
            List<ImageData> imageDataList = new List<ImageData>();

            foreach (var image in imageList)
            {
                // Hvert billede omdannes til bytes, som gemmes i et array af bytes.
                byte[] imgData;

                // Bytes bliver læst i hvert billede, hvor det gemmes i imgData array'et
                using (var memoryStream = new MemoryStream())
                {
                    _logger.LogInformation("Converting image to binary data...");
                    await image.CopyToAsync(memoryStream);
                    imgData = memoryStream.ToArray();
                }

                // Billedet gemmes i en ImageData-objekt og føjes til listen, sammen med typen, som bruges til at hente dem ned igen.
                if (image.Length > 0)
                {
                    _logger.LogInformation("Adding processed image to list...");
                    var imageData = new ImageData { Type = image.ContentType.ToString(), Data = imgData };
                    imageDataList.Add(imageData);
                }
                else
                {
                    throw new Exception("Could not process image.");
                }
            }

            _logger.LogInformation("Successfully processed images of item");
            return imageDataList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            throw;
        }
    }
}
