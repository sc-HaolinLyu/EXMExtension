using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EXMExtension.Models
{
    public class BaseTaskWrapper
    {
        public Task Task;

        public List<string> ErrorList;

        public bool Success;

        public BaseTaskWrapper()
        {
            Task = null;
        }

        public virtual void Reset()
        {

        }
    }
}
