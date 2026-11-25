using System.Runtime.CompilerServices;

namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Eight inline integers so shallow element stacks need no heap array.
    /// </summary>
    [InlineArray(8)]
    internal struct Int8Buffer
    {

        #region Fields

        private int _element0;

        #endregion

    }

}
