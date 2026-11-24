using System.Collections.Generic;
using System.Text;

namespace CloudNimble.XmlEssentials.SourceGeneration
{

    /// <summary>
    /// Emits the write fast path of an object type: the <c>__Serialize_X</c> method of a generated context.
    /// </summary>
    /// <remarks>
    /// Nothing here reads XML. Reading, and writing with options the fast path was not compiled for, are done by the
    /// runtime library from the metadata <see cref="MetadataBuilder"/> writes.
    /// </remarks>
    internal static class ConverterEmitter
    {

        #region Public Methods

        /// <summary>
        /// Appends the write fast path of <paramref name="type"/>: a method that writes the attributes and the
        /// content of an element for a value.
        /// </summary>
        /// <param name="builder">The source.</param>
        /// <param name="indent">The indent of the context class.</param>
        /// <param name="type">The type spec.</param>
        /// <remarks>
        /// The method is the <c>SerializeHandler</c> of the type's metadata. The runtime library calls it only when
        /// the options are the ones it was compiled for; reading, and writing with any other options, go through
        /// the metadata.
        /// </remarks>
        internal static void Emit(StringBuilder builder, string indent, TypeSpec type)
        {
            var inner = indent + "    ";
            builder.Append(inner).Append("private static void __Serialize_").Append(type.PropertyName).Append("(Utf8XmlWriter writer, ").Append(type.ClrTypeName).AppendLine(" value, XmlSerializerOptions options)");
            builder.Append(inner).AppendLine("{");
            EmitWrite(builder, inner + "    ", type);
            builder.Append(inner).AppendLine("}");
            builder.AppendLine();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Emits the body of the write fast path: the statements that write each member.
        /// </summary>
        /// <param name="builder">The source.</param>
        /// <param name="indent">The indent.</param>
        /// <param name="type">The type spec.</param>
        internal static void EmitWrite(StringBuilder builder, string indent, TypeSpec type)
        {
            foreach (var member in type.Members)
            {
                // A member that is never written is left out here.
                if (!member.IsExtensionData && member.IgnoreCondition != 4)
                {
                    EmitMemberWrite(builder, indent, member);
                }
            }
        }

        /// <summary>
        /// Emits the statements that write one member.
        /// </summary>
        /// <param name="builder">The source.</param>
        /// <param name="indent">The indent.</param>
        /// <param name="member">The member.</param>
        internal static void EmitMemberWrite(StringBuilder builder, string indent, MemberSpec member)
        {
            var access = "value." + member.ClrName;
            if (member.NullGetOwner is not null && member.IgnoreCondition is not (2 or 3))
            {
                // A null the ignore condition leaves out is not written, so it is not refused either. A member
                // with no condition of its own follows the options.
                builder.Append(indent).Append("if (options.RespectNullableAnnotations && ").Append(access).Append(" is null")
                    .Append(member.IgnoreCondition == -1 ? " && options.DefaultIgnoreCondition is not (XmlIgnoreCondition.WhenWritingNull or XmlIgnoreCondition.WhenWritingDefault)" : "")
                    .Append(") throw new XmlException(\"The property or field '").Append(Escape(member.ClrName)).Append("' on type '\" + typeof(").Append(member.NullGetOwner)
                    .AppendLine(") + \"' doesn't allow getting null values. Consider updating its nullability annotation.\");");
            }

            if (member.IsAttribute)
            {
                // An attribute has no way to say null, so a null one is left out.
                var inner = EmitPresenceCheck(builder, indent, member, access);
                if (IsLeafScalar(member.ClrTypeName))
                {
                    EmitStartAttribute(builder, inner, member.LocalName, member.NamespaceUri);
                    builder.Append(inner).Append(WriteScalar(access, member.ClrTypeName)).AppendLine(";");
                    builder.Append(inner).AppendLine("writer.WriteEndAttribute();");
                }
                else
                {
                    // Any other type writes the attribute through its converter, as the reflection resolver
                    // does, so a converter the caller supplied decides the value.
                    builder.Append(inner).Append("XmlMetadataServices.WriteAttribute(writer, new XmlName(\"").Append(Escape(member.LocalName)).Append('"');
                    if (!string.IsNullOrEmpty(member.NamespaceUri))
                    {
                        builder.Append(", \"").Append(Escape(member.NamespaceUri)).Append('"');
                    }

                    builder.Append("), ").Append(access).Append(", __GetInfo<").Append(member.ClrTypeName).AppendLine(">(options));");
                }

                EmitCloseCheck(builder, indent, inner);
                return;
            }

            if (member.IsText)
            {
                var inner = EmitPresenceCheck(builder, indent, member, access);
                if (member.IsCData)
                {
                    builder.Append(inner).Append("XmlMetadataServices.WriteCData(writer, ").Append(IsString(member.ClrTypeName) ? access : access + ".ToString()").AppendLine(");");
                }
                else
                {
                    builder.Append(inner).Append(WriteScalar(access, member.ClrTypeName)).AppendLine(";");
                }

                EmitCloseCheck(builder, indent, inner);
                return;
            }

            if (member.IsCollection)
            {
                // The property pattern also compiles for a collection that is a value type, such as ImmutableArray<T>.
                builder.Append(indent).Append("if (").Append(access).AppendLine(" is { })");
                builder.Append(indent).AppendLine("{");
                if (member.IsWrapped)
                {
                    EmitStartElement(builder, indent + "    ", member.LocalName, member.NamespaceUri, leaf: false);
                }

                // The member's namespace belongs to the element named after the member: the wrapper when
                // there is one, each item when there is not. An item inside a wrapper is written as it is
                // named, as the reflection resolver writes it.
                var itemName = member.IsWrapped ? member.ItemLocalName : member.LocalName;
                var itemNamespace = member.IsWrapped ? "" : member.NamespaceUri;
                var itemLeaf = IsLeafScalar(member.ItemTypeName) && CanLeafTag(itemName, itemNamespace);
                builder.Append(indent).Append("    foreach (var item in ").Append(access).AppendLine(")");
                builder.Append(indent).AppendLine("    {");
                EmitStartElement(builder, indent + "        ", itemName, itemNamespace, itemLeaf);
                // The property pattern is true for every value of a value type, where "is not null" would not compile.
                builder.Append(indent).Append("        if (item is { }) ").Append(WriteContent("item", member.ItemTypeName)).AppendLine(";");
                EmitEndElement(builder, indent + "        ", itemName, itemNamespace, itemLeaf);
                builder.Append(indent).AppendLine("    }");
                if (member.IsWrapped)
                {
                    EmitEndElement(builder, indent + "    ", member.LocalName, member.NamespaceUri, leaf: false);
                }

                builder.Append(indent).AppendLine("}");
                if (member.IsWrapped)
                {
                    // Only a wrapper can stand for a null collection. Repeated items with no wrapper have no element to mark.
                    EmitNull(builder, indent, member);
                }

                return;
            }

            var leaf = IsLeafScalar(member.ClrTypeName) && CanLeafTag(member.LocalName, member.NamespaceUri);
            var body = EmitPresenceCheck(builder, indent, member, access);
            EmitStartElement(builder, body, member.LocalName, member.NamespaceUri, leaf);
            builder.Append(body).Append(WriteContent(access, member.ClrTypeName)).AppendLine(";");
            EmitEndElement(builder, body, member.LocalName, member.NamespaceUri, leaf);
            EmitCloseCheck(builder, indent, body);
            if (member.IsNillable)
            {
                EmitNull(builder, indent, member);
            }
        }

        /// <summary>
        /// Emits the test that decides whether a member is written, and opens its block.
        /// </summary>
        /// <param name="builder">The source.</param>
        /// <param name="indent">The indent of the member's statements.</param>
        /// <param name="member">The member.</param>
        /// <param name="access">The expression that reads the member.</param>
        /// <returns>The indent for the statements that write the member: deeper when a block was opened.</returns>
        /// <remarks>
        /// A member that can be null is written when it is not null. A value type is written unless the
        /// ignore condition is <c>WhenWritingDefault</c> and it holds its default, as the reflection resolver has it.
        /// </remarks>
        internal static string EmitPresenceCheck(StringBuilder builder, string indent, MemberSpec member, string access)
        {
            if (member.IsNillable)
            {
                builder.Append(indent).Append("if (").Append(access).AppendLine(" is not null)");
                builder.Append(indent).AppendLine("{");
                return indent + "    ";
            }

            var skipsDefault = member.IgnoreCondition switch
            {
                2 => "true",
                -1 => "options.DefaultIgnoreCondition is XmlIgnoreCondition.WhenWritingDefault",
                _ => null,
            };
            if (skipsDefault is null)
            {
                return indent;
            }

            builder.Append(indent).Append("if (!(").Append(skipsDefault).Append(" && System.Collections.Generic.EqualityComparer<").Append(member.ClrTypeName).Append(">.Default.Equals(").Append(access).AppendLine(", default)))");
            builder.Append(indent).AppendLine("{");
            return indent + "    ";
        }

        /// <summary>
        /// Closes the block that <see cref="EmitPresenceCheck"/> opened, if it opened one.
        /// </summary>
        /// <param name="builder">The source.</param>
        /// <param name="indent">The indent of the member's statements.</param>
        /// <param name="inner">The indent that <see cref="EmitPresenceCheck"/> returned.</param>
        internal static void EmitCloseCheck(StringBuilder builder, string indent, string inner)
        {
            if (inner.Length != indent.Length)
            {
                builder.Append(indent).AppendLine("}");
            }
        }

        /// <summary>
        /// Emits what is written for a null element member, after the block that writes a value.
        /// </summary>
        /// <param name="builder">The source.</param>
        /// <param name="indent">The indent of the member's statements.</param>
        /// <param name="member">The member.</param>
        /// <remarks>
        /// As the reflection resolver has it: nothing when the member is ignored when it is null, and
        /// otherwise the element, marked <c>xsi:nil</c>, which is how XML writes null.
        /// </remarks>
        internal static void EmitNull(StringBuilder builder, string indent, MemberSpec member)
        {
            if (member.IgnoreCondition is 2 or 3)
            {
                return;
            }

            // A member with no condition of its own follows the options, which are read when the fast path runs.
            builder.Append(indent).AppendLine(member.IgnoreCondition == -1 ? "else if (options.DefaultIgnoreCondition is not (XmlIgnoreCondition.WhenWritingNull or XmlIgnoreCondition.WhenWritingDefault))" : "else");
            builder.Append(indent).AppendLine("{");
            EmitStartElement(builder, indent + "    ", member.LocalName, member.NamespaceUri, leaf: false);
            builder.Append(indent).AppendLine("    writer.WriteNilValue();");
            EmitEndElement(builder, indent + "    ", member.LocalName, member.NamespaceUri, leaf: false);
            builder.Append(indent).AppendLine("}");
        }

        /// <summary>
        /// Returns whether <paramref name="localName"/> can be written as a prebuilt UTF-8 leaf tag.
        /// </summary>
        /// <param name="localName">The XML local name.</param>
        /// <param name="namespaceUri">The namespace URI.</param>
        /// <returns><see langword="true"/> for an unprefixed ASCII NCName.</returns>
        internal static bool CanLeafTag(string localName, string namespaceUri)
        {
            return string.IsNullOrEmpty(namespaceUri) && Naming.IsAsciiNcName(localName);
        }

        /// <summary>
        /// Returns whether <paramref name="clrTypeName"/> is written as a single text value.
        /// </summary>
        /// <param name="clrTypeName">The CLR type name.</param>
        /// <returns><see langword="true"/> for strings and primitives.</returns>
        internal static bool IsLeafScalar(string clrTypeName)
        {
            var core = clrTypeName.EndsWith("?", System.StringComparison.Ordinal) ? clrTypeName.Substring(0, clrTypeName.Length - 1) : clrTypeName;
            return IsString(core)
                || core is "bool" or "global::System.Boolean"
                or "int" or "global::System.Int32"
                or "long" or "global::System.Int64"
                or "uint" or "global::System.UInt32"
                or "ulong" or "global::System.UInt64"
                or "float" or "global::System.Single"
                or "double" or "global::System.Double"
                or "decimal" or "global::System.Decimal"
                or "short" or "global::System.Int16"
                or "ushort" or "global::System.UInt16"
                or "byte" or "global::System.Byte"
                or "sbyte" or "global::System.SByte"
                or "global::System.DateTime"
                or "global::System.DateTimeOffset"
                or "global::System.DateOnly"
                or "global::System.TimeOnly"
                or "global::System.TimeSpan"
                or "global::System.Guid";
        }

        /// <summary>
        /// Emits a start tag, using a prebuilt UTF-8 leaf tag when <paramref name="leaf"/> is true.
        /// </summary>
        /// <param name="builder">The source.</param>
        /// <param name="indent">The indent.</param>
        /// <param name="localName">The XML local name.</param>
        /// <param name="namespaceUri">The namespace URI.</param>
        /// <param name="leaf">Whether this is a scalar leaf.</param>
        internal static void EmitStartElement(StringBuilder builder, string indent, string localName, string namespaceUri, bool leaf)
        {
            if (leaf && CanLeafTag(localName, namespaceUri))
            {
                builder.Append(indent).Append("XmlMetadataServices.WriteLeafStart(writer, ").Append(Utf8Tag(localName, close: false)).AppendLine(");");
                return;
            }

            if (string.IsNullOrEmpty(namespaceUri))
            {
                // No namespace was declared for the member, so the name is written as it is and inherits the
                // default namespace in scope, as the reflection resolver writes it.
                builder.Append(indent).Append("writer.WriteStartElement(\"").Append(Escape(localName)).AppendLine("\");");
                return;
            }

            builder.Append(indent).Append("writer.WriteStartElement(\"").Append(Escape(localName)).Append("\", \"").Append(Escape(namespaceUri)).AppendLine("\");");
        }

        /// <summary>
        /// Emits the start of an attribute, in its namespace when it has one.
        /// </summary>
        /// <param name="builder">The source.</param>
        /// <param name="indent">The indent.</param>
        /// <param name="localName">The XML local name.</param>
        /// <param name="namespaceUri">The namespace URI, or empty for none.</param>
        internal static void EmitStartAttribute(StringBuilder builder, string indent, string localName, string namespaceUri)
        {
            if (string.IsNullOrEmpty(namespaceUri))
            {
                builder.Append(indent).Append("writer.WriteStartAttribute(\"").Append(Escape(localName)).AppendLine("\");");
                return;
            }

            builder.Append(indent).Append("writer.WriteStartAttribute(null, \"").Append(Escape(localName)).Append("\", \"").Append(Escape(namespaceUri)).AppendLine("\");");
        }

        /// <summary>
        /// Emits an end tag, using a prebuilt UTF-8 leaf tag when <paramref name="leaf"/> is true.
        /// </summary>
        /// <param name="builder">The source.</param>
        /// <param name="indent">The indent.</param>
        /// <param name="localName">The XML local name.</param>
        /// <param name="namespaceUri">The namespace URI.</param>
        /// <param name="leaf">Whether this is a scalar leaf.</param>
        internal static void EmitEndElement(StringBuilder builder, string indent, string localName, string namespaceUri, bool leaf)
        {
            if (leaf && CanLeafTag(localName, namespaceUri))
            {
                builder.Append(indent).Append("XmlMetadataServices.WriteLeafEnd(writer, ").Append(Utf8Tag(localName, close: true)).AppendLine(");");
                return;
            }

            builder.Append(indent).AppendLine("writer.WriteEndElement();");
        }

        /// <summary>
        /// Returns a UTF-8 string literal for <c>&lt;name&gt;</c> or <c>&lt;/name&gt;</c>.
        /// </summary>
        /// <param name="localName">The XML local name.</param>
        /// <param name="close">Whether this is the end tag.</param>
        /// <returns>The C# UTF-8 literal.</returns>
        internal static string Utf8Tag(string localName, bool close)
        {
            return close
                ? "\"</" + Escape(localName) + ">\"u8"
                : "\"<" + Escape(localName) + ">\"u8";
        }

        /// <summary>
        /// Returns generated code that writes a scalar.
        /// </summary>
        /// <param name="expression">The value expression.</param>
        /// <param name="clrTypeName">The type name.</param>
        /// <returns>The statement.</returns>
        internal static string WriteScalar(string expression, string clrTypeName)
        {
            var nullable = clrTypeName.EndsWith("?", System.StringComparison.Ordinal);
            var core = nullable ? clrTypeName.Substring(0, clrTypeName.Length - 1) : clrTypeName;
            var value = nullable && !IsString(core) ? expression + ".Value" : expression;
            return core switch
            {
                "string" or "global::System.String" => "writer.WriteStringValue(" + expression + ")",
                "bool" or "global::System.Boolean" => "writer.WriteBooleanValue(" + value + ")",
                "int" or "global::System.Int32" => "writer.WriteNumberValue(" + value + ")",
                "long" or "global::System.Int64" => "writer.WriteNumberValue(" + value + ")",
                "uint" or "global::System.UInt32" => "writer.WriteNumberValue(" + value + ")",
                "ulong" or "global::System.UInt64" => "writer.WriteNumberValue(" + value + ")",
                "float" or "global::System.Single" => "writer.WriteNumberValue(__Finite(" + value + ", options))",
                "double" or "global::System.Double" => "writer.WriteNumberValue(__Finite(" + value + ", options))",
                "decimal" or "global::System.Decimal" => "writer.WriteNumberValue(" + value + ")",
                "short" or "global::System.Int16" => "writer.WriteNumberValue((int)" + value + ")",
                "ushort" or "global::System.UInt16" => "writer.WriteNumberValue((uint)" + value + ")",
                "byte" or "global::System.Byte" => "writer.WriteNumberValue((uint)" + value + ")",
                "sbyte" or "global::System.SByte" => "writer.WriteNumberValue((int)" + value + ")",
                "global::System.DateTime" => "writer.WriteStringValue(" + value + ")",
                "global::System.DateTimeOffset" => "writer.WriteStringValue(" + value + ")",
                "global::System.DateOnly" => "writer.WriteStringValue(" + value + ")",
                "global::System.TimeOnly" => "writer.WriteStringValue(" + value + ")",
                "global::System.TimeSpan" => "writer.WriteStringValue(" + value + ")",
                "global::System.Guid" => "writer.WriteStringValue(" + value + ")",
                _ => "XmlMetadataServices.Write<" + clrTypeName + ">(writer, " + expression + ", __GetInfo<" + clrTypeName + ">(options))",
            };
        }

        /// <summary>
        /// Returns generated code that writes element content.
        /// </summary>
        /// <param name="expression">The value expression.</param>
        /// <param name="clrTypeName">The type name.</param>
        /// <returns>The statement.</returns>
        internal static string WriteContent(string expression, string clrTypeName)
        {
            return WriteScalar(expression, clrTypeName);
        }

        /// <summary>
        /// Returns whether <paramref name="clrTypeName"/> is string.
        /// </summary>
        /// <param name="clrTypeName">The type name.</param>
        /// <returns><see langword="true"/> for string.</returns>
        internal static bool IsString(string clrTypeName)
        {
            return clrTypeName is "global::System.String" or "string" or "global::System.String?";
        }

        /// <summary>
        /// Escapes a string for a C# literal.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The escaped value.</returns>
        internal static string Escape(string value)
        {
            return ContextEmitter.Escape(value);
        }

        #endregion

    }

}
