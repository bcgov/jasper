using System;
using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;

namespace Scv.Api.Infrastructure.Validation;

/// <summary>
/// Validates that a route/query value is a well-formed MongoDB ObjectId.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class ObjectIdAttribute : ValidationAttribute
{
    public ObjectIdAttribute() : base("Invalid id.") { }

    public override bool IsValid(object value) =>
        value is string id && ObjectId.TryParse(id, out _);
}
