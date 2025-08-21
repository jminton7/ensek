using System.Text;
using System;
using System.IO;
using System.Linq;
using Ensek.Data;
using Ensek.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Xunit;
using System.Threading.Tasks;
using System.Threading;

namespace Ensek.Tests;

public class MeterReadingServiceTests
{
    private static EnsekDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<EnsekDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new EnsekDbContext(options);
    db.Database.EnsureCreated();
        return db;
    }

    private static FormFile MakeCsv(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", "meter.csv");
    }

    [Fact]
    public async Task UploadAsync_ValidRows_AreSaved()
    {
        await using var db = CreateDb();
        var service = new MeterReadingService(db);
        var csv = "AccountId,MeterReadingDateTime,MeterReadValue\n2344,2025-08-01T12:00:00,01234\n2233,2025-08-01T13:00:00,99999\n";
        var file = MakeCsv(csv);

        var result = await service.UploadAsync(file);

        result.SuccessCount.Should().Be(2);
        result.FailureCount.Should().Be(0);
        db.MeterReadings.Count().Should().Be(2);
    }

    [Fact]
    public async Task UploadAsync_InvalidFormat_Fails()
    {
        await using var db = CreateDb();
        var service = new MeterReadingService(db);
        var csv = "AccountId,MeterReadingDateTime,MeterReadValue\n2344,2025-08-01T12:00:00,1234A\n";
        var file = MakeCsv(csv);

    var result = await service.UploadAsync(file, includeDetails: true);
        result.SuccessCount.Should().Be(0);
        result.FailureCount.Should().Be(1);
    result.Details.Should().NotBeNull();
    result.Details!.Count.Should().Be(1);
    result.Details![0].Error.Should().Contain("5 digits");
    }

    [Fact]
    public async Task UploadAsync_Duplicate_Fails()
    {
        await using var db = CreateDb();
        var service = new MeterReadingService(db);
        var csv = "AccountId,MeterReadingDateTime,MeterReadValue\n2344,2025-08-01T12:00:00,01234\n2344,2025-08-01T12:00:00,01234\n";
        var file = MakeCsv(csv);

    var result = await service.UploadAsync(file);

        result.SuccessCount.Should().Be(1);
        result.FailureCount.Should().Be(1);
    }

    [Fact]
    public async Task UploadAsync_UnknownAccount_Fails()
    {
        await using var db = CreateDb();
        var service = new MeterReadingService(db);
        var csv = "AccountId,MeterReadingDateTime,MeterReadValue\n9999,2025-08-01T12:00:00,01234\n";
        var file = MakeCsv(csv);

    var result = await service.UploadAsync(file);
        result.SuccessCount.Should().Be(0);
        result.FailureCount.Should().Be(1);
    }

    [Fact]
    public async Task UploadAsync_NullFile_ReturnsZeroes()
    {
        await using var db = CreateDb();
        var service = new MeterReadingService(db);
    var result = await service.UploadAsync(null!);
        result.SuccessCount.Should().Be(0);
        result.FailureCount.Should().Be(0);
    }

    [Fact]
    public async Task UploadAsync_EmptyFile_ReturnsZeroes()
    {
        await using var db = CreateDb();
        var service = new MeterReadingService(db);
        var file = new FormFile(new MemoryStream(Array.Empty<byte>()), 0, 0, "file", "empty.csv");
    var result = await service.UploadAsync(file);
        result.SuccessCount.Should().Be(0);
        result.FailureCount.Should().Be(0);
    }

    private sealed class ThrowingDbContext(DbContextOptions<EnsekDbContext> options) : EnsekDbContext(options)
    {
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => throw new DbUpdateException("boom");
    }

    [Fact]
    public async Task UploadAsync_SaveChangesThrows_CountsFailure()
    {
    var options = new DbContextOptionsBuilder<EnsekDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new ThrowingDbContext(options);
        db.Database.EnsureCreated();

        var service = new MeterReadingService(db);
        var csv = "AccountId,MeterReadingDateTime,MeterReadValue\n2344,2025-08-01T12:00:00,01234\n";
        var file = MakeCsv(csv);

        var result = await service.UploadAsync(file);
        result.SuccessCount.Should().Be(0);
        result.FailureCount.Should().Be(1);
    }
}
