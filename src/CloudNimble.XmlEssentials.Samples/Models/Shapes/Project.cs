namespace CloudNimble.XmlEssentials.Samples.Models.Shapes
{

    /// <summary>
    /// A project with two roles that the same employee can fill.
    /// </summary>
    public sealed class Project
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the project name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the employee who owns the project.
        /// </summary>
        public Employee? Owner { get; set; }

        /// <summary>
        /// Gets or sets the employee who reviews the project.
        /// </summary>
        public Employee? Reviewer { get; set; }

        #endregion

    }

}
