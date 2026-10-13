using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Caching.Memory;
using Server.Endpoints;
using Server.Services;
using Shared.Models;

namespace Server.Tests;

public class RecipeTests
{
    private static HttpClient ClientFor(Func<HttpRequestMessage, HttpResponseMessage> respond)
        => new(new FuncHandler(respond));

    private static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK)
        => new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    [Fact]
    public async Task SearchByIngredients_Intersects_Meals()
    {
        var service = new ThemealDbRecipeService(
            ClientFor(req => Json(req.RequestUri!.Query.Contains("tomato")
                ? """{"meals":[{"idMeal":"1","strMeal":"Pasta","strMealThumb":"t","strArea":"Italian"},{"idMeal":"2","strMeal":"Salad","strMealThumb":"t","strArea":"Greek"}]}"""
                : """{"meals":[{"idMeal":"1","strMeal":"Pasta","strMealThumb":"t","strArea":"Italian"},{"idMeal":"3","strMeal":"Pizza","strMealThumb":"t","strArea":"Italian"}]}""")),
            new MemoryCache(new MemoryCacheOptions()));

        var meals = await service.SearchByIngredientsAsync(["tomato", "cheese"], CancellationToken.None);

        var meal = Assert.Single(meals);
        Assert.Equal("1", meal.Id);
        Assert.Equal("Pasta", meal.Name);
    }

    [Fact]
    public async Task SearchByIngredients_Unknown_Returns_Empty()
    {
        var service = new ThemealDbRecipeService(
            ClientFor(_ => Json("""{"meals":null}""")),
            new MemoryCache(new MemoryCacheOptions()));

        var meals = await service.SearchByIngredientsAsync(["unicornium"], CancellationToken.None);

        Assert.Empty(meals);
    }

    [Fact]
    public async Task GetMeal_Maps_Ingredients_And_Measures()
    {
        var service = new ThemealDbRecipeService(
            ClientFor(_ => Json("""
                {"meals":[{
                  "idMeal":"52772","strMeal":"Teriyaki Chicken Casserole",
                  "strArea":"Japanese","strCategory":"Chicken",
                  "strInstructions":"Do things.","strMealThumb":"thumb",
                  "strIngredient1":"soy sauce","strMeasure1":"3/4 cup",
                  "strIngredient2":"chicken","strMeasure2":"2",
                  "strIngredient3":"","strMeasure3":"",
                  "strIngredient4":null,"strMeasure4":null
                }]}
                """)),
            new MemoryCache(new MemoryCacheOptions()));

        var meal = await service.GetMealAsync("52772", CancellationToken.None);

        Assert.NotNull(meal);
        Assert.Equal("Teriyaki Chicken Casserole", meal.Name);
        Assert.Equal(2, meal.Ingredients.Count);
        Assert.Equal("soy sauce", meal.Ingredients[0].Name);
        Assert.Equal("3/4 cup", meal.Ingredients[0].Measure);
    }

    [Fact]
    public async Task GetIngredientNames_Caches_List()
    {
        var calls = 0;
        var service = new ThemealDbRecipeService(
            ClientFor(_ =>
            {
                calls++;
                return Json("""{"meals":[{"strIngredient":"Tomatoes"},{"strIngredient":"Chicken"}]}""");
            }),
            new MemoryCache(new MemoryCacheOptions()));

        var first = await service.GetIngredientNamesAsync(CancellationToken.None);
        var second = await service.GetIngredientNamesAsync(CancellationToken.None);

        Assert.Equal(1, calls);
        Assert.Equal(["Chicken", "Tomatoes"], second);
        Assert.Equal(first, second);
    }

    [Fact]
    public async Task Translate_Returns_Translated_Text()
    {
        var service = new MyMemoryTranslationService(
            ClientFor(_ => Json("""{"responseData":{"translatedText":"milk"}}""")),
            new MemoryCache(new MemoryCacheOptions()));

        Assert.Equal("milk", await service.TranslateAsync("latte", "it", "en", CancellationToken.None));
    }

    [Fact]
    public async Task Translate_Failure_Returns_Input()
    {
        var service = new MyMemoryTranslationService(
            ClientFor(_ => Json("{}", HttpStatusCode.InternalServerError)),
            new MemoryCache(new MemoryCacheOptions()));

        Assert.Equal("latte", await service.TranslateAsync("latte", "it", "en", CancellationToken.None));
    }

    [Fact]
    public async Task SearchEndpoint_Translates_Tags_And_Names()
    {
        var recipes = new FakeRecipes(meals: [new ThemealDbMeal("1", "Pasta", null, "Italian")]);
        var translations = new FakeTranslations(new Dictionary<string, string>
        {
            ["it|en|pomodoro"] = "tomato",
            ["en|it|Pasta"] = "Pasta al sugo"
        });
        var endpoints = new RecipeEndpoints(recipes, translations);

        var response = await endpoints.SearchAsync(
            new SearchRecipesRequest("pomodoro", "it"), CancellationToken.None);

        Assert.Equal(["tomato"], recipes.SeenIngredients);
        Assert.NotNull(response.Value);
        var meal = Assert.Single(response.Value.Meals);
        Assert.Equal("Pasta", meal.Name);
        Assert.Equal("Pasta al sugo", meal.TranslatedName);
    }

    [Fact]
    public async Task SearchEndpoint_English_Skips_Translation()
    {
        var recipes = new FakeRecipes(meals: [new ThemealDbMeal("1", "Pasta", null, "Italian")]);
        var translations = new ThrowingTranslations();
        var endpoints = new RecipeEndpoints(recipes, translations);

        var response = await endpoints.SearchAsync(
            new SearchRecipesRequest("tomato", "en"), CancellationToken.None);

        Assert.NotNull(response.Value);
        var meal = Assert.Single(response.Value.Meals);
        Assert.Null(meal.TranslatedName);
    }

    [Fact]
    public async Task SearchEndpoint_Empty_Returns_Empty_Without_Calling_Services()
    {
        var endpoints = new RecipeEndpoints(
            new ThrowingRecipes(), new ThrowingTranslations());

        var response = await endpoints.SearchAsync(
            new SearchRecipesRequest(null, "it"), CancellationToken.None);

        Assert.NotNull(response.Value);
        Assert.Empty(response.Value.Meals);
    }

    private sealed class FuncHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(respond(request));
    }

    private sealed class FakeRecipes(IReadOnlyList<ThemealDbMeal> meals) : IRecipeService
    {
        public List<string>? SeenIngredients { get; private set; }

        public Task<IReadOnlyList<ThemealDbMeal>> SearchByIngredientsAsync(
            IReadOnlyList<string> ingredients, CancellationToken ct)
        {
            SeenIngredients = ingredients.ToList();
            return Task.FromResult(meals);
        }

        public Task<ThemealDbMealDetail?> GetMealAsync(string id, CancellationToken ct)
            => Task.FromResult<ThemealDbMealDetail?>(null);

        public Task<IReadOnlyList<string>> GetIngredientNamesAsync(CancellationToken ct)
            => Task.FromResult<IReadOnlyList<string>>([]);
    }

    private sealed class FakeTranslations(Dictionary<string, string> map) : ITranslationService
    {
        public Task<string> TranslateAsync(string text, string from, string to, CancellationToken ct)
            => Task.FromResult(map.TryGetValue($"{from}|{to}|{text}", out var translated)
                ? translated
                : text);
    }

    private sealed class ThrowingRecipes : IRecipeService
    {
        public Task<IReadOnlyList<ThemealDbMeal>> SearchByIngredientsAsync(
            IReadOnlyList<string> ingredients, CancellationToken ct)
            => throw new InvalidOperationException("must not be called");

        public Task<ThemealDbMealDetail?> GetMealAsync(string id, CancellationToken ct)
            => throw new InvalidOperationException("must not be called");

        public Task<IReadOnlyList<string>> GetIngredientNamesAsync(CancellationToken ct)
            => throw new InvalidOperationException("must not be called");
    }

    private sealed class ThrowingTranslations : ITranslationService
    {
        public Task<string> TranslateAsync(string text, string from, string to, CancellationToken ct)
            => throw new InvalidOperationException("must not be called");
    }
}
