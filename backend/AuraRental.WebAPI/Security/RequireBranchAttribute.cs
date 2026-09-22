namespace AuraRental.WebAPI.Security;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireBranchAttribute : Attribute;
