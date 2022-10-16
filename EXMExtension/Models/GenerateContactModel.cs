using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EXMExtension.Models
{
    public class GenerateContactModel
    {
        public bool isActive;

        public Task _task;

        public List<string> errorList;

        public bool success;

        public int currentProgress;

        public int target;

        public GenerateContactModel()
        {
            isActive = false;

            _task = null;

            errorList = new List<string>();

            success = false;

            currentProgress = -1;

            target = -1;
        }

        public void Reset()
        {
            isActive = false;

            _task = null;

            errorList = new List<string>();

            success = false;

            currentProgress = -1;

            target = -1;
        }
    }
}
