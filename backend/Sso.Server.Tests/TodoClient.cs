using System.Net.Http.Json;
using Sso.Server.Todos;

namespace Sso.Server.Tests;

public static class TodoClient
{
    public static async Task<TodoResponse> CreateTodoAsync(this HttpClient client, string title)
    {
        var created = await client.PostAsJsonAsync("/api/todos", new TodoRequest(title), TestContext.Current.CancellationToken);
        return (await created.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<TodoResponse>(TestContext.Current.CancellationToken))!;
    }
}
