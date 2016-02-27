﻿/*
* This code was auto-converted from a java project.
*/

using System.Collections.Generic;
using WarLight.AI.Wunderwaffe.Bot;
using WarLight.AI.Wunderwaffe.Evaluation;

using WarLight.AI.Wunderwaffe.Move;

namespace WarLight.AI.Wunderwaffe.Tasks
{
    /// <summary>
    /// NoPlanDefendBestTerritoryTask is responsible for defending the highest priority territory without following a specified plan.
    /// </summary>
    public class NoPlanDefendBestTerritoryTask
    {
        public static Moves CalculateNoPlanDefendBestTerritoryTask(BotMain state, int maxDeployment, BotMap visibleMap, BotMap workingMap)
        {
            var wmOpponentBorderingTerritories = workingMap.GetOpponentBorderingTerritories();
            var vmOpponentBorderingTerritories = visibleMap.CopyTerritories(wmOpponentBorderingTerritories);
            var sortedVMOpponentBorderingTerritories = state.TerritoryValueCalculator.SortDefenseValue(vmOpponentBorderingTerritories);

            foreach (var vmTerritory in sortedVMOpponentBorderingTerritories)
            {
                var defendTerritoryMoves = state.DefendTerritoryTask.CalculateDefendTerritoryTask(vmTerritory, maxDeployment, true, BotTerritory.DeploymentType.Normal, BotTerritory.DeploymentType.Normal);
                if (defendTerritoryMoves != null && defendTerritoryMoves.GetTotalDeployment() > 0)
                    return defendTerritoryMoves;
            }

            return null;
        }
    }
}
