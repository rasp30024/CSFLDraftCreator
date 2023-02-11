using System.Collections.Generic;

namespace CSFLDraftCreator.Models
{
    public class PostionalSkillsModel
    { 
        public string Position { get; set; } = string.Empty;
        public List<string> KeySkill { get; set; } = new List<string>();
        public List<string> SecondarySkill { get; set; } = new List<string>();

    }
}
