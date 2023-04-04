using CSFLDraftCreator.Mapping;
using CsvHelper.Configuration.Attributes;
using System.Collections.Generic;

namespace CSFLDraftCreator.ConfigModels
{
    public class StyleModel 
    {
        [Name("StyleName")]
        public string StyleName { get; set; } = string.Empty;

        [Name("ApplyToPosition")]
        public string ApplyToPosition { get; set; }

        
        [Name("RandomWeight")]
        public int RandomWeight { get; set; } = 0;


        [Name("KeyAttributes")]
        [TypeConverter(typeof(ToStringListConverter))]
        public List<string> KeyAttributes { get; set; } = new List<string>();

        [Name("EnhancePersonalities")]
        [TypeConverter(typeof(ToStringListConverter))]
        public List<string> EnhancePersonality { get; set; } = new List<string>();

        [Name("MufflePersonalities")]
        [TypeConverter(typeof(ToStringListConverter))]
        public List<string> MufflePersonality { get; set; } = new List<string>();

        
        [Name("AllowedPositionalTraits")]
        [TypeConverter(typeof(ToStringListConverter))]
        public List<string> AllowedPosTraits { get; set; } = new List<string>();

    }
}
