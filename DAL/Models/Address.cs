using System;
using System.Collections.Generic;

namespace DAL.Models;

public partial class Address
{
    public int AddressId { get; set; }

    public int AccountId { get; set; }

    public string AddressLine { get; set; } = null!;

    public string City { get; set; } = null!;

    public string? Ward { get; set; }

    public bool IsDefault { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;
}
