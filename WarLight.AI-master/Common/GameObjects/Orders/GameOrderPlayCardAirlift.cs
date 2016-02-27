﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WarLight.AI
{
    public class GameOrderPlayCardAirlift : GameOrderPlayCard
    {
        public TerritoryIDType FromTerritoryID;
        public TerritoryIDType ToTerritoryID;
        public Armies ArmiesToAirlift;
        public static GameOrderPlayCardAirlift Create(CardInstanceIDType cardInstanceID, PlayerIDType playerID, TerritoryIDType fromTerritoryID, TerritoryIDType toTerritoryID, Armies numArmies)
        {
            var o = new GameOrderPlayCardAirlift();
            o.CardInstanceID = cardInstanceID;
            o.PlayerID = playerID;
            o.FromTerritoryID = fromTerritoryID;
            o.ToTerritoryID = toTerritoryID;
            o.ArmiesToAirlift = numArmies;
            return o;
        }

        public override TurnPhase? OccursInPhase
        {
            get { return TurnPhase.Airlift; }
        }


    }
}
