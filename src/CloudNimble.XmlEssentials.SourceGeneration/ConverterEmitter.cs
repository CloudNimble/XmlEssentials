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
            if (type.PopulateRefusal.Length > 0)
            {
                // The reflection resolver would set the members of the object this member holds. A generated
                // converter creates a new object, so it says so instead of replacing what the caller asked to keep.
                builder.Append(inner).Append("        if (options.PreferredObjectCreationHandling is XmlObjectCreationHandling.Populate) throw new NotSupportedException(\"The generated converter for '")
                    .Append(type.ClrTypeName).Append("' cannot populate the member '").Append(Escape(type.PopulateRefusal))
                    .AppendLine("'. Mark the member [XmlObjectCreationHandling(XmlObjectCreationHandling.Replace)], or read this type through the reflection resolver.\");");
            }

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
            var keepsUnmapped = false;
            foreach (var member in type.Members)
            {
                keepsUnmapped |= member.IsExtensionData;
            }

            builder.Append(indent).AppendLine("var __ignoreCase = options.PropertyNameCaseInsensitive;");
            builder.Append(indent).AppendLine("var __populates = options.PreferredObjectCreationHandling is XmlObjectCreationHandling.Populate;");

            foreach (var member in type.Members)
            {
                if (member.IsExtensionData)
                {
                    continue;
                }

                if (member.IsCollection)
                {
                    // The items are read into a list, whatever the member's type is. The declared type is made from it at the end.
                    builder.Append(indent).Append(ListType(member)).Append("? ").Append(Local(member.ClrName)).AppendLine(" = null;");
                }
                else
                {
                    builder.Append(indent).Append(member.ClrTypeName).Append(" ").Append(Local(member.ClrName)).Append(" = default!;").AppendLine();
                }

                if (member.IsText && !IsString(member.ClrTypeName))
                {
                    builder.Append(indent).Append("string? ").Append(TextLocal(member.ClrName)).AppendLine(" = null;");
                }

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
                // An attribute that is never read is still a known one: it is seen, and what it holds is not read.
                var storedAttribute = Stored(member);
                builder.Append("if (").Append(Match(member.LocalName)).Append(") { ").Append(Seen(member.ClrName)).Append(" = true; ");
                if (storedAttribute != "false")
                {
                    builder.Append(storedAttribute == "true" ? "" : "if (" + storedAttribute + ") ").Append(Local(member.ClrName)).Append(" = ")
                        .Append(IsString(member.ClrTypeName) ? "reader.GetString()" : ReadAttribute(member.ClrTypeName)).Append("; ");
                }

                builder.AppendLine("}");
            }

            // An attribute or element no member is mapped to is skipped, or refused when the options say so. A
            // type with an extension-data member keeps unmapped content, so nothing is refused for it.
            if (!keepsUnmapped)
            {
                builder.Append(indent).Append("        ").Append(firstAttr ? "" : "else ").AppendLine("XmlMetadataServices.RejectUnmappedAttribute(ref reader, options);");
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
                builder.Append("if (").Append(Match(member.LocalName)).Append(")");
                builder.AppendLine();
                builder.Append(indent).AppendLine("            {");
                builder.Append(indent).Append("                ").Append(Seen(member.ClrName)).AppendLine(" = true;");
                // An element for a member that takes nothing from the document is passed over without being read,
                // as the reflection resolver passes over it.
                var storedElement = Stored(member);
                if (storedElement == "false")
                {
                    builder.Append(indent).AppendLine("                reader.Skip();");
                    builder.Append(indent).AppendLine("            }");
                    continue;
                }

                if (storedElement != "true")
                {
                    builder.Append(indent).Append("                if (!(").Append(storedElement).AppendLine(")) { reader.Skip(); continue; }");
                }

                if (member.IsCollection && member.IsWrapped)
                {
                    // A wrapper marked nil is a null collection, which is not the same as an empty one.
                    builder.Append(indent).AppendLine("                if (reader.HasAttributes && reader.GetNil())");
                    builder.Append(indent).AppendLine("                {");
                    builder.Append(indent).Append("                    ").Append(Local(member.ClrName)).AppendLine(" = null;");
                    builder.Append(indent).AppendLine("                    if (!reader.IsEmptyElement) reader.Skip();");
                    builder.Append(indent).AppendLine("                    continue;");
                    builder.Append(indent).AppendLine("                }");
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
                if (!keepsUnmapped)
                {
                    builder.Append(indent).AppendLine("                XmlMetadataServices.RejectUnmappedElement(ref reader, options);");
                }

                builder.Append(indent).AppendLine("                reader.Skip();");
                builder.Append(indent).AppendLine("            }");
            }
            else
            {
                if (!keepsUnmapped)
                {
                    builder.Append(indent).AppendLine("            XmlMetadataServices.RejectUnmappedElement(ref reader, options);");
                }

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

                // Text can arrive in pieces: adjacent CDATA sections, or text on both sides of a comment.
                var text = IsString(member.ClrTypeName) ? Local(member.ClrName) : TextLocal(member.ClrName);
                builder.Append(indent).Append("            ").Append(text).Append(" = ").Append(Seen(member.ClrName)).Append(" ? ").Append(text).Append(" + reader.GetString() : reader.GetString();").AppendLine();
                builder.Append(indent).Append("            ").Append(Seen(member.ClrName)).AppendLine(" = true;");
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

                var stored = Stored(member);
                if (member.IsText && !IsString(member.ClrTypeName) && stored != "false")
                {
                    // Text of any other type is read by the type's converter, once every piece has arrived.
                    builder.Append(indent).Append("if (").Append(Seen(member.ClrName)).Append(stored == "true" ? "" : " && " + stored).Append(") ").Append(Local(member.ClrName)).Append(" = __GetInfo<").Append(member.ClrTypeName)
                        .Append(">(options).Converter.ReadLexical(System.Text.Encoding.UTF8.GetBytes(").Append(TextLocal(member.ClrName)).AppendLine("!), options)!;");
                }

                // A member the document gave that is still null was marked nil.
                var refusal = member.ConstructorParameter is not null
                    ? member.ParameterRefusesNull ? "The constructor parameter '" + Escape(member.ConstructorParameter) + "' on type '\" + typeof(" + type.ClrTypeName + ") + \"' doesn't allow null values." : null
                    : member.NullSetOwner is not null ? "The property or field '" + Escape(member.ClrName) + "' on type '\" + typeof(" + member.NullSetOwner + ") + \"' doesn't allow setting null values." : null;
                if (refusal is not null && stored != "false")
                {
                    builder.Append(indent).Append("if (options.RespectNullableAnnotations && ").Append(Seen(member.ClrName)).Append(stored == "true" ? "" : " && " + stored).Append(" && ").Append(Local(member.ClrName))
                        .Append(" is null) throw new XmlException(\"").Append(refusal).AppendLine(" Consider updating its nullability annotation.\");");
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
                    builder.Append(Value(member));
                }
                else
                {
                    builder.Append(Seen(member.ClrName)).Append(" ? ").Append(Value(member)).Append(" : ").Append(member.ConstructorDefault);
                }
            }

            // A parameter whose member is ignored is passed the default of its type. With no argument
            // before it, the leading comma of the first one is dropped.
            builder.Append(arguments.Count == 0 && type.ConstructorTail.Length > 0 ? type.ConstructorTail.Substring(2) : type.ConstructorTail);
            builder.Append(')');

            // A member with an init accessor can be set only here, so it is set whether or not it was read. A
            // required member has to be set here for the compiler, and has been checked above.
            var firstInit = true;
            foreach (var member in type.Members)
            {
                if (member.IsExtensionData || member.ConstructorParameter is not null || member.IgnoreCondition == 5 || !(member.IsInitOnly || (member.IsRequired && member.CanSet)))
                {
                    continue;
                }

                builder.Append(firstInit ? " { " : ", ").Append(member.ClrName).Append(" = ").Append(Value(member));
                firstInit = false;
            }

            builder.AppendLine(firstInit ? ";" : " };");
            foreach (var member in type.Members)
            {
                if (member.IsExtensionData || member.ConstructorParameter is not null || member.IgnoreCondition == 5)
                {
                    continue;
                }

                if (member.Populate != 0)
                {
                    EmitPopulate(builder, indent, member);
                    continue;
                }

                if (!member.CanSet || member.IsRequired)
                {
                    continue;
                }

                // Only a member the document gave is set, so one it left out keeps what the type initialized it to.
                builder.Append(indent).Append("if (").Append(Seen(member.ClrName)).Append(") obj.").Append(member.ClrName).Append(" = ").Append(Value(member)).AppendLine(";");
            }

            builder.Append(indent).AppendLine("return obj;");
        }

        /// <summary>
        /// Emits the statements that give a collection or dictionary member what was read for it, when the value the
        /// member already holds may be populated.
        /// </summary>
        /// <param name="builder">The source.</param>
        /// <param name="indent">The indent.</param>
        /// <param name="member">A member whose <see cref="MemberSpec.Populate"/> is not 0.</param>
        /// <remarks>
        /// As the reflection resolver has it. When the member is populated, what was read is added to the value it
        /// holds; a member that holds nothing is given a new value when it has a setter; and a nil element sets a
        /// member with a setter to null and is refused for one without. When it is not populated, a member with a
        /// setter is given a new value and one without is not read.
        /// </remarks>
        internal static void EmitPopulate(StringBuilder builder, string indent, MemberSpec member)
        {
            var local = Local(member.ClrName);
            var populates = member.Populate == 1 ? "" : "__populates && ";
            var add = member.PopulateEntryTypes is null
                ? "XmlMetadataServices.AddItems(__existing, " + local + ", \"" + Escape(member.LocalName) + "\");"
                : "XmlMetadataServices.AddEntries<" + member.PopulateEntryTypes + ">(__existing, " + local + ");";

            // Each member has its own block, because a variable declared in the condition of an if statement is in
            // scope for the rest of the block the statement is in.
            builder.Append(indent).Append("if (").Append(Seen(member.ClrName)).AppendLine(")");
            builder.Append(indent).AppendLine("{");
            if (member.CanSet)
            {
                builder.Append(indent).Append("    if (").Append(populates).Append(local).Append(" is not null && obj.").Append(member.ClrName).Append(" is { } __existing) ").AppendLine(add);
                builder.Append(indent).Append("    else obj.").Append(member.ClrName).Append(" = ").Append(Value(member)).AppendLine(";");
            }
            else
            {
                builder.Append(indent).Append("    if (").Append(populates).AppendLine("true)");
                builder.Append(indent).AppendLine("    {");
                builder.Append(indent).Append("        if (").Append(local).Append(" is null) throw new InvalidOperationException(\"Unable to assign 'null' to the property or field of type '\" + typeof(")
                    .Append(member.ClrTypeName.TrimEnd('?')).AppendLine(") + \"'.\");");
                builder.Append(indent).Append("        if (obj.").Append(member.ClrName).Append(" is { } __existing) ").AppendLine(add);
                builder.Append(indent).AppendLine("    }");
            }

            builder.Append(indent).AppendLine("}");
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

            // A member with no condition of its own follows the options, which are read when the converter runs.
            builder.Append(indent).AppendLine(member.IgnoreCondition == -1 ? "else if (options.DefaultIgnoreCondition is not (XmlIgnoreCondition.WhenWritingNull or XmlIgnoreCondition.WhenWritingDefault))" : "else");
            builder.Append(indent).AppendLine("{");
            EmitStartElement(builder, indent + "    ", member.LocalName, member.NamespaceUri, leaf: false);
            builder.Append(indent).AppendLine("    writer.WriteNilValue();");
            EmitEndElement(builder, indent + "    ", member.LocalName, member.NamespaceUri, leaf: false);
            builder.Append(indent).AppendLine("}");
        }

        /// <summary>
        /// Returns the expression that tests whether the reader's local name is <paramref name="localName"/>.
        /// </summary>
        /// <param name="localName">The XML local name.</param>
        /// <returns>The expression. The exact comparison comes first; the one that ignores ASCII case runs only when the options ask for it.</returns>
        internal static string Match(string localName)
        {
            var literal = "\"" + Escape(localName) + "\"u8";
            return "(reader.LocalNameEquals(" + literal + ") || (__ignoreCase && System.Text.Ascii.EqualsIgnoreCase(reader.LocalNameSpan, " + literal + ")))";
        }

        /// <summary>
        /// Returns the expression that gives a member the value that was read for it.
        /// </summary>
        /// <param name="member">The member.</param>
        /// <returns>
        /// The local that holds the value. For a collection, the list when the member is declared as that list,
        /// and otherwise the declared type made from the list, or null when no items were read.
        /// </returns>
        internal static string Value(MemberSpec member)
        {
            var local = Local(member.ClrName);
            if (!member.IsCollection)
            {
                return local;
            }

            var declared = member.ClrTypeName.EndsWith("?", System.StringComparison.Ordinal) ? member.ClrTypeName.Substring(0, member.ClrTypeName.Length - 1) : member.ClrTypeName;
            if (declared.EndsWith("[]", System.StringComparison.Ordinal))
            {
                // An array is made from the list directly. The helper for other collection types keeps their
                // constructors and methods for the trimmer, which an array does not need.
                return "(" + local + " is null ? default! : " + local + ".ToArray())";
            }

            return declared == "global::" + ListType(member)
                ? local + "!"
                : "(" + local + " is null ? default! : XmlMetadataServices.CreateCollection<" + declared + ", " + member.ItemTypeName + ">(" + local + "))";
        }

        /// <summary>
        /// Returns the C# expression that says whether what the document gives for a member is kept.
        /// </summary>
        /// <param name="member">The member.</param>
        /// <returns>
        /// <c>true</c>, <c>false</c>, or the local <c>__populates</c>, which
        /// holds what the options say when the converter runs.
        /// </returns>
        /// <remarks>
        /// A constructor parameter is always filled. Any other member is kept when it is read (its ignore condition
        /// is not <c>WhenReading</c>) and there is a way to give it the value: a setter, or populating what it holds.
        /// A member that is not kept is passed over without being read, as the reflection resolver passes over it.
        /// </remarks>
        internal static string Stored(MemberSpec member)
        {
            if (member.ConstructorParameter is not null)
            {
                return "true";
            }

            var reads = member.IgnoreCondition switch
            {
                5 => "false",
                _ => "true",
            };
            var keeps = member.CanSet || member.IsInitOnly
                ? "true"
                : member.Populate switch
                {
                    1 => "true",
                    -1 => "__populates",
                    _ => "false",
                };
            if (reads == "false" || keeps == "false")
            {
                return "false";
            }

            if (reads == "true")
            {
                return keeps;
            }

            return keeps == "true" ? reads : reads + " && " + keeps;
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
        /// Returns the name of the local that gathers the text of a member that is not a string.
        /// </summary>
        /// <param name="clrName">The member name.</param>
        /// <returns>The local name.</returns>
        internal static string TextLocal(string clrName)
        {
            return "__text_" + clrName;
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
