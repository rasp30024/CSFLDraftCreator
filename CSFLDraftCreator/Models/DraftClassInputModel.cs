using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSFLDraftCreator.Models
{
    internal class DraftClassInputModel
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string College { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public int Age { get; set; } = 0;
        public string Height { get; set; } = string.Empty;
        public int Weight { get; set; } = 0;
        public string Tier { get; set; } = string.Empty;
        public string Trait { get; set; } = string.Empty;
        public string Style { get; set; } = string.Empty;



    }
}
