using System.ComponentModel.DataAnnotations;

namespace Ensek.Domain;

public class MeterReading
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [RegularExpression("^\\d{5}$")] // NNNNN
    public string MeterReadValue { get; set; } = string.Empty; 
    //I assume this is wrong intentionally, if meant to be 1 - 5 digits instead... ^\d{1,5}$

    public DateTime ReadingDateTime { get; set; }

    public int AccountId { get; set; }
    public Account? Account { get; set; }
}
