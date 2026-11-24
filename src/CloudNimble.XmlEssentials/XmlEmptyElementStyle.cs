namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Specifies how a <see cref="Utf8XmlWriter"/> writes an element that has no content.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The three forms mean the same thing to a reader. The setting makes the output consistent: every
    /// element with no content is written in the one form, whichever code wrote it.
    /// </para>
    /// <para>
    /// <see cref="SelfClosing"/> is the zero value, so it is the default in <see cref="XmlWriterOptions"/>,
    /// created with <see langword="new"/> or as <see langword="default"/>, and in <see cref="XmlSerializerOptions"/>.
    /// </para>
    /// <para>
    /// <see cref="Utf8XmlWriter.WriteFullEndElement"/> always writes a start tag and an end tag, whatever the
    /// setting is.
    /// </para>
    /// <para>
    /// An element that was given an empty string has content: a value of no characters. It is written as
    /// <c>&lt;e&gt;&lt;/e&gt;</c> whatever the setting is, as the serializer writes a string that is empty,
    /// which keeps it apart from an element that was given nothing and from one that is nil.
    /// </para>
    /// </remarks>
    public enum XmlEmptyElementStyle : byte
    {

        /// <summary>
        /// The element is written as one tag, <c>&lt;e/&gt;</c>. This is the default.
        /// </summary>
        SelfClosing = 0,

        /// <summary>
        /// The element is written as one tag with a space before the slash, <c>&lt;e /&gt;</c>, which is the
        /// form the writers in <c>System.Xml</c> use.
        /// </summary>
        SelfClosingWithSpace = 1,

        /// <summary>
        /// The element is written as a start tag and an end tag, <c>&lt;e&gt;&lt;/e&gt;</c>.
        /// </summary>
        StartAndEndTag = 2,

    }

}
