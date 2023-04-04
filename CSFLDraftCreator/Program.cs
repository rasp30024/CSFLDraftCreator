using CSFLDraftCreator.BusLogic;
using CSFLDraftCreator.ConfigModels;
using CSFLDraftCreator.Mapping;
using CSFLDraftCreator.Models;
using CsvHelper;
using Newtonsoft.Json;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSFLDraftCreator
{
    internal class Program
    {
        private static AppSettingsModel _settings;
        private static Logger _log = null;
        static void Main(string[] args)
        {
            try
            {
                //load our application settings
                if (!GetSettings())
                {
                    Console.WriteLine("Could not find or load the file Settings.json");
                    ExitProgram();
                }

                //Setup logging options 
                _log = new LoggerConfiguration()
                   .MinimumLevel.Debug()
                   .WriteTo.Console(outputTemplate: "{Message:lj}{NewLine}")
                   .WriteTo.File(_settings.AppLogLocation + "DraftNormalizer.log", rollingInterval: RollingInterval.Day)
                   .CreateLogger();

                if (!ValidateSettings())
                    ExitProgram();

                //start menu loop for prompting user with actions to take
                bool userAskedToExit = false;
                Console.WriteLine($"Welcome to CSFL DDSPF23 Draft Creator!");
                do
                {
                    Console.WriteLine();

                    //display the menu options... these should match the menu options below
                    char menuOption = GetMenuOption();

                    switch (menuOption)
                    {
                        case '1'://Convert draft class csv to draft json
                            ConvertDraftClass();
                            break;
                        case '2': //Get League Postion Summary
                            GetLeaguePositionSummary();
                            break;
                        case '3'://Convert Update Draft Json from CSV
                            ConvertDraftJSONFromCSV();
                            break;
                        case '4'://Convert Allplayer to csv
                            ConvertDraftJSONToCSV();
                            break;
                        case 'x': //exit
                            userAskedToExit = true;
                            break;
                        default:
                            Console.WriteLine($"Invalid menu item selected...{Environment.NewLine}");
                            break;
                    }

                } while (!userAskedToExit);

                ExitProgram();

            }
            catch
            {
                _log = null;
                Console.WriteLine("An unknown error occurred.");
                ExitProgram();
            }
        }

        #region Menu Methods
        private static void ConvertDraftClass()
        {
            try
            {
                //get input from user
                Console.WriteLine($"*** Just hit enter to use the defaults ***");

                string activePlayersCSV_Location = String.Empty;
                if (!_settings.UsePassedInPercentileChart)
                {
                    Console.WriteLine("Enter the location and filename of the League's active player CSV export");
                    activePlayersCSV_Location = GetUserInput_FileName("", true, _settings.ActivePlayerExportCSV_InputFile);
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine("Enter the location and filename of the Percentile Chart CSV export");
                    activePlayersCSV_Location = GetUserInput_FileName("", true, _settings.PercentileChartCSV_InputFile);
                    Console.WriteLine();
                }
                Console.WriteLine("Enter the location and filename of the Created Upcoming Draft CSV to import");
                string draftClassCSV_Location = GetUserInput_FileName("", true, _settings.DraftClassCSV_InputFile);
                Console.WriteLine();

                Console.WriteLine("Enter the location and filename of the Upcoming DDSPF Draft JSON file to create");
                string outputFile = GetUserInput_FileName("", false, _settings.DraftUpdateJSON_OutputFile);


                CSFLDraftCreatorActivity util = new CSFLDraftCreatorActivity(_log, _settings);
                util.ConvertDraftClass(outputFile, activePlayersCSV_Location, draftClassCSV_Location);


            }
            catch (Exception e)
            {
                _log.Error("Error running GetLeaguePositionSummary - " + e.Message);
            }
        }
        private static void ConvertDraftJSONFromCSV()
        {
            try
            {
                //get input from user
                Console.WriteLine($"*** Just hit enter to use the defaults ***");

                Console.WriteLine("Enter the location and filename of the Created Upcoming Draft CSV to import");
                string draftClassCSV_Location = GetUserInput_FileName("", true, _settings.DraftUpdateCSV_InputFile);
                Console.WriteLine();

                Console.WriteLine("Enter the location and filename of the Upcoming DDSPF Draft JSON file to create");
                string outputFile = GetUserInput_FileName("", false, _settings.DraftUpdateJSON_OutputFile);


                CSFLDraftCreatorActivity util = new CSFLDraftCreatorActivity(_log, _settings);
                util.ConvertDraftJSONFromCSV(outputFile, draftClassCSV_Location);


            }
            catch (Exception e)
            {
                _log.Error("Error running GetLeaguePositionSummary - " + e.Message);
            }
        }
        private static void ConvertDraftJSONToCSV()
        {
            try
            {
                //get input from user
                Console.WriteLine($"*** Just hit enter to use the defaults ***");

                Console.WriteLine("Enter the location and filename of the Upcoming Draft JSON to import");
                string upcomingDraftJson = GetUserInput_FileName("", true, _settings.UpcomingDraftJSON_InputFile);
                Console.WriteLine();

                Console.WriteLine("Enter the location and filename of the Upcoming Draft CSV file to output");
                string outputFile = GetUserInput_FileName("", false, _settings.UpcomingDraftCSV_OutputFile);

                CSFLDraftCreatorActivity util = new CSFLDraftCreatorActivity(_log, _settings);
                util.ConvertDraftJSONToCSV(upcomingDraftJson, outputFile);



            }
            catch (Exception e)
            {
                _log.Error("Error running GetLeaguePositionSummary - " + e.Message);
            }
        }
        private static void GetLeaguePositionSummary()
        {
            try
            {
                //get input from user
                Console.WriteLine($"*** Just hit enter to use the defaults ***");
                Console.WriteLine("Enter the location and filename of the League's active player CSV export");
                string activePlayersCSV_Location = GetUserInput_FileName("", true, _settings.ActivePlayerExportCSV_InputFile);
                Console.WriteLine();
                Console.WriteLine("Enter the location and filename of the Summary file to output");
                string outputFile = GetUserInput_FileName("", false, _settings.PlayerSummaryHTML_OutputFile);

                CSFLDraftCreatorActivity util = new CSFLDraftCreatorActivity(_log, _settings);
                util.GetLeaguePositionSummary(activePlayersCSV_Location, outputFile);

            }
            catch (Exception e)
            {
                _log.Error("Error running GetLeaguePositionSummary - " + e.Message);
            }
        }
        #endregion


        #region System Methods  
        private static void ExitProgram()
        {
            if (_log != null)
                _log.Information("Press any key to exit...");
            else
                Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            Environment.Exit(0);
        }
        private static char GetMenuOption()
        {
            try
            {
                List<char> validOptions = new List<char> { 'x', '1', '2', '3', '4' };
                bool userInputIsGood = true;
                char userInput = 'x';

                do
                {
                    if (!userInputIsGood)
                    {
                        Console.WriteLine();
                        Console.WriteLine();
                        Console.WriteLine($"Invalid input, please enter a valid selection.{Environment.NewLine}");
                    }
                    Console.WriteLine("Please select an option from the menu below");
                    Console.WriteLine();
                    Console.WriteLine("*** Draft Creator ***");
                    Console.WriteLine(" [1] Create draft file json from draft class csv");
                    Console.WriteLine();
                    Console.WriteLine("*** Utilities ***");
                    Console.WriteLine(" [2] Get League Player Summary");
                    Console.WriteLine(" [3] Update draft export from csv");
                    Console.WriteLine(" [4] Convert Draft export (JSON) from csv");
                    Console.WriteLine(" [x] Exit");
                    Console.WriteLine();
                    Console.Write("Choose option: ");

                    try
                    {

                        var inputKey = Console.ReadKey();
                        userInput = Char.ToLower(inputKey.KeyChar);

                        //string input = Console.ReadLine();
                        //userInput = char.Parse(input.Substring(0, 1).ToLower());
                        if (validOptions.Contains(userInput))
                            userInputIsGood = true;
                        else
                            userInputIsGood = false;
                    }
                    catch
                    {
                        //if we get an error we have an invalid number 
                        userInputIsGood = false;
                    }
                } while (!userInputIsGood);

                Console.WriteLine(Environment.NewLine);

                return userInput;
            }
            catch (Exception ex)
            {
                _log.Error($"GetMenuOption: {ex.Message}");
                return '*';
            }
        }
        private static bool GetSettings()
        {
            try
            {


                string settingsFile = File.ReadAllText("appsettings.json");
                _settings = JsonConvert.DeserializeObject<AppSettingsModel>(settingsFile);

                if (_settings == null)
                    return false;

                if (string.IsNullOrEmpty(_settings.AppLogLocation))
                    _settings.AppLogLocation = System.IO.Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);

                if (!Directory.Exists(_settings.AppLogLocation))
                {
                    try
                    {
                        Directory.CreateDirectory(_settings.AppLogLocation);
                    }
                    catch
                    {
                        _settings.AppLogLocation = System.IO.Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);
                    }
                }

                if (_settings.AppLogLocation.Substring(_settings.AppLogLocation.Length - 1, 1) != "\\")
                    _settings.AppLogLocation += "\\";

                //Load Tier Definitions
                if (!File.Exists("Tiers.csv"))
                {
                    Console.WriteLine("Failed to load the application, Cannot find Tiers.csv");
                }
                using (var reader = new StreamReader("Tiers.csv"))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    _settings.TierDefinitions = csv.GetRecords<TierModel>().ToList();
                }
                if (_settings.TierDefinitions == null || _settings.TierDefinitions.Count == 0)
                {
                    _log.Error($"Tiers.csv has an invalid format");
                    return false;
                }

                //Load Positional Attributes
                if (!File.Exists("PositionalAttributes.csv"))
                {
                    Console.WriteLine("Failed to load the application, Cannot find PositionalAttributes.csv");
                }
                using (var reader = new StreamReader("PositionalAttributes.csv"))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    _settings.PositionalAttributes = csv.GetRecords<PositionalAttributesModel>().ToList();
                }
                if (_settings.PositionalAttributes == null || _settings.PositionalAttributes.Count == 0)
                {
                    _log.Error($"PositionalAttributes.csv has an invalid format");
                    return false;
                }

                //Load Styles
                if (!File.Exists("Styles.csv"))
                {
                    Console.WriteLine("Failed to load the application, Cannot find Styles.csv");
                }
                using (var reader = new StreamReader("Styles.csv"))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    _settings.Styles = csv.GetRecords<StyleModel>().ToList();
                }
                if (_settings.Styles == null || _settings.Styles.Count == 0)
                {
                    _log.Error($"Styles.csv has an invalid format");
                    return false;
                }

                //Load Triats
                if (!File.Exists("Traits.csv"))
                {
                    Console.WriteLine("Failed to load the application, Cannot find Traits.csv");
                }
                using (var reader = new StreamReader("Traits.csv"))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    _settings.Traits = csv.GetRecords<TraitModel>().ToList();
                }
                if (_settings.Traits == null || _settings.Traits.Count == 0)
                {
                    _log.Error($"Traits.csv has an invalid format");
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error reading appsettings.json - " + e.Message);
                return false;
            }
        }
        private static bool ValidateSettings()
        {
            try
            {

                //Validate Tiers and update percentiles to 5% increments
                foreach (TierModel tier in _settings.TierDefinitions)
                {
                    tier.KeyAttributeMin = tier.KeyAttributeMin < 1 ? 1 : tier.KeyAttributeMin;
                    tier.KeyAttributeMax = tier.KeyAttributeMax > 100 ? 100 : tier.KeyAttributeMax;

                    tier.PriAttributeMin = tier.PriAttributeMin < 1 ? 1 : tier.PriAttributeMin;
                    tier.PriAttributeMax = tier.PriAttributeMax > 100 ? 100 : tier.PriAttributeMax;

                    tier.SecAttributeMin = tier.SecAttributeMin < 1 ? 1 : tier.SecAttributeMin;
                    tier.SecAttributeMax = tier.SecAttributeMax > 100 ? 100 : tier.SecAttributeMax;

                    tier.SkillMin = tier.SkillMin < 1 ? 1 : tier.SkillMin;
                    tier.SkillMax = tier.SkillMax > 100 ? 100 : tier.SkillMax;

                    if (tier.KeyAttributeMin > tier.KeyAttributeMax)
                    {
                        _log.Error($"Tier {tier.TierName} - KeyMin({tier.KeyAttributeMin}) cannot be larger than KeyMax({tier.KeyAttributeMax})");
                        return false;
                    }

                    if (tier.PriAttributeMin > tier.PriAttributeMax)
                    {
                        _log.Error($"Tier {tier.TierName} - PriMin({tier.PriAttributeMin}) cannot be larger than PriMax({tier.PriAttributeMax})");
                        return false;
                    }

                    if (tier.SecAttributeMin > tier.SecAttributeMax)
                    {
                        _log.Error($"Tier {tier.TierName} - SecMin({tier.SecAttributeMin}) cannot be larger than SecMax({tier.SecAttributeMax})");
                        return false;
                    }

                    //We only have every 5 percentiles defined so we will make sure
                    //  the settings are rounded to the nearest 5 to match
                    tier.KeyAttributeMin = 5 * (int)Math.Round(tier.KeyAttributeMin / 5.0);
                    tier.KeyAttributeMax = 5 * (int)Math.Round(tier.KeyAttributeMax / 5.0);
                    tier.PriAttributeMin = 5 * (int)Math.Round(tier.PriAttributeMin / 5.0);
                    tier.PriAttributeMax = 5 * (int)Math.Round(tier.PriAttributeMax / 5.0);
                    tier.SecAttributeMin = 5 * (int)Math.Round(tier.SecAttributeMin / 5.0);
                    tier.SecAttributeMax = 5 * (int)Math.Round(tier.SecAttributeMax / 5.0);
                }

                //Sort our Tiers
                _settings.TierDefinitions = _settings.TierDefinitions
                    .OrderBy(o => o.Order)
                    .ThenByDescending(o => o.KeyAttributeMax)
                    .ThenByDescending(o => o.KeyAttributeMin)
                    .ThenByDescending(o => o.PriAttributeMax)
                    .ThenByDescending(o => o.PriAttributeMin)
                    .ThenByDescending(o => o.SecAttributeMax)
                    .ThenByDescending(o => o.SecAttributeMin)
                    .ToList();

                //Validate and mark Attributes as Uppercase inside PostionalAttributes
                foreach (PositionalAttributesModel attributeModel in _settings.PositionalAttributes)
                {
                    for (int i = 0; i < attributeModel.PrimaryAttributes.Count; i++)
                    {
                        attributeModel.PrimaryAttributes[i] = attributeModel.PrimaryAttributes[i].ToUpper();
                        if (!Info.AttributeList.Contains(attributeModel.PrimaryAttributes[i]))
                        {
                            _log.Error($"PostionalAttributes for {attributeModel.PositionName} has an invalid PrimaryAttribute ({attributeModel.PrimaryAttributes[i]})");
                            return false;
                        }
                    }

                    for (int i = 0; i < attributeModel.SecondaryAttributes.Count; i++)
                    {
                        attributeModel.SecondaryAttributes[i] = attributeModel.SecondaryAttributes[i].ToUpper();
                        if (!Info.AttributeList.Contains(attributeModel.SecondaryAttributes[i]))
                        {
                            _log.Error($"PostionalAttributes for {attributeModel.PositionName} has an invalid SecondaryAttribute ({attributeModel.SecondaryAttributes[i]})");
                            return false;
                        }
                    }
                }

                //valdiate styles
                foreach (StyleModel styleModel in _settings.Styles)
                {
                    if (!Info.PositionList.Contains(styleModel.ApplyToPosition))
                    {
                        _log.Error($"Style {styleModel.StyleName} has an invalid ApplyToPosition value ({styleModel.ApplyToPosition})");
                        return false;

                    }

                    if (styleModel.KeyAttributes.Count > 0 && styleModel.KeyAttributes[0].ToLower() == "none")
                        styleModel.KeyAttributes.Clear();

                    for (int i = 0; i < styleModel.KeyAttributes.Count; i++)
                    {
                        styleModel.KeyAttributes[i] = styleModel.KeyAttributes[i].ToUpper();
                        if (!Info.AttributeList.Contains(styleModel.KeyAttributes[i]))
                        {
                            _log.Error($"Style {styleModel.StyleName} has an invalid KeyAttribute ({styleModel.KeyAttributes[i]})");
                            return false;
                        }
                    }
                    

                    if (styleModel.EnhancePersonality.Count > 0 && styleModel.EnhancePersonality[0].ToLower() == "none")
                        styleModel.EnhancePersonality.Clear();

                    for (int i = 0; i < styleModel.EnhancePersonality.Count; i++)
                    {
                        if (!Info.PersonalityList.Contains(styleModel.EnhancePersonality[i].ToUpper()))
                        {
                            _log.Error($"Style {styleModel.StyleName} has an invalid EnhancePersonality ({styleModel.EnhancePersonality[i]})");
                            return false;
                        }
                    }


                    if (styleModel.MufflePersonality.Count > 0 && styleModel.MufflePersonality[0].ToLower() == "none")
                        styleModel.MufflePersonality.Clear();

                    for (int i = 0; i < styleModel.MufflePersonality.Count; i++)
                    {
                        if (!Info.PersonalityList.Contains(styleModel.MufflePersonality[i]))
                        {
                            _log.Error($"Style {styleModel.StyleName} has an invalid MuffledPersonality ({styleModel.MufflePersonality[i]})");
                            return false;
                        }
                    }
                    

                    if (styleModel.AllowedPosTraits.Count > 0 && styleModel.AllowedPosTraits[0].ToLower() == "any")
                        styleModel.AllowedPosTraits.Clear();
                    
                    for (int i = 0; i < styleModel.AllowedPosTraits.Count; i++)
                    {
                        if (!Info.TraitList.Contains(styleModel.AllowedPosTraits[i]))
                        {
                            _log.Error($"Style {styleModel.StyleName} has an invalid Trait ({styleModel.AllowedPosTraits[i]})");
                            return false;
                        }
                    }

                }

                //validation for traits
                foreach (var trait in _settings.Traits)
                {
                    if (trait.Type.ToLower() != "personality" && trait.Type.ToLower() != "position")
                    {
                        _log.Error($"Style {trait.TraitName} has an invalid Typle ({trait.Type}).  This should be either personality or position.");
                        return false;
                    }

                    if (trait.AllowedPositions == null || trait.AllowedPositions.Count == 0 || trait.AllowedPositions[0].ToLower() == "any")
                        trait.AllowedPositions = new List<string>();

                    for (int index = 0; index < trait.AllowedPositions.Count; index++)
                    {
                        if (!Info.PositionList.Contains(trait.AllowedPositions[index].ToUpper()))
                        {
                            _log.Error($"Trait {trait.TraitName} has an invalid position({trait.AllowedPositions[index]}) defined in AllowedPosition");
                            return false;
                        }
                        trait.AllowedPositions[index] = trait.AllowedPositions[index].ToUpper();
                    }

                    if (string.IsNullOrEmpty(trait.GameTraitName) || !Info.TraitList.Contains(trait.GameTraitName))
                    {
                        _log.Error($"Style {trait.TraitName} has an invalid GameTraitName ({trait.GameTraitName}).");
                        return false;
                    }

                    if (trait.EnhanceAttributes == null || trait.EnhanceAttributes.Count == 0 || trait.EnhanceAttributes[0].ToLower() == "none")
                        trait.EnhanceAttributes = new List<string>();

                    foreach (var enhancedAtt in trait.EnhanceAttributes)
                    {
                        if (!Info.AttributeList.Contains(enhancedAtt.ToUpper()))
                        {
                            _log.Error($"Trait {trait.TraitName} has an invalid EnhanceAttribute({enhancedAtt})");
                            return false;
                        }
                    }

                    if (trait.MuffleAttributes == null || trait.MuffleAttributes.Count == 0 || trait.MuffleAttributes[0].ToLower() == "none")
                        trait.MuffleAttributes = new List<string>();

                    foreach (var muffleAtt in trait.MuffleAttributes)
                    {
                        if (!Info.AttributeList.Contains(muffleAtt.ToUpper()))
                        {
                            _log.Error($"Trait {trait.TraitName} has an invalid MuffleAttribute({muffleAtt})");
                            return false;
                        }
                    }

                    if (trait.EnhancePersonailities == null || trait.EnhancePersonailities.Count == 0 || trait.EnhancePersonailities[0].ToLower() == "none")
                        trait.EnhancePersonailities = new List<string>();

                    foreach (var enhancePer in trait.EnhancePersonailities)
                    {
                        if (!Info.PersonalityList.Contains(enhancePer.ToUpper()))
                        {
                            _log.Error($"Trait {trait.TraitName} has an invalid EnahancePersonality({enhancePer})");
                            return false;
                        }
                    }


                    if (trait.MufflePersonalities == null || trait.MufflePersonalities.Count == 0 || trait.MufflePersonalities[0].ToLower() == "none")
                        trait.MufflePersonalities = new List<string>();

                    foreach (var mufflePer in trait.MufflePersonalities)
                    {
                        if (!Info.PersonalityList.Contains(mufflePer.ToUpper()))
                        {
                            _log.Error($"Trait {trait.TraitName} has an invalid MufflePersonality({mufflePer})");
                            return false;
                        }
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                _log.Error("ValidateSettings - " + e.Message);
                return false;
            }
        }
        private static string GetUserInput_FileName(string message, bool checkForFile, string defaultValue)
        {
            try
            {
                string fileLocation = string.Empty;
                bool userInputIsGood = true;
                do
                {
                    if (!userInputIsGood)
                    {
                        Console.WriteLine($"{Environment.NewLine}The file location or name is not valid. Please try again.");
                    }

                    if (string.IsNullOrEmpty(message))
                        Console.Write($"(Default - {defaultValue}): ");
                    else
                        Console.Write($"{message} (Default - {defaultValue}): ");

                    fileLocation = Console.ReadLine();

                    if (string.IsNullOrEmpty(fileLocation))
                        fileLocation = defaultValue;

                    if (checkForFile && !File.Exists(fileLocation))
                    {
                        return string.Empty;
                    }
                    else if (!checkForFile || !string.IsNullOrWhiteSpace(fileLocation))
                    {
                        string filePath = fileLocation;
                        int indexOfLastPathTerminator = fileLocation.LastIndexOf('/');
                        if (indexOfLastPathTerminator >= 0)
                            filePath = fileLocation.Substring(0, indexOfLastPathTerminator);

                        if (indexOfLastPathTerminator >= 0 && !Directory.Exists(filePath))
                        {
                            return string.Empty;
                        }

                    }
                } while (!userInputIsGood);

                return fileLocation;
            }
            catch (Exception ex)
            {
                _log.Error($"GetUserInput_FileName: {ex.Message}");
                return String.Empty;
            }
        }

        #endregion
    }
}
