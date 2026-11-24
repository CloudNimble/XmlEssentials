using CloudNimble.Text.Xml;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// A <c>TryGet</c> method of <see cref="XmlElement"/>, such as <see cref="XmlElement.TryGetInt32"/>, as a
    /// value a test can pass.
    /// </summary>
    /// <typeparam name="T">The type the method reads.</typeparam>
    /// <param name="element">The element to read.</param>
    /// <param name="value">When the method returns, the value that was read, or the default value.</param>
    /// <returns><see langword="true"/> if the value was read.</returns>
    internal delegate bool TryRead<T>(XmlElement element, out T value);

}
