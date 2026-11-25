using System;
using System.Collections.Generic;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;
using CloudNimble.XmlEssentials.Tests.POCOs;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// A resolver that describes a set of test types with hand-written metadata, the way a source-generated context
    /// describes them: getter and setter delegates, constructed attributes, and constructor delegates. Nothing in it
    /// reflects over a member.
    /// </summary>
    public sealed class MetadataResolver : IXmlTypeInfoResolver
    {

        #region Fields

        private static readonly Dictionary<Type, Func<XmlSerializerOptions, XmlTypeInfo>> Types = new()
        {
            [typeof(Person)] = static options => XmlMetadataServices.CreateObjectInfo(options, new XmlObjectInfoValues<Person>
            {
                ObjectCreator = static () => new Person(),
                PropertyMetadataInitializer = static _ =>
                [
                    Property<Person, string?>(nameof(Person.Name), static p => p.Name, static (p, v) => p.Name = v),
                    Property<Person, int>(nameof(Person.Age), static p => p.Age, static (p, v) => p.Age = v),
                ],
            }),
            [typeof(MetadataLine)] = static options => XmlMetadataServices.CreateObjectInfo(options, new XmlObjectInfoValues<MetadataLine>
            {
                ObjectWithParameterizedConstructorCreator = static args => new MetadataLine((int)args[0]!, (string)args[1]!),
                ConstructorParameterMetadataInitializer = static () =>
                [
                    new XmlParameterInfoValues { Name = "Qty", ParameterType = typeof(int), Position = 0, IsNullable = false },
                    new XmlParameterInfoValues { Name = "Sku", ParameterType = typeof(string), Position = 1, IsNullable = false },
                ],
                PropertyMetadataInitializer = static _ =>
                [
                    // The properties of a record have init accessors, which are setters: a generated context
                    // calls them through an accessor, and here reflection stands in for it.
                    Property<MetadataLine, int>(nameof(MetadataLine.Qty), static l => l.Qty, static (l, v) => typeof(MetadataLine).GetProperty(nameof(MetadataLine.Qty))!.SetValue(l, v)),
                    new XmlPropertyInfoValues<string> { PropertyName = nameof(MetadataLine.Sku), DeclaringType = typeof(MetadataLine), Getter = static o => ((MetadataLine)o).Sku, Setter = static (o, v) => typeof(MetadataLine).GetProperty(nameof(MetadataLine.Sku))!.SetValue(o, v), IsGetNullable = false, IsSetNullable = false },
                    Property<MetadataLine, decimal>(nameof(MetadataLine.Price), static l => l.Price, static (l, v) => l.Price = v),
                ],
            }),
            [typeof(MetadataOrder)] = static options => XmlMetadataServices.CreateObjectInfo(options, new XmlObjectInfoValues<MetadataOrder>
            {
                ObjectCreator = static () => new MetadataOrder(),
                PropertyMetadataInitializer = static _ =>
                [
                    Property<MetadataOrder, Dictionary<string, int>>(nameof(MetadataOrder.Counts), static o => o.Counts, static (o, v) => o.Counts = v),
                    Property<MetadataOrder, int>(nameof(MetadataOrder.Id), static o => o.Id, static (o, v) => o.Id = v, new XmlAttributeAttribute()),
                    XmlMetadataServices.CreateCollectionPropertyInfo<List<MetadataLine>, MetadataLine>(
                        Property<MetadataOrder, List<MetadataLine>>(nameof(MetadataOrder.Lines), static o => o.Lines, null, new XmlArrayAttribute())),
                    Property<MetadataOrder, string?>(nameof(MetadataOrder.Name), static o => o.Name, static (o, v) => SetName(o, v)),
                    XmlMetadataServices.CreateCollectionPropertyInfo<List<int>, int>(
                        Property<MetadataOrder, List<int>>(nameof(MetadataOrder.Numbers), static o => o.Numbers, static (o, v) => o.Numbers = v, new XmlElementAttribute("N"))),
                    Property<MetadataOrder, PopulatePoint>(nameof(MetadataOrder.Origin), static o => o.Origin, static (o, v) => o.Origin = v),
                    Property<MetadataOrder, string?>(nameof(MetadataOrder.Note), static o => o.Note, static (o, v) => o.Note = v, new XmlIgnoreAttribute { Condition = XmlIgnoreCondition.WhenWritingNull }),
                    new XmlPropertyInfoValues<int> { PropertyName = nameof(MetadataOrder.Revision), DeclaringType = typeof(MetadataOrder), IsProperty = false, Getter = static o => ((MetadataOrder)o).Revision, Setter = static (o, v) => ((MetadataOrder)o).Revision = v },
                ],
            }),
            [typeof(PopulatePoint)] = static options => XmlMetadataServices.CreateObjectInfo(options, new XmlObjectInfoValues<PopulatePoint>
            {
                ObjectCreator = static () => default,
                PropertyMetadataInitializer = static _ =>
                [
                    new XmlPropertyInfoValues<int> { PropertyName = nameof(PopulatePoint.X), DeclaringType = typeof(PopulatePoint), Getter = static o => ((PopulatePoint)o).X, Setter = static (o, v) => System.Runtime.CompilerServices.Unsafe.Unbox<PopulatePoint>(o).X = v },
                    new XmlPropertyInfoValues<int> { PropertyName = nameof(PopulatePoint.Y), DeclaringType = typeof(PopulatePoint), Getter = static o => ((PopulatePoint)o).Y, Setter = static (o, v) => System.Runtime.CompilerServices.Unsafe.Unbox<PopulatePoint>(o).Y = v },
                ],
            }),
            [typeof(ExtraBag)] = static options => XmlMetadataServices.CreateObjectInfo(options, new XmlObjectInfoValues<ExtraBag>
            {
                ObjectCreator = static () => new ExtraBag(),
                PropertyMetadataInitializer = static _ =>
                [
                    Property<ExtraBag, XmlExtensionData>(nameof(ExtraBag.Extra), static b => b.Extra, static (b, v) => b.Extra = v, new XmlExtensionDataAttribute()),
                    Property<ExtraBag, string?>(nameof(ExtraBag.Name), static b => b.Name, static (b, v) => b.Name = v),
                ],
            }),
            [typeof(FallbackProduct)] = static options => XmlMetadataServices.CreateObjectInfo(options, new XmlObjectInfoValues<FallbackProduct>
            {
                ObjectCreator = static () => new FallbackProduct(),
                PropertyMetadataInitializer = static _ => [Property<FallbackProduct, string?>(nameof(FallbackProduct.Title), static p => p.Title, static (p, v) => p.Title = v)],
            }),
            [typeof(FallbackBook)] = static options => XmlMetadataServices.CreateObjectInfo(options, new XmlObjectInfoValues<FallbackBook>
            {
                ObjectCreator = static () => new FallbackBook(),
                PropertyMetadataInitializer = static _ =>
                [
                    Property<FallbackProduct, string?>(nameof(FallbackProduct.Title), static p => p.Title, static (p, v) => p.Title = v),
                    Property<FallbackBook, string?>(nameof(FallbackBook.Author), static b => b.Author, static (b, v) => b.Author = v),
                ],
            }),
            [typeof(MemberConverted)] = static options => XmlMetadataServices.CreateObjectInfo(options, new XmlObjectInfoValues<MemberConverted>
            {
                ObjectCreator = static () => new MemberConverted(),
                PropertyMetadataInitializer = static _ =>
                [
                    Property<MemberConverted, int>(nameof(MemberConverted.Flags), static m => m.Flags, static (m, v) => m.Flags = v, new XmlAttributeAttribute(), new XmlConverterAttribute<HexInt32Converter>()),
                    Property<MemberConverted, int>(nameof(MemberConverted.Plain), static m => m.Plain, static (m, v) => m.Plain = v),
                    Property<MemberConverted, int>(nameof(MemberConverted.Hex), static m => m.Hex, static (m, v) => m.Hex = v, new XmlConverterAttribute<HexInt32Converter>()),
                    XmlMetadataServices.CreateNullablePropertyInfo(
                        Property<MemberConverted, int?>(nameof(MemberConverted.Maybe), static m => m.Maybe, static (m, v) => m.Maybe = v, new XmlConverterAttribute<HexInt32Converter>())),
                    Property<MemberConverted, List<int>?>(nameof(MemberConverted.Joined), static m => m.Joined, static (m, v) => m.Joined = v, new XmlConverterAttribute<JoinedInt32ListConverter>()),
                ],
            }),
            [typeof(Node)] = static options => XmlMetadataServices.CreateObjectInfo(options, new XmlObjectInfoValues<Node>
            {
                ObjectCreator = static () => new Node(),
                PropertyMetadataInitializer = static _ =>
                [
                    Property<Node, Node?>(nameof(Node.Child), static n => n.Child, static (n, v) => n.Child = v),
                    Property<Node, string?>(nameof(Node.Name), static n => n.Name, static (n, v) => n.Name = v),
                ],
            }),
            [typeof(PopulateNested)] = static options => XmlMetadataServices.CreateObjectInfo(options, new XmlObjectInfoValues<PopulateNested>
            {
                ObjectCreator = static () => new PopulateNested(),
                PropertyMetadataInitializer = static _ => [Property<PopulateNested, PopulateInner>(nameof(PopulateNested.Inner), static n => n.Inner, null)],
            }),
            [typeof(PopulateInner)] = static options => XmlMetadataServices.CreateObjectInfo(options, new XmlObjectInfoValues<PopulateInner>
            {
                ObjectCreator = static () => new PopulateInner(),
                PropertyMetadataInitializer = static _ =>
                [
                    Property<PopulateInner, PopulateChild>(nameof(PopulateInner.Child), static i => i.Child, null),
                    XmlMetadataServices.CreateCollectionPropertyInfo<List<int>, int>(Property<PopulateInner, List<int>>(nameof(PopulateInner.List), static i => i.List, null, new XmlArrayAttribute())),
                    Property<PopulateInner, int>(nameof(PopulateInner.Size), static i => i.Size, static (i, v) => i.Size = v),
                ],
            }),
            [typeof(PopulateChild)] = static options => XmlMetadataServices.CreateObjectInfo(options, new XmlObjectInfoValues<PopulateChild>
            {
                ObjectCreator = static () => new PopulateChild(),
                PropertyMetadataInitializer = static _ =>
                [
                    Property<PopulateChild, int>(nameof(PopulateChild.A), static c => c.A, static (c, v) => c.A = v),
                    Property<PopulateChild, int>(nameof(PopulateChild.B), static c => c.B, static (c, v) => c.B = v),
                ],
            }),
            [typeof(List<MetadataLine>)] = static options => XmlMetadataServices.CreateCollectionInfo<List<MetadataLine>, MetadataLine>(options),
            [typeof(List<int>)] = static options => XmlMetadataServices.CreateCollectionInfo<List<int>, int>(options),
            [typeof(Dictionary<string, int>)] = static options => XmlMetadataServices.CreateDictionaryInfo<Dictionary<string, int>, string, int>(options),
        };

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public XmlTypeInfo? GetTypeInfo(Type type, XmlSerializerOptions options)
        {
            return Types.TryGetValue(type, out var create)
                ? create(options)
                : XmlMetadataServices.GetBuiltInTypeInfo(type, options);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Describes a property of a class as generated code describes it.
        /// </summary>
        private static XmlPropertyInfoValues<TValue> Property<TDeclaring, TValue>(string name, Func<TDeclaring, TValue> get, Action<TDeclaring, TValue>? set, params Attribute[] attributes)
            where TDeclaring : class
        {
            return new XmlPropertyInfoValues<TValue>
            {
                PropertyName = name,
                DeclaringType = typeof(TDeclaring),
                Getter = instance => get((TDeclaring)instance),
                Setter = set is null ? null : (instance, value) => set((TDeclaring)instance, value),
                Attributes = attributes,
            };
        }

        /// <summary>
        /// Sets the init-only name of an order, as generated code sets it: through an accessor that is not reflection.
        /// </summary>
        [System.Runtime.CompilerServices.UnsafeAccessor(System.Runtime.CompilerServices.UnsafeAccessorKind.Method, Name = "set_Name")]
        private static extern void SetName(MetadataOrder order, string? value);

        #endregion

    }

}
