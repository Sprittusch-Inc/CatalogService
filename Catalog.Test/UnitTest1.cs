namespace Catalog.Test;

[TestFixture]
public class Tests
{
    [SetUp]
    public void Setup()
    {
    }


    [TestCase(-1, "Furniture", "at@gmail.com", "En stol")]
    [TestCase(1, null, "at@gmail.com", "En stol")]
    [TestCase(1, "Furniture", null, "En stol")]
    [TestCase(1, "Furniture", "at@gmail.com", null)]
    public void ConstructItem_NullValues(int itemId, string category, string userId, string itemDesc)
    {
        // Klargøring
        List<FileInfo> imgList = new();

        // Asserts
        Assert.Throws<ArgumentException>(() =>
        {
            Item item = new Item(itemId, category, userId, itemDesc, imgList);
        });
    }


    [TestCase(1, ".DoesNotExist", "at@gmail.com", "En stol")]
    [TestCase(1, "1DoesNotExist", "at@gmail.com", "En stol")]
    [TestCase(1, "£DoesNotExist", "at@gmail.com", "En stol")]
    [TestCase(1, " DoesNotExist", "at@gmail.com", "En stol")]
    public void ConstructItem_InvalidCategory(int itemId, string category, string userId, string itemDesc)
    {
        // Klargøring
        Setup();
        List<FileInfo> imgList = new();

        // Asserts
        Assert.Throws<ArgumentException>(() =>
        {
            Item item = new Item(itemId, category, userId, itemDesc, imgList);
        });
        Assert.IsTrue(char.IsPunctuation(category[0]) || char.IsDigit(category[0]) || char.IsSymbol(category[0]) || char.IsWhiteSpace(category[0]));
    }


    [TestCase(1, "Furniture", "at@gmail.com", "En stol")]
    public void ConstructItem_ValidSubmit(int itemId, string category, string userId, string itemDesc)
    {
        // Klargøring
        List<FileInfo> imgList = new();

        // Oprettelse
        Item item = new Item(itemId, category, userId, itemDesc, imgList);

        // Asserts
        Assert.NotNull(item);
        Assert.That(itemId, Is.EqualTo(item.ItemId));
        Assert.That(category, Is.EqualTo(item.Category));
        Assert.That(userId, Is.EqualTo(item.UserId));
        Assert.That(itemDesc, Is.EqualTo(item.ItemDesc));
        Assert.That(imgList, Is.EqualTo(item.ImageList));
        Assert.IsFalse(char.IsPunctuation(category[0]) || char.IsDigit(category[0]) || char.IsSymbol(category[0]) || char.IsWhiteSpace(category[0]));
    }
}