using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EXMExtension.Tools;

namespace EXMExtension.Models
{
    public class GenerateContactModel:BaseTaskWrapper
    {
        public bool isActive;

        public Task _task;

        public List<string> errorList;

        public bool success;

        public int currentProgressContact;

        public int currentProgressList;

        public int targetContact;

        public int targetList;

        public string [] currentInfo;

        public string [] targetInfo;

        public string title;

        public ContactOperations current;

        public GenerateContactModel()
        {
            isActive = false;

            _task = null;

            errorList = new List<string>();

            success = false;

            currentProgressContact = -1;

            currentProgressList = -1;

            targetContact = -1;

            targetList = -1;

            current = ContactOperations.Idle;

            currentInfo = new string[2];

            targetInfo = new string[2];
        }

        public void Reset()
        {
            isActive = false;

            _task = null;

            success = false;

            currentProgressContact = -1;

            currentProgressList = -1;

            targetContact = -1;

            targetList = -1;

            current = ContactOperations.Idle;

            currentInfo = new string[2];

            targetInfo = new string[2];
        }
    }
}
