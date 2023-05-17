namespace Catalog.Test;

[TestFixture]
public class Tests
{
    private List<string>? categories;

    [SetUp]
    public void Setup()
    {
        categories = new List<string> { "FU", "CH", "LA" };
    }


    [TestCase(-1, "FU", "at@gmail.com", "En stol")]
    [TestCase(1, null, "at@gmail.com", "En stol")]
    [TestCase(1, "FU", null, "En stol")]
    [TestCase(1, "FU", "at@gmail.com", null)]
    public void CreateItem_NullValues(int itemId, string CategoryId, string userId, string itemDesc)
    {
        // Klargøring
        List<FileInfo> imgList = new();

        // Asserts
        Assert.Throws<ArgumentException>(() =>
        {
            Item item = new Item(itemId, CategoryId, userId, itemDesc, imgList);
        });
    }

    
    [TestCase(1, "DoesNotExist", "at@gmail.com", "En stol")]
    public void CreateItem_InvalidCategory(int itemId, string categoryId, string userId, string itemDesc)
    {
        // Klargøring
        Setup();
        List<FileInfo> imgList = new();

        // Asserts
        Assert.Throws<ArgumentException>(() =>
        {
            Item item = new Item(itemId, categoryId, userId, itemDesc, imgList);
        });
        Assert.IsFalse(categoryId.Length-1 == 2);
    }


    [TestCase(1, "FU", "at@gmail.com", "En stol")]
    public void CreateItem_ValidSubmit(int itemId, string categoryId, string userId, string itemDesc)
    {
        // Klargøring
        List<FileInfo> imgList = new();

        // Oprettelse
        Item item = new Item(itemId, categoryId, userId, itemDesc, imgList);

        // Asserts
        Assert.NotNull(item);
        Assert.That(itemId, Is.EqualTo(item.ItemId));
        Assert.That(categoryId, Is.EqualTo(item.CategoryId));
        Assert.That(userId, Is.EqualTo(item.UserId));
        Assert.That(itemDesc, Is.EqualTo(item.ItemDesc));
        Assert.That(imgList, Is.EqualTo(item.ImageList));
        Assert.Contains(categoryId, categories);
    }
}