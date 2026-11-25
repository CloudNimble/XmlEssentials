using System.Runtime.CompilerServices;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// The <see cref="XmlHeldBox{TValue}"/> of a value type.
    /// </summary>
    /// <typeparam name="TValue">The member type, a value type that is not <see cref="System.Nullable{T}"/>.</typeparam>
    internal sealed class XmlStructHeldBox<TValue> : XmlHeldBox<TValue>
        where TValue : struct
    {

        #region Private Methods

        /// <inheritdoc />
        internal override object Create()
        {
            return default(TValue);
        }

        /// <inheritdoc />
        internal override void Set(object box, TValue value)
        {
            Unsafe.Unbox<TValue>(box) = value;
        }

        #endregion

    }

}
