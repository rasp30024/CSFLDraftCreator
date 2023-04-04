using CSFLDraftCreator.Mapping;
using CsvHelper.Configuration.Attributes;
using System.Collections.Generic;

namespace CSFLDraftCreator.ConfigModels
{
    public class PositionalAttributesModel 
    {
        [Name("PositionName")]
        public string PositionName { get; set; } = string.Empty;

        [Name("PrimaryAttributes")]
        [TypeConverter(typeof(ToStringListConverter))]
        public List<string> PrimaryAttributes { get; set; } = new List<string>();

        [Name("SecondaryAttributes")]
        [TypeConverter(typeof(ToStringListConverter))] 
        public List<string> SecondaryAttributes { get; set; } = new List<string>();

    }
}
