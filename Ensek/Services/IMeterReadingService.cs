namespace Ensek.Services;

public interface IMeterReadingService
{
    Task<MeterReadingUploadResult> UploadAsync(IFormFile csvFile, CancellationToken cancellationToken = default);
    Task<MeterReadingUploadResult> UploadAsync(IFormFile csvFile, bool includeDetails, CancellationToken cancellationToken = default);
}

public record MeterReadingUploadResult(int SuccessCount, int FailureCount)
{
    public List<RowErrorDetail>? Details { get; init; }
}

public sealed record RowErrorDetail(int LineNumber, string Error);
