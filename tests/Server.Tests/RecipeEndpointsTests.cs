using System.Net;
using System.Net.Http.Json;
using System.Text;

using Microsoft.Extensions.DependencyInjection;

using Shared.Models;

namespace Server.Tests;

[Collection("api")]
public sealed class RecipeEndpointsTests(PantryApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    private static string UniqueName(string prefix)
        => $"{prefix}-{Guid.NewGuid():N}";

    private static DateOnly InDays(int days)
        => DateOnly.FromDateTime(DateTime.Today).AddDays(days);

    private async Task<string> CreateItemAsync(string name, DateOnly expires)
    {
        var request = new CreatePantryItemRequest(name, 1, "pcs", "pantry", null, expires);

        using var response = await _client.PostAsJsonAsync("/api/pantry", request);
        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<PantryItemDto>();
        Assert.NotNull(created);
        return created.Id;
    }

    private async Task DeleteItemAsync(string id)
    {
        using var response = await _client.DeleteAsync($"/api/pantry/{id}");
        response.EnsureSuccessStatusCode();
    }

    private static void ServeMeals(StubOffHandler stub)
    {
        stub.Responder = request =>
        {
            var path = request.RequestUri?.PathAndQuery ?? string.Empty;

            if (path.Contains("filter.php", StringComparison.Ordinal))
            {
                return Json("""
                    {"meals":[
                      {"idMeal":"1","strMeal":"Chicken Soup","strMealThumb":"https://www.themealdb.com/images/media/meals/abc.jpg"},
                      {"idMeal":"2","strMeal":"Garlic Bread","strMealThumb":"https://www.themealdb.com/images/media/meals/def.jpg"}
                    ]}
                    """);
            }

            if (path.Contains("lookup.php?i=1", StringComparison.Ordinal))
            {
                return Json("""
                    {"meals":[{
                      "idMeal":"1","strMeal":"Chicken Soup",
                      "strMealThumb":"https://www.themealdb.com/images/media/meals/abc.jpg",
                      "strArea":"British",
                      "strIngredient1":"chicken","strIngredient2":"water","strIngredient3":"",
                      "strIngredient4":null,"strMeasure1":"1","strMeasure2":"2","strMeasure3":"","strMeasure4":null
                    }]}
                    """);
            }

            if (path.Contains("lookup.php", StringComparison.Ordinal))
            {
                return Json("""
                    {"meals":[{
                      "idMeal":"2","strMeal":"Garlic Bread",
                      "strMealThumb":"https://www.themealdb.com/images/media/meals/def.jpg",
                      "strArea":"Italian",
                      "strIngredient1":"garlic","strIngredient2":"bread","strIngredient3":"",
                      "strMeasure1":"3","strMeasure2":"1","strMeasure3":""
                    }]}
                    """);
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        };
    }

    private static HttpResponseMessage Json(string body)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };

    private static async Task<GetRecipeSuggestionsResponse?> SuggestionsAsync(HttpClient client, string? lang = null)
    {
        var url = lang is null ? "/api/recipes/suggestions" : $"/api/recipes/suggestions?lang={lang}";

        using var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<GetRecipeSuggestionsResponse>();
    }

    [Fact]
    public async Task Suggestions_Ranks_By_Expiring_Coverage()
    {
        var stub = factory.Services.GetRequiredService<StubOffHandler>();
        ServeMeals(stub);

        var chickenId = await CreateItemAsync("chicken", InDays(2));
        var garlicId = await CreateItemAsync("garlic", InDays(2));

        try
        {
            var page = await SuggestionsAsync(_client);
            Assert.NotNull(page);
            Assert.NotEmpty(page.Items);

            var soup = Assert.Single(page.Items, m => m.IdMeal == "1");
            Assert.Equal("Chicken Soup", soup.Name);
            Assert.EndsWith("/small", soup.ThumbnailUrl);
            Assert.Equal("https://www.themealdb.com/meal/1", soup.SourceUrl);
            Assert.Contains("chicken", soup.MatchedIngredients);
        }
        finally
        {
            await DeleteItemAsync(chickenId);
            await DeleteItemAsync(garlicId);
        }
    }

    [Fact]
    public async Task Suggestions_Mix_Candidates_Across_Ingredients()
    {
        var stub = factory.Services.GetRequiredService<StubOffHandler>();
        stub.Responder = request =>
        {
            var path = request.RequestUri?.PathAndQuery ?? string.Empty;

            // First ingredient alone could fill the whole candidate budget.
            if (path.Contains("filter.php?i=chicken", StringComparison.Ordinal))
            {
                var meals = string.Join(",", Enumerable.Range(10, 8)
                    .Select(n => $$"""{"idMeal":"{{n}}","strMeal":"Chicken Dish {{n}}","strMealThumb":"https://www.themealdb.com/images/media/meals/x{{n}}.jpg"}"""));

                return Json($"{{\"meals\":[{meals}]}}");
            }

            if (path.Contains("filter.php", StringComparison.Ordinal))
            {
                return Json("""
                    {"meals":[
                      {"idMeal":"20","strMeal":"Garlic Mix","strMealThumb":"https://www.themealdb.com/images/media/meals/g.jpg"}
                    ]}
                    """);
            }

            if (path.Contains("lookup.php", StringComparison.Ordinal))
            {
                var id = path.Contains("i=20", StringComparison.Ordinal) ? "20" : "10";
                var ingredient = id == "20" ? "garlic" : "chicken";

                return Json($"{{\"meals\":[{{\"idMeal\":\"{id}\",\"strMeal\":\"Dish {id}\",\"strMealThumb\":\"https://www.themealdb.com/images/media/meals/y.jpg\",\"strArea\":null,\"strIngredient1\":\"{ingredient}\",\"strIngredient2\":\"\"}}]}}");
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        };

        var chickenId = await CreateItemAsync("chicken", InDays(2));
        var garlicId = await CreateItemAsync("garlic", InDays(2));

        try
        {
            var page = await SuggestionsAsync(_client);
            Assert.NotNull(page);
            Assert.NotEmpty(page.Items);

            // Garlic's only meal must survive despite chicken's oversupply.
            Assert.Contains(page.Items, m => m.IdMeal == "20");
            Assert.All(page.Items, m => Assert.NotEmpty(m.MatchedIngredients));
        }
        finally
        {
            await DeleteItemAsync(chickenId);
            await DeleteItemAsync(garlicId);
        }
    }

    [Fact]
    public async Task Suggestions_Are_Empty_When_Nothing_Matches()
    {
        var stub = factory.Services.GetRequiredService<StubOffHandler>();
        stub.Responder = _ => Json("""{"meals":null}""");

        var before = stub.RequestedPaths.Count;
        var itemId = await CreateItemAsync(UniqueName("nomatch"), InDays(2));

        try
        {
            var page = await SuggestionsAsync(_client);
            Assert.NotNull(page);
            Assert.Empty(page.Items);
            Assert.True(stub.RequestedPaths.Count > before);
        }
        finally
        {
            await DeleteItemAsync(itemId);
        }
    }

    [Fact]
    public async Task Suggestions_Tolerate_Upstream_Outages()
    {
        var stub = factory.Services.GetRequiredService<StubOffHandler>();
        stub.Responder = _ => new HttpResponseMessage(HttpStatusCode.InternalServerError);

        var itemId = await CreateItemAsync(UniqueName("outage"), InDays(2));

        try
        {
            var page = await SuggestionsAsync(_client);
            Assert.NotNull(page);
            Assert.Empty(page.Items);
        }
        finally
        {
            await DeleteItemAsync(itemId);
        }
    }

    [Fact]
    public async Task Suggestions_Are_Cached_Per_Ingredient_Set()
    {
        var stub = factory.Services.GetRequiredService<StubOffHandler>();
        ServeMeals(stub);

        var itemId = await CreateItemAsync(UniqueName("cached"), InDays(2));

        try
        {
            var first = await SuggestionsAsync(_client);
            var callsAfterFirst = stub.RequestedPaths.Count;

            var second = await SuggestionsAsync(_client);

            Assert.NotNull(first);
            Assert.NotNull(second);
            Assert.Equal(first.Items.Count, second.Items.Count);
            Assert.Equal(callsAfterFirst, stub.RequestedPaths.Count);
        }
        finally
        {
            await DeleteItemAsync(itemId);
        }
    }
}

[Collection("api")]
public sealed class TranslationPipelineTests(PantryApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    private static string UniqueName(string prefix)
        => $"{prefix}-{Guid.NewGuid():N}";

    private static DateOnly InDays(int days)
        => DateOnly.FromDateTime(DateTime.Today).AddDays(days);

    private static HttpResponseMessage Json(string body)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };

    private static void ServeItalianPipeline(StubOffHandler stub)
    {
        stub.Responder = request =>
        {
            var path = request.RequestUri?.PathAndQuery ?? string.Empty;

            if (path.Contains("get?q=", StringComparison.Ordinal))
            {
                return Json("""
                    {"responseData":{"translatedText":"muscovado sugar"},
                     "responseStatus":200,
                     "matches":[{"translation":"muscovado sugar"}]}
                    """);
            }

            if (path.Contains("filter.php", StringComparison.Ordinal))
            {
                return path.Contains("muscovado_sugar", StringComparison.Ordinal)
                    ? Json("""
                        {"meals":[
                          {"idMeal":"3","strMeal":"Muscovado Cake","strMealThumb":"https://www.themealdb.com/images/media/meals/ghi.jpg"}
                        ]}
                        """)
                    : Json("""{"meals":null}""");
            }

            if (path.Contains("lookup.php", StringComparison.Ordinal))
            {
                return Json("""
                    {"meals":[{
                      "idMeal":"3","strMeal":"Muscovado Cake",
                      "strMealThumb":"https://www.themealdb.com/images/media/meals/ghi.jpg",
                      "strArea":"British",
                      "strIngredient1":"muscovado sugar","strIngredient2":"flour","strIngredient3":""
                    }]}
                    """);
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        };
    }

    [Fact]
    public async Task Italian_Word_Is_Translated_Then_Matched()
    {
        var stub = factory.Services.GetRequiredService<StubOffHandler>();
        ServeItalianPipeline(stub);

        using var created = await _client.PostAsJsonAsync(
            "/api/pantry",
            new CreatePantryItemRequest("zucchero muscovado", 1, "pcs", "pantry", null, InDays(2)));
        created.EnsureSuccessStatusCode();
        var item = await created.Content.ReadFromJsonAsync<PantryItemDto>();
        Assert.NotNull(item);

        try
        {
            using var response = await _client.GetAsync("/api/recipes/suggestions?lang=it");
            response.EnsureSuccessStatusCode();

            var page = await response.Content.ReadFromJsonAsync<GetRecipeSuggestionsResponse>();
            Assert.NotNull(page);

            var cake = Assert.Single(page.Items);
            Assert.Equal("Muscovado Cake", cake.Name);
            Assert.Contains("zucchero muscovado", cake.MatchedIngredients);
        }
        finally
        {
            using var deleted = await _client.DeleteAsync($"/api/pantry/{item.Id}");
            deleted.EnsureSuccessStatusCode();
        }
    }

    [Fact]
    public async Task Translation_Quota_Refusal_Falls_Back_Gracefully()
    {
        var stub = factory.Services.GetRequiredService<StubOffHandler>();
        stub.Responder = request =>
        {
            var path = request.RequestUri?.PathAndQuery ?? string.Empty;

            if (path.Contains("get?q=", StringComparison.Ordinal))
            {
                return Json("""
                    {"responseData":{"translatedText":"MYMEMORY WARNING: YOU USED ALL AVAILABLE FREE TRANSLATIONS FOR TODAY"},
                     "responseStatus":429,"matches":[]}
                    """);
            }

            return Json("""{"meals":null}""");
        };

        using var created = await _client.PostAsJsonAsync(
            "/api/pantry",
            new CreatePantryItemRequest(UniqueName("quota"), 1, "pcs", "pantry", null, InDays(2)));
        created.EnsureSuccessStatusCode();
        var item = await created.Content.ReadFromJsonAsync<PantryItemDto>();
        Assert.NotNull(item);

        try
        {
            using var response = await _client.GetAsync("/api/recipes/suggestions?lang=it");
            response.EnsureSuccessStatusCode();

            var page = await response.Content.ReadFromJsonAsync<GetRecipeSuggestionsResponse>();
            Assert.NotNull(page);
            Assert.Empty(page.Items);
        }
        finally
        {
            using var deleted = await _client.DeleteAsync($"/api/pantry/{item.Id}");
            deleted.EnsureSuccessStatusCode();
        }
    }

    [Fact]
    public async Task Translations_Are_Cached()
    {
        var stub = factory.Services.GetRequiredService<StubOffHandler>();
        ServeItalianPipeline(stub);

        using var created = await _client.PostAsJsonAsync(
            "/api/pantry",
            new CreatePantryItemRequest(UniqueName("cached-tr"), 1, "pcs", "pantry", null, InDays(2)));
        created.EnsureSuccessStatusCode();
        var item = await created.Content.ReadFromJsonAsync<PantryItemDto>();
        Assert.NotNull(item);

        try
        {
            using var first = await _client.GetAsync("/api/recipes/suggestions?lang=it");
            first.EnsureSuccessStatusCode();

            var translationCalls = stub.RequestedPaths.Count(p => p.Contains("get?q=", StringComparison.Ordinal));

            using var second = await _client.GetAsync("/api/recipes/suggestions?lang=it");
            second.EnsureSuccessStatusCode();

            // Suggestion cache hit: no new upstream calls at all.
            Assert.Equal(translationCalls, stub.RequestedPaths.Count(p => p.Contains("get?q=", StringComparison.Ordinal)));
        }
        finally
        {
            using var deleted = await _client.DeleteAsync($"/api/pantry/{item.Id}");
            deleted.EnsureSuccessStatusCode();
        }
    }
}
