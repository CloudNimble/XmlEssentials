using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// Holds one member of each kind of collection that is not a list or an array.
    /// </summary>
    public sealed class CollectionKinds
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets a queue.
        /// </summary>
        public Queue<int>? Queue { get; set; }

        /// <summary>
        /// Gets or sets a stack.
        /// </summary>
        public Stack<int>? Stack { get; set; }

        /// <summary>
        /// Gets or sets a linked list.
        /// </summary>
        public LinkedList<int>? Linked { get; set; }

        /// <summary>
        /// Gets or sets a set, declared as its interface.
        /// </summary>
        public ISet<int>? Set { get; set; }

        /// <summary>
        /// Gets or sets a read-only collection.
        /// </summary>
        public ReadOnlyCollection<int>? ReadOnly { get; set; }

        /// <summary>
        /// Gets or sets an immutable array.
        /// </summary>
        public ImmutableArray<int> Array { get; set; }

        /// <summary>
        /// Gets or sets an immutable list.
        /// </summary>
        public ImmutableList<int>? List { get; set; }

        /// <summary>
        /// Gets or sets an immutable dictionary.
        /// </summary>
        public ImmutableDictionary<string, int>? Map { get; set; }

        /// <summary>
        /// Gets or sets a dictionary, declared as its read-only interface.
        /// </summary>
        public IReadOnlyDictionary<string, int>? View { get; set; }

        #endregion

    }

}
