using System.Runtime.CompilerServices;

namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Eight inline writer frames so shallow documents need no heap element stack.
    /// </summary>
    [InlineArray(8)]
    internal struct XmlWriterFrame8
    {

        #region Fields

        private XmlWriterFrame _element0;

        #endregion

    }

}
