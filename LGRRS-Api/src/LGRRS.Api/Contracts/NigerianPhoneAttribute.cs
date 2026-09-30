using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace LGRRS.Api.Contracts;

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public sealed class NigerianPhoneAttribute : ValidationAttribute
{
    public NigerianPhoneAttribute() : base("Enter a complete 11-digit Nigerian mobile number starting with 07, 08 or 09, for example 08139662026.") { }
    public override bool IsValid(object? value) => value is string phone && Regex.IsMatch(phone, @"\A0[789][0-9]{9}\z");
}
