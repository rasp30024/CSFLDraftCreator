using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSFLDraftCreator.Models
{
    internal class PlayerPersonalitiesModel
    {
        public int Lea { get; set; } = 1; //leadership
        public int Wor { get; set; } = 1; //Work Ethic
        public int Com { get; set; } = 1; // Competitive
        public int TmPl { get; set; } = 1; //Team Player
        public int Spor { get; set; } = 1; //Sportsmanship
        public int Soc { get; set; } = 1; //Disposition
        public int Mny { get; set; } = 1; //Money
        public int Sec { get; set; } = 1; //Security
        public int Loy { get; set; } = 1; //Loyality
        public int Win { get; set; } = 1; //Winning
        public int PT { get; set; } = 1; //playing time
        public int Home { get; set; } = 1; //Close to Home
        public int Mkt { get; set; } = 1; //Market Size
        public int Mor { get; set; } = 1;  //Morale
    }
}
