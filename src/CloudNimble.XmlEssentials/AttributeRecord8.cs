using System.Runtime.CompilerServices;

namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Eight inline attribute slots so typical start tags need no heap array.
    /// </summary>
    [InlineArray(8)]
    internal struct AttributeRecord8
    {

        #region Fields

        private AttributeRecord _element0;

        #endregion

    }

}
