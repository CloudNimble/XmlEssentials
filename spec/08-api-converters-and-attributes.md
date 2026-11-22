# Converters and mapping attributes

Part of the [XmlEssentials spec](README.md).

## Converters

```csharp
namespace CloudNimble.Text.Xml.Serialization;

public abstract class XmlConverter
{
    public abstract bool CanConvert(Type typeToConvert);
}

public abstract class XmlConverter<T> : XmlConverter
{
    public virtual bool HandleNull => false;

    public abstract T? Read(ref Utf8XmlReader reader, Type typeToConvert, XmlSerializerOptions options);
    public abstract void Write(Utf8XmlWriter writer, T value, XmlSerializerOptions options);

    public virtual T? ReadAsAttribute(ref Utf8XmlReader reader, Type typeToConvert, XmlSerializerOptions options);

    public virtual void WriteAsAttribute(Utf8XmlWriter writer, XmlName name, T value, XmlSerializerOptions options);

    public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(T);
}

public abstract class XmlConverterFactory : XmlConverter
{
    public abstract XmlConverter? CreateConverter(Type typeToConvert, XmlSerializerOptions options);
}
```

`HandleNull`: when **false** (default), the serializer does not call `Read` for `xsi:nil` / omitted-null; it returns `default`. When **true**, `Read` is invoked on the nil element (STJ analogue). The nil check lives in one place, `XmlTypeInfo<T>`, so a converter with `HandleNull` false never sees a nil element, and the `Nullable<T>` converter only ever handles a value.

`XmlConverter` cannot be derived from directly (its constructor is `private protected`); derive from `XmlConverter<T>` or `XmlConverterFactory`, as with `JsonConverter`. A factory's `CreateConverter` must return an `XmlConverter<T>` for exactly the requested type. `GetConverter` throws `NotSupportedException` when nothing handles the type and `InvalidOperationException` when a converter is misconfigured ([serializer](07-api-serializer.md#behavior-every-overload-shares)).

`Nullable<T>` wraps whatever converter the options have for `T`, including one from `options.Converters` or `[XmlConverter]`. The built-in nullable primitives are constructed without reflection; `Nullable<T>` of an enum or of a caller-defined value type closes a generic converter at run time, so Native AOT callers need a context for those (PR 7 / PR 8).

**Lookup order** (first match wins):

1. `[XmlConverter]` on the **member**
2. `options.Converters` in list order (`CanConvert`)
3. `[XmlConverter]` on the **type**
4. Built-in converters
5. `TypeInfoResolver` / source-gen `XmlTypeInfo.Converter`

