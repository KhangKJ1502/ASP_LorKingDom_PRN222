using System;
using System.Collections.Generic;

namespace DAL.Models;

public partial class VoucherType
{
    public int VoucherTypeId { get; set; }

    public string VoucherTypeName { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Voucher> Vouchers { get; set; } = new List<Voucher>();
}
