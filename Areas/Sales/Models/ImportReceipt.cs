using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace httt.Models;

[Table("importreceipt")]
public class ImportReceipt
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("ipreceiptid")]
    public int Ipreceiptid { get; set; }

    [Column("supplierid")]
    public int? Supplierid { get; set; }

    [Column("createdby")]
    public int? Createdby { get; set; }

    [Column("createddate")]
    public DateTime? Createddate { get; set; }

    [Column("ipreceiptcode")]
    public string? Ipreceiptcode { get; set; }

    [Column("status")]
    public string? Status { get; set; }

    [ForeignKey("Supplierid")]
    public virtual Supplier? Supplier { get; set; }

    public virtual ICollection<ImportReceiptDetail> ImportReceiptDetails { get; set; } = new List<ImportReceiptDetail>();
}