Built-in converters (on `XmlSerializerOptions.Default`): primitives in the [getter table](02-xml-fidelity.md#lexical-rules-xsd-10-invariant), `string`, `Uri`, `Version`, `enum` (incl. `[Flags]` space-separated names), `Nullable<T>`, arrays, `List<T>` and the [collection set](09-object-mapping.md#collections), `Dictionary<TKey,TValue>` as a map, `XmlElement`/`XmlNode` islands. Extension-data bags use the extension-data converter, not the dictionary map converter.

A converter's `Read` is invoked with the reader positioned on the **StartElement** of that member (or on **Attribute** for attribute-mapped members). The converter must consume through the matching end (or handle `IsEmptyElement`).

## Attributes (`CloudNimble.Text.Xml.Serialization`)

STJ-parallel:

```csharp
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class XmlPropertyNameAttribute : Attribute
{
    public XmlPropertyNameAttribute(string name);
    public string Name { get; }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class XmlIgnoreAttribute : Attribute
{
    public XmlIgnoreCondition Condition { get; set; } // default Always
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class XmlConverterAttribute : Attribute
{
    public XmlConverterAttribute(Type converterType);
    public Type ConverterType { get; }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class XmlExtensionDataAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false)]
public sealed class XmlConstructorAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class XmlRequiredAttribute : Attribute { }

/// <summary>
/// STJ [JsonInclude]: include non-public properties/fields.
/// NOT BCL XmlInclude (known types). See [XmlDerivedType].
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class XmlIncludeAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class XmlPropertyOrderAttribute : Attribute
{
    public XmlPropertyOrderAttribute(int order);
    public int Order { get; }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false)]
public sealed class XmlPolymorphicAttribute : Attribute
{
    public string? TypeDiscriminatorPropertyName { get; set; }
    public XmlDiscriminatorPlacement DiscriminatorPlacement { get; set; }
    public XmlUnknownDerivedTypeHandling UnknownDerivedTypeHandling { get; set; }
    public bool InferClosedTypePolymorphism { get; set; }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class XmlMixedAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class XmlNamingPolicyAttribute : Attribute
{
    public XmlNamingPolicyAttribute(XmlKnownNamingPolicy policy);
    public XmlKnownNamingPolicy Policy { get; }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
public sealed class XmlDerivedTypeAttribute : Attribute
{
    public XmlDerivedTypeAttribute(Type derivedType);
    public XmlDerivedTypeAttribute(Type derivedType, string typeDiscriminator);
    public Type DerivedType { get; }
    public object? TypeDiscriminator { get; }
    public string? Namespace { get; set; }
}

public enum XmlUnknownDerivedTypeHandling
{
    FailSerialization = 0,
    FallBackToBaseType = 1,
    FallBackToNearestAncestor = 2,
}
```

XML-specific:

```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, AllowMultiple = false)]
public sealed class XmlRootAttribute : Attribute
{
    public XmlRootAttribute();
    public XmlRootAttribute(string elementName);
    public string? ElementName { get; set; }
    public string? Namespace { get; set; }
    public bool IsNullable { get; set; }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class XmlAttributeAttribute : Attribute
{
    public XmlAttributeAttribute();
    public XmlAttributeAttribute(string attributeName);
    public XmlAttributeAttribute(string attributeName, string? namespaceUri);
    public string? AttributeName { get; set; }
    public string? Namespace { get; set; }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
public sealed class XmlElementAttribute : Attribute
{
    public XmlElementAttribute();
    public XmlElementAttribute(string elementName);
    public XmlElementAttribute(string elementName, string? namespaceUri);
    public XmlElementAttribute(Type type);
    public XmlElementAttribute(string elementName, Type type);
    public string? ElementName { get; set; }
    public string? Namespace { get; set; }
    public int Order { get; set; } // default -1 = unset (K36)
    public bool IsNullable { get; set; }
    public Type? Type { get; set; }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class XmlArrayAttribute : Attribute
{
    public XmlArrayAttribute();
    public XmlArrayAttribute(string elementName);
    public string? ElementName { get; set; }
    public string? Namespace { get; set; }
    public int Order { get; set; } // default -1 = unset
    public bool IsNullable { get; set; }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
public sealed class XmlArrayItemAttribute : Attribute
{
    public XmlArrayItemAttribute();
    public XmlArrayItemAttribute(string elementName);
    public XmlArrayItemAttribute(string elementName, Type type);
    public string? ElementName { get; set; }
    public string? Namespace { get; set; }
    public Type? Type { get; set; }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class XmlTextAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class XmlCDataAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum, AllowMultiple = false)]
public sealed class XmlTypeAttribute : Attribute
{
    public XmlTypeAttribute();
    public XmlTypeAttribute(string typeName);
    public string? TypeName { get; set; }
    public string? Namespace { get; set; }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, AllowMultiple = false)]
public sealed class XmlNamespaceAttribute : Attribute
{
    public XmlNamespaceAttribute(string namespaceUri);
    public string NamespaceUri { get; }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, AllowMultiple = true)]
public sealed class XmlNamespaceDeclarationAttribute : Attribute
{
    public XmlNamespaceDeclarationAttribute(string prefix, string namespaceUri);
    public string Prefix { get; }
    public string NamespaceUri { get; }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class XmlNamespaceDeclarationsAttribute : Attribute { }
```

Context attributes: [serializer page](07-api-serializer.md#type-info--context). Multiple `[XmlElement]`: [mapping](09-object-mapping.md#element-vs-attribute-vs-text).

## Disambiguation

| Want | Attribute |
| --- | --- |
| Serialize a non-public property | `[XmlInclude]` (STJ meaning; property/field only; **no** `Type` ctor) |
| Known derived type | `[XmlDerivedType]` |
| BCL known-types (`System.Xml.Serialization.XmlIncludeAttribute`) | **not used, not honored** |

XMLXE007 (generator): `System.Xml.Serialization.XmlIncludeAttribute` present on this **type graph** (typically on the **class**, BCL muscle memory). Message: use `[XmlDerivedType]`. Optionally also our `[XmlInclude]` on a type (invalid target). The runtime library does not reference BCL XML; the generator inspects consumer symbols.

---

[Index](README.md) · [Previous: XmlSerializer](07-api-serializer.md) · [Next: Object mapping](09-object-mapping.md)
