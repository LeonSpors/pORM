using System.ComponentModel.DataAnnotations;

namespace pORM.Tests.Models;

public class TestEntityWithKey
{
    [Key]
    public int Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
}