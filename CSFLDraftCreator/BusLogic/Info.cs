using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSFLDraftCreator.BusLogic
{
    public static class Info
    {
        private static List<string> _attributeListCaseSensitive = new List<string> { "Str", "Agi", "Arm", "Spe", "Han", "Intel", "Acc", "PBl", "RBl", "Tck", "KDi", "KAc", "End" };
        private static List<String> _attributeList = new List<string>() { "STR", "AGI", "ARM", "SPE", "HAN", "INTEL", "ACC", "PBL", "RBL", "TCK", "KDI", "KAC", "END" };
        private static List<String> _personalityListCaseSensitive = new List<string>() { "Lea", "Wor", "Com", "TmPl", "Spor", "Soc", "Mny", "Sec", "Loy", "Win", "PT", "Home", "Mkt", "Mor" };
        private static List<String> _personalityList = new List<string>() { "LEA", "WOR", "COM", "TMPL", "SPOR", "SOC", "MNY", "SEC", "LOY", "WIN", "PT", "HOME", "MKT", "MOR" };
        private static List<String> _postionList = new List<string>() { "QB", "RB", "FB", "G", "T", "C", "TE", "WR", "CB", "LB", "DT", "DE", "FS", "SS", "K", "P" };
        private static List<String> _traitList = new List<string>()
                {
                    "Athlete", "BadInfluence", "CommunityBenefactor", "Competitor", "ConsummatePro", "Distraction", "Diva", "FanFavorite",
                    "InjuryProne", "Journeyman", "LockerLeader", "FilmGeek", "MediaDarling", "Perceptive", "ProBloodline",
                    "RawTalent", "RoleModel", "TeamPlayer", "WorkoutFanatic", "AllPurposeRB", "PowerRunner", "ScatBack", "ClutchQB",
                    "Dualthreat", "GameManager", "Gunslinger", "RunningQB", "BlockingFB", "PowerRunner", "ReceiveFB", "AthBlocker",
                    "TenaciousBlocker", "AthBlocker", "BookEndTackle", "BlockingTE", "ReceivingTE", "DeepThreat", "PossessionWR",
                    "SlotReceiver", "PressCorner", "ShutDownCorner", "SlotCorner", "ZoneCorner", "CoverageLB", "HybridLB", "Thumper",
                    "BullRusher", "SpeedRusher", "NoseTackle", "BoxSafety", "Centerfielder", "ClutchKicker", "PowerKicker"
                };
        public static List<String> AttributeList { get { return _attributeList; }}
        public static List<String> AttributeListCaseSensitive { get { return _attributeListCaseSensitive; } }
        public static List<String> PersonalityList { get { return _personalityList; } }
        public static List<String> PositionList { get { return _postionList; } }
        public static List<String> TraitList { get { return _traitList; } }

    }
}
