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
        public bool IsActive;

        public int CurrentProgressContact;

        public int CurrentProgressList;

        public int TargetContact;

        public int TargetList;

        public string [] CurrentInfo;

        public string [] TargetInfo;

        public string Title;

        public ContactOperations Current;

        public GenerateContactModel()
        {
            IsActive = false;

            Task = null;

            ErrorList = new List<string>();

            Success = false;

            CurrentProgressContact = -1;

            CurrentProgressList = -1;

            TargetContact = -1;

            TargetList = -1;

            Current = ContactOperations.Idle;

            CurrentInfo = new string[2];

            TargetInfo = new string[2];
        }

        public override void Reset()
        {
            IsActive = false;

            Task = null;

            Success = false;

            CurrentProgressContact = -1;

            CurrentProgressList = -1;

            TargetContact = -1;

            TargetList = -1;

            Current = ContactOperations.Idle;

            CurrentInfo = new string[2];

            TargetInfo = new string[2];
        }
    }
}
