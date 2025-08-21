using Ensek.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Ensek.Data;

public class EnsekDbContext(DbContextOptions<EnsekDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<MeterReading> MeterReadings => Set<MeterReading>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(b =>
        {
            b.HasKey(a => a.Id);
            b.Property(a => a.Id).ValueGeneratedNever();
            b.Property(a => a.FirstName).HasMaxLength(100);
            b.Property(a => a.LastName).HasMaxLength(100);
            b.HasData(
                new Account { Id = 2344, FirstName = "Tommy", LastName = "Test" },
                new Account { Id = 2233, FirstName = "Barry", LastName = "Test" },
                new Account { Id = 8766, FirstName = "Sally", LastName = "Test" },
                new Account { Id = 2345, FirstName = "Jerry", LastName = "Test" },
                new Account { Id = 2346, FirstName = "Ollie", LastName = "Test" },
                new Account { Id = 2347, FirstName = "Tara", LastName = "Test" },
                new Account { Id = 2348, FirstName = "Tammy", LastName = "Test" },
                new Account { Id = 2349, FirstName = "Simon", LastName = "Test" },
                new Account { Id = 2350, FirstName = "Colin", LastName = "Test" },
                new Account { Id = 2351, FirstName = "Gladys", LastName = "Test" },
                new Account { Id = 2352, FirstName = "Greg", LastName = "Test" },
                new Account { Id = 2353, FirstName = "Tony", LastName = "Test" },
                new Account { Id = 2355, FirstName = "Arthur", LastName = "Test" },
                new Account { Id = 2356, FirstName = "Craig", LastName = "Test" },
                new Account { Id = 6776, FirstName = "Laura", LastName = "Test" },
                new Account { Id = 4534, FirstName = "JOSH", LastName = "TEST" },
                new Account { Id = 1234, FirstName = "Freya", LastName = "Test" },
                new Account { Id = 1239, FirstName = "Noddy", LastName = "Test" },
                new Account { Id = 1240, FirstName = "Archie", LastName = "Test" },
                new Account { Id = 1241, FirstName = "Lara", LastName = "Test" },
                new Account { Id = 1242, FirstName = "Tim", LastName = "Test" },
                new Account { Id = 1243, FirstName = "Graham", LastName = "Test" },
                new Account { Id = 1244, FirstName = "Tony", LastName = "Test" },
                new Account { Id = 1245, FirstName = "Neville", LastName = "Test" },
                new Account { Id = 1246, FirstName = "Jo", LastName = "Test" },
                new Account { Id = 1247, FirstName = "Jim", LastName = "Test" },
                new Account { Id = 1248, FirstName = "Pam", LastName = "Test" }
            );
        });

        modelBuilder.Entity<MeterReading>(b =>
        {
            b.HasKey(m => m.Id);
            b.HasIndex(m => new { m.AccountId, m.ReadingDateTime }).IsUnique();
            b.Property(m => m.MeterReadValue).IsRequired();
            var utcConverter = new ValueConverter<DateTime, DateTime>(
                v => v.Kind == DateTimeKind.Utc
                    ? v
                    : (v.Kind == DateTimeKind.Local
                        ? v.ToUniversalTime()
                        : DateTime.SpecifyKind(v, DateTimeKind.Local).ToUniversalTime()),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
            b.Property(m => m.ReadingDateTime).IsRequired().HasConversion(utcConverter);
            b.HasOne(m => m.Account)
                .WithMany(a => a.MeterReadings)
                .HasForeignKey(m => m.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
