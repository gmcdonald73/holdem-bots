using System;
using System.Collections.Generic;
using HoldemPlayerContract;

namespace HoldemController
{

    public class Deck
    {
        private Card[] cards = new Card[52];
        private int topCard;

        public Deck()
        {
            int i;

            for (i = 0; i < 52; i++)
            {
                eRankType rank = (eRankType)(i % 13);
                eSuitType suit = (eSuitType)(i / 13);

                Card card = new Card(rank, suit);

                cards[i] = card;
            }

            Shuffle();
        }

        public void Shuffle()
        {
            Card[] shuffledDeck = new Card[52];
            List<Card> unshuffledList = new List<Card>();
            int i,pos;
            Random rnd = new Random();

            topCard = 0;

            for (i = 0; i < 52; i++)
            {
                unshuffledList.Add(cards[i]);
            }

            for (i = 0; i < 52; i++)
            {
                pos = rnd.Next(unshuffledList.Count);
                shuffledDeck[i] = unshuffledList[pos];
                unshuffledList.RemoveAt(pos);
            }

            // Copy shuffled deck back to deck
            for (i = 0; i < 52; i++)
            {
                cards[i] = shuffledDeck[i];
            }
        }

        public Card DealCard()
        {
            Card card = cards[topCard];
            topCard++;
            return card;
        }
    }

}