using System.Globalization;
using System.ComponentModel.DataAnnotations;
using CsvHelper;
using CsvHelper.Configuration;
using Ensek.Data;
using Ensek.Domain;
using Microsoft.EntityFrameworkCore;

namespace Ensek.Services;

public partial class MeterReadingService(EnsekDbContext db, ILogger<MeterReadingService>? logger = null) : IMeterReadingService
{
    private readonly EnsekDbContext _db = db;
    private readonly ILogger<MeterReadingService>? _logger = logger;

    public Task<MeterReadingUploadResult> UploadAsync(IFormFile csvFile, CancellationToken cancellationToken = default)
        => UploadAsync(csvFile, includeDetails: false, cancellationToken);

    public async Task<MeterReadingUploadResult> UploadAsync(IFormFile csvFile, bool includeDetails, CancellationToken cancellationToken = default)
    {
        if (csvFile is null || csvFile.Length == 0)
            return new MeterReadingUploadResult(0, 0);

        int success = 0, fail = 0;
        List<RowErrorDetail>? details = includeDetails ? new() : null;

        using var stream = csvFile.OpenReadStream();
        using var reader = new StreamReader(stream);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null
        };

        using var csv = new CsvReader(reader, config);
        var dtOpts = csv.Context.TypeConverterOptionsCache.GetOptions<DateTime>();
            dtOpts.Formats = ["dd/MM/yyyy HH:mm", "dd/MM/yyyy H:mm", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-ddTHH:mm:ss", "M/d/yyyy H:mm", "M/d/yyyy HH:mm"];
            dtOpts.CultureInfo = CultureInfo.InvariantCulture;
        csv.Context.RegisterClassMap<MeterReadingRowMap>();

        var rows = csv.GetRecordsAsync<MeterReadingRow>(cancellationToken);
        var line = 1; 
        await foreach (var row in rows.WithCancellation(cancellationToken))
        {
            line++;
            if (!IsValidRow(row)) { fail++; _logger?.LogDebug("Row invalid: {@Row}", row); details?.Add(new RowErrorDetail(line, "Invalid row: missing or invalid AccountId")); continue; }

            var accountExists = await _db.Accounts.AnyAsync(a => a.Id == row.AccountId, cancellationToken);
            if (!accountExists) { fail++; _logger?.LogDebug("Unknown account {AccountId}", row.AccountId); details?.Add(new RowErrorDetail(line, $"Unknown account: {row.AccountId}")); continue; }

            // Normalize to UTC BEFORE any DB usage
            DateTime dt = row.MeterReadingDateTime;
            if (dt.Kind == DateTimeKind.Unspecified)
                dt = DateTime.SpecifyKind(dt, DateTimeKind.Local).ToUniversalTime();
            else if (dt.Kind == DateTimeKind.Local)
                dt = dt.ToUniversalTime();

            var duplicate = await _db.MeterReadings.AnyAsync(m => m.AccountId == row.AccountId && m.ReadingDateTime == dt, cancellationToken);
            if (duplicate) { fail++; _logger?.LogDebug("Duplicate reading for account {AccountId} at {At}", row.AccountId, dt); details?.Add(new RowErrorDetail(line, "Duplicate reading for account at timestamp")); continue; }
            // trimming whitespace
            var value = row.MeterReadValue?.Trim();
            var entity = new MeterReading
            {
                AccountId = row.AccountId,
                MeterReadValue = value ?? string.Empty,
                ReadingDateTime = dt
            };

            if (!ValidateEntity(entity)) { fail++; _logger?.LogDebug("Validation failed for entity {AccountId} {At} {Value}", entity.AccountId, entity.ReadingDateTime, entity.MeterReadValue); details?.Add(new RowErrorDetail(line, "MeterReadValue must be 5 digits (NNNNN)")); continue; }

            _db.MeterReadings.Add(entity);
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
                success++;
                _logger?.LogInformation("Saved reading for {AccountId} at {At}", entity.AccountId, entity.ReadingDateTime);
            }
            catch (DbUpdateException)
            {
                _db.ChangeTracker.Clear();
                fail++;
                _logger?.LogWarning("DB update failed for {AccountId} at {At}", entity.AccountId, entity.ReadingDateTime);
                details?.Add(new RowErrorDetail(line, "Database error while saving"));
            }
        }

        return new MeterReadingUploadResult(success, fail) { Details = details };
    }

    private static bool IsValidRow(MeterReadingRow row)
    {
        if (row == null) return false;
        if (row.AccountId <= 0) return false;
        return true;
    }

    private static bool ValidateEntity(MeterReading entity)
    {
        var ctx = new ValidationContext(entity);
        var results = new List<ValidationResult>();
        return Validator.TryValidateObject(entity, ctx, results, validateAllProperties: true);
    }
}

public sealed class MeterReadingRow
{
    public int AccountId { get; set; }
    public DateTime MeterReadingDateTime { get; set; }
    public string? MeterReadValue { get; set; }
}

public sealed class MeterReadingRowMap : ClassMap<MeterReadingRow>
{
    public MeterReadingRowMap()
    {
        Map(m => m.AccountId).Name("AccountId", "Account Id", "Account_ID");
        Map(m => m.MeterReadingDateTime).Name("MeterReadingDateTime", "ReadingDateTime");
        Map(m => m.MeterReadValue).Name("MeterReadValue", "ReadValue");
    }
}
