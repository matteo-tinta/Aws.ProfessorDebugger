namespace Momo.Commands.Rest;

public class RestMomoCommand(
    string Endpoint,
    HttpMethod Method,
    string? JsonBody) : IMomoCommand
{
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var handler = new HttpClientHandler()
        {
            AllowAutoRedirect = true,
        };
        
        var httpClient = new HttpClient(handler);
        
        httpClient.BaseAddress = new Uri(Endpoint);
        httpClient.Timeout = TimeSpan.FromSeconds(60);
        
        if (!String.IsNullOrWhiteSpace(JsonBody))
        {
            //TODO: add json body
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        var message = new HttpRequestMessage(Method, "/");
        
        var result = await httpClient.SendAsync(message, cancellationToken);
        
        result.EnsureSuccessStatusCode();
        
        //No returns?
    }

    public override string ToString() => $"REST -> {Method}:{Endpoint}";
}