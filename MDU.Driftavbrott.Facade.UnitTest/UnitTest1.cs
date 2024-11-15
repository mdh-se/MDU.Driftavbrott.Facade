namespace SE.MDU.Driftavbrott.Facade.UnitTest;

[TestClass]
public class UnitTest1
{
    [TestMethod]
    public void TestGetJokes()
    {
        // setup
        var jokeService = new JokeService();
        
        // act
        var joke = jokeService.GetJoke();

        // assert
        Assert.IsInstanceOfType<string?>(joke);
    }
}