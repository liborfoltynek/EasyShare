using System.Net.Http.Headers;
using System.Text.Json;

namespace EasyShare.Android.Services;

public class FileUploadService
{
    private readonly HttpClient _httpClient;

    public FileUploadService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(30)
        };
    }

    public string UploadEndpoint
    {
        get => Preferences.Get("upload_endpoint", "");
        set => Preferences.Set("upload_endpoint", value);
    }

    public string ApiKey
    {
        get => Preferences.Get("api_key", "");
        set => Preferences.Set("api_key", value);
    }

    public int ShareCodeLength
    {
        get => Preferences.Get("share_code_length", 8);
        set => Preferences.Set("share_code_length", value);
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(UploadEndpoint)
        && !string.IsNullOrWhiteSpace(ApiKey);

    public async Task<UploadResult> UploadFileAsync(
        string filePath,
        string? expires = null,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var fileName = Path.GetFileName(filePath);

        using var fileStream = File.OpenRead(filePath);
        using var streamContent = new ProgressStreamContent(fileStream, progress);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        using var formData = new MultipartFormDataContent();
        formData.Add(streamContent, "file", fileName);

        if (!string.IsNullOrEmpty(expires))
        {
            formData.Add(new StringContent(expires), "expires");
        }

        var uploadUrl = UploadEndpoint;

        using var request = new HttpRequestMessage(HttpMethod.Post, uploadUrl);
        request.Headers.Add("X-Api-Key", ApiKey);
        request.Content = formData;

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorResult = TryParseResponse(responseBody);
            var errorMsg = errorResult?.Error ?? $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}";
            return new UploadResult { Success = false, Error = errorMsg };
        }

        var result = TryParseResponse(responseBody);
        if (result == null)
        {
            return new UploadResult { Success = false, Error = Strings.InvalidServerResponse };
        }

        return result;
    }

    public async Task<UploadResult> UploadStreamAsync(
        Stream stream,
        string fileName,
        string? expires = null,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        using var streamContent = new ProgressStreamContent(stream, progress);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        using var formData = new MultipartFormDataContent();
        formData.Add(streamContent, "file", fileName);

        if (!string.IsNullOrEmpty(expires))
        {
            formData.Add(new StringContent(expires), "expires");
        }

        var uploadUrl = UploadEndpoint;

        using var request = new HttpRequestMessage(HttpMethod.Post, uploadUrl);
        request.Headers.Add("X-Api-Key", ApiKey);
        request.Content = formData;

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorResult = TryParseResponse(responseBody);
            var errorMsg = errorResult?.Error ?? $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}";
            return new UploadResult { Success = false, Error = errorMsg };
        }

        var result = TryParseResponse(responseBody);
        if (result == null)
        {
            return new UploadResult { Success = false, Error = Strings.InvalidServerResponse };
        }

        return result;
    }

    private static UploadResult? TryParseResponse(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            return new UploadResult
            {
                Success = root.TryGetProperty("success", out var s) && s.GetBoolean(),
                Url = root.TryGetProperty("url", out var u) ? u.GetString() : null,
                Key = root.TryGetProperty("key", out var k) ? k.GetString() : null,
                Expires = root.TryGetProperty("expires", out var e) && e.ValueKind != JsonValueKind.Null ? e.GetString() : null,
                Error = root.TryGetProperty("error", out var er) ? er.GetString() : null,
            };
        }
        catch
        {
            return null;
        }
    }
}

public class UploadResult
{
    public bool Success { get; set; }
    public string? Url { get; set; }
    public string? Key { get; set; }
    public string? Expires { get; set; }
    public string? Error { get; set; }
}

internal class ProgressStreamContent : StreamContent
{
    public ProgressStreamContent(Stream stream, IProgress<double>? progress)
        : base(new ProgressStream(stream, progress))
    {
    }
}

internal class ProgressStream : Stream
{
    private readonly Stream _inner;
    private readonly IProgress<double>? _progress;
    private readonly long _totalLength;
    private long _bytesRead;

    public ProgressStream(Stream inner, IProgress<double>? progress)
    {
        _inner = inner;
        _progress = progress;
        _totalLength = inner.CanSeek ? inner.Length : -1;
    }

    public override bool CanRead => _inner.CanRead;
    public override bool CanSeek => _inner.CanSeek;
    public override bool CanWrite => _inner.CanWrite;
    public override long Length => _inner.Length;
    public override long Position
    {
        get => _inner.Position;
        set => _inner.Position = value;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var bytesRead = _inner.Read(buffer, offset, count);
        _bytesRead += bytesRead;
        ReportProgress();
        return bytesRead;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var bytesRead = await _inner.ReadAsync(buffer, offset, count, cancellationToken);
        _bytesRead += bytesRead;
        ReportProgress();
        return bytesRead;
    }

    private void ReportProgress()
    {
        if (_progress != null && _totalLength > 0)
        {
            _progress.Report((double)_bytesRead / _totalLength);
        }
    }

    public override void Flush() => _inner.Flush();
    public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
    public override void SetLength(long value) => _inner.SetLength(value);
    public override void Write(byte[] buffer, int offset, int count) => _inner.Write(buffer, offset, count);
}
