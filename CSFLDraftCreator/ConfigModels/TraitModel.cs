using CSFLDraftCreator.Mapping;
using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSFLDraftCreator.ConfigModels
{
    public class TraitModel
    {
        [Name("TraitName")]
        public string TraitName { get; set; }

        [Name("RandomWeight")]
        public int RandomWeight { get; set; }

        [Name("Type")]
        public string Type { get; set; }

        [Name("AllowedPositions")]
        [TypeConverter(typeof(ToStringListConverter))]
        public List<string> AllowedPositions { get; set; } = new List<string>();

        [Name("GameTraitName")]
        public string GameTraitName { get; set; }

        [Name("EnhanceAttributes")]
        [TypeConverter(typeof(ToStringListConverter))]
        public List<string> EnhanceAttributes { get; set; }

        [Name("MuffleAttributes")]
        [TypeConverter(typeof(ToStringListConverter))]
        public List<string> MuffleAttributes { get; set; }

        [Name("EnhancePersonailities")]
        [TypeConverter(typeof(ToStringListConverter))]
        public List<string> EnhancePersonailities { get; set; }

        [Name("MufflePersonalities")]
        [TypeConverter(typeof(ToStringListConverter))]
        public List<string> MufflePersonalities { get; set; }

    }
}

