namespace Catalog.Models;

public class Item
{
    public int ItemId { get; set; }
    public string? CategoryId { get; set; }
    public string? UserId { get; set; }
    public string? ItemDesc { get; set; }
    public List<FileInfo>? ImageList { get; set; }

    // Constructor
    public Item(int itemId, string? categoryId, string? userId, string itemDesc, List<FileInfo>? imgList)
    {
        // Exceptions
        if (itemId <= 0) throw new ArgumentException("ItemId cannot be less or equal to 0");
        if (categoryId == null) throw new ArgumentException("CategoryId cannot be null");
        if (categoryId.Length - 1 > 2) throw new ArgumentException("CategoryId cannot exceed 2 characters");
        if (userId == null) throw new ArgumentException("UserId cannot be null");
        if (itemDesc == null) throw new ArgumentException("ItemDescription cannot be null");

        // Creation
        this.ItemId = itemId;
        this.CategoryId = categoryId;
        this.UserId = userId;
        this.ItemDesc = itemDesc;
        this.ImageList = imgList;
    }
}