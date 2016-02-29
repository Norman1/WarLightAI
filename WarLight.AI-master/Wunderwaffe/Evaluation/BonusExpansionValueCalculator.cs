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
        private double TerritoryMultiplicator = 0.9;
        private double NeutralKillsMultiplicator = 1.0;
        private double NeutralsMultiplicator = 0.5;

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
            bonus.ExpansionValue += bonus.ExpansionValue * 0.5;
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


        private double GetIncomeCostsRatio(BotBonus bonus)
        {
            var income = (double)bonus.Amount;
            var neutrals = (double)bonus.NeutralArmies.DefensePower;
            neutrals += bonus.Territories.Count(o => o.OwnerPlayerID == TerritoryStanding.AvailableForDistribution) * BotState.Settings.InitialNeutralsInDistribution;

            int neutralKills = 0;
            foreach (BotTerritory territory in bonus.NeutralTerritories)
            {
                neutralKills += (int)Math.Round(territory.Armies.DefensePower * BotState.Settings.DefensiveKillRate);
            }

            int territories = bonus.Territories.Count;

            double adjustedTerritoryFactor = territories * TerritoryMultiplicator;
            double adjustedNeutralKillsFactor = neutralKills * NeutralKillsMultiplicator;
            double adjustedNeutralsFactor = neutrals * NeutralsMultiplicator;

            return income * 100000 / (adjustedTerritoryFactor + adjustedNeutralKillsFactor + adjustedNeutralsFactor);
        }

        private double GetNeutralArmiesFactor(int neutralArmies)
        {
            double factor = 0.0;
            factor = neutralArmies * 0.01;
            factor = Math.Min(factor, 0.2);
            return factor;
        }

        private double GetTerritoryFactor(int territories)
        {
            double factor = 0.0;
            factor = territories * 0.01;
            factor = Math.Min(factor, 0.05);
            return factor;
        }

        private double GetImmediatelyCounteredTerritoryFactor(int immediatelyCounteredTerritories)
        {
            double factor = 0.0;
            factor = immediatelyCounteredTerritories * 0.1;
            factor = Math.Min(factor, 0.2);
            return factor;
        }

        private double GetAllCounteredTerritoryFactor(int allCounteredTerritories)
        {
            double factor = 0.0;
            factor = allCounteredTerritories * 0.1;
            factor = Math.Min(factor, 0.2);
            return factor;
        }

        private double GetOpponentInNeighborBonusFactor(int amountNeighborBonuses)
        {
            double factor = 0.0;
            if (amountNeighborBonuses > 0)
            {
                factor = 0.075;
            }
            return factor;
        }

        // positive then bad, negative then good
        private double GetNeighborBonusesFactor(BotBonus bonus)
        {
            if (BotState.NumberOfTurns != -1)
            {
                return 1;
            }
            int amountBetterSmallerBonuses = 0;
            int amountBetterLargerBonuses = 0;
            double neighborBonusValueToleranceFactor = 1.3;
            double betterBiggerBonusesMultiplicator = 0.001;
            double betterSmallerBonusesMultiplicator = 0.01;

            List<BotBonus> neighborBonuses = bonus.GetNeighborBonuses();
            double ourValue = GetExpansionValue(bonus, false);
            foreach (BotBonus neighborBonus in neighborBonuses)
            {
                if (neighborBonus.Territories.Count(o => o.OwnerPlayerID == TerritoryStanding.AvailableForDistribution) == 0)
                {
                    continue;
                }
                double neighborBonusValue = GetExpansionValue(neighborBonus, false);
                neighborBonusValue = neighborBonusValue * neighborBonusValueToleranceFactor;
                if (neighborBonusValue >= ourValue && bonus.Territories.Count > neighborBonus.Territories.Count)
                {
                    amountBetterSmallerBonuses++;
                }
                else if (neighborBonusValue >= ourValue && bonus.Territories.Count < neighborBonus.Territories.Count)
                {
                    amountBetterLargerBonuses++;
                }
            }
            double adjustedFactor = (betterBiggerBonusesMultiplicator * amountBetterLargerBonuses - betterSmallerBonusesMultiplicator * amountBetterSmallerBonuses);
            adjustedFactor = Math.Max(-0.2, Math.Min(0.2, adjustedFactor));

            return -1 * adjustedFactor;
        }


        public double GetExpansionValue(BotBonus bonus, Boolean useNeighborBonusFactor)
        {
            double expansionValue = 0.0;
            if (IsExpansionWorthless(bonus))
            {
                return expansionValue;
            }

            expansionValue = GetIncomeCostsRatio(bonus);

            var neutralArmies = bonus.NeutralArmies.DefensePower;
            double neutralArmiesFactor = GetNeutralArmiesFactor(neutralArmies);

            int allTerritories = bonus.Territories.Count;
            double territoryFactor = GetTerritoryFactor(allTerritories);


            int immediatelyCounteredTerritories = bonus.GetOwnedTerritoriesBorderingNeighborsOwnedByOpponentOrDistribution().Count;
            double immediatelyCounteredTerritoriesFactor = GetImmediatelyCounteredTerritoryFactor(immediatelyCounteredTerritories);

            var allCounteredTerritories = GetCounteredTerritories(bonus, BotState.Me.ID);
            double allCounteredTerritoriesFactor = GetAllCounteredTerritoryFactor(allCounteredTerritories);

            int amountNeighborBonusesWithOpponent = 0;
            var neighborBonuses = bonus.GetNeighborBonuses();
            foreach (var neighborBonus in neighborBonuses)
            {
                if (neighborBonus.Territories.Any(o => BotState.IsOpponent(o.OwnerPlayerID)))
                {
                    amountNeighborBonusesWithOpponent++;
                }
            }
            double opponentNeighborBonusFactor = GetOpponentInNeighborBonusFactor(amountNeighborBonusesWithOpponent);

            double completeFactor = neutralArmiesFactor + territoryFactor + immediatelyCounteredTerritoriesFactor + allCounteredTerritoriesFactor + opponentNeighborBonusFactor;
            if (useNeighborBonusFactor)
            {
                completeFactor += GetNeighborBonusesFactor(bonus);
            }
            completeFactor = Math.Min(completeFactor, 0.8);

            expansionValue = expansionValue - (expansionValue * completeFactor);

            return expansionValue;
        }


        private bool IsExpansionWorthless(BotBonus bonus)
        {
            if (bonus.Amount <= 0)
            {
                return true;
            }

            if (bonus.ContainsOpponentPresence())
            {
                return true;
            }

            if (bonus.IsOwnedByMyself() && BotState.NumberOfTurns != -1)
            {
                return true;
            }

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
