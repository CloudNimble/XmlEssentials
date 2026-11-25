using System.Collections.Generic;
using System.Collections.Immutable;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type whose members already hold values, of every kind that can and cannot be populated.
    /// </summary>
    public sealed class PopulateTarget
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets an array, which cannot be populated.
        /// </summary>
        [XmlArray]
        public int[] Array { get; set; } = [1];

        /// <summary>
        /// Gets or sets an object.
        /// </summary>
        public PopulateChild Child { get; set; } = new() { A = 7 };

        /// <summary>
        /// Gets a wrapped list.
        /// </summary>
        [XmlArray]
        public List<int> GetOnly { get; } = [1];

        /// <summary>
        /// Gets an object.
        /// </summary>
        public PopulateChild GetOnlyChild { get; } = new() { A = 7 };

        /// <summary>
        /// Gets a dictionary.
        /// </summary>
        public Dictionary<string, int> GetOnlyMap { get; } = new() { ["a"] = 1 };

        /// <summary>
        /// Gets a value type, which cannot be populated without a setter.
        /// </summary>
        public PopulatePoint GetOnlyPoint { get; } = new() { X = 7 };

        /// <summary>
        /// Gets or sets an immutable list, which cannot be populated.
        /// </summary>
        [XmlArray]
        public ImmutableList<int> Immutable { get; set; } = [1];

        /// <summary>
        /// Gets or sets an immutable dictionary, which cannot be populated.
        /// </summary>
        public ImmutableDictionary<string, int> ImmutableMap { get; set; } = ImmutableDictionary<string, int>.Empty.Add("a", 1);

        /// <summary>
        /// Gets a list written as repeated elements.
        /// </summary>
        [XmlElement("Loose")]
        public List<int> Loose { get; } = [1];

        /// <summary>
        /// Gets or sets a dictionary.
        /// </summary>
        public Dictionary<string, int> Map { get; set; } = new() { ["a"] = 1 };

        /// <summary>
        /// Gets or sets a nullable value type.
        /// </summary>
        public PopulatePoint? MaybePoint { get; set; } = new PopulatePoint { X = 7 };

        /// <summary>
        /// Gets a list that holds nothing.
        /// </summary>
        [XmlArray]
        public List<int>? NullGetOnly { get; }

        /// <summary>
        /// Gets or sets a list that holds nothing.
        /// </summary>
        [XmlArray]
        public List<int>? NullSettable { get; set; }

        /// <summary>
        /// Gets or sets a dictionary that is a value type, which cannot be populated.
        /// </summary>
        public StructMap StructMap { get; set; } = new() { ["a"] = 1 };

        /// <summary>
        /// Gets or sets a value type.
        /// </summary>
        public PopulatePoint Point { get; set; } = new() { X = 7 };

        /// <summary>
        /// Gets a queue.
        /// </summary>
        [XmlArray]
        public Queue<int> Queue { get; } = new([1]);

        /// <summary>
        /// Gets a set.
        /// </summary>
        [XmlArray]
        public HashSet<int> Set { get; } = [1];

        /// <summary>
        /// Gets or sets a wrapped list.
        /// </summary>
        [XmlArray]
        public List<int> Settable { get; set; } = [1];

        /// <summary>
        /// Gets a stack.
        /// </summary>
        [XmlArray]
        public Stack<int> Stack { get; } = new([1]);

        #endregion

    }

}
