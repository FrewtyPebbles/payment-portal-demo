using System.ComponentModel.DataAnnotations;

namespace InvoicingApp.Forms.Product;

public class ProductCreationModel
{
    [Required(ErrorMessage = "A product name is required.", AllowEmptyStrings = false)]
    [StringLength(50, ErrorMessage = "The product name can only be up to 50 characters long.")]
    public string Name { get; set; }

    [Required(ErrorMessage = "A product description is required.", AllowEmptyStrings = false)]
    [StringLength(1500, ErrorMessage = "The product description can only be up to 1500 characters long.")]
    public string Description { get; set; }

    [Required(ErrorMessage = "A product price is required.")]
    [Range(0f, float.MaxValue, ErrorMessage = "The product price must be greater than $0.00.")]
    public float Price { get; set; }
}