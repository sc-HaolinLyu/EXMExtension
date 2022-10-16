using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;




namespace EXMExtension.Models
{
    public class ContactFormModel
    {
        [Required]
        [Range(1,10000)]
        public int contactNumber;

        [Range(1,5)]
        public int listNumber;
    }
}
