/*
* This code was auto-converted from a java project.
*/

using System.Collections.Generic;
using WarLight.AI.Wunderwaffe.Bot;


using WarLight.AI.Wunderwaffe.Strategy;
using System;
using System.Text;

namespace WarLight.AI.Wunderwaffe.Debug
{
    public class Debug
    {


        public static void PrintDebugOutputBeginTurn(BotMain state)
        {
            AILog.Log("========================= NumTurns=" + state.NumberOfTurns + " ==========================");
        }

        public static void PrintDebugOutput(BotMain state)
        {
            foreach (var opp in state.Opponents)
                PrintOpponentBonuses(opp.ID, state);
        }


        private static void PrintDistances(BotMain state)
        {
            AILog.Log("Territory distances:");
            foreach (var territory in state.VisibleMap.GetOwnedTerritories())
            {
                var message = territory.ID + " --> " + territory.DirectDistanceToOpponentBorder + " | " + territory.DistanceToUnimportantSpot + " | " + territory.DistanceToImportantSpot + " | " + territory.DistanceToHighlyImportantSpot + " | " + territory.DistanceToOpponentBorder + " | " + territory.DistanceToImportantOpponentBorder + " || " + TransferMovesChooser.GetAdjustedDistance(territory);

                AILog.Log(message);
            }
        }


        public static void printExpandBonusValues(BotMap map, BotMain BotState)
        {
            AILog.Log("Bonus expansion values:");
            foreach (BotBonus bonus in map.Bonuses.Values)
            {
                if(bonus.GetOwnedTerritoriesAndNeighbors().Count > 0 && !bonus.IsOwnedByMyself())
                {
                    AILog.Log(bonus.Details.Name + ": " + bonus.GetExpansionValue());
                }
            }
        }

        public static void PrintTerritoryValues(BotMap map, BotMain BotState)
        {
            AILog.Log("Territory attack values:");
            foreach (BotTerritory territory in map.Territories.Values)
            {
                if (territory.IsVisible && BotState.IsOpponent(territory.OwnerPlayerID))
                {
                    AILog.Log(territory.Details.Name + ": " + territory.AttackTerritoryValue);
                }
            }

            AILog.Log("Territory expansion values:");
            foreach (BotTerritory territory in map.Territories.Values)
            {
                if (territory.IsVisible &&  territory.OwnerPlayerID == TerritoryStanding.NeutralPlayerID)
                {
                    AILog.Log(territory.Details.Name + ": " + territory.ExpansionTerritoryValue);
                }
            }

            AILog.Log("Territory defend values:");
            foreach (BotTerritory territory in map.Territories.Values)
            {
                if (territory.OwnerPlayerID == BotState.Me.ID && territory.GetOpponentNeighbors().Count > 0)
                {
                    AILog.Log(territory.Details.Name + ": " + territory.DefenceTerritoryValue);
                }
            }





        }

        public static void PrintAllTerritories(BotMain state, BotMap map)
        {
            AILog.Log("Territories:");
            foreach (var territory in map.Territories.Values)
            {
                var id = territory.ID;
                var player = territory.OwnerPlayerID;
                var armies = territory.Armies;
                var ownershipHeuristic = territory.IsOwnershipHeuristic;
                var deployment = territory.GetTotalDeployment(BotTerritory.DeploymentType.Normal);
                AILog.Log(" - Territory " + id + " (" + player + " | " + armies + " | " + ownershipHeuristic + " | " + deployment + ")");
            }
        }

        private static void PrintOpponentBonuses(PlayerIDType opponentID, BotMain state)
        {
            var message = new StringBuilder();
            message.Append("Opponent " + opponentID + " owns Bonuses: ");
            foreach (var bonus in state.VisibleMap.Bonuses.Values)
            {
                if (bonus.IsOwnedByOpponent(opponentID))
                    message.Append(bonus.ID + ", ");
            }
            AILog.Log(message.ToString());
        }
    }
}
