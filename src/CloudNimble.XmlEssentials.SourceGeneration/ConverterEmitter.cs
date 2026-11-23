using System.Collections.Generic;
using System.Text;

namespace CloudNimble.XmlEssentials.SourceGeneration
{

    /// <summary>
    /// Emits a source-generated <c>XmlConverter&lt;T&gt;</c> for an object type.
    /// </summary>
    internal static class ConverterEmitter
    {

        #region Public Methods

        /// <summary>
        /// Appends a nested converter class for <paramref name="type"/>.
        /// </summary>
        /// <param name="builder">The source.</param>
        /// <param name="indent">The type indent.</param>
        /// <param name="type">The type spec.</param>
        internal static void Emit(StringBuilder builder, string indent, TypeSpec type)
        {
            var inner = indent + "    ";
            builder.Append(inner).Append("private sealed class ").Append(type.PropertyName).Append("XmlConverter : XmlConverter<").Append(type.ClrTypeName).AppendLine(">");
            builder.Append(inner).AppendLine("{");
            builder.Append(inner).AppendLine("    public override ").Append(type.ClrTypeName).Append(type.IsValueType ? "" : "?").Append(" Read(ref Utf8XmlReader reader, Type typeToConvert, XmlSerializerOptions options)");
            builder.Append(inner).AppendLine("    {");
            builder.Append(inner).Append("        if (options.ReferenceHandler is not null) throw new NotSupportedException(\"The generated converter for '").Append(type.ClrTypeName).AppendLine("' does not support ReferenceHandler. Read and write this type through the reflection resolver.\");");
            EmitRead(builder, inner + "    ", type);
            builder.Append(inner).AppendLine("    }");
            builder.AppendLine();
            builder.Append(inner).AppendLine("    public override void Write(Utf8XmlWriter writer, ").Append(type.ClrTypeName).Append(" value, XmlSerializerOptions options)");
            builder.Append(inner).AppendLine("    {");
            builder.Append(inner).Append("        if (options.ReferenceHandler is not null) throw new NotSupportedException(\"The generated converter for '").Append(type.ClrTypeName).AppendLine("' does not support ReferenceHandler. Read and write this type through the reflection resolver.\");");
            EmitWrite(builder, inner + "    ", type);
            builder.Append(inner).AppendLine("    }");
            builder.Append(inner).AppendLine("}");
            builder.AppendLine();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Emits the Read method body.
        /// </summary>
        /// <param name="builder">The source.</param>
        /// <param name="indent">The indent.</param>
        /// <param name="type">The type spec.</param>
        internal static void EmitRead(StringBuilder builder, string indent, TypeSpec type)
        {
            foreach (var member in type.Members)
            {
                if (member.IsExtensionData)
                {
                    continue;
                }

                builder.Append(indent).Append(member.ClrTypeName).Append(" ").Append(Local(member.ClrName)).Append(" = default!;").AppendLine();
                builder.Append(indent).Append("var ").Append(Seen(member.ClrName)).AppendLine(" = false;");
            }

            builder.Append(indent).AppendLine("if (reader.HasAttributes)");
            builder.Append(indent).AppendLine("{");
            builder.Append(indent).AppendLine("    while (reader.MoveToNextAttribute())");
            builder.Append(indent).AppendLine("    {");
            var firstAttr = true;
            foreach (var member in type.Members)
            {
                if (!member.IsAttribute)
                {
                    continue;
                }

                builder.Append(indent).Append("        ");
                if (!firstAttr)
                {
                    builder.Append("else ");
                }

                firstAttr = false;
                builder.Append("if (reader.LocalNameEquals(\"").Append(Escape(member.LocalName)).Append("\"u8)) { ").Append(Seen(member.ClrName)).Append(" = true; ").Append(Local(member.ClrName)).Append(" = ");
                if (IsString(member.ClrTypeName))
                {
                    builder.AppendLine("reader.GetString(); }");
                }
                else
                {
                    builder.Append(ReadAttribute(member.ClrTypeName)).AppendLine("; }");
                }
            }

            builder.Append(indent).AppendLine("    }");
            builder.Append(indent).AppendLine("    reader.MoveToElement();");
            builder.Append(indent).AppendLine("}");
            builder.Append(indent).AppendLine("if (!reader.IsEmptyElement)");
            builder.Append(indent).AppendLine("{");
            builder.Append(indent).AppendLine("    while (reader.Read())");
            builder.Append(indent).AppendLine("    {");
            builder.Append(indent).AppendLine("        if (reader.TokenType is XmlTokenType.EndElement)");
            builder.Append(indent).AppendLine("        {");
            builder.Append(indent).AppendLine("            break;");
            builder.Append(indent).AppendLine("        }");
            builder.Append(indent).AppendLine("        if (reader.TokenType is XmlTokenType.StartElement)");
            builder.Append(indent).AppendLine("        {");
            var firstEl = true;
            foreach (var member in type.Members)
            {
                if (member.IsAttribute || member.IsText || member.IsExtensionData)
                {
                    continue;
                }

                builder.Append(indent).Append("            ");
                if (!firstEl)
                {
                    builder.Append("else ");
                }

                firstEl = false;
                builder.Append("if (reader.LocalNameEquals(\"").Append(Escape(member.LocalName)).Append("\"u8))");
                builder.AppendLine();
                builder.Append(indent).AppendLine("            {");
                builder.Append(indent).Append("                ").Append(Seen(member.ClrName)).AppendLine(" = true;");
                if (member.IsCollection && member.IsWrapped)
                {
                    builder.Append(indent).Append("                ").Append(Local(member.ClrName)).Append(" ??= new ").Append(ListType(member)).AppendLine("();");
                    builder.Append(indent).AppendLine("                if (!reader.IsEmptyElement)");
                    builder.Append(indent).AppendLine("                {");
                    builder.Append(indent).AppendLine("                    while (reader.Read())");
                    builder.Append(indent).AppendLine("                    {");
                    builder.Append(indent).AppendLine("                        if (reader.TokenType is XmlTokenType.EndElement) break;");
                    builder.Append(indent).AppendLine("                        if (reader.TokenType is XmlTokenType.StartElement)");
                    builder.Append(indent).AppendLine("                        {");
                    builder.Append(indent).Append("                            ").Append(Local(member.ClrName)).Append(".Add(").Append(ReadValue(member.ItemTypeName, "options")).AppendLine(");");
                    builder.Append(indent).AppendLine("                        }");
                    builder.Append(indent).AppendLine("                    }");
                    builder.Append(indent).AppendLine("                }");
                }
                else if (member.IsCollection)
                {
                    builder.Append(indent).Append("                ").Append(Local(member.ClrName)).Append(" ??= new ").Append(ListType(member)).AppendLine("();");
                    builder.Append(indent).Append("                ").Append(Local(member.ClrName)).Append(".Add(").Append(ReadValue(member.ItemTypeName, "options")).AppendLine(");");
                }
                else
                {
                    if (member.IsRequired)
                    {
                        builder.Append(indent).Append("                if (reader.HasAttributes && reader.GetNil()) throw new XmlException(\"The required member '").Append(Escape(member.LocalName)).AppendLine("' cannot be nil.\");");
                    }

                    builder.Append(indent).Append("                ").Append(Local(member.ClrName)).Append(" = ").Append(ReadValue(member.ClrTypeName, "options")).AppendLine(";");
                }

                builder.Append(indent).AppendLine("            }");
            }

            if (!firstEl)
            {
                builder.Append(indent).AppendLine("            else");
                builder.Append(indent).AppendLine("            {");
                builder.Append(indent).AppendLine("                reader.Skip();");
                builder.Append(indent).AppendLine("            }");
            }
            else
            {
                builder.Append(indent).AppendLine("            reader.Skip();");
            }

            builder.Append(indent).AppendLine("            continue;");
            builder.Append(indent).AppendLine("        }");
            builder.Append(indent).AppendLine("        if (reader.TokenType is XmlTokenType.Text or XmlTokenType.CData)");
            builder.Append(indent).AppendLine("        {");
            foreach (var member in type.Members)
            {
                if (!member.IsText)
                {
                    continue;
                }

                builder.Append(indent).Append("            ").Append(Seen(member.ClrName)).AppendLine(" = true;");
                builder.Append(indent).Append("            ").Append(Local(member.ClrName)).Append(" = reader.GetString();").AppendLine();
            }

            builder.Append(indent).AppendLine("        }");
            builder.Append(indent).AppendLine("    }");
            builder.Append(indent).AppendLine("}");
            foreach (var member in type.Members)
            {
                if (member.IsExtensionData)
                {
                    continue;
                }

                if (member.IsRequired)
                {
                    builder.Append(indent).Append("if (!").Append(Seen(member.ClrName)).Append(") throw new XmlException(\"The required member '").Append(Escape(member.LocalName)).AppendLine("' is missing.\");");
                }
                else if (member.ConstructorParameter is not null && member.ConstructorDefault is null)
                {
                    builder.Append(indent).Append("if (!").Append(Seen(member.ClrName)).Append(" && options.RespectRequiredConstructorParameters) throw new XmlException(\"The required constructor parameter '").Append(Escape(member.LocalName)).AppendLine("' is missing.\");");
                }
            }

            // Arguments are passed by name, so a parameter no member fills is left out and takes its default.
            var arguments = new List<MemberSpec>();
            foreach (var member in type.Members)
            {
                if (member.ConstructorParameter is not null)
                {
                    arguments.Add(member);
                }
            }

            arguments.Sort(static (left, right) => left.ConstructorOrder.CompareTo(right.ConstructorOrder));
            builder.Append(indent).Append("var obj = new ").Append(type.ClrTypeName).Append('(');
            for (var i = 0; i < arguments.Count; i++)
            {
                var member = arguments[i];
                builder.Append(i == 0 ? "" : ", ").Append('@').Append(member.ConstructorParameter).Append(": ");
                if (member.ConstructorDefault is null)
                {
                    builder.Append(Local(member.ClrName));
                }
                else
                {
                    builder.Append(Seen(member.ClrName)).Append(" ? ").Append(Local(member.ClrName)).Append(" : ").Append(member.ConstructorDefault);
                }
            }

            builder.Append(')');

            // A member with an init accessor can be set only here, so it is set whether or not it was read. A
            // required member has to be set here for the compiler, and has been checked above.
            var firstInit = true;
            foreach (var member in type.Members)
            {
                if (member.IsExtensionData || member.ConstructorParameter is not null || !(member.IsInitOnly || (member.IsRequired && member.CanSet)))
                {
                    continue;
                }

                builder.Append(firstInit ? " { " : ", ").Append(member.ClrName).Append(" = ").Append(Local(member.ClrName));
                firstInit = false;
            }

            builder.AppendLine(firstInit ? ";" : " };");
            foreach (var member in type.Members)
            {
                if (member.IsExtensionData || !member.CanSet || member.IsRequired || member.ConstructorParameter is not null)
                {
                    continue;
                }

                // Only a member the document gave is set, so one it left out keeps what the type initialized it to.
                builder.Append(indent).Append("if (").Append(Seen(member.ClrName)).Append(") obj.").Append(member.ClrName).Append(" = ").Append(Local(member.ClrName)).AppendLine(";");
            }

            builder.Append(indent).AppendLine("return obj;");
        }

        /// <summary>
        /// Emits the Write method body.
        /// </summary>
        /// <param name="builder">The source.</param>
        /// <param name="indent">The indent.</param>
        /// <param name="type">The type spec.</param>
        internal static void EmitWrite(StringBuilder builder, string indent, TypeSpec type)
        {
            foreach (var member in type.Members)
            {
                if (member.IsExtensionData)
                {
                    continue;
                }

                if (member.IsAttribute)
                {
                    if (member.IsNillable)
                    {
                        builder.Append(indent).Append("if (value.").Append(member.ClrName).AppendLine(" is not null)");
                        builder.Append(indent).AppendLine("{");
                        builder.Append(indent).Append("    writer.WriteStartAttribute(\"").Append(Escape(member.LocalName)).AppendLine("\");");
                        builder.Append(indent).Append("    ").Append(WriteScalar("value." + member.ClrName, member.ClrTypeName)).AppendLine(";");
                        builder.Append(indent).AppendLine("    writer.WriteEndAttribute();");
                        builder.Append(indent).AppendLine("}");
                    }
                    else
                    {
                        builder.Append(indent).Append("writer.WriteStartAttribute(\"").Append(Escape(member.LocalName)).AppendLine("\");");
                        builder.Append(indent).Append(WriteScalar("value." + member.ClrName, member.ClrTypeName)).AppendLine(";");
                        builder.Append(indent).AppendLine("writer.WriteEndAttribute();");
                    }

                    continue;
                }

                if (member.IsText)
                {
                    if (member.IsCData)
                    {
                        builder.Append(indent).Append("writer.WriteCData(value.").Append(member.ClrName).AppendLine("?.ToString() ?? \"\");");
                    }
                    else
                    {
                        builder.Append(indent).Append(WriteScalar("value." + member.ClrName, member.ClrTypeName)).AppendLine(";");
                    }

                    continue;
                }

                if (member.IsCollection)
                {
                    builder.Append(indent).Append("if (value.").Append(member.ClrName).AppendLine(" is not null)");
                    builder.Append(indent).AppendLine("{");
                    if (member.IsWrapped)
                    {
                        EmitStartElement(builder, indent + "    ", member.LocalName, member.NamespaceUri, leaf: false);
                    }

                    var itemName = member.IsWrapped ? member.ItemLocalName : member.LocalName;
                    var itemLeaf = IsLeafScalar(member.ItemTypeName) && CanLeafTag(itemName, member.NamespaceUri);
                    builder.Append(indent).Append("    foreach (var item in value.").Append(member.ClrName).AppendLine(")");
                    builder.Append(indent).AppendLine("    {");
                    EmitStartElement(builder, indent + "        ", itemName, member.NamespaceUri, itemLeaf);
                    builder.Append(indent).Append("        if (item is not null) ").Append(WriteContent("item", member.ItemTypeName)).AppendLine(";");
                    EmitEndElement(builder, indent + "        ", itemName, member.NamespaceUri, itemLeaf);
                    builder.Append(indent).AppendLine("    }");
                    if (member.IsWrapped)
                    {
                        EmitEndElement(builder, indent + "    ", member.LocalName, member.NamespaceUri, leaf: false);
                    }

                    builder.Append(indent).AppendLine("}");
                    continue;
                }

                var leaf = IsLeafScalar(member.ClrTypeName) && CanLeafTag(member.LocalName, member.NamespaceUri);
                if (member.IsNillable)
                {
                    builder.Append(indent).Append("if (value.").Append(member.ClrName).AppendLine(" is not null)");
                    builder.Append(indent).AppendLine("{");
                    EmitStartElement(builder, indent + "    ", member.LocalName, member.NamespaceUri, leaf);
                    builder.Append(indent).Append("    ").Append(WriteContent("value." + member.ClrName, member.ClrTypeName)).AppendLine(";");
                    EmitEndElement(builder, indent + "    ", member.LocalName, member.NamespaceUri, leaf);
                    builder.Append(indent).AppendLine("}");
                    builder.Append(indent).AppendLine("else");
                    builder.Append(indent).AppendLine("{");
                    EmitStartElement(builder, indent + "    ", member.LocalName, member.NamespaceUri, leaf: false);
                    builder.Append(indent).AppendLine("    writer.WriteNilValue();");
                    EmitEndElement(builder, indent + "    ", member.LocalName, member.NamespaceUri, leaf: false);
                    builder.Append(indent).AppendLine("}");
                }
                else
                {
                    EmitStartElement(builder, indent, member.LocalName, member.NamespaceUri, leaf);
                    builder.Append(indent).Append(WriteContent("value." + member.ClrName, member.ClrTypeName)).AppendLine(";");
                    EmitEndElement(builder, indent, member.LocalName, member.NamespaceUri, leaf);
                }
            }
        }

        /// <summary>
        /// Returns a unique local variable name for <paramref name="clrName"/>.
        /// </summary>
        /// <param name="clrName">The member name.</param>
        /// <returns>The local name.</returns>
        internal static string Local(string clrName)
        {
            return "__" + clrName;
        }

        /// <summary>
        /// Returns the name of the local that records whether <paramref name="clrName"/> was read.
        /// </summary>
        /// <param name="clrName">The member name.</param>
        /// <returns>The local name.</returns>
        internal static string Seen(string clrName)
        {
            return "__seen_" + clrName;
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

            builder.Append(indent).Append("writer.WriteStartElement(\"").Append(Escape(localName)).AppendLine("\");");
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
        /// Returns generated code that reads a value of <paramref name="clrTypeName"/>.
        /// </summary>
        /// <param name="clrTypeName">The fully qualified type name.</param>
        /// <param name="options">The options expression.</param>
        /// <returns>The expression.</returns>
        internal static string ReadValue(string clrTypeName, string options)
        {
            var core = clrTypeName.EndsWith("?", System.StringComparison.Ordinal) ? clrTypeName.Substring(0, clrTypeName.Length - 1) : clrTypeName;
            if (IsString(core))
            {
                return "(reader.HasAttributes && reader.GetNil() ? __ReadNil<" + clrTypeName + ">(ref reader) : ReadStringElement(ref reader))";
            }

            if (core is "int" or "global::System.Int32")
            {
                return "(reader.HasAttributes && reader.GetNil() ? __ReadNil<" + clrTypeName + ">(ref reader) : ReadInt32Element(ref reader))";
            }

            if (core is "decimal" or "global::System.Decimal")
            {
                return "(reader.HasAttributes && reader.GetNil() ? __ReadNil<" + clrTypeName + ">(ref reader) : ReadDecimalElement(ref reader))";
            }

            return "XmlMetadataServices.Read<" + clrTypeName + ">(ref reader, __GetInfo<" + clrTypeName + ">(options))";
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
                "float" or "global::System.Single" => "writer.WriteNumberValue(" + value + ")",
                "double" or "global::System.Double" => "writer.WriteNumberValue(" + value + ")",
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
        /// Returns generated code that reads the current attribute as <paramref name="clrTypeName"/>.
        /// </summary>
        /// <param name="clrTypeName">The type name.</param>
        /// <returns>The expression.</returns>
        internal static string ReadAttribute(string clrTypeName)
        {
            return "XmlMetadataServices.ReadAttribute<" + clrTypeName + ">(ref reader, __GetInfo<" + clrTypeName + ">(options))";
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
        /// Returns a List type name for a collection member.
        /// </summary>
        /// <param name="member">The member.</param>
        /// <returns>The list type.</returns>
        internal static string ListType(MemberSpec member)
        {
            return "System.Collections.Generic.List<" + member.ItemTypeName + ">";
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
