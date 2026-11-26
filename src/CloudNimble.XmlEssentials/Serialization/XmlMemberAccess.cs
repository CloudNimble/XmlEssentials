using System;
using System.Runtime.CompilerServices;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Calls the typed accessors of a member on an object the serializer holds as an <see cref="object"/>.
    /// </summary>
    internal static class XmlMemberAccess
    {

        #region Public Methods

        /// <summary>
        /// Returns the <see cref="XmlUnbox{TDeclaring}"/> a setter of <typeparamref name="TDeclaring"/> needs, and
        /// refuses a setter on a value type that has none.
        /// </summary>
        /// <typeparam name="TDeclaring">The type that declares the member.</typeparam>
        /// <param name="facts">The facts of the member.</param>
        /// <param name="hasSetter">Whether the member is set after the object is created.</param>
        /// <returns>The delegate, or null when <typeparamref name="TDeclaring"/> is a reference type or the member is not set.</returns>
        /// <exception cref="InvalidOperationException">The member is set, is declared by a value type, and its metadata does not say how to reach into a box.</exception>
        /// <remarks>
        /// A value type that is being read is in a box. Its setter changes the value in the box, which takes a
        /// method that is constrained to value types. Metadata supplies it through
        /// <see cref="XmlMetadataServices.CreateStructPropertyInfo"/>.
        /// </remarks>
        internal static XmlUnbox<TDeclaring>? RequireUnbox<TDeclaring>(XmlMemberFacts facts, bool hasSetter)
        {
            var unbox = (XmlUnbox<TDeclaring>?)facts.Unbox;
            if (typeof(TDeclaring).IsValueType && hasSetter && unbox is null)
            {
                throw new InvalidOperationException($"The metadata of member '{facts.Name}' on type '{facts.DeclaringType}' sets a member of a value type. Pass it to XmlMetadataServices.CreateStructPropertyInfo.");
            }

            return unbox;
        }

        /// <summary>
        /// Sets a member on <paramref name="instance"/>.
        /// </summary>
        /// <typeparam name="TDeclaring">The type that declares the member.</typeparam>
        /// <typeparam name="TValue">The type of the member.</typeparam>
        /// <param name="set">The setter.</param>
        /// <param name="unbox">Reaches the value inside a box, when <typeparamref name="TDeclaring"/> is a value type.</param>
        /// <param name="instance">The object, which is a box when <typeparamref name="TDeclaring"/> is a value type.</param>
        /// <param name="value">The value to set.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Set<TDeclaring, TValue>(XmlPropertySetter<TDeclaring, TValue> set, XmlUnbox<TDeclaring>? unbox, object instance, TValue value)
        {
            if (typeof(TDeclaring).IsValueType)
            {
                // The member is set in the box, so the object the serializer holds is the one that changes.
                set(ref unbox!(instance), value);
                return;
            }

            var typed = (TDeclaring)instance;
            set(ref typed, value);
        }

        #endregion

    }

}
