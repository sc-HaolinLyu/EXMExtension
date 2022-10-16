using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EXMExtension.Models
{
    public class TaskWrapper
    {
        public Task task;

        public GenerateContactModel model;

        public TaskWrapper()
        {
            task = null;

            model = new GenerateContactModel();
        }
    }
}
