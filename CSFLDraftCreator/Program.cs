using CSFLDraftCreator.BusLogic;
using CSFLDraftCreator.Models;
using Newtonsoft.Json;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
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

                Console.WriteLine("Enter the location and filename of the League's active player CSV export");
                string activePlayersCSV_Location = GetUserInput_FileName("", true, _settings.ActivePlayerExportCSV_InputFile);
                Console.WriteLine();

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

                if (_settings.TierDefinitions == null)
                    return false;

                //We only have every 5 percentiles defined so we will make sure
                //  the settings are rounded to the nearest 5 to match
                foreach (var tier in _settings.TierDefinitions)
                {
                    tier.KeyMin = 5 * (int)Math.Round(tier.KeyMin / 5.0);
                    tier.KeyMax = 5 * (int)Math.Round(tier.KeyMax / 5.0);
                    tier.Skill = 5 * (int)Math.Round(tier.Skill / 5.0);
                    tier.WE = 5 * (int)Math.Round(tier.WE / 5.0);

                    //just to cover incorrect config
                    if (tier.KeyMax < tier.KeyMin)
                        tier.KeyMax = tier.KeyMin;
                }
                
                //Sort our Tiers
                _settings.TierDefinitions = _settings.TierDefinitions
                    .OrderByDescending(o => o.Order)
                    .ThenByDescending(o=> o.KeyMax)
                    .ThenByDescending(o => o.KeyMin)
                    .ToList();

                //make sure we are uppercase on skills
                foreach (PostionalSkillsModel skillModel in _settings.PostionalSkills)
                {
                    for (int i = 0; i < skillModel.KeySkill.Count; i++)
                    {
                        skillModel.KeySkill[i] = skillModel.KeySkill[i].ToUpper();
                    }
                    
                    for (int i = 0; i < skillModel.SecondarySkill.Count; i++)
                    {
                        skillModel.SecondarySkill[i] = skillModel.SecondarySkill[i].ToUpper();
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error reading appsettings.json - " + e.Message);
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
