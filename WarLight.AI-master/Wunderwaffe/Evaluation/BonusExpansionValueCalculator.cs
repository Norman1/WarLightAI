/*
* This code was auto-converted from a java project.
*/

using System;
using System.Linq;
using System.Collections.Generic;
using WarLight.AI.Wunderwaffe.Bot;



namespace WarLight.AI.Wunderwaffe.Evaluation
{
    /// <summary>This class is responsible for finding out which Bonuses to expand into.
    /// </summary>
    /// <remarks>
    /// This class is responsible for finding out which Bonuses to expand into. This happens by giving all Bonuses
    /// values. Furthermore this class is used during picking stage.
    /// </remarks>
    public class BonusExpansionValueCalculator
    {
        public BotMain BotState;
        public BonusExpansionValueCalculator(BotMain state)
        {
            this.BotState = state;
        }

        public List<BotBonus> SortBonuses(BotMap mapToUse, PlayerIDType playerID)
        {
            var allBonuses = mapToUse.Bonuses.Values.ToList();
            var sortedBonuses = new List<BotBonus>();
            //mapToUse.SetOpponentExpansionValue();
            while (!allBonuses.IsEmpty())
            {
                var bestBonus = allBonuses[0];
                double bestValue = 0;
                if (playerID == BotState.Me.ID)
                    bestValue = bestBonus.ExpansionValue;
                else
                {
                    bestValue = -1;
                }
                foreach (BotBonus bonus in allBonuses)
                {
                    double value = 0;
                    if (playerID == BotState.Me.ID)
                        value = bonus.ExpansionValue;
                    else
                        value = -1;

                    if (value > bestValue)
                    {
                        bestBonus = bonus;
                        bestValue = value;
                    }
                }
                allBonuses.Remove(bestBonus);
                sortedBonuses.Add(bestBonus);
            }
            return sortedBonuses;
        }

        public List<BotBonus> SortAccessibleBonuses(BotMap mapToUse)
        {
            var copy = new List<BotBonus>();
            copy.AddRange(mapToUse.Bonuses.Values);
            var outvar = new List<BotBonus>();
            while (!copy.IsEmpty())
            {
                var highestPrioBonus = copy[0];
                foreach (BotBonus bonus in copy)
                {
                    if (bonus.GetExpansionValue() > highestPrioBonus.GetExpansionValue())
                        highestPrioBonus = bonus;
                }
                copy.Remove(highestPrioBonus);
                outvar.Add(highestPrioBonus);
            }
            // Remove the non accessible Bonuses
            List<BotBonus> nonAccessibleBonuses = new List<BotBonus>();
            foreach (BotBonus bonus_1 in mapToUse.Bonuses.Values)
            {
                if (bonus_1.GetOwnedTerritoriesAndNeighbors().Count == 0)
                    nonAccessibleBonuses.Add(bonus_1);
            }
            outvar.RemoveAll(nonAccessibleBonuses);
            return outvar;
        }

        public void AddExtraValueForFirstTurnBonus(BotBonus bonus)
        {
     //       bonus.MyExpansionValueHeuristic.AddExtraValueForFirstTurnBonus(bonus);
        }

        /// <summary>Classifies the Bonus according to the intel from the temporaryMap.
        /// </summary>
        /// <remarks>
        /// Classifies the Bonus according to the intel from the temporaryMap. However the results of the classification aren't written to the temporary map but to the visible map.
        /// </remarks>
        /// <param name="temporaryMap"></param>
        public void ClassifyBonuses(BotMap temporaryMap, BotMap mapToWriteIn)
        {
            foreach (var bonus in temporaryMap.Bonuses.Values)
            {
                bonus.SetMyExpansionValueHeuristic();

                // Categorize the expansion values. Possible values are 0 = rubbish and 1 = good
                var toMuchNeutrals = false;
                var neutralArmies = bonus.NeutralArmies.DefensePower;
                if (neutralArmies > 28)
                    toMuchNeutrals = true;
                else if (neutralArmies >= 20 && bonus.Amount <= 3)
                    toMuchNeutrals = true;
                else if (neutralArmies >= 16 && bonus.Amount <= 2)
                    toMuchNeutrals = true;
                else if (neutralArmies >= 12 && bonus.Amount <= 1)
                    toMuchNeutrals = true;

                if (bonus.IsOwnedByMyself() || bonus.Amount == 0 || bonus.ContainsOpponentPresence() || toMuchNeutrals)
                    mapToWriteIn.Bonuses[bonus.ID].ExpansionValueCategory = 0;
                else
                    mapToWriteIn.Bonuses[bonus.ID].ExpansionValueCategory = 1;
            }
        }



