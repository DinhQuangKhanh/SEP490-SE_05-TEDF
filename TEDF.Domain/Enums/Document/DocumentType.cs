using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TEDF.Domain.Enums.Document
{
    public enum DocumentType
    {
        Proposal = 0,
        Report = 1,
        Slide = 2,
        Code = 3,
        Diagram = 4,
        Minutes = 5,
        Reference = 6,
        Other = 99
    }
}
