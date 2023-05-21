using Microsoft.AspNetCore.Http;
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
        List<IFormFile> imgList = new();

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
        List<IFormFile> imgList = new();

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
        List<IFormFile> imgList = new();

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


     [Test]
    public void UpdateItem_ShouldUpdateItemProperties()
    {
        // Arrange
        int itemIdToUpdate = 1;
        string newCategory = "Electronics";
        string newUserId = "user456";
        string newItemDesc = "Updated item description";
        List<IFormFile> newImageList = new List<IFormFile>();

        Item item = list!.Find(i => i.ItemId == itemIdToUpdate)!;

        // Act
        item!.UpdateItem(itemIdToUpdate, newCategory, newUserId, newItemDesc, newImageList);

        // Assert
        Assert.AreEqual(newCategory, item.Category);
        Assert.AreEqual(newUserId, item.UserId);
        Assert.AreEqual(newItemDesc, item.ItemDesc);
        Assert.AreEqual(newImageList, item.ImageList);
        Assert.IsTrue(list!.Contains(item));
        Assert.IsNotNull(item.Category);
        Assert.IsNotNull(item.UserId);
        Assert.IsNotNull(item.ItemDesc);
        Assert.IsNotNull(item.ImageList);
    }

    [Test]
    public void UpdateItem_ShouldNotUpdateItemProperties_WhenItemIdDoesNotMatch()
    {
    // Arrange
    int itemIdToUpdate = 2; // ItemId that does not exist in the list
    string newCategory = "Electronics";
    string newUserId = "user456";
    string newItemDesc = "Updated item description";
    List<IFormFile> newImageList = new List<IFormFile>();

    Item item = list?.Find(i => i.ItemId == itemIdToUpdate)!;

    // Act
        if (item != null)
        {
            item.UpdateItem(itemIdToUpdate, newCategory, newUserId, newItemDesc, newImageList);
        }

    // Assert
    Assert.IsNull(item); // The item should remain null since the itemId does not match
    }

    [Test]
    public void DeleteItem_ShouldRemoveItemFromList()
    {
        // Arrange
        int itemIdToDelete = 1;

        Item item = list!.Find(i => i.ItemId == itemIdToDelete)!;

        // Act
        if (item != null)
        {
            list.Remove(item);
        }
        else
        {
            throw new ArgumentException("Item does not exist");
        }

        // Assert
        Assert.IsFalse(list.Contains(item!));
    }

    [Test]
    public void GetItem_ShouldReturnItemFromList()
    {
        // Arrange
        int itemIdToGet = 1;

        Item item = list!.Find(i => i.ItemId == itemIdToGet)!;

        // Act
        if (item != null)
        {
            list.Find(i => i.ItemId == itemIdToGet);
        }

        // Assert
        Assert.IsTrue(list.Contains(item!));
    }

}