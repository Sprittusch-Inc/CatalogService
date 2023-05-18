using Microsoft.Extensions.Logging;

namespace Catalog.Test;

[TestFixture]
public class Tests
{
    private List<Item>? list;
    private ILogger? logger;

    [SetUp]
    public void Setup()
    {
        logger = Mock.Of<ILogger>();
        list = new List<Item>();
        list.Add(new Item(1, "Hatte", "MH@gmail.com", "Sort tophat", null));
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

    [Test]
    public void CreateItem_NullValues()
    {
        // Klargøring
        try
        {
            Item item = new Item(1, null, "JG@Sprit.Net", "Rød stol med nakkestøtte", null);
            item.CreateItem(item, list!, logger!);
        }
        catch (ArgumentException ex)
        {
            Assert.AreEqual("Category cannot be null", ex.Message);
            return;
        }

        // Hvis forventet exception ikke kastes failer testen
        Assert.Fail("Expected exception was not thrown");
    }

    [Test]
    public void CreateItem_ItemAlreadyExists()
    {
        // Klargøring
        Item item = new Item(1, "Hatte", "MH@gmail.com", "Sort tophat", null);
        try
        {
            item.CreateItem(item, list!, logger!);
        }
        catch (Exception ex)
        {
            Assert.AreEqual("Item already exists", ex.Message);
            return;
        }

        // Asserts
        Assert.IsTrue(list?.Contains(item));
        Assert.AreEqual(list?[0].ItemId, item.ItemId);
        Assert.AreEqual(list?[0].Category, item.Category);
        Assert.AreEqual(list?[0].UserId, item.UserId);
        Assert.AreEqual(list?[0].ItemDesc, item.ItemDesc);
        Assert.AreEqual(list?[0].ImageList, item.ImageList);
    }

    [Test]
    public void CreateItem_ValidSubmit()
    {
        // Klargøring
        Item item = new Item(2, "Stole", "JG@Sprit.Net", "Rød stol med nakkestøtte", null);
        
        item.CreateItem(item, list!, logger!);

        // Asserts
        Assert.IsTrue(list?.Count == 2);
        Assert.AreEqual(list?[1], item);
        Assert.IsTrue(list?[1].ItemId == item.ItemId);
        Assert.IsTrue(list?[1].Category == item.Category);
        Assert.IsTrue(list?[1].UserId == item.UserId);
        Assert.IsTrue(list?[1].ItemDesc == item.ItemDesc);
        Assert.IsTrue(list?[1].ImageList?.Count == item.ImageList?.Count);
    }



}