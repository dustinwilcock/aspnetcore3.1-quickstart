using System;
using System.Collections.Generic;

namespace QuickStart.Models
{
    public class Teacher
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public School School { get; set; }

        public ICollection<Class> Classes { get; set; }
    }
}
