using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace puck.core.Exceptions
{
    public class NodeNameExistsException : Exception
    {
        public NodeNameExistsException() { }
        public NodeNameExistsException(string msg) : base(msg) { }
        public NodeNameExistsException(string msg, Exception inner) : base(msg, inner) { }
    }
    public class NoParentExistsException : Exception
    {
        public NoParentExistsException() { }
        public NoParentExistsException(string msg) : base(msg) { }
        public NoParentExistsException(string msg, Exception inner) : base(msg, inner) { }
    }
}
