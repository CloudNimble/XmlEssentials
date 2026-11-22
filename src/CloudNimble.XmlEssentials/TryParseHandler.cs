using System.Globalization;

namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Integer <c>TryParse</c> signature used by <see cref="Utf8XmlReader"/> typed getters.
    /// </summary>
    /// <typeparam name="T">
    /// The integer type being parsed.
    /// </typeparam>
    /// <param name="s">The lexical value.</param>
    /// <param name="styles">Number styles to apply.</param>
    /// <param name="provider">The format provider.</param>
    /// <param name="value">When this method returns, the parsed value.</param>
    /// <returns><see langword="true"/> if parsing succeeded.</returns>
    internal delegate bool TryParseHandler<T>(string s, NumberStyles styles, IFormatProvider provider, out T value);

}
