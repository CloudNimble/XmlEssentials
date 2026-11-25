using System.Runtime.CompilerServices;

namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Eight inline xmlns slots so typical documents need no heap namespace stack.
    /// </summary>
    [InlineArray(8)]
    internal struct NamespaceBinding8
    {

        #region Fields

        private NamespaceBinding _element0;

        #endregion

    }

}
