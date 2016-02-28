// /*
// * This code was auto-converted from a java project.
// */

//using System;
//using System.Linq;
//using System.Collections.Generic;
//using WarLight.AI.Wunderwaffe.Bot;



//namespace WarLight.AI.Wunderwaffe.Heuristics
//{
//    /// <summary>The heuristic expansion value of a Bonus.</summary>
//    public class BonusExpansionValueHeuristic
//    {
//        public double ExpansionValue = 0.0;
//        public BotMain BotState;

//        public BonusExpansionValueHeuristic(BotMain state, BotBonus bonus, PlayerIDType playerID)
//        {
//            this.BotState = state;
//            // public static final int HIGH = 1000;
//            // public static final int LOW = 10;
//            SetExpansionValue2(bonus, playerID);
//        }

//        private double IncomeNeutralsRatio(BotBonus bonus)
//        {
//            var income = (double)bonus.Amount;
//            var neutrals = (double)bonus.NeutralArmies.DefensePower;

//            neutrals += bonus.Territories.Count(o => o.OwnerPlayerID == TerritoryStanding.AvailableForDistribution) * BotState.Settings.InitialNeutralsInDistribution;

//            return income / neutrals;
//        }

//        public void SetExpansionValue2(BotBonus bonus, PlayerIDType playerID)
//        {
//            this.ExpansionValue = 0.0;
//            if (IsExpansionWorthless(bonus, playerID))
//                return;

//            var points = IncomeNeutralsRatio(bonus) * 200.0;
//            if (BotState.NumberOfTurns == -1)
//            {
//                Assert.Fatal(playerID == BotState.Me.ID); //we only ever call this during territory picking for ourselves.
//                if (bonus.AreAllTerritoriesVisible())
//                    points += AddExtraValueForFirstTurnBonus(bonus);
//            }

//            var neutralArmies = bonus.NeutralArmies.DefensePower;

//            if (neutralArmies > 8)
//                points -= neutralArmies * 4.5;
//            else if (neutralArmies > 6)
//                points -= neutralArmies * 3.5;
//            else if (neutralArmies > 4)
//                points -= neutralArmies * 2.5;

//            points -= 0.5 * bonus.Territories.Count;

//            var immediatelyCounteredTerritories = 0;
//            if (playerID == BotState.Me.ID)
//                immediatelyCounteredTerritories = bonus.GetOwnedTerritoriesBorderingNeighborsOwnedByOpponentOrDistribution().Count;
//            else
//                immediatelyCounteredTerritories = bonus.GetOpponentTerritoriesBorderingOwnedNeighbors().Count;


//            points -= 7 * immediatelyCounteredTerritories;

//            var allCounteredTerritories = GetCounteredTerritories(bonus, playerID);
//            points -= 4 * allCounteredTerritories;

//            var neighborBonuses = bonus.GetNeighborBonuses();
//            foreach (var neighborBonus in neighborBonuses)
//            {
//                if ((neighborBonus.Territories.Any(o => BotState.IsOpponent(o.OwnerPlayerID) || o.OwnerPlayerID == TerritoryStanding.AvailableForDistribution) && playerID == BotState.Me.ID) || (neighborBonus.ContainsOwnPresence() && BotState.IsOpponent(playerID)))
//                    points -= 1;
//                else if (neighborBonus.GetOwnedTerritories().Count > 0)
//                    points += 0.5;
//                else
//                    points -= 0.4;
//            }

//            if (allCounteredTerritories > 0)
//                points -= 7;

//            if (immediatelyCounteredTerritories > 0)
//                points -= Math.Abs(points * 0.1);

//            // double value = this.incomeNeutralsRatio * HIGH;
//            // double deductions = getDeductions(Bonus, playerID, value);
//            // double additions = getAdditions(Bonus, playerID, value);
//            // value += additions;
//            // value -= deductions;

//            var distanceFromUs = bonus.DistanceFrom(terr => terr.OwnerPlayerID == playerID);
//            if (distanceFromUs > 2)
//            {
//                //Penalize weight of bonuses far away
//                points *= (12 - distanceFromUs) / 10.0;
//            }

//            this.ExpansionValue = points;
//        }



//        private bool IsExpansionWorthless(BotBonus bonus, PlayerIDType playerID)
//        {
//            if (bonus.Amount == 0)
//                return true;

//            if ((BotState.IsOpponent(playerID) && bonus.ContainsOwnPresence()) || (playerID == BotState.Me.ID && bonus.ContainsOpponentPresence()))
//                return true;

//            if ((playerID == BotState.Me.ID && bonus.IsOwnedByMyself()) || (BotState.IsOpponent(playerID) && bonus.IsOwnedByAnyOpponent()))
//                return true;

//            return false;
//        }

//        private int GetCounteredTerritories(BotBonus bonus, PlayerIDType playerID)
//        {
//            var outvar = 0;
//            foreach (var territory in bonus.Territories)
//            {
//                if (territory.GetOpponentNeighbors().Count > 0 && playerID == BotState.Me.ID)
//                    outvar++;
//                else if (territory.GetOwnedNeighbors().Count > 0 && BotState.IsOpponent(playerID))
//                    outvar++;
//            }
//            return outvar;
//        }


//        public virtual double AddExtraValueForFirstTurnBonus(BotBonus bonus)
//        {
//            var neutrals = bonus.NeutralArmies.DefensePower;
//            if (neutrals <= 4)
//                return bonus.Amount * 15;
//            else if (neutrals <= 6)
//                return bonus.Amount * 7;
//            else
//                return 0;
//        }

//        public override string ToString()
//        {
//            return "BonusExpansionValue: " + ExpansionValue;
//        }
//    }
//}
