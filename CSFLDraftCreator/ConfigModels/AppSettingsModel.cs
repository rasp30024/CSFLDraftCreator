using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSFLDraftCreator.ConfigModels
{
    internal class AppSettingsModel
    {
        public bool UsePassedInPercentileChart { get; set; }
        public string AppLogLocation { get; set; } = string.Empty;
        public string PercentileChartCSV_InputFile { get; set; } = string.Empty;
        public string ActivePlayerExportCSV_InputFile { get; set; } = string.Empty;
        public string DraftClassCSV_InputFile { get; set; } = string.Empty;
        public string DraftUpdateJSON_OutputFile { get; set; } = string.Empty;
        public string PlayerSummaryHTML_OutputFile { get; set; } = string.Empty;
        public string DraftUpdateCSV_InputFile { get; set; } = string.Empty;
        public string UpcomingDraftJSON_InputFile { get; set; } = string.Empty;
        public string UpcomingDraftCSV_OutputFile { get; set; } = string.Empty;

        public int PosTraitPercentage { get; set; }
        public int PerTraitPercentage { get; set; }
        public int AddPersonalityTraitToPosTraitPercentage { get; set; }
        public bool UseStyles { get; set; }
        public int MinPersonalityEmphasis { get; set; }
        public int MaxPersonalityDeemphasis { get; set; }
        public int MinEnhanceAttrPercentage { get; set; }
        public int MaxMuffleAttrPercentage { get; set; }
        public int MaxAllowedForUnimportantSkills { get; set; }
        public int MaxSecondarySkill { get; set; }
        public int SecondarySkillChance { get; set; }

        public List<TierModel> TierDefinitions { get; set; } = new List<TierModel>();
        public List<PositionalAttributesModel> PositionalAttributes { get; set; } = new List<PositionalAttributesModel>();
        public List<StyleModel> Styles { get; set; } = new List<StyleModel>();
        public List<TraitModel> Traits { get; set; } = new List<TraitModel>();

    }
}
