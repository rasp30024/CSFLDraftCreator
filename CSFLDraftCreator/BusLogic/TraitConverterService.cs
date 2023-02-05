using CSFLDraftCreator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CSFLDraftCreator.BusLogic
{
    internal class TraitConverterService
    {
        private Random _rnd = new Random(Guid.NewGuid().GetHashCode());
        private TierDefinitionModel _tierInfo = null;
        private Dictionary<string, List<PlayerAttributesModel>> _posPercentileTiers = null;
        private AppSettingsModel _settings;
        private List<string> _attributes = new List<string> { "Str", "Agi", "Arm", "Spe", "Han", "Intel", "Acc", "PBl", "RBl", "Tck", "KDi", "KAc", "End" };
        public TraitConverterService(AppSettingsModel settings, string tier, Dictionary<string, List<PlayerAttributesModel>> posPercentileTiers)
        {
            _posPercentileTiers = posPercentileTiers;
            _settings = settings;
            _tierInfo = _settings.TierDefinitions.Where(t => t.Id.ToLower() == tier.ToLower()).FirstOrDefault();
            if (_tierInfo == null)
            {
                new NullReferenceException("tier names in csv does not match tier id in settings");
            }
        }

        public string GetTrait(string pos)
        {

            Dictionary<string, List<string>> traitsByPosition = new Dictionary<string, List<string>>();
            traitsByPosition.Add("QB", _settings.PosTraits.QB);
            traitsByPosition.Add("RB", _settings.PosTraits.RB);
            traitsByPosition.Add("FB", _settings.PosTraits.FB);
            traitsByPosition.Add("C", _settings.PosTraits.C);
            traitsByPosition.Add("G", _settings.PosTraits.G);
            traitsByPosition.Add("T", _settings.PosTraits.T);
            traitsByPosition.Add("TE", _settings.PosTraits.TE);
            traitsByPosition.Add("WR", _settings.PosTraits.WR);
            traitsByPosition.Add("CB", _settings.PosTraits.CB);
            traitsByPosition.Add("LB", _settings.PosTraits.LB);
            traitsByPosition.Add("DE", _settings.PosTraits.DE);
            traitsByPosition.Add("DT", _settings.PosTraits.DT);
            traitsByPosition.Add("FS", _settings.PosTraits.FS);
            traitsByPosition.Add("SS", _settings.PosTraits.SS);
            traitsByPosition.Add("K", _settings.PosTraits.K);
            traitsByPosition.Add("P", _settings.PosTraits.P);

            //traitsByPosition.Add("QB", new List<string>() { "ClutchQB", "Dualthreat", "GameManager", "Gunslinger", "BadInfluence", "InjuryProne", "LockerLeader", "FilmGeek", "ProBloodline", "WorkoutFanatic" });
            //traitsByPosition.Add("RB", new List<string>() { "AllPurposeRB", "PowerRunner", "ScatBack", "BadInfluence", "InjuryProne", "LockerLeader", "FilmGeek", "ProBloodline", "WorkoutFanatic" });
            //traitsByPosition.Add("FB", new List<string>() { "BlockingFB", "PowerRunner", "ReceiveFB", "BadInfluence", "InjuryProne", "LockerLeader", "FilmGeek", "ProBloodline", "WorkoutFanatic" });
            //traitsByPosition.Add("C", new List<string>() { "AthBlocker", "TenaciousBlocker", "BadInfluence", "InjuryProne", "LockerLeader", "FilmGeek", "ProBloodline", "WorkoutFanatic" });
            //traitsByPosition.Add("G", new List<string>() { "AthBlocker", "TenaciousBlocker", "BadInfluence", "InjuryProne", "LockerLeader", "FilmGeek", "ProBloodline", "WorkoutFanatic" });
            //traitsByPosition.Add("T", new List<string>() { "AthBlocker", "TenaciousBlocker", "BadInfluence", "InjuryProne", "LockerLeader", "FilmGeek", "ProBloodline", "WorkoutFanatic" });
            //traitsByPosition.Add("TE", new List<string>() { "BlockingTE", "ReceivingTE", "BadInfluence", "InjuryProne", "LockerLeader", "FilmGeek", "ProBloodline", "WorkoutFanatic" });
            //traitsByPosition.Add("WR", new List<string>() { "DeepThreat", "PossessionWR", "SlotReceiver", "BadInfluence", "InjuryProne", "LockerLeader", "FilmGeek", "ProBloodline", "WorkoutFanatic" });
            //traitsByPosition.Add("CB", new List<string>() { "PressCorner", "ShutDownCorner", "SlotCorner", "BadInfluence", "InjuryProne", "LockerLeader", "FilmGeek", "ProBloodline", "WorkoutFanatic" });
            //traitsByPosition.Add("LB", new List<string>() { "CoverageLB", "HybridLB", "Thumper", "BadInfluence", "InjuryProne", "LockerLeader", "FilmGeek", "ProBloodline", "WorkoutFanatic" });
            //traitsByPosition.Add("DE", new List<string>() { "BullRusher", "SpeedRusher", "BadInfluence", "InjuryProne", "LockerLeader", "FilmGeek", "ProBloodline", "WorkoutFanatic" });
            //traitsByPosition.Add("DT", new List<string>() { "BullRusher", "NoseTackle", "BadInfluence", "InjuryProne", "LockerLeader", "FilmGeek", "ProBloodline", "WorkoutFanatic" });
            //traitsByPosition.Add("FS", new List<string>() { "BoxSafety", "Centerfielder", "BadInfluence", "InjuryProne", "LockerLeader", "FilmGeek", "ProBloodline", "WorkoutFanatic" });
            //traitsByPosition.Add("SS", new List<string>() { "BoxSafety", "Centerfielder", "BadInfluence", "InjuryProne", "LockerLeader", "FilmGeek", "ProBloodline", "WorkoutFanatic" });
            //traitsByPosition.Add("K", new List<string>() { "ClutchKicker", "PowerKicker", "BadInfluence", "InjuryProne", "LockerLeader", "FilmGeek", "ProBloodline", "WorkoutFanatic" });
            //traitsByPosition.Add("P", new List<string>() { "ClutchKicker", "PowerKicker", "BadInfluence", "InjuryProne", "LockerLeader", "FilmGeek", "ProBloodline", "WorkoutFanatic" });

            _rnd = new Random(Guid.NewGuid().GetHashCode());
            List<string> traits = traitsByPosition[pos];
            Shuffle<string>(traits);
            int indexToGet = _rnd.Next(0, traits.Count - 1);

            return traits[indexToGet];

        }

        private PlayerModel UpdateCriticalValues(List<string> attributes, PlayerModel player)
        {
            int minPer = _tierInfo.Min / 5;
            int maxPer = _tierInfo.Max / 5;

            List<PlayerAttributesModel> posAttributes = _posPercentileTiers[player.Pos];

            //Compare each score to thier respective percenage and record result
            foreach (string attrToGet in attributes)
            {

                PlayerAttributesModel minPosAttr = posAttributes[minPer];
                Type mintype = minPosAttr.GetType();
                PropertyInfo minPosProp = mintype.GetProperty(attrToGet);
                int minPosRating = (int)minPosProp.GetValue(minPosAttr, null);

                PlayerAttributesModel maxPosAttr = posAttributes[maxPer];
                Type maxtype = maxPosAttr.GetType();
                PropertyInfo maxPosProp = maxtype.GetProperty(attrToGet);
                int maxPosRating = (int)maxPosProp.GetValue(maxPosAttr, null);

                minPosRating += (int)Math.Floor((maxPosRating - minPosRating) * .65);

                int newAttrRating = _rnd.Next(minPosRating, maxPosRating);

                Type playerType = player.Attr.GetType();
                PropertyInfo playerProp = playerType.GetProperty(attrToGet);
                int currentAtt = (int)maxPosProp.GetValue(maxPosAttr, null);
                if (currentAtt < minPosRating)
                    playerProp.SetValue(player.Attr, Convert.ChangeType(newAttrRating, playerProp.PropertyType), null);


            }

            return player;
        }
        private void Shuffle<T>(IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = _rnd.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
        public PlayerModel SetAllPurposeRB(PlayerModel player)
        {
            try
            {
                List<string> criticalAttributes = new List<string>() { "Str", "Agi", "Spe", "Han" };
                player = UpdateCriticalValues(criticalAttributes, player);

                return player;
            }
            catch
            {
                throw;
            }
        }
        public PlayerModel SetAthBlocker(PlayerModel player)
        {
            try
            {
                List<string> criticalAttributes = new List<string>() { "Str", "Agi", "Spe" };
                player = UpdateCriticalValues(criticalAttributes, player);

                return player;
            }
            catch
            {
                throw;
            }
        }
        public PlayerModel SetBlockingFB(PlayerModel player)
        {
            try
            {
                List<string> criticalAttributes = new List<string>() { "Str", "Agi", "Spe", "PBl", "RBl" };
                player = UpdateCriticalValues(criticalAttributes, player);

                return player;
            }
            catch
            {
                throw;
            }
        }
        public PlayerModel SetBlockingTE(PlayerModel player)
        {
            try
            {
                List<string> criticalAttributes = new List<string>() { "Str", "Agi", "Spe", "PBl", "RBl" };
                player = UpdateCriticalValues(criticalAttributes, player);

                return player;
            }
            catch
            {
                throw;
            }
        }
        public PlayerModel SetBoxSafety(PlayerModel player)
        {
            try
            {
                List<string> criticalAttributes = new List<string>() { "Str", "Tck", "End" };
                player = UpdateCriticalValues(criticalAttributes, player);

                return player;
            }
            catch
            {
                throw;
            }
        }
        public PlayerModel SetBullRusher(PlayerModel player)
        {
            try
            {
                List<string> criticalAttributes = new List<string>() { "Str", "Agi", "Tck", "End" };
                player = UpdateCriticalValues(criticalAttributes, player);

                //competitiveness
                if (player.Per.Com < 65)
                    player.Per.Com = _rnd.Next(65, 100);


                return player;
            }
            catch
            {
                throw;
            }
        }
        public PlayerModel SetCenterfielder(PlayerModel player)
        {
            try
            {
                List<string> criticalAttributes = new List<string>() { "Agi", "Spe", "Han", "Intel" };
                player = UpdateCriticalValues(criticalAttributes, player);

                return player;
            }
            catch
            {
                throw;
            }
        }
        public PlayerModel SetClutchKicker(PlayerModel player)
        {
            try
            {

                //team player
                if (player.Per.TmPl < 65)
                    player.Per.TmPl = _rnd.Next(65, 100);

                //competitiveness
                if (player.Per.Com < 65)
                    player.Per.Com = _rnd.Next(65, 100);

                //Disposition?
                if (player.Per.Soc < 65)
                    player.Per.Soc = _rnd.Next(65, 100);


                return player;
            }
            catch
            {
                throw;
            }
        }
        public PlayerModel SetClutchQB(PlayerModel player)
        {
            try
            {

                //team player
                if (player.Per.TmPl < 65)
                    player.Per.TmPl = _rnd.Next(65, 100);

                //competitiveness
                if (player.Per.Com < 65)
                    player.Per.Com = _rnd.Next(65, 100);

                //Disposition?
                if (player.Per.Soc < 65)
                    player.Per.Soc = _rnd.Next(65, 100);


                return player;
            }
            catch
            {
                throw;
            }
        }
        public PlayerModel SetCoverageLB(PlayerModel player)
        {
            try
            {
                List<string> criticalAttributes = new List<string>() { "Agi", "Spe", "Han", "Intel" };
                player = UpdateCriticalValues(criticalAttributes, player);

                return player;
            }
            catch
            {
                throw;
            }
        }
        public PlayerModel SetDeepThreat(PlayerModel player)
        {
            try
            {
                List<string> criticalAttributes = new List<string>() { "Spe" };
                player = UpdateCriticalValues(criticalAttributes, player);

                return player;
            }
            catch
            {
                throw;
            }
        }
        public PlayerModel SetDualthreat(PlayerModel player)
        {
            try
            {
                List<string> criticalAttributes = new List<string>() { "Agi", "Arm", "Spe", "Acc" };
                player = UpdateCriticalValues(criticalAttributes, player);

                return player;
            }
            catch
            {
                throw;
            }
        }
        public PlayerModel SetFilmGeek(PlayerModel player)
        {
            try
            {
                List<string> criticalAttributes = new List<string>() { "Intel" };
                player = UpdateCriticalValues(criticalAttributes, player);

                return player;
            }
            catch
            {
                throw;
            }
        }
        public PlayerModel SetGameManager(PlayerModel player)
        {
            try
            {
                List<string> criticalAttributes = new List<string>() { "Intel", "Acc" };
                player = UpdateCriticalValues(criticalAttributes, player);

                //Leadership
                if (player.Per.Lea < 65)
                    player.Per.Lea = _rnd.Next(65, 100);

                return player;
            }
            catch
            {
                throw;
            }
        }
        public PlayerModel SetGunslinger(PlayerModel player)
        {
            try
            {
                List<string> criticalAttributes = new List<string>() { "Str", "Arm", "Acc" };
                player = UpdateCriticalValues(criticalAttributes, player);

                return player;
            }
            catch
            {
                throw;
            }
        }
        public PlayerModel SetHybridLB(PlayerModel player)
        {
            try
            {
                List<string> criticalAttributes = new List<string>() { "Str", "Agi", "Tck", "End" };
                player = UpdateCriticalValues(criticalAttributes, player);

                return player;
            }
            catch
            {
                throw;
            }
        }
        public PlayerModel SetLockerLeader(PlayerModel player)
        {
            try
            {
                //Leadership
                if (player.Per.Lea < 65)
                    player.Per.Lea = _rnd.Next(65, 100);

                return player;
            }
            catch
            {
                throw;
            }
        }
        public PlayerModel SetNoseTackle(PlayerModel player)
        {
            try
            {
                List<string> criticalAttributes = new List<string>() { "Str", "Agi", "Tck", "End" };
                player = UpdateCriticalValues(criticalAttributes, player);

                return player;
            }
            catch
            {
                throw;
            }
        }
        public PlayerModel SetPossessionWR(PlayerModel player)
        {
            try
            {
                List<string> criticalAttributes = new List<string>() { "Str", "Agi", "Han", "Intel" };
                player = UpdateCriticalValues(criticalAttributes, player);

                return player;
            }
            catch
            {
                throw;
            }
        }
        public PlayerModel SetPowerKicker(PlayerModel player)
        {
            try
            {
                List<string> criticalAttributes = new List<string>() { "Str", "KDi" };
                player = UpdateCriticalValues(criticalAttributes, player);

                return player;
            }
            catch
            {
                throw;
            }
        }
        public PlayerModel SetPowerRunner(PlayerModel player)
        {
            try
            {
                List<string> criticalAttributes = new List<string>() { "Str", "Agi", "End" };
                player = UpdateCriticalValues(criticalAttributes, player);

                return player;
            }
            catch
            {
                throw;
            }
        }
        public PlayerModel SetPressCorner(PlayerModel player)
        {
            try
            {
                List<string> criticalAttributes = new List<string>() { "Str", "Agi", "Spe", "Intel", "End" };
                player = UpdateCriticalValues(criticalAttributes, player);

                return player;
            }
            catch
            {
                throw;
            }
        }
        public PlayerModel SetProBloodline(PlayerModel player)
        {
            try
            {
                List<string> criticalAttributes = new List<string>() { "Str", "Agi", "Spe", "Intel", "End" };
                player = UpdateCriticalValues(criticalAttributes, player);

                //competitiveness
                if (player.Per.Com < 65)
                    player.Per.Com = _rnd.Next(65, 100);

                return player;
            }
            catch
            {
                throw;
            }
        }
        public PlayerModel SetReceiveFB(PlayerModel player)
        {
            try
            {
                List<string> criticalAttributes = new List<string>() { "Agi", "Spe", "Han", "Intel" };
                player = UpdateCriticalValues(criticalAttributes, player);

                return player;
            }
            catch
            {
                throw;
            }
        }
        public PlayerModel SetReceivingTE(PlayerModel player)
        {
            try
            {
                List<string> criticalAttributes = new List<string>() { "Agi", "Spe", "Han", "Intel" };
                player = UpdateCriticalValues(criticalAttributes, player);

                return player;
            }
            catch
            {
                throw;
            }
        }
        public PlayerModel SetScatBack(PlayerModel player)
        {
            try
            {
                List<string> criticalAttributes = new List<string>() { "Agi", "Spe", "Han", "Intel" };
                player = UpdateCriticalValues(criticalAttributes, player);

                return player;
            }
            catch
            {
                throw;
            }
        }
        public PlayerModel SetShutDownCorner(PlayerModel player)
        {
            try
            {
                List<string> criticalAttributes = new List<string>() { "Str", "Agi", "Spe", "Han", "Intel", "End" };
                player = UpdateCriticalValues(criticalAttributes, player);

                //competitiveness
                if (player.Per.Com < 65)
                    player.Per.Com = _rnd.Next(65, 100);

                return player;
            }
            catch
            {
                throw;
            }
        }
        public PlayerModel SetSlotCorner(PlayerModel player)
        {
            try
            {
                List<string> criticalAttributes = new List<string>() { "Str", "Agi", "Intel", "Tck" };
                player = UpdateCriticalValues(criticalAttributes, player);

                return player;
            }
            catch
            {
                throw;
            }
        }
        public PlayerModel SetSlotReceiver(PlayerModel player)
        {
            try
            {
                List<string> criticalAttributes = new List<string>() { "Str", "Agi", "Intel", "Han" };
                player = UpdateCriticalValues(criticalAttributes, player);

                return player;
            }
            catch
            {
                throw;
            }
        }
        public PlayerModel SetSpeedRusher(PlayerModel player)
        {
            try
            {
                List<string> criticalAttributes = new List<string>() { "Agi", "Spe", "Intel" };
                player = UpdateCriticalValues(criticalAttributes, player);

                //competitiveness
                if (player.Per.Com < 65)
                    player.Per.Com = _rnd.Next(65, 100);


                return player;
            }
            catch
            {
                throw;
            }
        }
        public PlayerModel SetTenaciousBlocker(PlayerModel player)
        {
            try
            {
                List<string> criticalAttributes = new List<string>() { "Str", "Agi", "Spe", "PBl", "RBl", "Intel", "End" };
                player = UpdateCriticalValues(criticalAttributes, player);

                //competitiveness
                if (player.Per.Com < 65)
                    player.Per.Com = _rnd.Next(65, 100);

                return player;
            }
            catch
            {
                throw;
            }
        }
        public PlayerModel SetThumper(PlayerModel player)
        {
            try
            {
                List<string> criticalAttributes = new List<string>() { "Str", "Agi", "Spe", "Intel", "Tck" };
                player = UpdateCriticalValues(criticalAttributes, player);

                return player;
            }
            catch
            {
                throw;
            }
        }
        public PlayerModel SetWorkoutFanatic(PlayerModel player)
        {
            try
            {
                //competitiveness
                if (player.Per.Wor < 65)
                    player.Per.Wor = _rnd.Next(65, 100);

                return player;
            }
            catch
            {
                throw;
            }
        }
        public PlayerModel SetZoneCorner(PlayerModel player)
        {
            try
            {
                List<string> criticalAttributes = new List<string>() { "Agi", "Spe", "Han", "Intel" };
                player = UpdateCriticalValues(criticalAttributes, player);

                return player;
            }
            catch
            {
                throw;
            }
        }
    }
}
