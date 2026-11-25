using System;
using System.Reflection;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// What the serializer has to know about the constructor it creates an object through: its parameters, and a
    /// way to call it.
    /// </summary>
    /// <remarks>
    /// The reflection resolver chooses a constructor and wraps it. A source-generated context names the constructor
    /// when the program is compiled and supplies a delegate that calls it, so nothing is invoked through reflection.
    /// </remarks>
    internal sealed class XmlConstructorFacts
    {

        #region Fields

        /// <summary>
        /// Creates the object. The array holds one argument for each parameter, in order, and may be longer.
        /// </summary>
        internal readonly Func<object?[], object> Invoke;

        /// <summary>
        /// The parameters, in the order they are declared.
        /// </summary>
        internal readonly XmlParameterFacts[] Parameters;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the facts of a constructor.
        /// </summary>
        /// <param name="parameters">The parameters, in the order they are declared.</param>
        /// <param name="invoke">Creates the object from an array that holds the arguments first.</param>
        internal XmlConstructorFacts(XmlParameterFacts[] parameters, Func<object?[], object> invoke)
        {
            Parameters = parameters;
            Invoke = invoke;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Wraps a reflected constructor.
        /// </summary>
        /// <param name="constructor">The constructor.</param>
        /// <param name="isIgnoredMember">Says whether a parameter belongs to a member that is never serialized.</param>
        /// <returns>The facts.</returns>
        internal static XmlConstructorFacts FromReflection(ConstructorInfo constructor, Func<string, bool> isIgnoredMember)
        {
            var reflected = constructor.GetParameters();
            var parameters = new XmlParameterFacts[reflected.Length];
            for (var i = 0; i < reflected.Length; i++)
            {
                var name = reflected[i].Name;
                parameters[i] = new XmlParameterFacts(
                    name,
                    reflected[i].ParameterType,
                    reflected[i].HasDefaultValue,
                    reflected[i].HasDefaultValue ? reflected[i].DefaultValue : null,
                    name is not null && isIgnoredMember(name))
                {
                    Parameter = reflected[i],
                };
            }

            var invoker = ConstructorInvoker.Create(constructor);
            var count = parameters.Length;
            return new XmlConstructorFacts(parameters, values => invoker.Invoke(values.AsSpan(0, count)));
        }

        #endregion

    }

}
