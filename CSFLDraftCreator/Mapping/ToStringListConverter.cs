using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSFLDraftCreator.Mapping
{
    public class ToStringListConverter : TypeConverter
    {
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            if (string.IsNullOrEmpty(text)) 
                return new List<int>();

            string[] allElements = text.Split(',');
            
            //trim all strings and add to list
            List<string> stringList = new List<string>();
            foreach (string element in allElements)
            {
                stringList.Add(element.Trim());
            }
            return stringList;
        }

        public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
        {
            return string.Join(",", ((List<string>)value).ToArray());
        }
    }
}