        private double IncomeNeutralsRatio(BotBonus bonus)
        {
            var income = (double)bonus.Amount;
            var neutrals = (double)bonus.NeutralArmies.DefensePower;

            neutrals += bonus.Territories.Count(o => o.OwnerPlayerID == TerritoryStanding.AvailableForDistribution) * BotState.Settings.InitialNeutralsInDistribution;
            return income / neutrals;
        }


        public double GetExpansionValue2(BotBonus bonus)
        {
            double expansionValue = 0.0;
            if (IsExpansionWorthless(bonus))
                return expansionValue;

             var points = IncomeNeutralsRatio(bonus) * 1000;
           // var points = IncomeNeutralsRatio(bonus);

             var neutralArmies = bonus.NeutralArmies.DefensePower;

             if (neutralArmies > 8)
                 points -= neutralArmies * 4.5;
             else if (neutralArmies > 6)
                 points -= neutralArmies * 3.5;
             else if (neutralArmies > 4)
                 points -= neutralArmies * 2.5;

             points -= 0.5 * bonus.Territories.Count;

             var immediatelyCounteredTerritories = 0;
             immediatelyCounteredTerritories = bonus.GetOwnedTerritoriesBorderingNeighborsOwnedByOpponentOrDistribution().Count;



             points -= 7 * immediatelyCounteredTerritories;

             var allCounteredTerritories = GetCounteredTerritories(bonus, BotState.Me.ID);
             points -= 4 * allCounteredTerritories;

             var neighborBonuses = bonus.GetNeighborBonuses();
             foreach (var neighborBonus in neighborBonuses)
             {
                 if ((neighborBonus.Territories.Any(o => BotState.IsOpponent(o.OwnerPlayerID) || o.OwnerPlayerID == TerritoryStanding.AvailableForDistribution)))
                     points -= 1;
                 else if (neighborBonus.GetOwnedTerritories().Count > 0)
                     points += 0.5;
                 else
                     points -= 0.4;
             }

             if (allCounteredTerritories > 0)
                 points -= 7;

             if (immediatelyCounteredTerritories > 0)
                 points -= Math.Abs(points * 0.1);


             var distanceFromUs = bonus.DistanceFrom(terr => terr.OwnerPlayerID == BotState.Me.ID);
             if (distanceFromUs > 2)
             {
                 //Penalize weight of bonuses far away
                 points *= (12 - distanceFromUs) / 10.0;
            }
             
            expansionValue = points;
            return expansionValue;
        }





        private bool IsExpansionWorthless(BotBonus bonus)
        {
            if (bonus.Amount == 0)
                return true;

            if ( bonus.ContainsOpponentPresence())
                return true;

            if (bonus.IsOwnedByMyself())
                return true;

            return false;
        }

        private int GetCounteredTerritories(BotBonus bonus, PlayerIDType playerID)
        {
            var outvar = 0;
            foreach (var territory in bonus.Territories)
            {
                if (territory.GetOpponentNeighbors().Count > 0 && playerID == BotState.Me.ID)
                    outvar++;
                else if (territory.GetOwnedNeighbors().Count > 0 && BotState.IsOpponent(playerID))
                    outvar++;
            }
            return outvar;
        }








    }



















}
