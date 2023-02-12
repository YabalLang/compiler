using Yabal.Exceptions;
using Zio;

namespace Yabal;

public sealed class FileReader : IDisposable
{
    private readonly HttpClient _client;
    private readonly IFileSystem? _fileSystem;

    public FileReader(IFileSystem? fileSystem)
    {
        _fileSystem = fileSystem;
        _client = new HttpClient();
    }

    public async Task<(Uri Uri, string Content)> ReadAllTextAsync(SourceRange range, string path)
    {
        await using var result = await GetStreamAsync(range, path);
        using var reader = new StreamReader(result.Stream);
        return (result.Uri, await reader.ReadToEndAsync());
    }

    public async Task<(Uri Uri, byte[] Bytes)> ReadAllBytesAsync(SourceRange range, string path)
    {
        await using var result = await GetStreamAsync(range, path);
        using var memoryStream = new MemoryStream();
        await result.Stream.CopyToAsync(memoryStream);
        return (result.Uri, memoryStream.ToArray());
    }

    public async Task<StreamResult> GetStreamAsync(SourceRange range, string path)
    {
        var uri = GetUri(range, path);

        if (uri == null)
        {
            throw new InvalidOperationException();
        }

        return new StreamResult(uri, await GetFromUri(range, uri));
    }

    public Uri GetUri(SourceRange range, string path)
    {
        Uri? uri;

        if (path.StartsWith("."))
        {
            uri = new Uri(range.File, path);
        }
        else if (path.StartsWith("/"))
        {
            uri = new Uri(range.File, "." + path);
        }
        else if (!Uri.TryCreate(path, UriKind.Absolute, out uri))
        {
            uri = new Uri($"https://yabal.dev/x/{path}.yabal");
        }

        return uri;
    }

    private Task<Stream> GetFromUri(SourceRange range, Uri uri)
    {
        return uri.Scheme switch
        {
            "file" => GetFromFile(range, uri),
            "http" or "https" => GetFromHttp(range, uri),
            _ => throw new InvalidCodeException("Invalid import scheme '" + uri.Scheme + "'", range)
        };
    }

    private Task<Stream> GetFromFile(SourceRange range, Uri uri)
    {
        if (_fileSystem is null)
        {
            throw new InvalidCodeException("File imports are not supported", range);
        }

        try
        {
            return Task.FromResult(_fileSystem.OpenFile(uri.LocalPath, FileMode.Open, FileAccess.Read, FileShare.Read));
        }
        catch (Exception)
        {
            throw new InvalidCodeException("Could not find or read file '" + _fileSystem.ConvertPathToInternal(uri.LocalPath) + "'", range);
        }
    }

    private async Task<Stream> GetFromHttp(SourceRange range, Uri uri)
    {
        try
        {
            var response = await _client.GetAsync(uri);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStreamAsync();
        }
        catch (Exception)
        {
            throw new InvalidCodeException("Failed to import '" + uri + "'", range);
        }
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}

public record struct StreamResult(Uri Uri, Stream Stream) : IAsyncDisposable
{
    public async ValueTask DisposeAsync()
    {
        await Stream.DisposeAsync();
    }
}
