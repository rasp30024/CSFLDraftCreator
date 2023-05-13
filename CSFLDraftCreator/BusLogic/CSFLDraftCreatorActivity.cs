using AutoMapper;
using CSFLDraftCreator.ConfigModels;
using CSFLDraftCreator.Models;
using CsvHelper;
using CsvHelper.Configuration;
using Newtonsoft.Json;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CSFLDraftCreator.BusLogic
{
    internal class CSFLDraftCreatorActivity
    {
        private Logger _log;
        private AppSettingsModel _settings;
        private Random _rnd = new Random(Guid.NewGuid().GetHashCode());
        public CSFLDraftCreatorActivity(Logger log, AppSettingsModel settings)
        {
            _log = log;
            _settings = settings;
        }

        #region public
        
        public void ConvertDraftClass(string outputFile, string percentileCSV_Location, string draftClassCSV_Location)
        {
            try
            {
                _log.Information("Starting Draft Class Conversion");

                if (string.IsNullOrEmpty(outputFile) || string.IsNullOrEmpty(percentileCSV_Location) || string.IsNullOrEmpty(draftClassCSV_Location))
                {
                    _log.Error("Cannot convert the draft class as an input is incorrect");
                    return;
                }

                _log.Information("Getting leagues position and attribute percentile break downs");
                Dictionary<string, List<PlayerAttributesModel>> posPercentileTiers = null;
                if (!_settings.UsePassedInPercentileChart)
                {
                    posPercentileTiers = GetPercentileDictionaryFromActivePlayers(percentileCSV_Location);
                }
                else
                {
                    posPercentileTiers = GetPercentileDictionaryFromPercentileChart(percentileCSV_Location);
                }
                
                if (posPercentileTiers == null || posPercentileTiers.Count() == 0)
                {
                    _log.Error("Cannot get active player percentile calculations");
                    return;
                }


                _log.Information($"Reading in draft class file from {draftClassCSV_Location}");
                List<DraftClassInputModel> drafteeCSVData;
                using (var reader = new StreamReader(draftClassCSV_Location))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    drafteeCSVData = csv.GetRecords<DraftClassInputModel>().ToList();
                }

                if (drafteeCSVData == null || drafteeCSVData.Count == 0)
                {
                    _log.Error("Cannot read draft class file");
                    return;
                }

                _log.Information("Generating player attributes");

                var configInput = new MapperConfiguration(cfg => cfg.CreateMap<DraftClassInputModel, AuditReporCSVModel>()
                    .ForMember(dest=> dest.InputFirstName, act => act.MapFrom(src => src.FirstName))
                    .ForMember(dest => dest.InputLastName, act => act.MapFrom(src => src.LastName))
                    .ForMember(dest => dest.InputCollege, act => act.MapFrom(src => src.College))
                    .ForMember(dest => dest.InputPosition, act => act.MapFrom(src => src.Position))
                    .ForMember(dest => dest.InputAge, act => act.MapFrom(src => src.Age))
                    .ForMember(dest => dest.InputWeight, act => act.MapFrom(src => src.Weight))
                    .ForMember(dest => dest.InputTier, act => act.MapFrom(src => src.Tier))
                    .ForMember(dest => dest.InputTrait, act => act.MapFrom(src => src.Trait))
                    .ForMember(dest => dest.InputStyle, act => act.MapFrom(src => src.Style))
                );
                var mapperInput = configInput.CreateMapper();

                var configPlayer = new MapperConfiguration(cfg => cfg.CreateMap<PlayerModel, AuditReporCSVModel>());
                var mapperPlayer = configPlayer.CreateMapper();

                var configAttr = new MapperConfiguration(cfg => cfg.CreateMap<PlayerAttributesModel, AuditReporCSVModel>());
                var mapperAttr = configAttr.CreateMapper();

                var configPer = new MapperConfiguration(cfg => cfg.CreateMap<PlayerPersonalitiesModel, AuditReporCSVModel>());
                var mapperPer = configPer.CreateMapper();

                var configSkills = new MapperConfiguration(cfg => cfg.CreateMap<PlayerSkillsModel, AuditReporCSVModel>());
                var mapperSkills = configSkills.CreateMapper();



                AllPlayersModel drafeeExportList = new AllPlayersModel();
                List<AuditReporCSVModel> auditReporCSVModels = new List<AuditReporCSVModel>();
                drafeeExportList.Players = new List<PlayerModel>();
                foreach (var draftee in drafteeCSVData)
                {

                    //Capture input information
                    AuditReporCSVModel auditPlayerInfo = mapperInput.Map<AuditReporCSVModel>(draftee);

                    if (draftee.FirstName == "Generic" && draftee.LastName == "Rotation")
                        Console.WriteLine("here");

                    int height = ConvertHeight(draftee.Height);

                    //Copy Base info from draft list csv to new draft record
                    PlayerModel drafteeExport = new PlayerModel();
                    drafteeExport.First = draftee.FirstName;
                    drafteeExport.Last = draftee.LastName;
                    drafteeExport.Pos = GetPosition(draftee.Position);
                    drafteeExport.Age = draftee.Age;
                    drafteeExport.Hgt = height;
                    drafteeExport.Wgt = draftee.Weight;
                    drafteeExport.Coll = draftee.College;

                    if (string.IsNullOrEmpty(drafteeExport.Pos))
                    {
                        _log.Error($"Invalid position({draftee.Position}) for {draftee.FirstName} {draftee.LastName}");
                        return;
                    }

                    //validate and assign traits
                    string addedTraits = string.Empty;
                    drafteeExport.Trait = GetTraits(draftee, out addedTraits);
                    if (drafteeExport.Trait == null)
                    {
                        return;
                    }
                    auditPlayerInfo.AddedTrait = addedTraits;

                    //validate and assign styles
                    if (_settings.UseStyles)
                    {
                        string style = GetStyle(draftee, draftee.Style);
                        if (style == null)
                        {
                            return;
                        }

                        if (draftee.Style != style)
                        {
                            auditPlayerInfo.AddedStyle = style;
                            draftee.Style = style;
                        }
                    }


                    drafteeExport = FixBlankInfo(drafteeExport);

                    drafteeExport.Per = RandomizePersonality();
                    drafteeExport.Skills = RandomizeSecondarySkills(draftee.Position);
                    drafteeExport = RandomizeAttributes(drafteeExport, draftee.Tier, draftee.Style, posPercentileTiers);

                    if (drafteeExport == null || drafteeExport.Per == null || drafteeExport.Attr == null || drafteeExport.Attr == null)
                    {
                        _log.Error($"Something when wrong processing {draftee.FirstName} {draftee.LastName} - {draftee.Position}");
                        return;
                    }

                    //drafteeExport = SetupPlayerTags(drafteeExport, posPercentileTiers, draftee);
                    //if (drafteeExport == null)
                    //{
                    //    _log.Error("ConvertUpcomingDraftCSVToJSON - cannot set players attributes");
                    //    return;
                    //}

                    drafteeExport = AdjustPersonalityProfile(drafteeExport, draftee);

                    drafeeExportList.Players.Add(drafteeExport);

                    //add the final result
                    mapperPlayer.Map(drafteeExport, auditPlayerInfo);
                    mapperAttr.Map(drafteeExport.Attr, auditPlayerInfo);
                    mapperPer.Map(drafteeExport.Per, auditPlayerInfo);
                    mapperSkills.Map(drafteeExport.Skills, auditPlayerInfo);

                    auditReporCSVModels.Add(auditPlayerInfo);
                }

                if (drafeeExportList == null || drafeeExportList.Players == null || drafeeExportList.Players.Count == 0)
                {
                    _log.Error("Unable to create Draft Class Conversion");
                    return;
                }


                string csvFilename = outputFile;
                if (csvFilename.ToLower().Contains(".json"))
                    csvFilename = csvFilename.Replace(".json", ".csv");
                else if (!csvFilename.ToLower().Contains(".csv"))
                    csvFilename += ".csv";

                string jsonFilename = outputFile;
                if (jsonFilename.ToLower().Contains(".csv"))
                    jsonFilename = jsonFilename.Replace(".csv", ".json");
                else if (!jsonFilename.ToLower().Contains(".json"))
                    jsonFilename += ".json";

                string csvAuditFilename = outputFile;
                if (csvAuditFilename.ToLower().Contains(".json"))
                    csvAuditFilename = csvAuditFilename.Replace(".json", "_audit.csv");
                else if (csvAuditFilename.ToLower().Contains(".csv"))
                    csvAuditFilename = csvAuditFilename.Replace(".csv", "_audit.csv");
                else if (!csvAuditFilename.ToLower().Contains(".csv"))
                    csvAuditFilename += "_audit.csv";


                string playerJSONData = JsonConvert.SerializeObject(drafeeExportList);
                File.WriteAllText(jsonFilename, playerJSONData);

                List<UpcomingDraftPlayerCSVModel> playerCSVData = ConvertAllPlayersModelToUpcomingDraftList(drafeeExportList);
                using (var writer = new StreamWriter(csvFilename))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(playerCSVData);
                }

                using (var writer = new StreamWriter(csvAuditFilename))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(auditReporCSVModels);
                }

                _log.Information("Draft Class Conversion Completed...");
            }
            catch (Exception e)
            {
                _log.Error("ConvertDraftClass - " + e.Message);
            }
        }
        public void ConvertDraftJSONFromCSV(string outputFile, string draftClassCSV_Location)
        {
            try
            {
                _log.Information($"Converting draft CSV ({draftClassCSV_Location}) to draft JSON ({outputFile})");

                List<UpcomingDraftPlayerCSVModel> drafteeCSVData = null;
                using (var reader = new StreamReader(draftClassCSV_Location))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    drafteeCSVData = csv.GetRecords<UpcomingDraftPlayerCSVModel>().ToList();
                }

                AllPlayersModel allPlayers = ConvertUpcomingDraftListToAllPlayersModel(drafteeCSVData);

                string playerJSONData = JsonConvert.SerializeObject(allPlayers);
                File.WriteAllText(outputFile, playerJSONData);

                _log.Information($"Json file create at {outputFile}");
            }
            catch (Exception e)
            {
                _log.Error("Cannot create file - " + e.Message);
                return;
            }
        }
        public void ConvertDraftJSONToCSV(string UpcomingDraftJson, string outputFile)
        {
            try
            {
                //Load JSON into our object
                string upcomingDraftData = File.ReadAllText(UpcomingDraftJson);
                AllPlayersModel upcomingDraftJSON = JsonConvert.DeserializeObject<AllPlayersModel>(upcomingDraftData);
                if (upcomingDraftJSON == null || upcomingDraftJSON.Players == null || upcomingDraftJSON.Players.Count == 0)
                {
                    _log.Error("Error running ConvertUpcomingDraftJSONToCSV - cannot convert JSON to an object");
                    return;
                }

                //Create automap configurations
                var configBase = new MapperConfiguration(cfg => cfg.CreateMap<PlayerModel, UpcomingDraftPlayerCSVModel>());
                var mapperBase = configBase.CreateMapper();

                var configAttr = new MapperConfiguration(cfg => cfg.CreateMap<PlayerAttributesModel, UpcomingDraftPlayerCSVModel>());
                var mapperAttr = configAttr.CreateMapper();

                var configPer = new MapperConfiguration(cfg => cfg.CreateMap<PlayerPersonalitiesModel, UpcomingDraftPlayerCSVModel>());
                var mapperPer = configPer.CreateMapper();

                var configSkills = new MapperConfiguration(cfg => cfg.CreateMap<PlayerSkillsModel, UpcomingDraftPlayerCSVModel>());
                var mapperSkills = configSkills.CreateMapper();


                List<UpcomingDraftPlayerCSVModel> upcomingDraftCSV = new List<UpcomingDraftPlayerCSVModel>();
                foreach (PlayerModel playerJSON in upcomingDraftJSON.Players)
                {

                    //Copy Base info
                    UpcomingDraftPlayerCSVModel playerCSV = mapperBase.Map<UpcomingDraftPlayerCSVModel>(playerJSON);
                    //Copy Attributes
                    playerCSV = mapperAttr.Map(playerJSON.Attr, playerCSV);
                    //Copy Personalities
                    playerCSV = mapperPer.Map(playerJSON.Per, playerCSV);
                    //Copy Skills
                    playerCSV = mapperSkills.Map(playerJSON.Skills, playerCSV);

                    upcomingDraftCSV.Add(playerCSV);
                }


                using (var writer = new StreamWriter(outputFile)) //cfaplayers.csv
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(upcomingDraftCSV);
                }


            }
            catch (Exception e)
            {
                _log.Error("Error running ConvertUpcomingDraftJSONToCSV - " + e.Message);
                return;
            }
        }
        public void GetLeaguePositionSummary(string activePlayersCSV_Location, string outputFile)
        {
            try
            {
                _log.Information("Creating percentile breakout for all positions");
                if (string.IsNullOrEmpty(outputFile) || string.IsNullOrEmpty(activePlayersCSV_Location))
                {
                    _log.Error("Cannot convert the draft class as an input is incorrect");
                    return;
                }

                _log.Information("Getting leagues position and attribute percentile break downs");
                Dictionary<string, List<PlayerAttributesModel>> posPercentileTiers = GetPercentileDictionaryFromActivePlayers(activePlayersCSV_Location);
                if (posPercentileTiers == null || posPercentileTiers.Count() == 0)
                {
                    _log.Error("Cannot get active player percentile calculations");
                    return;
                }

                List<string> outputTokens = outputFile.Split('.').ToList();
                if (outputTokens == null || outputTokens.Count < 2)
                {
                    _log.Error($"Output File Name ({outputFile}) is invalid");
                }

                string outputFileHTML = outputFile;
                string htmlOutputData = FormatAllPlayerSummaryForHTMLOutput(posPercentileTiers);
                File.WriteAllText(outputFileHTML, htmlOutputData);

                outputTokens.RemoveAt(outputTokens.Count - 1);
                outputTokens.Add("csv");
                string outputFileCSV = string.Join(".", outputTokens);
                List<PercentileChartModel> csvOutputData = FormatAllPlayerSummaryForCSVOutput(posPercentileTiers);
                
                using (var writer = new StreamWriter(outputFileCSV)) 
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(csvOutputData);
                }

                _log.Information($"File created: {outputFile}");

            }
            catch (Exception e)
            {
                _log.Error("Error running GetLeaguePositionSummary - " + e.Message);
            }
        }
        #endregion

        #region private
        private PlayerModel AdjustPersonalityProfile(PlayerModel player, DraftClassInputModel playerCSV)
        {
            try
            {
                if (string.IsNullOrEmpty(playerCSV.Style))
                {
                    return player;
                }

                StyleModel style = _settings.Styles.Where(s => s.StyleName.ToLower() == playerCSV.Style.ToLower()).FirstOrDefault();
                if (style == null)
                {
                    _log.Error($"Could not find position {player.Pos} or style ({playerCSV.Style}) defined in PostionalSkills");
                    return null;
                }
                List<string> enhancePersonality = style.EnhancePersonality;
                List<string> mufflePersonality = style.MufflePersonality;

                if (!string.IsNullOrEmpty(playerCSV.Trait))
                {
                    List<string> traitsInPlayer = playerCSV.Trait.Split('|').ToList();
                    List<TraitModel> traitModels = _settings.Traits.Where(t => traitsInPlayer.Contains(t.TraitName)).ToList();
                    foreach (TraitModel trait in traitModels)
                    {
                        enhancePersonality.AddRange(trait.EnhancePersonailities);
                        mufflePersonality.AddRange(trait.MufflePersonalities);
                    }
                }

                //remove duplicates
                enhancePersonality = enhancePersonality.Distinct().ToList();

                //Enhance personalities
                int minValue = _settings.MinPersonalityEmphasis < 1 || _settings.MinPersonalityEmphasis > 99 ? 60 : _settings.MinPersonalityEmphasis;

                foreach (string personality in enhancePersonality)
                {
                    int newValue = _rnd.Next(minValue, 101);

                    switch (personality.ToLower())
                    {
                        case "lea":
                            if (player.Per.Lea < newValue)
                                player.Per.Lea = newValue;
                            break;
                        case "wor":
                            if (player.Per.Wor < newValue)
                                player.Per.Wor = newValue;
                            break;
                        case "com":
                            if (player.Per.Com < newValue)
                                player.Per.Com = newValue;
                            break;
                        case "tmpl":
                            if (player.Per.TmPl < newValue)
                                player.Per.TmPl = newValue;
                            break;
                        case "spor":
                            if (player.Per.Spor < newValue)
                                player.Per.Spor = newValue;
                            break;
                        case "soc":
                            if (player.Per.Soc < newValue)
                                player.Per.Soc = newValue;
                            break;
                        case "mny":
                            if (player.Per.Mny < newValue)
                                player.Per.Mny = newValue;
                            break;
                        case "sec":
                            if (player.Per.Sec < newValue)
                                player.Per.Sec = newValue;
                            break;
                        case "loy":
                            if (player.Per.Loy < newValue)
                                player.Per.Loy = newValue;
                            break;
                        case "win":
                            if (player.Per.Win < newValue)
                                player.Per.Win = newValue;
                            break;
                        case "pt":
                            if (player.Per.PT < newValue)
                                player.Per.PT = newValue;
                            break;
                        case "home":
                            if (player.Per.Home < newValue)
                                player.Per.Home = newValue;
                            break;
                        case "mkt":
                            if (player.Per.Mkt < newValue)
                                player.Per.Mkt = newValue;
                            break;
                        case "mor":
                            if (player.Per.Mor < newValue)
                                player.Per.Mor = newValue;
                            break;
                    }
                }



                //Muffle personalities
                int maxValue = _settings.MaxPersonalityDeemphasis < 1 || _settings.MaxPersonalityDeemphasis > 99 ? 40 : _settings.MaxPersonalityDeemphasis;
                foreach (string personality in mufflePersonality)
                {
                    int newValue = _rnd.Next(0, maxValue) + 1;

                    switch (personality.ToLower())
                    {
                        case "lea":
                            if (player.Per.Lea > newValue)
                                player.Per.Lea = newValue;
                            break;
                        case "wor":
                            if (player.Per.Wor > newValue)
                                player.Per.Wor = newValue;
                            break;
                        case "com":
                            if (player.Per.Com > newValue)
                                player.Per.Com = newValue;
                            break;
                        case "tmpl":
                            if (player.Per.TmPl > newValue)
                                player.Per.TmPl = newValue;
                            break;
                        case "spor":
                            if (player.Per.Spor > newValue)
                                player.Per.Spor = newValue;
                            break;
                        case "soc":
                            if (player.Per.Soc > newValue)
                                player.Per.Soc = newValue;
                            break;
                        case "mny":
                            if (player.Per.Mny > newValue)
                                player.Per.Mny = newValue;
                            break;
                        case "sec":
                            if (player.Per.Sec > newValue)
                                player.Per.Sec = newValue;
                            break;
                        case "loy":
                            if (player.Per.Loy > newValue)
                                player.Per.Loy = newValue;
                            break;
                        case "win":
                            if (player.Per.Win > newValue)
                                player.Per.Win = newValue;
                            break;
                        case "pt":
                            if (player.Per.PT > newValue)
                                player.Per.PT = newValue;
                            break;
                        case "home":
                            if (player.Per.Home > newValue)
                                player.Per.Home = newValue;
                            break;
                        case "mkt":
                            if (player.Per.Mkt > newValue)
                                player.Per.Mkt = newValue;
                            break;
                        case "mor":
                            if (player.Per.Mor > newValue)
                                player.Per.Mor = newValue;
                            break;

                    }
                }

                return player;
            }
            catch (Exception e)
            {
                _log.Error("AdjustPersonalityProfile - " + e.Message);
                return null;

            }
        }
        private Dictionary<string, List<PlayerAttributesModel>> CalculatePosPercentileTiers(Dictionary<string, List<SearchPlayerExportCSVModel>> playerBreakout)
        {
            try
            {
                Dictionary<string, List<PlayerAttributesModel>> posPercentiles = new Dictionary<string, List<PlayerAttributesModel>>();

                foreach (string pos in playerBreakout.Keys)
                {
                    posPercentiles.Add(pos, new List<PlayerAttributesModel>());
                    for (double perc = 0.0; perc < 1.01; perc += .05)
                    {
                        //Console.WriteLine(perc);
                        PlayerAttributesModel attributes = GetRatingPercentile(playerBreakout[pos], pos, perc);
                        posPercentiles[pos].Add(attributes);
                    }
                }
                return posPercentiles;
            }
            catch (Exception e)
            {
                _log.Error("Error running CalculatePosPercentileTiers - " + e.Message);
                return null;
            }
        }
        private List<UpcomingDraftPlayerCSVModel> ConvertAllPlayersModelToUpcomingDraftList(AllPlayersModel upcomingDraftJSON)
        {
            try
            {

                //Create automap configurations
                var configBase = new MapperConfiguration(cfg => cfg.CreateMap<PlayerModel, UpcomingDraftPlayerCSVModel>());
                var mapperBase = configBase.CreateMapper();

                var configAttr = new MapperConfiguration(cfg => cfg.CreateMap<PlayerAttributesModel, UpcomingDraftPlayerCSVModel>());
                var mapperAttr = configAttr.CreateMapper();

                var configPer = new MapperConfiguration(cfg => cfg.CreateMap<PlayerPersonalitiesModel, UpcomingDraftPlayerCSVModel>());
                var mapperPer = configPer.CreateMapper();

                var configSkills = new MapperConfiguration(cfg => cfg.CreateMap<PlayerSkillsModel, UpcomingDraftPlayerCSVModel>());
                var mapperSkills = configSkills.CreateMapper();


                List<UpcomingDraftPlayerCSVModel> upcomingDraftCSV = new List<UpcomingDraftPlayerCSVModel>();
                foreach (var playerJSON in upcomingDraftJSON.Players)
                {

                    //Copy Base info
                    UpcomingDraftPlayerCSVModel playerCSV = mapperBase.Map<UpcomingDraftPlayerCSVModel>(playerJSON);
                    //Copy Attributes
                    playerCSV = mapperAttr.Map(playerJSON.Attr, playerCSV);
                    //Copy Personalities
                    playerCSV = mapperPer.Map(playerJSON.Per, playerCSV);
                    //Copy Skills
                    playerCSV = mapperSkills.Map(playerJSON.Skills, playerCSV);

                    upcomingDraftCSV.Add(playerCSV);
                }

                return upcomingDraftCSV;


            }
            catch (Exception e)
            {
                _log.Error("Error running ConvertUpcomingDraftJSONToCSV - " + e.Message);
                return null;
            }
        }
        private int ConvertHeight(string height)
        {
            try
            {
                int newHeight = 0;
                if (string.IsNullOrEmpty(height) || height == "0")
                    return -1;
                    

                int tickLocation = height.IndexOf("'");
                //Console.WriteLine("tick location = " + tickLocation);
                

                if (tickLocation < 0)
                {
                    newHeight = 0;
                    bool isNum = int.TryParse(height, out newHeight);

                    if (newHeight == 0) //not a number mark as errored
                        newHeight = -1;
                    else if (newHeight < 8) //Assume this is feet and convert to inches
                        newHeight = newHeight * 12;
                    else if (newHeight < 60 || newHeight > 96) //Mark as error if not between to 8 feet tall
                        newHeight = -1;

                    //otherwise use the number as inches
                }
                else
                {
                    
                    int count = 0;
                    bool done = false;

                    //trim non-numbers from front
                    if (!Char.IsNumber(height[0]))
                    {
                        do
                        {
                            if (Char.IsNumber(height[count]))
                                done = true;
                            else
                                count++;

                        } while (!done && count < height.Length);

                        if (count > 0)
                            height = height.Substring(count, height.Length - count);
                    }

                    //trim non-numbers from end
                    if (!Char.IsNumber(height[height.Length - 1]))
                    {
                        count = height.Length - 1;
                        done = false;
                        do
                        {
                            if (Char.IsNumber(height[count]))
                                done = true;
                            else
                                count--;

                        } while (!done && count > 0);

                        if (count != height.Length - 1)
                            height = height.Substring(0, count + 1);
                    }

                    string feetText = tickLocation > 0 ? height.Substring(0, tickLocation) : "0";
                    //Console.WriteLine("Feet = " + feetText);
                    int feet = int.Parse(feetText) * 12;

                    if (feet <= 0)
                        return -1;

                    int inches = 0;
                    //Console.WriteLine(height.Length - 1 + " > " + tickLocation);
                    if ((height.Length - 1) > tickLocation)
                    {
                        string inchText = height.Substring(tickLocation + 1, height.Length - tickLocation - 1);
                        //Console.WriteLine("inches = " + inchText);
                        inches = int.Parse(inchText);
                    }

                    newHeight = feet + inches;
                }

                return newHeight;

            }
            catch (Exception e)
            {
                return -1;
            }
        }
        private AllPlayersModel ConvertUpcomingDraftListToAllPlayersModel(List<UpcomingDraftPlayerCSVModel> PlayersCSV)
        {
            try
            {

                //Create automap configurations
                var configBase = new MapperConfiguration(cfg => cfg.CreateMap<UpcomingDraftPlayerCSVModel, PlayerModel>());
                var mapperBase = configBase.CreateMapper();

                var configAttr = new MapperConfiguration(cfg => cfg.CreateMap<UpcomingDraftPlayerCSVModel, PlayerAttributesModel>());
                var mapperAttr = configAttr.CreateMapper();

                var configPer = new MapperConfiguration(cfg => cfg.CreateMap<UpcomingDraftPlayerCSVModel, PlayerPersonalitiesModel>());
                var mapperPer = configPer.CreateMapper();

                var configSkills = new MapperConfiguration(cfg => cfg.CreateMap<UpcomingDraftPlayerCSVModel, PlayerSkillsModel>());
                var mapperSkills = configSkills.CreateMapper();


                List<UpcomingDraftPlayerCSVModel> upcomingDraftCSV = new List<UpcomingDraftPlayerCSVModel>();

                List<PlayerModel> playerListJSON = new List<PlayerModel>();
                foreach (var playerCSV in PlayersCSV)
                {

                    //Copy Base info
                    PlayerModel playerJSON = mapperBase.Map<PlayerModel>(playerCSV);
                    //Copy Attributes
                    PlayerAttributesModel attribute = mapperAttr.Map<PlayerAttributesModel>(playerCSV);
                    //Copy Personalities
                    PlayerPersonalitiesModel personality = mapperPer.Map<PlayerPersonalitiesModel>(playerCSV);
                    //Copy Skills
                    PlayerSkillsModel skill = mapperSkills.Map<PlayerSkillsModel>(playerCSV);

                    playerJSON.Attr = attribute;
                    playerJSON.Per = personality;
                    playerJSON.Skills = skill;

                    playerListJSON.Add(playerJSON);
                }

                AllPlayersModel allPlayersJSON = new AllPlayersModel();
                allPlayersJSON.Players = playerListJSON;

                return allPlayersJSON;

            }
            catch (Exception e)
            {
                _log.Error("Error running ConvertUpcomingDraftJSONToCSV - " + e.Message);
                return null;
            }
        }
        private PlayerModel FixBlankInfo(PlayerModel drafteeExport)
        {
            int minHeight = 0;
            int maxHeight = 0;
            int minWeight = 0;
            int maxWeight = 0;

            switch (drafteeExport.Pos)
            {
                case "QB":
                    minHeight = 70;
                    maxHeight = 79;
                    minWeight = 183;
                    maxWeight = 260;
                    break;
                case "RB":
                    minHeight = 70;
                    maxHeight = 79;
                    minWeight = 175;
                    maxWeight = 260;
                    break;
                case "FB":
                    minHeight = 70;
                    maxHeight = 79;
                    minWeight = 190;
                    maxWeight = 260;
                    break;
                case "G":
                    minHeight = 73;
                    maxHeight = 79;
                    minWeight = 270;
                    maxWeight = 345;
                    break;
                case "T":
                    minHeight = 73;
                    maxHeight = 79;
                    minWeight = 270;
                    maxWeight = 345;
                    break;
                case "C":
                    minHeight = 73;
                    maxHeight = 79;
                    minWeight = 270;
                    maxWeight = 345;
                    break;
                case "TE":
                    minHeight = 75;
                    maxHeight = 79;
                    minWeight = 235;
                    maxWeight = 257;
                    break;
                case "WR":
                    minHeight = 70;
                    maxHeight = 79;
                    minWeight = 175;
                    maxWeight = 225;
                    break;
                case "CB":
                    minHeight = 70;
                    maxHeight = 79;
                    minWeight = 175;
                    maxWeight = 225;
                    break;
                case "LB":
                    minHeight = 75;
                    maxHeight = 79;
                    minWeight = 235;
                    maxWeight = 257;
                    break;
                case "DT":
                    minHeight = 73;
                    maxHeight = 79;
                    minWeight = 270;
                    maxWeight = 345;
                    break;
                case "DE":
                    minHeight = 73;
                    maxHeight = 79;
                    minWeight = 270;
                    maxWeight = 345;
                    break;
                case "FS":
                    minHeight = 70;
                    maxHeight = 79;
                    minWeight = 175;
                    maxWeight = 225;
                    break;
                case "SS":
                    minHeight = 70;
                    maxHeight = 79;
                    minWeight = 175;
                    maxWeight = 225;
                    break;
                case "K":
                    minHeight = 70;
                    maxHeight = 79;
                    minWeight = 183;
                    maxWeight = 260;
                    break;
                case "P":
                    minHeight = 70;
                    maxHeight = 79;
                    minWeight = 183;
                    maxWeight = 260;
                    break;
            }

            if (drafteeExport.Age <= 0)
                drafteeExport.Age = _rnd.Next(20, 25);

            if (drafteeExport.Hgt <= 0)
                drafteeExport.Hgt = _rnd.Next(minHeight, maxHeight + 1);

            if (drafteeExport.Wgt <= 0)
                drafteeExport.Wgt = _rnd.Next(minWeight, maxWeight + 1);

            if (string.IsNullOrEmpty(drafteeExport.Coll))
                drafteeExport.Coll = GetRandomCollege();

            return drafteeExport;
        }
        private List<PercentileChartModel> FormatAllPlayerSummaryForCSVOutput(Dictionary<string, List<PlayerAttributesModel>> posPercentileTiers)
        {
            try
            {
                var configAttr = new MapperConfiguration(cfg => cfg.CreateMap<PlayerAttributesModel, PercentileChartModel>());
                var mapperAttr = configAttr.CreateMapper();

                List<PercentileChartModel> precentileChart = new List<PercentileChartModel>();
                foreach (string pos in Info.PositionList)
                {
                    List<PlayerAttributesModel> posAttributes = posPercentileTiers[pos];
                    
                    int perc = 100;
                    for (int index = 20; index >= 0; index--)
                    {
                        PlayerAttributesModel attr = posAttributes[index];
                        PercentileChartModel perChart = mapperAttr.Map<PercentileChartModel>(attr);
                        perChart.Pos = pos;
                        perChart.Per = perc;
                        precentileChart.Add(perChart);

                        perc += -5;
                    }
                }


                return precentileChart;
            }
            catch (Exception e)
            {
                _log.Error("Error running FormatAllPlayerSummaryForCSVOutput - " + e.Message);
                return null;
            }
        }
        private string FormatAllPlayerSummaryForHTMLOutput(Dictionary<string, List<PlayerAttributesModel>> posPercentileTiers)
        {
            try
            {
                StringBuilder s = new StringBuilder();
                s.AppendLine("<!DOCTYPE html>");
                s.AppendLine("<html>");

                foreach (string pos in Info.PositionList)
                {
                    s.AppendLine(GeneratePositionSummary(posPercentileTiers[pos], pos));
                }

                s.AppendLine("</html>");

                return s.ToString();
            }
            catch (Exception e)
            {
                _log.Error("Error running FormatAllPlayerSummaryForOutput - " + e.Message);
                return null;
            }
        }
        private string GeneratePositionSummary(List<PlayerAttributesModel> posAttributes, string pos)
        {
            try
            {
                StringBuilder s = new StringBuilder();


                //QB Position
                //s.AppendLine("  <p>");
                s.AppendLine($"  <h2>{pos} Percentile Breakdown</h2>");
                s.AppendLine("  <table class=\"percentile-table\">");
                s.AppendLine("    <tr>");
                s.AppendLine("      <th class=\"percentile-table-column-first\">Percentile</td>");
                s.AppendLine("      <th class=\"percentile - table - column\">Str</td>");
                s.AppendLine("      <th class=\"percentile - table - column\">Agi</td>");
                s.AppendLine("      <th class=\"percentile - table - column\">Arm</td>");
                s.AppendLine("      <th class=\"percentile - table - column\">Spe</td>");
                s.AppendLine("      <th class=\"percentile - table - column\">Han</td>");
                s.AppendLine("      <th class=\"percentile - table - column\">Int</td>");
                s.AppendLine("      <th class=\"percentile - table - column\">Acc</td>");
                s.AppendLine("      <th class=\"percentile - table - column\">PBl</td>");
                s.AppendLine("      <th class=\"percentile - table - column\">RBl</td>");
                s.AppendLine("      <th class=\"percentile - table - column\">Tck</td>");
                s.AppendLine("      <th class=\"percentile - table - column\">KDi</td>");
                s.AppendLine("      <th class=\"percentile - table - column\">KAc</td>");
                s.AppendLine("      <th class=\"percentile - table - column\">End</td>");
                s.AppendLine("    </tr>");

                int perc = 100;
                for (int index = 20; index >= 0; index--)
                {
                    string tr = "<tr>";
                    if (perc == 60)
                        tr = "<tr style=\"color: orangered; text-decoration: underline; \">";
                    else if (perc == 95)
                        tr = "<tr style=\"color: red; text-decoration: underline; \">";

                    PlayerAttributesModel attr = posAttributes[index];
                    s.AppendLine($"          {tr}");
                    s.AppendLine($"              <td>{perc}%</td>");
                    s.AppendLine($"              <td>{attr.Str}</td>");
                    s.AppendLine($"              <td>{attr.Agi}</td>");
                    s.AppendLine($"              <td>{attr.Arm}</td>");
                    s.AppendLine($"              <td>{attr.Spe}</td>");
                    s.AppendLine($"              <td>{attr.Han}</td>");
                    s.AppendLine($"              <td>{attr.Intel}</td>");
                    s.AppendLine($"              <td>{attr.Acc}</td>");
                    s.AppendLine($"              <td>{attr.PBl}</td>");
                    s.AppendLine($"              <td>{attr.RBl}</td>");
                    s.AppendLine($"              <td>{attr.Tck}</td>");
                    s.AppendLine($"              <td>{attr.KDi}</td>");
                    s.AppendLine($"              <td>{attr.KAc}</td>");
                    s.AppendLine($"              <td>{attr.End}</td>");
                    s.AppendLine("          </tr>");
                    perc += -5;
                }

                s.AppendLine("      </table>");
                //s.AppendLine("  </p>");

                return s.ToString();

            }
            catch (Exception e)
            {
                _log.Error("Error running GeneratePositionSummary - " + e.Message);
                return null;
            }
        }
        private MinMaxModel GetAttributeMinMax(string attrToGet, int minIndex, int maxIndex, List<PlayerAttributesModel> playerAttr)
        {
            try
            {
                var result = new MinMaxModel();

                //use reflexion to pull the property of the attribute to get (e.g. str, intel) for min number
                PlayerAttributesModel minPosAttr = playerAttr[minIndex];
                Type mintype = minPosAttr.GetType();
                PropertyInfo minPosProp = mintype.GetProperty(attrToGet);
                result.Min = (int)minPosProp.GetValue(minPosAttr, null);

                //use reflexion to pull the property of the attribute to get (e.g. str, intel) for max number
                PlayerAttributesModel maxPosAttr = playerAttr[maxIndex];
                Type maxtype = maxPosAttr.GetType();
                PropertyInfo maxPosProp = maxtype.GetProperty(attrToGet);
                result.Max = (int)maxPosProp.GetValue(maxPosAttr, null);

                return result;


            }
            catch (Exception e)
            {
                _log.Error("GetAttributeMinMax - " + e.Message);
                return null;
            }
        }
        private Dictionary<string, List<PlayerAttributesModel>> GetPercentileDictionaryFromActivePlayers(string activePlayersCSV_Location)
        {
            try
            {

                //Load JSON into our object
                _log.Information($"Reading in Active Players from file {activePlayersCSV_Location}");
                List<SearchPlayerExportCSVModel> activePlayerCSVData;
                using (var reader = new StreamReader(activePlayersCSV_Location))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    activePlayerCSVData = csv.GetRecords<SearchPlayerExportCSVModel>().ToList();
                }

                if (activePlayerCSVData == null || activePlayerCSVData.Count == 0)
                {
                    _log.Error("Error Getting Active Players");
                    return null;
                }

                //break active players by pos
                _log.Information($"Breaking active players into positions");
                Dictionary<string, List<SearchPlayerExportCSVModel>> playerBreakout = SplitPositions(activePlayerCSVData);
                if (playerBreakout == null || playerBreakout.Count == 0)
                {
                    _log.Error("Error running GetLeaguePositionSummary - cannot split players into positions");
                    return null;
                }

                //Get position percentile tiers 
                _log.Information($"Calculating percentiles for each active position");
                Dictionary<string, List<PlayerAttributesModel>> posPercentileTiers = CalculatePosPercentileTiers(playerBreakout);
                if (posPercentileTiers == null)
                {
                    _log.Error("Error running GetLeaguePositionSummary - cannot calculate positional percentile tiers");
                    return null;
                }

                return posPercentileTiers;
            }
            catch (Exception e)
            {
                _log.Error("Error running GetPosPerctileDictionary - " + e.Message);
                return null;
            }
        }
        private Dictionary<string, List<PlayerAttributesModel>> GetPercentileDictionaryFromPercentileChart(string percentileChartCSV_Location)
        {
            try
            {

                //Load JSON into our object
                _log.Information($"Reading in Percentile Chart from file {percentileChartCSV_Location}");
                List<PercentileChartModel> percentileChartCSVData;
                using (var reader = new StreamReader(percentileChartCSV_Location))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    percentileChartCSVData = csv.GetRecords<PercentileChartModel>().ToList();
                }

                if (percentileChartCSVData == null || percentileChartCSVData.Count == 0)
                {
                    _log.Error("Error Getting Active Players");
                    return null;
                }

                var configAttr = new MapperConfiguration(cfg => cfg.CreateMap<PercentileChartModel, PlayerAttributesModel>());
                var mapperAttr = configAttr.CreateMapper();

                //Get position percentile tiers 
                _log.Information($"Calculating percentiles for each active position");
                Dictionary<string, List<PlayerAttributesModel>> posPercentileTiers = new Dictionary<string, List<PlayerAttributesModel>>();

                percentileChartCSVData = percentileChartCSVData.OrderBy(o => o.Pos).ThenBy(o => o.Per).ToList();
                string pos = String.Empty;
                List< PlayerAttributesModel > attributes = new List<PlayerAttributesModel >();
                foreach (var row in percentileChartCSVData)
                {
                    PlayerAttributesModel attr = mapperAttr.Map<PlayerAttributesModel>(row);

                    //if (row.Pos.ToLower() == "wr")
                    //    Console.WriteLine("here");

                    if (string.IsNullOrEmpty(pos))
                    {
                        pos = row.Pos;
                    }
                    else if (pos != row.Pos)
                    {
                        posPercentileTiers.Add(pos, attributes);
                        pos = row.Pos;
                        attributes = new List<PlayerAttributesModel>();
                    }

                    attributes.Add(attr);
                }
                
                posPercentileTiers.Add(pos, attributes);


                return posPercentileTiers;
            }
            catch (Exception e)
            {
                _log.Error("Error running GetPosPerctileDictionary - " + e.Message);
                return null;
            }
        }
        private string GetPosition(string pos)
        {
            try
            {
                string newPos = string.Empty;
                if (Info.PositionList.Contains(pos.ToUpper()))
                {
                    newPos = pos.ToUpper();
                }
                else if (pos.ToUpper() == "DB")
                {
                    //Convert to a random DB ... i.e CB, FS, SS
                    int r = _rnd.Next(0, 3) + 1;
                    switch (r)
                    {
                        case 1:
                            newPos = "CB";
                            break;
                        case 2:
                            newPos = "FS";
                            break;
                        case 3:
                            newPos = "SS";
                            break;
                    }
                }
                else if (pos.ToUpper() == "S")
                {
                    //Convert to a random S ... i.e FS, SS
                    int r = _rnd.Next(0, 2) + 1;
                    switch (r)
                    {
                        case 1:
                            newPos = "SS";
                            break;
                        case 2:
                            newPos = "FS";
                            break;
                    }
                }
                else if (pos.ToUpper() == "OL")
                {
                    //Convert to a random OL ... i.e C, T, G
                    int r = _rnd.Next(0, 3) + 1;
                    switch (r)
                    {
                        case 1:
                            newPos = "C";
                            break;
                        case 2:
                            newPos = "T";
                            break;
                        case 3:
                            newPos = "G";
                            break;
                    }
                }
                else if (pos.ToUpper() == "OT" || pos.ToUpper() == "RT" || pos.ToUpper() == "LT")
                {
                    newPos = "T";
                }
                else if (pos.ToUpper() == "OG" || pos.ToUpper() == "RG" || pos.ToUpper() == "LG")
                {
                    newPos = "G";
                }
                else if (pos.ToUpper() == "DL")
                {
                    //Convert to a random DL ... i.e DT, DE
                    int r = _rnd.Next(0, 2) + 1;
                    switch (r)
                    {
                        case 1:
                            newPos = "DE";
                            break;
                        case 2:
                            newPos = "DT";
                            break;
                    }
                }
                else if (pos.ToUpper() == "NT")
                {
                    newPos = "DT";
                }
                else if (pos.ToUpper() == "ILB" || pos.ToUpper() == "OLB" || pos.ToUpper() == "MLB")
                {
                    newPos = "LB";
                }
                else if (pos.ToUpper() == "LS")
                {
                    newPos = "C";
                }
                else
                {
                    newPos = string.Empty;
                }

                return newPos;
            }
            catch (Exception e)
            {
                _log.Error(e.Message);
                return string.Empty;
            }
        }
        private string GetRandomCollege()
        {
            List<string> colleges = new List<string>
            {
                "Alabama",
                "Alabama A&M",
                "Alabama St.",
                "Alcorn St.",
                "Amherst",
                "Arizona St.",
                "Arkansas",
                "Auburn",
                "Azusa Pacific",
                "Ball St.",
                "Baylor",
                "Bethune-Cookman",
                "Bowdoin",
                "Bowling Green",
                "BYU",
                "Cal Poly-San Luis Obispo",
                "California",
                "Central Missouri St.",
                "Chattanooga",
                "Cincinnati",
                "Clemson",
                "Colgate",
                "Colorado",
                "Colorado St.",
                "Concordia-Moorhead (MN)",
                "Dartmouth",
                "Duke",
                "East Carolina",
                "East Tennessee St.",
                "East. Michigan",
                "Elon",
                "Florida",
                "Florida A&M",
                "Florida St.",
                "Fresno St.",
                "Georgia",
                "Georgia Tech",
                "Grambling St.",
                "Hampton",
                "Houston",
                "Howard",
                "Illinois",
                "Illinois St.",
                "Indiana",
                "Iowa",
                "Iowa St.",
                "Jackson St.",
                "Kansas",
                "Kansas St.",
                "Kent St.",
                "Kentucky",
                "Lafayette",
                "Lamar",
                "Lehigh",
                "Livingstone",
                "Long Beach St.",
                "Louisiana Tech",
                "Louisville",
                "LSU",
                "Maryland",
                "Memphis",
                "Miami (FL)",
                "Michigan",
                "Michigan St.",
                "Middle Tenn. St.",
                "Minnesota",
                "Mississippi",
                "Mississippi St.",
                "Missouri",
                "Missouri State",
                "Montana",
                "Montana St.",
                "Morningside",
                "NE State (OK)",
                "Nebraska",
                "Nebraska-Omaha",
                "Nevada",
                "New Mexico St.",
                "North Carolina",
                "North Carolina St.",
                "Northern Arizona",
                "Northern Colorado",
                "Northwestern",
                "Notre Dame",
                "Ohio St.",
                "Oklahoma",
                "Oklahoma St.",
                "Oregon",
                "Oregon St.",
                "Pacific",
                "Penn St.",
                "Pittsburgh",
                "Portland St.",
                "Purdue",
                "Redlands",
                "Rhode Island",
                "Richmond",
                "Rutgers",
                "San Diego St.",
                "San Jose St.",
                "Santa Clara",
                "SE Missouri St.",
                "SMU",
                "South Carolina St.",
                "South Dakota St.",
                "Southern",
                "Southern Miss",
                "Stanford",
                "Syracuse",
                "TCU",
                "Tennessee",
                "Tennessee St.",
                "Texas",
                "Texas A&M",
                "Texas A&M-Kingsville",
                "Texas Tech",
                "Texas-Arlington",
                "Troy",
                "Truman St.",
                "Tulane",
                "UCLA",
                "UNLV",
                "USC",
                "UT Martin",
                "Utah St.",
                "Vanderbilt",
                "Virginia",
                "Wake Forest",
                "Washington",
                "Washington St.",
                "West Texas A&M",
                "West Virginia",
                "Western Illinois",
                "Wichita St.",
                "Wyoming"
            };

            int index = _rnd.Next(0, colleges.Count);
            return colleges[index];

        }
        private PlayerAttributesModel GetRatingPercentile(List<SearchPlayerExportCSVModel> positionList, string position, double percentile)
        {
            try
            {
                PlayerAttributesModel attributes = new PlayerAttributesModel();
                int percentileIndex = 0;
                if (percentile > 0 && percentile < 1)
                    percentileIndex = Convert.ToInt32(Math.Ceiling((double)positionList.Count * percentile));
                else if (percentile >= 1)
                    percentileIndex = positionList.Count - 1;

                List<int> ratingList;

                ratingList = positionList.Select(p => p.Str).OrderBy(p => p).ToList();
                attributes.Str = ratingList[percentileIndex];

                ratingList = positionList.Select(p => p.Agi).OrderBy(p => p).ToList();
                attributes.Agi = ratingList[percentileIndex];

                ratingList = positionList.Select(p => p.Arm).OrderBy(p => p).ToList();
                attributes.Arm = ratingList[percentileIndex];

                ratingList = positionList.Select(p => p.Spe).OrderBy(p => p).ToList();
                attributes.Spe = ratingList[percentileIndex];

                ratingList = positionList.Select(p => p.Hnd).OrderBy(p => p).ToList();
                attributes.Han = ratingList[percentileIndex];

                ratingList = positionList.Select(p => p.Int).OrderBy(p => p).ToList();
                attributes.Intel = ratingList[percentileIndex];

                ratingList = positionList.Select(p => p.Acc).OrderBy(p => p).ToList();
                attributes.Acc = ratingList[percentileIndex];

                ratingList = positionList.Select(p => p.PBl).OrderBy(p => p).ToList();
                attributes.PBl = ratingList[percentileIndex];

                ratingList = positionList.Select(p => p.RBl).OrderBy(p => p).ToList();
                attributes.RBl = ratingList[percentileIndex];

                ratingList = positionList.Select(p => p.Tck).OrderBy(p => p).ToList();
                attributes.Tck = ratingList[percentileIndex];

                ratingList = positionList.Select(p => p.KDi).OrderBy(p => p).ToList();
                attributes.KDi = ratingList[percentileIndex];

                ratingList = positionList.Select(p => p.KAc).OrderBy(p => p).ToList();
                attributes.KAc = ratingList[percentileIndex];

                ratingList = positionList.Select(p => p.End).OrderBy(p => p).ToList();
                attributes.End = ratingList[percentileIndex];

                return attributes;
            }
            catch (Exception ex)
            {
                _log.Error($"GetRatingPercentile: {ex.Message}");
                return null;
            }
        }
        private string GetStyle(DraftClassInputModel player, string existingStyle)
        {
            try
            {
                List<string> styleNamesForPos = _settings.Styles.Where(x =>
                    x.ApplyToPosition.ToLower() == player.Position.ToLower())
                    .Select(s => s.StyleName.ToLower())
                    .ToList();

                //If we have an existing style make sure it matches up with the position
                if (!string.IsNullOrEmpty(existingStyle) && !styleNamesForPos.Contains(existingStyle.ToLower()))
                {
                    _log.Error($"Player {player.FirstName} {player.LastName} has a position({player.Position}) that does not match the style({player.Style})");
                    return null;
                }
                else if (!string.IsNullOrEmpty(existingStyle))
                {
                    return existingStyle;
                }

                //Need to randomize styles but first need to get any traits that might be assigned. 
                string style = string.Empty;
                List<string> traits = new List<string>();
                if (!string.IsNullOrEmpty(player.Trait))
                {
                    traits = player.Trait.Split('|').ToList();
                    if (traits == null || traits.Count == 0)
                    {
                        _log.Error($"Invalid trait {player.Trait} found for {player.FirstName} {player.LastName}");
                        return null;
                    }

                    foreach (var trait in traits)
                    {
                        if (!Info.TraitList.Contains(trait))
                        {
                            _log.Error($"Invalid trait {trait} found for {player.FirstName} {player.LastName}");
                            return null;
                        }
                    }
                }

                //get styles for the position that allows for any traits added
                List<StyleModel> availableStyles = _settings.Styles.Where(s =>
                    s.ApplyToPosition.ToLower() == player.Position.ToLower()
                    && (
                            s.AllowedPosTraits.Count == 0
                            || traits == null
                            || s.AllowedPosTraits.Any(a => Info.TraitList.Any(t => t == a))
                        )
                     ).ToList();

                //if we have a list get the random style base on weights
                if (availableStyles != null && availableStyles.Count > 0)
                {
                    List<WeightedListModel> packageList = availableStyles.Select(s => new WeightedListModel { Weight = s.RandomWeight, Item = s.StyleName }).ToList();
                    style = SelectFromWeightedList(packageList);
                }

                return style;

            }
            catch (Exception e)
            {
                _log.Error("GetStyle - " + e.Message);
                return null;
            }
        }
        private string GetTraits(DraftClassInputModel draftee, out string addedTraits)
        {
            addedTraits = string.Empty;
            try
            {
                TierModel tier = _settings.TierDefinitions.Where(t => t.TierName.ToLower() == draftee.Tier.ToLower()).FirstOrDefault();
                if (tier == null)
                {
                    _log.Error($"{draftee.FirstName} {draftee.LastName} has an invalid Tier({draftee.Tier})");
                    return null;
                }

                bool hasPosTrait = false;
                bool hasPerTrait = false;

                List<string> posTraits = new List<string>();
                List<string> perTraits = new List<string>();
                List<string> auditTraitsList = new List<string>();

                List<TraitModel> allPosTraits = _settings.Traits.Where(t => t.Type.ToLower() == "position" && t.AllowedPositions.Contains(draftee.Position.ToUpper())).ToList();
                List<TraitModel> allPerTraits = _settings.Traits.Where(t => t.Type.ToLower() == "personality").ToList();

                if (!string.IsNullOrEmpty(draftee.Trait))
                {
                    string[] playerTraits = draftee.Trait.Split('|');
                    if (playerTraits == null || playerTraits.Length == 0)
                    {
                        _log.Error($"{draftee.FirstName} {draftee.LastName} has an invalid trait assigned");
                        return null;
                    }

                    foreach (var playerTrait in playerTraits)
                    {
                        bool traitFound = false;

                        if (allPosTraits.Select(s => s.GameTraitName).Contains(playerTrait.Trim()))
                        {
                            traitFound = true;
                            posTraits.Add(playerTrait.Trim());
                        }

                        if (allPerTraits.Select(s => s.GameTraitName).Contains(playerTrait.Trim()))
                        {
                            traitFound = true;
                            perTraits.Add(playerTrait.Trim());
                        }

                        if (!traitFound)
                        {
                            _log.Error($"{draftee.FirstName} {draftee.LastName} has an invalid trait({playerTrait}) assigned");
                            return null;
                        }
                    }
                }

                bool addPersonalityTrait = false;
                if ((posTraits.Count > 0 && perTraits.Count == 0))
                {
                    int roll = _rnd.Next(0, 100) + 1;
                    if (roll <= _settings.AddPersonalityTraitToPosTraitPercentage)
                    {
                        addPersonalityTrait = true;
                    }
                }

                //if we need to randomly select a positional trait
                if (tier.AllowPositionalTag && _settings.PosTraitPercentage > 0 && posTraits.Count == 0 && allPosTraits.Count > 0)
                {
                    int roll = _rnd.Next(0, 100) + 1;
                    if (roll <= _settings.PosTraitPercentage)
                    {
                        List<WeightedListModel> packageList = allPosTraits.Select(s => new WeightedListModel { Weight = s.RandomWeight, Item = s.GameTraitName }).ToList();
                        string posTrait = SelectFromWeightedList(packageList);

                        if (string.IsNullOrEmpty(posTrait))
                        {
                            _log.Error($"{draftee.FirstName} {draftee.LastName} failed to fetch a trait");
                            return null;
                        }

                        posTraits.Add(posTrait);
                        auditTraitsList.Add(posTrait);

                        roll = _rnd.Next(0, 100) + 1;
                        if (roll <= _settings.AddPersonalityTraitToPosTraitPercentage && perTraits.Count == 0)
                            addPersonalityTrait = true;
                    }
                }

                //if we need to randomly select a positional trait
                if (addPersonalityTrait || (tier.AllowPersonalityTag && _settings.PerTraitPercentage > 0 && perTraits.Count == 0))
                {
                    int roll = _rnd.Next(0, 100) + 1;
                    if (roll <= _settings.PerTraitPercentage || addPersonalityTrait)
                    {
                        List<WeightedListModel> packageList = allPerTraits.Select(s => new WeightedListModel { Weight = s.RandomWeight, Item = s.GameTraitName }).ToList();
                        bool traitFound = tier.AllowNegativeTrait;
                        string perTrait = String.Empty;
                        int retryCount = 0;
                        do
                        {
                            perTrait = SelectFromWeightedList(packageList);
                            if (!traitFound)
                            {
                                TraitModel checkTrait = allPerTraits.Where(t => t.TraitName == perTrait).FirstOrDefault();
                                if (checkTrait != null && !checkTrait.NegativeTrait)
                                {
                                    traitFound = true;
                                }
                                else
                                {
                                    perTrait = String.Empty;
                                }
                            }
                            retryCount++;
                        } while (retryCount < 100 && !traitFound);

                        //if (retryCount > 50)
                        //    Console.WriteLine("Debug");
                        

                        if (string.IsNullOrEmpty(perTrait))
                        {
                            _log.Error($"{draftee.FirstName} {draftee.LastName} failed to fetch a trait");
                            return null;
                        }

                        perTraits.Add(perTrait);
                        auditTraitsList.Add(perTrait);
                    }
                }

                string results = string.Empty;
                if (perTraits.Count > 0 || posTraits.Count > 0)
                {
                    perTraits.AddRange(posTraits);
                    results = string.Join("|", perTraits);
                }

                if (auditTraitsList.Count > 0)
                {
                    addedTraits = string.Join("|", auditTraitsList);
                }

                return results;
            }
            catch (Exception e)
            {
                _log.Error("GetTraits - " + e.Message);
                return null;
            }

        }
        private PlayerModel RandomizeAttributes(PlayerModel player, string tier, string styleName, Dictionary<string, List<PlayerAttributesModel>> posPercentileTiers)
        {
            try
            {

                //get the active player pos so we can use this to set our attrubutes
                List<PlayerAttributesModel> playerAttr = posPercentileTiers[player.Pos];

                //find our grade from the tier listing in the draft file
                TierModel tierInfo = _settings.TierDefinitions.Where(t => t.TierName.ToLower() == tier.ToLower()).FirstOrDefault();
                if (tierInfo == null)
                {
                    _log.Error("SetPlayersNewAttributes - tier names in csv does not match tier id in settings");
                    return null;
                }

                //used for debugging
                if (player.First == "FieldGeneral" && player.Last == "Legend")
                    Console.WriteLine("here");

                //convert grades of min and max to a useful index needed to pull the info from our active player position 
                int keyMinPer = tierInfo.KeyAttributeMin / 5;
                int keyMaxPer = tierInfo.KeyAttributeMax / 5;
                int priMinPer = tierInfo.PriAttributeMin / 5;
                int priMaxPer = tierInfo.PriAttributeMax / 5;
                int secMinPer = tierInfo.SecAttributeMin / 5;
                int secMaxPer = tierInfo.SecAttributeMax / 5;

                //Get Lists for skills
                PositionalAttributesModel positionalAttr = _settings.PositionalAttributes.Where(s => s.PositionName.ToLower() == player.Pos.ToLower()).FirstOrDefault();
                if (positionalAttr == null)
                {
                    _log.Error($"Could not find position ({player.Pos}) defined in PostionalSkills");
                    return null;
                }

                StyleModel style = new StyleModel();
                if (!string.IsNullOrEmpty(styleName))
                {
                    style = _settings.Styles.Where(s => s.StyleName.ToLower() == styleName.ToLower()).FirstOrDefault();
                    if (style == null)
                    {
                        _log.Error($"Could not find Style ({styleName}) defined in PostionalSkills");
                        return null;
                    }

                    //if (style.KeyAttributes != null && style.KeyAttributes.Count > 0)
                    //    skills.PrimaryAttributes.AddRange(style.KeyAttributes);
                }


                //Need to add enhance and muffle attributes based on trait membership
                List<string> enhanceAttList = new List<string>();
                List<string> muffleAttList = new List<string>();
                if (!string.IsNullOrEmpty(player.Trait))
                {
                    List<string> playerTraits = player.Trait.Split('|').ToList();
                    foreach (var playerTrait in playerTraits)
                    {
                        TraitModel trait = _settings.Traits.Where(t => t.TraitName.ToLower() == playerTrait.ToLower()).FirstOrDefault();
                        if (trait != null)
                        {
                            enhanceAttList.AddRange(trait.EnhanceAttributes);
                            muffleAttList.AddRange(trait.MuffleAttributes);
                        }

                    }
                }


                //Go through each attribute and set a random value from min to max indexes
                foreach (string attrToGet in Info.AttributeListCaseSensitive)
                {
                    bool enhanceAtt = false;
                    bool muffleAtt = false;

                    if (enhanceAttList.Count > 0 && enhanceAttList.Contains(attrToGet.ToUpper()))
                        enhanceAtt = true;

                    if (muffleAttList.Count > 0 && muffleAttList.Contains(attrToGet.ToUpper()))
                        muffleAtt = true;

                    MinMaxModel minMaxAttr = null;

                    if (style.KeyAttributes.Contains(attrToGet, StringComparer.OrdinalIgnoreCase))
                    {
                        minMaxAttr = GetAttributeMinMax(attrToGet, keyMinPer, keyMaxPer, playerAttr);
                    }
                    else if (positionalAttr.PrimaryAttributes.Contains(attrToGet, StringComparer.OrdinalIgnoreCase))
                    {
                        minMaxAttr = GetAttributeMinMax(attrToGet, priMinPer, priMaxPer, playerAttr);
                    }
                    else if (positionalAttr.SecondaryAttributes.Contains(attrToGet, StringComparer.OrdinalIgnoreCase))
                    {
                        minMaxAttr = GetAttributeMinMax(attrToGet, secMinPer, secMaxPer, playerAttr);
                    }
                    else //Unimportant so just randomize
                    {
                        minMaxAttr = GetAttributeMinMax(attrToGet, 0, 19, playerAttr);

                        if (minMaxAttr.Max > _settings.MaxAllowedForUnimportantSkills && attrToGet.ToLower() != "end")
                            minMaxAttr.Max = _settings.MaxAllowedForUnimportantSkills;
                    }

                    if (enhanceAtt)
                    {
                        double adj = _settings.MinEnhanceAttrPercentage / 100;
                        minMaxAttr.Min += (int)Math.Floor((minMaxAttr.Max - minMaxAttr.Min) * adj);

                    }

                    if (muffleAtt)
                    {
                        double adj = _settings.MaxMuffleAttrPercentage / 100;
                        minMaxAttr.Max += (int)Math.Floor((minMaxAttr.Max - minMaxAttr.Min) * adj);
                    }

                    if (minMaxAttr.Min > minMaxAttr.Max)
                        minMaxAttr.Min = minMaxAttr.Max;

                    //Randomize the attribute
                    int newAttrRating = _rnd.Next(minMaxAttr.Min, minMaxAttr.Max + 1);

                    //use reflexion to update our attribute object to send back by the method
                    Type playerType = player.Attr.GetType();
                    PropertyInfo playerProp = playerType.GetProperty(attrToGet);
                    playerProp.SetValue(player.Attr, Convert.ChangeType(newAttrRating, playerProp.PropertyType), null);

                }

                //Work Ethic 
                if (tierInfo.WE == 0)
                    tierInfo.WE = 1;
                if (tierInfo.WE > 99)
                    tierInfo.WE = 99;
                if (tierInfo.WE >= 0)
                {
                    player.Per.Wor = _rnd.Next(tierInfo.WE > 0 ? tierInfo.WE : 25, 101);
                }

                //Endurance
                if (tierInfo.End == 0)
                    tierInfo.End = 1;
                if (tierInfo.End > 99)
                    tierInfo.End = 99;
                if (tierInfo.End >= 0)
                {
                    player.Attr.End = _rnd.Next(tierInfo.End > 0 ? tierInfo.End : 25, 101);
                }

                //Competitiveness
                if (tierInfo.Comp == 0)
                    tierInfo.Comp = 1;
                if (tierInfo.Comp > 99)
                    tierInfo.Comp = 99;
                if (tierInfo.Comp >= 0)
                {
                    player.Per.Com = _rnd.Next(tierInfo.Comp > 0 ? tierInfo.Comp : 25, 101);
                }


                //Primary Position Skill
                int skill = _rnd.Next(tierInfo.SkillMin, tierInfo.SkillMax + 1);
                Type playerSkillType = player.Skills.GetType();
                PropertyInfo playerSkillProp = playerSkillType.GetProperty(player.Pos);
                playerSkillProp.SetValue(player.Skills, Convert.ChangeType(skill, playerSkillProp.PropertyType), null);

                return player;
            }
            catch (Exception e)
            {
                _log.Error("SetPlayersNewAttributes - " + e.Message);
                return null;
            }
        }
        private PlayerPersonalitiesModel RandomizePersonality()
        {
            try
            {
                PlayerPersonalitiesModel personality = new PlayerPersonalitiesModel();

                personality.Lea = _rnd.Next(5, 101);
                personality.Wor = _rnd.Next(5, 101);
                personality.Com = _rnd.Next(5, 101);
                personality.TmPl = _rnd.Next(5, 101);
                personality.Spor = _rnd.Next(5, 101);
                personality.Soc = _rnd.Next(5, 101);
                personality.Mny = _rnd.Next(5, 101);
                personality.Sec = _rnd.Next(5, 101);
                personality.Loy = _rnd.Next(5, 101);
                personality.Win = _rnd.Next(5, 101);
                personality.PT = _rnd.Next(5, 101);
                personality.Home = _rnd.Next(5, 101);
                personality.Mkt = _rnd.Next(5, 101);
                personality.Mor = _rnd.Next(5, 101);

                return personality;
            }
            catch (Exception e)
            {
                _log.Error(e.Message);
                return null;
            }
        }
        private PlayerSkillsModel RandomizeSecondarySkills(string pos)
        {
            try
            {
                PlayerSkillsModel skills = new PlayerSkillsModel();

                int chanceForSecondSkill = _rnd.Next(1, 101); //rolls to set the skill
                int chanceForThirdSkill = _rnd.Next(1, 101);


                switch (pos)
                {
                    case "QB":
                        break;
                    case "RB":
                        if (chanceForSecondSkill <= _settings.SecondarySkillChance)
                            skills.FB = _rnd.Next(0, _settings.MaxSecondarySkill) + 1;
                        if (chanceForThirdSkill <= _settings.SecondarySkillChance)
                            skills.WR = _rnd.Next(0, _settings.MaxSecondarySkill) + 1;
                        break;
                    case "FB":
                        if (chanceForSecondSkill <= _settings.SecondarySkillChance)
                            skills.RB = _rnd.Next(0, _settings.MaxSecondarySkill) + 1;
                        break;
                    case "G":
                        if (chanceForSecondSkill <= _settings.SecondarySkillChance)
                            skills.T = _rnd.Next(0, _settings.MaxSecondarySkill) + 1;
                        if (chanceForThirdSkill <= _settings.SecondarySkillChance)
                            skills.C = _rnd.Next(0, _settings.MaxSecondarySkill) + 1;
                        break;
                    case "T":
                        if (chanceForSecondSkill <= _settings.SecondarySkillChance)
                            skills.G = _rnd.Next(0, _settings.MaxSecondarySkill) + 1;
                        if (chanceForThirdSkill <= _settings.SecondarySkillChance)
                            skills.C = _rnd.Next(0, _settings.MaxSecondarySkill) + 1;
                        break;
                    case "C":
                        if (chanceForSecondSkill <= _settings.SecondarySkillChance)
                            skills.T = _rnd.Next(0, _settings.MaxSecondarySkill) + 1;
                        if (chanceForThirdSkill <= _settings.SecondarySkillChance)
                            skills.G = _rnd.Next(0, _settings.MaxSecondarySkill) + 1;
                        break;
                    case "TE":
                        if (chanceForSecondSkill <= _settings.SecondarySkillChance)
                            skills.WR = _rnd.Next(0, _settings.MaxSecondarySkill) + 1;
                        break;
                    case "WR":
                        if (chanceForSecondSkill <= _settings.SecondarySkillChance)
                            skills.TE = _rnd.Next(0, _settings.MaxSecondarySkill) + 1;
                        if (chanceForThirdSkill <= _settings.SecondarySkillChance)
                            skills.RB = _rnd.Next(0, _settings.MaxSecondarySkill) + 1;
                        break;
                    case "CB":
                        if (chanceForSecondSkill <= _settings.SecondarySkillChance)
                            skills.FS = _rnd.Next(0, _settings.MaxSecondarySkill) + 1;
                        if (chanceForThirdSkill <= _settings.SecondarySkillChance)
                            skills.SS = _rnd.Next(0, _settings.MaxSecondarySkill) + 1;
                        break;
                    case "LB":
                        if (chanceForSecondSkill <= _settings.SecondarySkillChance)
                            skills.SS = _rnd.Next(0, _settings.MaxSecondarySkill) + 1;
                        if (chanceForThirdSkill <= _settings.SecondarySkillChance)
                            skills.DE = _rnd.Next(0, _settings.MaxSecondarySkill) + 1;
                        break;
                    case "DT":
                        if (chanceForSecondSkill <= _settings.SecondarySkillChance)
                            skills.DE = _rnd.Next(0, _settings.MaxSecondarySkill) + 1;
                        break;
                    case "DE":
                        if (chanceForSecondSkill <= _settings.SecondarySkillChance)
                            skills.DT = _rnd.Next(0, _settings.MaxSecondarySkill) + 1;
                        if (chanceForThirdSkill <= _settings.SecondarySkillChance)
                            skills.LB = _rnd.Next(0, _settings.MaxSecondarySkill) + 1;
                        break;
                    case "FS":
                        if (chanceForSecondSkill <= _settings.SecondarySkillChance)
                            skills.CB = _rnd.Next(0, _settings.MaxSecondarySkill) + 1;
                        if (chanceForThirdSkill <= _settings.SecondarySkillChance)
                            skills.SS = _rnd.Next(0, _settings.MaxSecondarySkill) + 1;
                        break;
                    case "SS":
                        if (chanceForSecondSkill <= _settings.SecondarySkillChance)
                            skills.LB = _rnd.Next(0, _settings.MaxSecondarySkill) + 1;
                        if (chanceForThirdSkill <= _settings.SecondarySkillChance)
                            skills.FS = _rnd.Next(0, _settings.MaxSecondarySkill) + 1;
                        break;
                    case "K":
                        if (chanceForSecondSkill <= _settings.SecondarySkillChance)
                            skills.P = _rnd.Next(0, _settings.MaxSecondarySkill) + 1;
                        break;
                    case "P":
                        if (chanceForSecondSkill <= _settings.SecondarySkillChance)
                            skills.K = _rnd.Next(0, _settings.MaxSecondarySkill) + 1;
                        break;
                }
                return skills;
            }
            catch (Exception e)
            {
                _log.Error(e.Message);
                return null;
            }
        }
        private string SelectFromWeightedList(List<WeightedListModel> packageList)
        {
            try
            {
                string result = string.Empty;

                int totalWeight = packageList.Sum(p => p.Weight);
                int roll = _rnd.Next(0, totalWeight) + 1;

                bool found = false;
                int index = 0;
                int runningCount = 0;
                do
                {
                    if (index == 50)
                        Console.WriteLine("here");
                    runningCount += packageList[index].Weight;
                    if (roll <= runningCount)
                    {
                        found = true;
                        result = packageList[index].Item;
                    }

                    index++;
                } while (!found && index < packageList.Count);

                return result;

            }
            catch (Exception e)
            {
                _log.Error("SelectFromWeightedList - " + e.Message);
                return null;
            }
        }
        private Dictionary<string, List<SearchPlayerExportCSVModel>> SplitPositions(List<SearchPlayerExportCSVModel> allPlayers)
        {
            try
            {
                Dictionary<string, List<SearchPlayerExportCSVModel>> playerBreakout = new Dictionary<string, List<SearchPlayerExportCSVModel>>();

                foreach (var player in allPlayers)
                {

                    if (playerBreakout.ContainsKey(player.Pos))
                    {
                        playerBreakout[player.Pos].Add(player);
                    }
                    else
                    {
                        playerBreakout.Add(player.Pos, new List<SearchPlayerExportCSVModel>() { player });
                    }
                }

                return playerBreakout;
            }
            catch (Exception e)
            {
                _log.Error("Error running SplitPositions - " + e.Message);
                return null;
            }
        }
        #endregion
    }
}
