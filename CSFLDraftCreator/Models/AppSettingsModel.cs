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
    }

    internal class PositionTraitListModel
    {
        public List<string> QB { get; set; } = new List<string>();
        public List<string> RB { get; set; } = new List<string>();
        public List<string> FB { get; set; } = new List<string>();
        public List<string> C { get; set; } = new List<string>();
        public List<string> G { get; set; } = new List<string>();
        public List<string> T { get; set; } = new List<string>();
        public List<string> TE { get; set; } = new List<string>();
        public List<string> WR { get; set; } = new List<string>();
        public List<string> CB { get; set; } = new List<string>();
        public List<string> LB { get; set; } = new List<string>();
        public List<string> DE { get; set; } = new List<string>();
        public List<string> DT { get; set; } = new List<string>();
        public List<string> FS { get; set; } = new List<string>();
        public List<string> SS { get; set; } = new List<string>();
        public List<string> K { get; set; } = new List<string>();
        public List<string> P { get; set; } = new List<string>();
        public List<string> Personality { get; set; } = new List<string>(); 

    }
}
