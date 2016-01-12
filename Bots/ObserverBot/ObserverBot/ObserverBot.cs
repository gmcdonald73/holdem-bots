using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using HoldemPlayerContract;

namespace ObserverBot
{
    public class ObserverBot : IHoldemPlayer
    {
        private int _playerNum;
        private Card [] _board;
        private int _handNum = 0;
        TextWriter _tw;

        public void InitPlayer(int playerNum)
        {
            // This is called once at the start of the game. playerNum is your unique identifer for the game
            _playerNum = playerNum;
            _board = new Card[5];
            _tw = new StreamWriter("playerinfo.txt", false);
        }

        public string Name
        {
            // return the name of your player
            get
            {
                return "ObserverBot";
            }
        }

        public bool IsObserver
        {
            get
            {
                return true;
            }
        }

        public void InitHand(int numPlayers, PlayerInfo[] players)
        {
            // this is called at the start of every hand and tells you the current status of all players (e.g. if is alive and stack size and who is dealer)
            // create a writer and open the file
            _handNum++;

            string sStackSizes = _handNum + "\t";

            foreach (PlayerInfo p in players)
            {
                sStackSizes += p.StackSize + "\t";
            }

            // write a line of text to the file
            _tw.WriteLine(sStackSizes);
        }

        public void ReceiveHoleCards(Card hole1, Card hole2)
        {
        }

        public void SeeAction(eStage stage, int playerNum, eActionType action, int amount)
        {
            // this is called to inform you when any player (including yourself) makes an action (eg puts in blinds, checks, folds, calls, raises, or wins hand)
        }

        public void GetAction(eStage stage, int callAmount, int minRaise, int maxRaise, int raisesRemaining, int potSize, out eActionType yourAction, out int amount)
        {
            amount = 0;
            yourAction = eActionType.ACTION_FOLD;
        }

        public void SeeBoardCard(eBoardCardType cardType, Card boardCard)
        {
            // this is called to inform you of the board cards (3 flop cards, turn and river)
        }

        public void SeePlayerHand(int playerNum, Card hole1, Card hole2, Hand bestHand)
        {
            // this is called to inform you of another players hand during the show down. 
            // bestHand is the best hand that they can form with their hole cards and the five board cards
        }

        public void EndOfGame(int numPlayers, PlayerInfo[] players)
        {
            _handNum++;

            string sStackSizes = _handNum + "\t";

            foreach (PlayerInfo p in players)
            {
                sStackSizes += p.StackSize + "\t";
            }

            // write a line of text to the file
            _tw.WriteLine(sStackSizes);

            // close the stream
            _tw.Close();
        }
    }
}
