using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SolGen
{
    internal class CircularReferenceException : Exception
    {
        public CircularReferenceException()
        {
        }

        public CircularReferenceException(string message)
            : base(message)
        {
        }

        public CircularReferenceException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
