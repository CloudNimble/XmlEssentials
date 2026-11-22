namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Default DoS limits applied when a max-* option is 0.
    /// </summary>
    /// <remarks>
    /// Every max-* except <see cref="DefaultMaxDocumentByteCount"/> treats 0 as “use this default”.
    /// <see cref="DefaultMaxDocumentByteCount"/> is 0, which stays unbounded.
    /// </remarks>
    internal static class XmlLimits
    {

        #region Fields

        /// <summary>
        /// Default stream and buffer growth size, in bytes.
        /// </summary>
        public const int DefaultBufferSize = 16_384;

        /// <summary>
        /// Default maximum attributes on one element, including xmlns*.
        /// </summary>
        public const int DefaultMaxAttributeCount = 1000;

        /// <summary>
        /// Default maximum comment payload, in UTF-8 bytes.
        /// </summary>
        public const int DefaultMaxCommentByteCount = 1_048_576;

        /// <summary>
        /// Default maximum element depth.
        /// </summary>
        public const int DefaultMaxDepth = 64;

        /// <summary>
        /// Default maximum document size. Zero means unbounded.
        /// </summary>
        public const int DefaultMaxDocumentByteCount = 0;

        /// <summary>
        /// Default maximum element or attribute name, in UTF-8 bytes.
        /// </summary>
        public const int DefaultMaxNameByteCount = 1024;

        /// <summary>
        /// Default maximum text or CDATA node, in UTF-8 bytes.
        /// </summary>
        public const int DefaultMaxTextByteCount = 16_777_216;

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns <paramref name="defaultValue"/> when <paramref name="value"/> is 0.
        /// </summary>
        /// <param name="value">The configured limit.</param>
        /// <param name="defaultValue">The table default.</param>
        /// <returns>The effective limit.</returns>
        /// <remarks>
        /// Do not use this for <see cref="DefaultMaxDocumentByteCount"/>. A document cap of 0 stays unbounded.
        /// </remarks>
        public static int Resolve(int value, int defaultValue)
        {
            return value == 0 ? defaultValue : value;
        }

        #endregion

    }

}
