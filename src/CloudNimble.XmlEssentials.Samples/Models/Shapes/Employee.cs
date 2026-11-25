using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Shapes
{

    /// <summary>
    /// An employee who has a manager and direct reports, so a graph of employees contains cycles.
    /// </summary>
    public sealed class Employee
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the employee's name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the employee's manager, or <see langword="null"/> for the head of the organization.
        /// </summary>
        public Employee? Manager { get; set; }

        /// <summary>
        /// Gets or sets the employees who report to this one, written as <c>Employee</c> elements inside one
        /// <c>DirectReports</c> element.
        /// </summary>
        [XmlArray]
        public List<Employee> DirectReports { get; set; } = [];

        #endregion

    }

}
