using System.Collections.Generic;

namespace CSFLDraftCreator.Models
{
    internal class PositionTraitListModel
    {
        public string Position { get; set; } = string.Empty;
        public List<string> Traits { get; set; } = new List<string>();

    }
}
