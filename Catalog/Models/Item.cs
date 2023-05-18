namespace Catalog.Models;

public class Item
{
    public int ItemId { get; set; }
    public string? Category { get; set; }
    public string? UserId { get; set; }
    public string? ItemDesc { get; set; }
    public List<FileInfo>? ImageList { get; set; }

    // Constructor
    public Item(int itemId, string? category, string? userId, string itemDesc, List<FileInfo>? imgList)
    {
        // Exceptions
        if (itemId <= 0) throw new ArgumentException("ItemId cannot be less or equal to 0");
        if (category == null) throw new ArgumentException("Category cannot be null");
        if (userId == null) throw new ArgumentException("UserId cannot be null");
        if (itemDesc == null) throw new ArgumentException("ItemDescription cannot be null");

        if (char.IsPunctuation(category[0]) || char.IsWhiteSpace(category[0]) || char.IsDigit(category[0]) || char.IsSymbol(category[0]))
        {
            throw new ArgumentException("Category cannot start with punctuation, spaces, digits or symbols");
        }

        // Creation
        this.ItemId = itemId;
        this.Category = category;
        this.UserId = userId;
        this.ItemDesc = itemDesc;
        this.ImageList = imgList;
    }


    public bool CreateItem(Item item, List<Item> list, ILogger logger)
    {
        try
        {
            if (list.Contains(item))
            {
                throw new Exception("Item already exists!");
            }
            else
            {
                list.Add(item);
                return true;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
            return false;
        }
    }
}