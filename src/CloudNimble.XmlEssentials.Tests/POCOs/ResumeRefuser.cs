using System;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type whose setter refuses a value with an exception of its own.
    /// </summary>
    public sealed class ResumeRefuser
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the name. The name "no" is refused.
        /// </summary>
        public string? Name
        {
            get;
            set => field = value is "no" ? throw new InvalidOperationException("The name is refused.") : value;
        }

        #endregion

    }

}
