using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSFLDraftCreator.Models
{
    internal class AppSettingsModel
    {
        public string AppLogLocation { get; set; } = string.Empty;
        public string ActivePlayerExportCSV_InputFile { get; set; } = string.Empty;
        public string DraftClassCSV_InputFile { get; set; } = string.Empty;
        public string DraftUpdateJSON_OutputFile { get; set; } = string.Empty;
        public string PlayerSummaryHTML_OutputFile { get; set; } = string.Empty;
        public string DraftUpdateCSV_InputFile { get; set; } = string.Empty;
        public string UpcomingDraftJSON_InputFile { get; set; } = string.Empty;
        public string UpcomingDraftCSV_OutputFile { get; set; } = string.Empty;

        public int PositionalTagPercentage { get; set; }
        public int PersonalityTagPercetage { get; set; }
        public int AddSecondTagPercentage { get; set; } 

        public List<TierDefinitionModel> TierDefinitions { get; set; }
        public PositionTraitListModel PosTraits { get; set; }
        public List<PostionalSkillsModel> PostionalSkills { get; set; } = new List<PostionalSkillsModel>();

    }
}
