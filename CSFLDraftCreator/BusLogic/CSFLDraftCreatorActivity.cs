using AutoMapper;
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
        private List<string> _positionList = new List<string> { "QB", "RB", "FB", "G", "T", "C", "TE", "WR", "CB", "LB", "DT", "DE", "FS", "SS", "K", "P" };
        private Random _rnd = new Random();
        public CSFLDraftCreatorActivity(Logger log, AppSettingsModel settings)
        {
            _log = log;
            _settings = settings;
        }

        #region public
        public void ConvertDraftClass(string outputFile, string activePlayersCSV_Location, string draftClassCSV_Location)
        {
            try
            {
                _log.Information("Starting Draft Class Conversion");

                if (string.IsNullOrEmpty(outputFile) || string.IsNullOrEmpty(activePlayersCSV_Location) || string.IsNullOrEmpty(draftClassCSV_Location))
                {
                    _log.Error("Cannot convert the draft class as an input is incorrect");
                    return;
                }

                _log.Information("Getting leagues position and attribute percentile break downs");
                Dictionary<string, List<PlayerAttributesModel>> posPercentileTiers = GetPercentileDictionary(activePlayersCSV_Location);
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

                AllPlayersModel drafeeExportList = new AllPlayersModel();
                drafeeExportList.Players = new List<PlayerModel>();
                foreach (var draftee in drafteeCSVData)
                {
                    //reseed our random generator... 
                    _rnd = new Random(Guid.NewGuid().GetHashCode());

                    int height = ConvertHeight(draftee.Height);
                    if (height == -1)
                    {
                        _log.Error($"Invalid Height found ({draftee.Height}) for {draftee.FirstName} {draftee.LastName}");
                        return;
                    }

                    //Copy Base info from draft list csv to new draft record
                    PlayerModel drafteeExport = new PlayerModel();
                    drafteeExport.First = draftee.FirstName;
                    drafteeExport.Last = draftee.LastName;
                    drafteeExport.Pos = GetPosition(draftee.Position);
                    drafteeExport.Age = draftee.Age;
                    drafteeExport.Hgt = height;
                    drafteeExport.Wgt = draftee.Weight;
                    drafteeExport.Coll = draftee.College;
                    drafteeExport.Trait = draftee.Trait;

                    if (string.IsNullOrEmpty(drafteeExport.Pos))
                    {
                        _log.Error($"Invalid position({draftee.Position}) for {draftee.FirstName} {draftee.LastName}");
                        return;
                    }

                    drafteeExport.Per = RandomizePersonality();
                    drafteeExport.Skills = RandomizeSecondarySkills(draftee.Position);
                    drafteeExport = RandomizeAttributes(drafteeExport, draftee.Tier, posPercentileTiers);

                    if (drafteeExport == null || drafteeExport.Per == null || drafteeExport.Attr == null || drafteeExport.Attr == null)
                    {
                        _log.Error($"Something when wrong processing {draftee.FirstName} {draftee.LastName} - {draftee.Position}");
                        return;
                    }

                    drafteeExport = SetupPlayerTags(drafteeExport, posPercentileTiers, draftee);
                    if (drafteeExport == null)
                    {
                        _log.Error("ConvertUpcomingDraftCSVToJSON - cannot set players attributes");
                        return;
                    }

                    drafeeExportList.Players.Add(drafteeExport);
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

                string playerJSONData = JsonConvert.SerializeObject(drafeeExportList);
                File.WriteAllText(jsonFilename, playerJSONData);

                List<UpcomingDraftPlayerCSVModel> playerCSVData = ConvertAllPlayersModelToUpcomingDraftList(drafeeExportList);
                using (var writer = new StreamWriter(csvFilename))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(playerCSVData);
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
                Dictionary<string, List<PlayerAttributesModel>> posPercentileTiers = GetPercentileDictionary(activePlayersCSV_Location);
                if (posPercentileTiers == null || posPercentileTiers.Count() == 0)
                {
                    _log.Error("Cannot get active player percentile calculations");
                    return;
                }

                
                string outputData = FormatAllPlayerSummaryForOutput(posPercentileTiers);
                File.WriteAllText(outputFile, outputData);

                _log.Information($"File created: {outputFile}");

            }
            catch (Exception e)
            {
                _log.Error("Error running GetLeaguePositionSummary - " + e.Message);
            }
        }
        #endregion

        #region private
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
                if (string.IsNullOrEmpty(height))
                    return -1;

                int tickLocation = height.IndexOf("'");
                //Console.WriteLine("tick location = " + tickLocation);
                int newHeight = 0;

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
        private Dictionary<string, List<PlayerAttributesModel>> GetPercentileDictionary(string activePlayersCSV_Location)
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
        private string GetPosition(string pos)
        {
            try
            {
                string newPos = string.Empty;
                if (_positionList.Contains(pos.ToUpper()))
                {
                    newPos = pos.ToUpper();
                }
                else if (pos.ToUpper() == "DB")
                {
                    //Convert to a random DB ... i.e CB, FS, SS
                    int r = _rnd.Next(1, 3);
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
                    int r = _rnd.Next(1, 2);
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
                    int r = _rnd.Next(1, 3);
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
                    int r = _rnd.Next(1, 2);
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
        private string FormatAllPlayerSummaryForOutput(Dictionary<string, List<PlayerAttributesModel>> posPercentileTiers)
        {
            try
            {
                StringBuilder s = new StringBuilder();
                s.AppendLine("<!DOCTYPE html>");
                s.AppendLine("<html>");

                foreach (string pos in _positionList)
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
        private PlayerModel RandomizeAttributes(PlayerModel player, string tier, Dictionary<string, List<PlayerAttributesModel>> posPercentileTiers)
        {
            try
            {

                List<string> attributes = new List<string> { "Str", "Agi", "Arm", "Spe", "Han", "Intel", "Acc", "PBl", "RBl", "Tck", "KDi", "KAc", "End" };

                //get the active player pos so we can use this to set our attrubutes
                List<PlayerAttributesModel> posAttributes = posPercentileTiers[player.Pos];

                //find our grade from the tier listing in the draft file
                TierDefinitionModel tierInfo = _settings.TierDefinitions.Where(t => t.Id.ToLower() == tier.ToLower()).FirstOrDefault();
                if (tierInfo == null)
                {
                    _log.Error("SetPlayersNewAttributes - tier names in csv does not match tier id in settings");
                    return null;
                }

                //convert grades of min and max to a useful index needed to pull the info from our active player position 
                int keyMinPer = tierInfo.KeyMin / 5;
                int keyMaxPer = tierInfo.KeyMax / 5;
                int secMinPer = tierInfo.SecMin / 5;
                int secMaxPer = tierInfo.SecMax / 5;

                //Get Lists for skills
                PostionalSkillsModel skillModel = _settings.PostionalSkills.Where(s => s.Position.ToLower() == player.Pos.ToLower()).FirstOrDefault();
                if (skillModel == null)
                {
                    _log.Error($"Could not find position {player.Pos} defined in PostionalSkills");
                    return null;
                }

                //if (player.Pos == "QB")
                //    Console.WriteLine("here");

                //Go through each attribute and set a random value from min to max indexes
                foreach (string attrToGet in attributes)
                {
                    string attType = "unimporant";

                    int minPosRating = 0;
                    int maxPosRating = 0;

                    if (skillModel.KeySkill.Contains(attrToGet.ToUpper()))
                    {
                        //use reflexion to pull the property of the attribute to get (e.g. str, intel) for min number
                        PlayerAttributesModel minPosAttr = posAttributes[keyMinPer];
                        Type mintype = minPosAttr.GetType();
                        PropertyInfo minPosProp = mintype.GetProperty(attrToGet);
                        minPosRating = (int)minPosProp.GetValue(minPosAttr, null);

                        //use reflexion to pull the property of the attribute to get (e.g. str, intel) for max number
                        PlayerAttributesModel maxPosAttr = posAttributes[keyMaxPer];
                        Type maxtype = maxPosAttr.GetType();
                        PropertyInfo maxPosProp = maxtype.GetProperty(attrToGet);
                        maxPosRating = (int)maxPosProp.GetValue(maxPosAttr, null);
                    }
                    else if (skillModel.SecondarySkill.Contains(attrToGet.ToUpper()))
                    {
                        //use reflexion to pull the property of the attribute to get (e.g. str, intel) for min number
                        PlayerAttributesModel minPosAttr = posAttributes[secMinPer];
                        Type mintype = minPosAttr.GetType();
                        PropertyInfo minPosProp = mintype.GetProperty(attrToGet);
                        minPosRating = (int)minPosProp.GetValue(minPosAttr, null);

                        //use reflexion to pull the property of the attribute to get (e.g. str, intel) for max number
                        PlayerAttributesModel maxPosAttr = posAttributes[secMaxPer];
                        Type maxtype = maxPosAttr.GetType();
                        PropertyInfo maxPosProp = maxtype.GetProperty(attrToGet);
                        maxPosRating = (int)maxPosProp.GetValue(maxPosAttr, null);
                    }
                    else //Unimportant so just randomize
                    {
                        //use reflexion to pull the property of the attribute to get (e.g. str, intel) for min number
                        PlayerAttributesModel minPosAttr = posAttributes[0];
                        Type mintype = minPosAttr.GetType();
                        PropertyInfo minPosProp = mintype.GetProperty(attrToGet);
                        minPosRating = (int)minPosProp.GetValue(minPosAttr, null);

                        //use reflexion to pull the property of the attribute to get (e.g. str, intel) for max number
                        PlayerAttributesModel maxPosAttr = posAttributes[19];
                        Type maxtype = maxPosAttr.GetType();
                        PropertyInfo maxPosProp = maxtype.GetProperty(attrToGet);
                        maxPosRating = (int)maxPosProp.GetValue(maxPosAttr, null);
                    }


                    //Randomize the attribute
                    int newAttrRating = _rnd.Next(minPosRating, maxPosRating);

                    //use reflexion to update our attribute object to send back by the method
                    Type playerType = player.Attr.GetType();
                    PropertyInfo playerProp = playerType.GetProperty(attrToGet);
                    playerProp.SetValue(player.Attr, Convert.ChangeType(newAttrRating, playerProp.PropertyType), null);

                }

                //Work Ethic 
                if (tierInfo.WE == 0)
                    tierInfo.WE = 1;
                if (tierInfo.WE >= 0)
                {
                    player.Per.Wor = _rnd.Next(tierInfo.WE > 0 ? tierInfo.WE : 25, 99);
                }

                //Endurance
                if (tierInfo.End == 0)
                    tierInfo.End = 1;
                if (tierInfo.End >= 0)
                {
                    player.Attr.End = _rnd.Next(tierInfo.End > 0 ? tierInfo.WE : 25, 99);
                }

                //Skill
                int minSkill = 1;
                int maxSkill = 99;

                if (tierInfo.Skill >= 0)
                {
                    minSkill = tierInfo.Skill;
                    maxSkill = 105 - (40 - (keyMaxPer * 2));

                    if (minSkill > maxSkill)
                    {
                        int temp = minSkill;
                        minSkill = maxSkill;
                        maxSkill = minSkill;
                    }

                    if (maxSkill >= 100)
                        maxSkill = 99;

                    if (minSkill <= 25)
                        minSkill = 25;

                }

                int skill = _rnd.Next(minSkill, maxSkill);
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

                personality.Lea = _rnd.Next(5, 99);
                personality.Wor = _rnd.Next(5, 99);
                personality.Com = _rnd.Next(5, 99);
                personality.TmPl = _rnd.Next(5, 99);
                personality.Spor = _rnd.Next(5, 99);
                personality.Soc = _rnd.Next(5, 99);
                personality.Mny = _rnd.Next(5, 99);
                personality.Sec = _rnd.Next(5, 99);
                personality.Loy = _rnd.Next(5, 99);
                personality.Win = _rnd.Next(5, 99);
                personality.PT = _rnd.Next(5, 99);
                personality.Home = _rnd.Next(5, 99);
                personality.Mkt = _rnd.Next(5, 99);
                personality.Mor = _rnd.Next(5, 99);

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

                const int SECONDARY_CHANCE = 30;  //Percentage of a secondary skills being trained
                const int MAX_SKILL_ALLOWED = 35;  //Percentage of a secondary skills being trained
                int chanceForSecondSkill = _rnd.Next(1, 100); //rolls to set the skill
                int chanceForThirdSkill = _rnd.Next(1, 100);


                switch (pos)
                {
                    case "QB":
                        break;
                    case "RB":
                        if (chanceForSecondSkill <= SECONDARY_CHANCE)
                            skills.FB = _rnd.Next(1, MAX_SKILL_ALLOWED);
                        if (chanceForThirdSkill <= SECONDARY_CHANCE)
                            skills.WR = _rnd.Next(1, MAX_SKILL_ALLOWED);
                        break;
                    case "FB":
                        if (chanceForSecondSkill <= SECONDARY_CHANCE)
                            skills.RB = _rnd.Next(1, MAX_SKILL_ALLOWED);
                        break;
                    case "G":
                        if (chanceForSecondSkill <= SECONDARY_CHANCE)
                            skills.T = _rnd.Next(1, MAX_SKILL_ALLOWED);
                        if (chanceForThirdSkill <= SECONDARY_CHANCE)
                            skills.C = _rnd.Next(1, MAX_SKILL_ALLOWED);
                        break;
                    case "T":
                        if (chanceForSecondSkill <= SECONDARY_CHANCE)
                            skills.G = _rnd.Next(1, MAX_SKILL_ALLOWED);
                        if (chanceForThirdSkill <= SECONDARY_CHANCE)
                            skills.C = _rnd.Next(1, MAX_SKILL_ALLOWED);
                        break;
                    case "C":
                        if (chanceForSecondSkill <= SECONDARY_CHANCE)
                            skills.T = _rnd.Next(1, MAX_SKILL_ALLOWED);
                        if (chanceForThirdSkill <= SECONDARY_CHANCE)
                            skills.G = _rnd.Next(1, MAX_SKILL_ALLOWED);
                        break;
                    case "TE":
                        if (chanceForSecondSkill <= SECONDARY_CHANCE)
                            skills.WR = _rnd.Next(1, MAX_SKILL_ALLOWED);
                        break;
                    case "WR":
                        if (chanceForSecondSkill <= SECONDARY_CHANCE)
                            skills.TE = _rnd.Next(1, MAX_SKILL_ALLOWED);
                        if (chanceForThirdSkill <= SECONDARY_CHANCE)
                            skills.RB = _rnd.Next(1, MAX_SKILL_ALLOWED);
                        break;
                    case "CB":
                        if (chanceForSecondSkill <= SECONDARY_CHANCE)
                            skills.FS = _rnd.Next(1, MAX_SKILL_ALLOWED);
                        if (chanceForThirdSkill <= SECONDARY_CHANCE)
                            skills.SS = _rnd.Next(1, MAX_SKILL_ALLOWED);
                        break;
                    case "LB":
                        if (chanceForSecondSkill <= SECONDARY_CHANCE)
                            skills.SS = _rnd.Next(1, MAX_SKILL_ALLOWED);
                        if (chanceForThirdSkill <= SECONDARY_CHANCE)
                            skills.DE = _rnd.Next(1, MAX_SKILL_ALLOWED);
                        break;
                    case "DT":
                        if (chanceForSecondSkill <= SECONDARY_CHANCE)
                            skills.DE = _rnd.Next(1, MAX_SKILL_ALLOWED);
                        break;
                    case "DE":
                        if (chanceForSecondSkill <= SECONDARY_CHANCE)
                            skills.DT = _rnd.Next(1, MAX_SKILL_ALLOWED);
                        if (chanceForThirdSkill <= SECONDARY_CHANCE)
                            skills.LB = _rnd.Next(1, MAX_SKILL_ALLOWED);
                        break;
                    case "FS":
                        if (chanceForSecondSkill <= SECONDARY_CHANCE)
                            skills.CB = _rnd.Next(1, MAX_SKILL_ALLOWED);
                        if (chanceForThirdSkill <= SECONDARY_CHANCE)
                            skills.SS = _rnd.Next(1, MAX_SKILL_ALLOWED);
                        break;
                    case "SS":
                        if (chanceForSecondSkill <= SECONDARY_CHANCE)
                            skills.LB = _rnd.Next(1, MAX_SKILL_ALLOWED);
                        if (chanceForThirdSkill <= SECONDARY_CHANCE)
                            skills.FS = _rnd.Next(1, MAX_SKILL_ALLOWED);
                        break;
                    case "K":
                        if (chanceForSecondSkill <= SECONDARY_CHANCE)
                            skills.P = _rnd.Next(1, MAX_SKILL_ALLOWED);
                        break;
                    case "P":
                        if (chanceForSecondSkill <= SECONDARY_CHANCE)
                            skills.K = _rnd.Next(1, MAX_SKILL_ALLOWED);
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
        private PlayerModel SetupPlayerTags(PlayerModel player, Dictionary<string, List<PlayerAttributesModel>> posPercentileTiers, DraftClassInputModel playerCSV)
        {
            try
            {

                TierDefinitionModel tierInfo = _settings.TierDefinitions.Where(t => t.Id.ToLower() == playerCSV.Tier.ToLower()).FirstOrDefault();
                if (tierInfo == null)
                {
                    _log.Error("SetPlayersNewAttributes - tier names in csv does not match tier id in settings");
                    return null;
                }

                TraitConverterService traitConverter = new TraitConverterService(_settings, playerCSV.Tier.ToLower(), posPercentileTiers);

                if (tierInfo.AllowTag || !string.IsNullOrEmpty(player.Trait))
                {
                    List<string> traits = new List<string>();
                    if (string.IsNullOrEmpty(player.Trait))
                    {
                        bool posTagAdded = false;
                        bool perTagAdded = false;

                        //first check for positional tags
                        int rollD100 = _rnd.Next(1, 100);
                        if (rollD100 <= _settings.PositionalTagPercentage)
                        {
                            string traitName = traitConverter.GetPositionalTrait(player.Pos, traits);
                            traits.Add(traitName);

                            posTagAdded = true;
                        }
                        
                        //next roll for personality 
                        rollD100 = _rnd.Next(1, 100);
                        if (rollD100 <= _settings.PersonalityTagPercetage)
                        {
                            string traitName = traitConverter.GetPersonalityTrait(traits);
                            traits.Add(traitName);

                            perTagAdded = true;
                        }

                        //now check if we need to double tag... only need to do this if only one tag is found
                        if ((perTagAdded && !posTagAdded) || (!perTagAdded && posTagAdded))
                        {
                            rollD100 = _rnd.Next(1, 100);
                            if (rollD100 <= _settings.AddSecondTagPercentage)
                            {
                                if (posTagAdded)
                                {
                                    string traitName = traitConverter.GetPersonalityTrait(traits);
                                    traits.Add(traitName);
                                }
                                else
                                {
                                    string traitName = traitConverter.GetPositionalTrait(player.Pos, traits);
                                    traits.Add(traitName);

                                }
                            }
                        }
                    }
                    else
                    {
                        string[] existingTraits = player.Trait.Split('|');
                        if (existingTraits != null)
                        {
                            traits = existingTraits.ToList();
                        }
                        else
                        {
                            traits.Add(player.Trait);
                        }
                    }

                    if (traits.Count > 0)
                    {
                        //RunningQB is actually a flag not a trait so treat it as such
                        int runningQBIndex = traits.IndexOf("RunningQB");
                        if (runningQBIndex >= 0)
                        {
                            traits.RemoveAt(runningQBIndex);
                            player.Flg = "RunningQB";
                        }
                        
                        if (traits.Count > 0)
                            player.Trait = string.Join("|", traits);

                    }

                    foreach (string trait in traits)
                    {
                        switch (trait)
                        {
                            case "AllPurposeRB":
                                player = traitConverter.SetAllPurposeRB(player);
                                break;
                            case "AthBlocker":
                                player = traitConverter.SetAthBlocker(player);
                                break;
                            case "Athlete":
                                player = traitConverter.SetAthlete(player);
                                break;
                            case "BlockingFB":
                                player = traitConverter.SetBlockingFB(player);
                                break;
                            case "BlockingTE":
                                player = traitConverter.SetBlockingTE(player);
                                break;
                            case "BoxSafety":
                                player = traitConverter.SetBoxSafety(player);
                                break;
                            case "BookEndTackle":
                                player = traitConverter.SetBookEndTackle(player);
                                break;
                            case "BullRusher":
                                player = traitConverter.SetBullRusher(player);
                                break;
                            case "Centerfielder":
                                player = traitConverter.SetCenterfielder(player);
                                break;
                            case "ClutchKicker":
                                player = traitConverter.SetClutchKicker(player);
                                break;
                            case "ClutchQB":
                                player = traitConverter.SetClutchQB(player);
                                break;
                            case "Competitor":
                                player = traitConverter.SetCompetitor(player);
                                break;
                            case "CommunityBenefactor":
                                //No adj needed
                                break;
                            case "ConsummatePro":
                                player = traitConverter.SetConsummatePro(player);
                                break;
                            case "CoverageLB":
                                player = traitConverter.SetCoverageLB(player);
                                break;
                            case "DeepThreat":
                                player = traitConverter.SetDeepThreat(player);
                                break;
                            case "Distraction":
                                //No adj needed
                                break;
                            case "Diva":
                                player = traitConverter.SetDiva(player);
                                break;
                            case "Dualthreat":
                                player = traitConverter.SetDualthreat(player);
                                break;
                            case "FilmGeek":
                                player = traitConverter.SetFilmGeek(player);
                                break;
                            case "FanFavorite":
                                player = traitConverter.SetFanFavorite(player);
                                break;
                            case "GameManager":
                                player = traitConverter.SetGameManager(player);
                                break;
                            case "Gunslinger":
                                player = traitConverter.SetGunslinger(player);
                                break;
                            case "HybridLB":
                                player = traitConverter.SetHybridLB(player);
                                break;
                            case "Journeyman":
                                player = traitConverter.SetJourneyman(player);
                                break;
                            case "LockerLeader":
                                player = traitConverter.SetLockerLeader(player);
                                break;
                            case "MediaDarling":
                                //No adj needed
                                break;
                            case "NoseTackle":
                                player = traitConverter.SetNoseTackle(player);
                                break;
                            case "PossessionWR":
                                player = traitConverter.SetPossessionWR(player);
                                break;
                            case "Perceptive":
                                player = traitConverter.SetPerceptive(player);
                                break;
                            case "PowerKicker":
                                player = traitConverter.SetPowerKicker(player);
                                break;
                            case "PowerRunner":
                                player = traitConverter.SetPowerRunner(player);
                                break;
                            case "PressCorner":
                                player = traitConverter.SetPressCorner(player);
                                break;
                            case "ProBloodline":
                                player = traitConverter.SetProBloodline(player);
                                break;
                            case "RawTalent":
                                player = traitConverter.SetRawTalent(player);
                                break;
                            case "ReceiveFB":
                                player = traitConverter.SetReceiveFB(player);
                                break;
                            case "ReceivingTE":
                                player = traitConverter.SetReceivingTE(player);
                                break;
                            case "RunningQB":
                                player = traitConverter.SetRunningQB(player);
                                break;
                            case "RoleModel":
                                player = traitConverter.SetRoleModel(player);
                                break;
                            case "ScatBack":
                                player = traitConverter.SetScatBack(player);
                                break;
                            case "ShutDownCorner":
                                player = traitConverter.SetShutDownCorner(player);
                                break;
                            case "SlotCorner":
                                player = traitConverter.SetSlotCorner(player);
                                break;
                            case "SlotReceiver":
                                player = traitConverter.SetSlotReceiver(player);
                                break;
                            case "SpeedRusher":
                                player = traitConverter.SetSpeedRusher(player);
                                break;
                            case "TenaciousBlocker":
                                player = traitConverter.SetTenaciousBlocker(player);
                                break;
                            case "TeamPlayer":
                                player = traitConverter.SetTeamPlayer(player);
                                break;
                            case "Thumper":
                                player = traitConverter.SetThumper(player);
                                break;
                            case "WorkoutFanatic":
                                player = traitConverter.SetWorkoutFanatic(player);
                                break;
                            case "ZoneCorner":
                                player = traitConverter.SetZoneCorner(player);
                                break;
                        }
                    }
                }

                return player;
            }
            catch (Exception e)
            {
                _log.Error("SetupPlayerTags - " + e.Message);
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
