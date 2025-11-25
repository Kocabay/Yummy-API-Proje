using ApiProjeKampi.WebApi.Entities;
using FluentValidation;

namespace ApiProjeKampi.WebApi.ValidationRules
{
    public class ProductValidator : AbstractValidator<Product>
    {
        public ProductValidator()
        {
            RuleFor(x => x.ProductName).NotEmpty().WithMessage("Lütfen ürün adını boş geçmeyiniz.");
            RuleFor(x => x.ProductName).MinimumLength(2).WithMessage("En az 2 karakter veri girişi yapınız.");
            RuleFor(x => x.ProductName).MaximumLength(50).WithMessage("En fazla 50 karakter girişi yapınız.");

            RuleFor(x=>x.Price).NotEmpty().WithMessage("Lütfen ürün fiyatını boş geçmeyiniz.").GreaterThan(0).WithMessage("Ürün fiyatı negatif olamaz.").LessThan(1000).WithMessage("Ürün fiyatı bu kadar yüksek olamaz, girdiğiniz fiyatı kontrol ediniz.");
            
            RuleFor(x => x.ProductDescription).NotEmpty().WithMessage("Lütfen ürün açıklamasını boş geçmeyiniz.");

        }
    }
}
