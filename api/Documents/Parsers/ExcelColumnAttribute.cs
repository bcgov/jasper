using System;

namespace Scv.Api.Documents.Parsers;

[AttributeUsage(AttributeTargets.Property)]
public sealed class ExcelColumnAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}
