using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoldemPlayerContract
{
    [Serializable]
    public class GameConfig
    {
        public int LittleBlindSize  {get; set; }
        public int BigBlindSize  {get; set; }
        public int StartingStack  {get; set; }
        public int MaxNumRaisesPerBettingRound  {get; set; }
        public int MaxHands  {get; set; }
        public int DoubleBlindFrequency  {get; set; }
        public int BotTimeOutMilliSeconds  {get; set; }
        public bool RandomDealer  {get; set; }
        public bool RandomSeating  {get; set; }
    }
}
