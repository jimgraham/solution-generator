using System;
using System.Collections.Generic;
using System.Text;

namespace SolGen
{
    /// <summary>
    /// Exception thrown when a circular reference is detected
    /// </summary>
    internal class CircularAssemblyReferenceException : Exception
    {
        private readonly IEnumerable<string> _assemblyStack;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="assemblyStack">stack of assembly dependencies</param>
        public CircularAssemblyReferenceException(IEnumerable<string> assemblyStack)
        {
            _assemblyStack = assemblyStack ?? new List<string>();
        }

        /// <summary>
        /// Gets a message that describes the current exception.
        /// </summary>
        /// <returns>
        /// The error message that explains the reason for the exception, or an empty string("").
        /// </returns>
        public override string Message
        {
            get { return FormatStack(); }
        }

        private string FormatStack()
        {
            var sb = new StringBuilder("Circular reference:\n");
            var level = 1;
            foreach (var assembly in _assemblyStack)
            {
                for (int i = 0; i < level; i++)
                {
                    sb.Append("\t");
                }
                sb.AppendLine(assembly);
                level++;
            }
            return sb.ToString();
        }
    }
}
