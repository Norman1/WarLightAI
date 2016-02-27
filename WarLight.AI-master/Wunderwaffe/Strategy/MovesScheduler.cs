﻿/*
* This code was auto-converted from a java project.
*/

using System;
using System.Linq;
using System.Collections.Generic;
using WarLight.AI.Wunderwaffe.Bot;
using WarLight.AI.Wunderwaffe.Evaluation;

using WarLight.AI.Wunderwaffe.Move;


namespace WarLight.AI.Wunderwaffe.Strategy
{
    public class MovesScheduler
    {
        public BotMain BotState;
        public MovesScheduler(BotMain state)
        {
            this.BotState = state;
        }
        private List<BotOrderAttackTransfer> EarlyAttacks = new List<BotOrderAttackTransfer>();
        private List<BotOrderAttackTransfer> SupportMovesWhereOpponentMightBreak = new List<BotOrderAttackTransfer>();
        private List<BotOrderAttackTransfer> SupportMovesWhereOpponentMightGetAGoodAttack = new List<BotOrderAttackTransfer>();
        private List<BotOrderAttackTransfer> CrushingAttackMovesToSlipperyTerritories = new List<BotOrderAttackTransfer>();
        private List<BotOrderAttackTransfer> SupportMovesWhereOpponentMightAttack = new List<BotOrderAttackTransfer>();
        private List<BotOrderAttackTransfer> DelayAttackMoves = new List<BotOrderAttackTransfer>();
        private List<BotOrderAttackTransfer> SafeAttackMovesWithGoodAttack = new List<BotOrderAttackTransfer>();
        private List<BotOrderAttackTransfer> NormalSupportMoves = new List<BotOrderAttackTransfer>();
        private List<BotOrderAttackTransfer> BigExpansionMovesNonAttack = new List<BotOrderAttackTransfer>();
        private List<BotOrderAttackTransfer> TransferMoves = new List<BotOrderAttackTransfer>();
        private List<BotOrderAttackTransfer> NonOpponentBorderingSmallExpansionMovesNonAttack = new List<BotOrderAttackTransfer>();
        private List<BotOrderAttackTransfer> OpponentBorderingSmallExpansionMovesNonAttack = new List<BotOrderAttackTransfer>();
        private List<BotOrderAttackTransfer> BigExpansionMovesWithAttack = new List<BotOrderAttackTransfer>();
        private List<BotOrderAttackTransfer> NonOpponentBorderingSmallExpansionMovesWithAttack = new List<BotOrderAttackTransfer>();
        private List<BotOrderAttackTransfer> OpponentBorderingSmallExpansionMovesWithAttack = new List<BotOrderAttackTransfer>();
        private List<BotOrderAttackTransfer> SafeAttackMovesWithPossibleBadAttack = new List<BotOrderAttackTransfer>();
        private List<BotOrderAttackTransfer> RiskyAttackMoves = new List<BotOrderAttackTransfer>();
        private List<BotOrderAttackTransfer> TransferingExpansionMoves = new List<BotOrderAttackTransfer>();
        private List<BotOrderAttackTransfer> SnipeMoves = new List<BotOrderAttackTransfer>();

        /// <summary>Schedules the AttackTransferMoves.</summary>
        /// <remarks>Schedules the AttackTransferMoves.</remarks>
        /// <param name="movesSoFar"></param>
        /// <returns></returns>
        public Moves ScheduleMoves(Moves movesSoFar)
        {
            return GetSortedMoves(movesSoFar.Orders);
        }

        private void ClearMoves()
        {
            EarlyAttacks.Clear();
            SupportMovesWhereOpponentMightBreak.Clear();
            SupportMovesWhereOpponentMightGetAGoodAttack.Clear();
            SupportMovesWhereOpponentMightAttack.Clear();
            CrushingAttackMovesToSlipperyTerritories.Clear();
            DelayAttackMoves.Clear();
            SafeAttackMovesWithGoodAttack.Clear();
            NormalSupportMoves.Clear();
            BigExpansionMovesNonAttack.Clear();
            TransferMoves.Clear();
            BigExpansionMovesWithAttack.Clear();
            NonOpponentBorderingSmallExpansionMovesNonAttack.Clear();
            OpponentBorderingSmallExpansionMovesNonAttack.Clear();
            NonOpponentBorderingSmallExpansionMovesWithAttack.Clear();
            OpponentBorderingSmallExpansionMovesWithAttack.Clear();
            SafeAttackMovesWithPossibleBadAttack.Clear();
            RiskyAttackMoves.Clear();
            TransferingExpansionMoves.Clear();
            SnipeMoves.Clear();
        }

        /// <summary>
        /// Schedules the attacks with 1 in a way that we first attack territories bordering multiple of our territories (since the
        /// stack might move).
        /// </summary>
        /// <remarks>
        /// Schedules the attacks with 1 in a way that we first attack territories bordering multiple of our territories (since the
        /// stack might move).
        /// </remarks>
        /// <param name="delayAttacks"></param>
        /// <returns></returns>
        private List<BotOrderAttackTransfer> ScheduleDelayAttacks(List<BotOrderAttackTransfer> delayAttacks)
        {
            var outvar = new List<BotOrderAttackTransfer>();
            List<BotOrderAttackTransfer> delayAttacksToLonelyTerritory = new List<BotOrderAttackTransfer>();
            List<BotOrderAttackTransfer> delayAttacksToNonLonelyTerritory = new List<BotOrderAttackTransfer>();
            foreach (BotOrderAttackTransfer atm in delayAttacks)
            {
                if (atm.To.GetOwnedNeighbors().Count == 1)
                    delayAttacksToLonelyTerritory.Add(atm);
                else
                    delayAttacksToNonLonelyTerritory.Add(atm);
            }
            outvar.AddRange(delayAttacksToNonLonelyTerritory);
            outvar.AddRange(delayAttacksToLonelyTerritory);
            return outvar;
        }

        private List<BotOrderAttackTransfer> ScheduleCrushingAttackToSlipperyTerritory(List<BotOrderAttackTransfer> attacks)
        {
            var outvar = new List<BotOrderAttackTransfer>();
            var copy = new List<BotOrderAttackTransfer>();
            copy.AddRange(attacks);
            while (copy.Count > 0)
            {
                var bestAttack = copy[0];
                foreach (BotOrderAttackTransfer atm in copy)
                {
                    if (GetSlipperyOpponentTerritoryNumber(atm.To) > GetSlipperyOpponentTerritoryNumber(bestAttack.To))
                        bestAttack = atm;
                }
                outvar.Add(bestAttack);
                copy.Remove(bestAttack);
            }
            return outvar;
        }

        private List<BotOrderAttackTransfer> ScheduleAttacksAttackingArmies(List<BotOrderAttackTransfer> attackTransferMoves)
        {
            var outvar = new List<BotOrderAttackTransfer>();
            var copy = new List<BotOrderAttackTransfer>();
            copy.AddRange(attackTransferMoves);
            while (copy.Count > 0)
            {
                var biggestAttack = copy[0];
                foreach (var atm in copy)
                {
                    if (atm.Armies.AttackPower > biggestAttack.Armies.AttackPower)
                        biggestAttack = atm;
                }
                outvar.Add(biggestAttack);
                copy.Remove(biggestAttack);
            }
            return outvar;
        }

        private List<BotOrderDeploy> GetSortedDeployment(List<BotOrderDeploy> unsortedDeployment)
        {
            unsortedDeployment = unsortedDeployment.OrderBy(o => o.Territory.ID).ToList();

            List<BotOrderDeploy> deploymentsNextOpponent = new List<BotOrderDeploy>();
            List<BotOrderDeploy> deploymentsInBackground = new List<BotOrderDeploy>();
            foreach (var deploy in unsortedDeployment)
            {
                if (deploy.Territory.GetOpponentNeighbors().Count == 0)
                {
                    deploymentsInBackground.Add(deploy);
                }
                else
                {
                    deploymentsNextOpponent.Add(deploy);
                }
            }
            List<BotOrderDeploy> sortedDeployment = new List<BotOrderDeploy>();
            sortedDeployment.AddRange(deploymentsNextOpponent);
            sortedDeployment.AddRange(deploymentsInBackground);
            return sortedDeployment;
        }


        private Moves GetSortedMoves(List<BotOrder> movesSoFar)
        {
            var sortedMoves = new Moves();

            List<BotOrderDeploy> deploymentsNextOpponent = new List<BotOrderDeploy>();
            List<BotOrderDeploy> deploymentsInBackground = new List<BotOrderDeploy>();

            foreach (var deploy in movesSoFar.OfType<BotOrderDeploy>())
            {
                if(deploy.Territory.GetOpponentNeighbors().Count == 0)
                {
                    deploymentsInBackground.Add(deploy);
                }
                else
                {
                    deploymentsNextOpponent.Add(deploy);
                }
            }
            sortedMoves.Orders.AddRange(deploymentsNextOpponent);
            sortedMoves.Orders.AddRange(deploymentsInBackground);


            var unhandledMoves = movesSoFar.Where(o => !(o is BotOrderDeploy)).ToList();
            var movesMap = BotState.VisibleMap.GetMapCopy();
            
            while (unhandledMoves.Count > 0)
            {
                var nextMove = GetNextMove(unhandledMoves, movesMap);
                unhandledMoves.Remove(nextMove);
                sortedMoves.AddOrder(nextMove);

                if (nextMove is BotOrderAttackTransfer)
                    BotState.MapUpdater.UpdateMap((BotOrderAttackTransfer)nextMove, movesMap, BotTerritory.DeploymentType.Conservative);
            }
            return sortedMoves;
        }

        private BotOrder GetNextMove(List<BotOrder> unhandledMoves, BotMap movesMap)
        {
            ClearMoves();
            FillMoveTypes(unhandledMoves, movesMap);
            var semiSortedMoves = new List<BotOrderAttackTransfer>();
            EarlyAttacks = ScheduleAttacksAttackingArmies(EarlyAttacks);
            SupportMovesWhereOpponentMightBreak = ScheduleAttacksAttackingArmies(SupportMovesWhereOpponentMightBreak);
            CrushingAttackMovesToSlipperyTerritories = ScheduleAttacksAttackingArmies(CrushingAttackMovesToSlipperyTerritories);
            CrushingAttackMovesToSlipperyTerritories = ScheduleCrushingAttackToSlipperyTerritory(CrushingAttackMovesToSlipperyTerritories);
            SupportMovesWhereOpponentMightGetAGoodAttack = ScheduleAttacksAttackingArmies(SupportMovesWhereOpponentMightGetAGoodAttack);
            SupportMovesWhereOpponentMightAttack = ScheduleAttacksAttackingArmies(SupportMovesWhereOpponentMightAttack);
            DelayAttackMoves = ScheduleDelayAttacks(DelayAttackMoves);
            SafeAttackMovesWithGoodAttack = ScheduleAttacksAttackingArmies(SafeAttackMovesWithGoodAttack);
            NormalSupportMoves = ScheduleAttacksAttackingArmies(NormalSupportMoves);
            BigExpansionMovesNonAttack = SortExpansionMovesOpponentDistance(BigExpansionMovesNonAttack, false);
            BigExpansionMovesNonAttack = ScheduleAttacksAttackingArmies(BigExpansionMovesNonAttack);
            NonOpponentBorderingSmallExpansionMovesNonAttack = SortExpansionMovesOpponentDistance(NonOpponentBorderingSmallExpansionMovesNonAttack, true);
            NonOpponentBorderingSmallExpansionMovesNonAttack = ScheduleAttacksAttackingArmies(NonOpponentBorderingSmallExpansionMovesNonAttack);
            OpponentBorderingSmallExpansionMovesNonAttack = ScheduleAttacksAttackingArmies(OpponentBorderingSmallExpansionMovesNonAttack);
            BigExpansionMovesWithAttack = SortExpansionMovesOpponentDistance(BigExpansionMovesWithAttack, false);
            BigExpansionMovesWithAttack = ScheduleAttacksAttackingArmies(BigExpansionMovesWithAttack);
            NonOpponentBorderingSmallExpansionMovesWithAttack = SortExpansionMovesOpponentDistance(NonOpponentBorderingSmallExpansionMovesWithAttack, true);
            NonOpponentBorderingSmallExpansionMovesWithAttack = ScheduleAttacksAttackingArmies(NonOpponentBorderingSmallExpansionMovesWithAttack);
            OpponentBorderingSmallExpansionMovesWithAttack = SortExpansionMovesOpponentDistance(OpponentBorderingSmallExpansionMovesWithAttack, true);
            OpponentBorderingSmallExpansionMovesWithAttack = ScheduleAttacksAttackingArmies(OpponentBorderingSmallExpansionMovesWithAttack);
            SafeAttackMovesWithPossibleBadAttack = ScheduleAttacksAttackingArmies(SafeAttackMovesWithPossibleBadAttack);
            RiskyAttackMoves = ScheduleAttacksAttackingArmies(RiskyAttackMoves);
            semiSortedMoves.AddRange(EarlyAttacks);
            semiSortedMoves.AddRange(SupportMovesWhereOpponentMightBreak);
            semiSortedMoves.AddRange(CrushingAttackMovesToSlipperyTerritories);
            semiSortedMoves.AddRange(SupportMovesWhereOpponentMightGetAGoodAttack);
            semiSortedMoves.AddRange(SupportMovesWhereOpponentMightAttack);
            semiSortedMoves.AddRange(DelayAttackMoves);
            semiSortedMoves.AddRange(SafeAttackMovesWithGoodAttack);
            semiSortedMoves.AddRange(NormalSupportMoves);
            semiSortedMoves.AddRange(BigExpansionMovesNonAttack);
            semiSortedMoves.AddRange(TransferMoves);
            semiSortedMoves.AddRange(NonOpponentBorderingSmallExpansionMovesNonAttack);
            semiSortedMoves.AddRange(OpponentBorderingSmallExpansionMovesNonAttack);
            semiSortedMoves.AddRange(BigExpansionMovesWithAttack);
            semiSortedMoves.AddRange(NonOpponentBorderingSmallExpansionMovesWithAttack);
            semiSortedMoves.AddRange(OpponentBorderingSmallExpansionMovesWithAttack);
            semiSortedMoves.AddRange(TransferingExpansionMoves);
            semiSortedMoves.AddRange(SnipeMoves);
            semiSortedMoves.AddRange(SafeAttackMovesWithPossibleBadAttack);
            semiSortedMoves.AddRange(RiskyAttackMoves);

            if (semiSortedMoves.Count == 0)
                return unhandledMoves[0];

            var nextMove = semiSortedMoves[0];
            if (movesMap.Territories[nextMove.To.ID].GetOpponentNeighbors().Count > 0)
            {
                var substituteMove = GetSubstituteMove(nextMove, movesMap, unhandledMoves);
                if (substituteMove != null)
                    nextMove = substituteMove;
            }
            return nextMove;
        }

        /// <summary>Tries to find an attack move to make the support move obsolete.</summary>
        private BotOrderAttackTransfer GetSubstituteMove(BotOrderAttackTransfer supportMove, BotMap movesMap, List<BotOrder> unhandledMoves)
        {
            var territoryToDefend = supportMove.To;
            var mmTerritoryToDefend = movesMap.Territories[territoryToDefend.ID];
            if (mmTerritoryToDefend.GetOpponentNeighbors().Count > 1)
                return null;

            var mmOpponentTerritory = mmTerritoryToDefend.GetOpponentNeighbors()[0];
            foreach (var unhandledMove in unhandledMoves.OfType<BotOrderAttackTransfer>())
            {
                if (unhandledMove.To.ID == mmOpponentTerritory.ID)
                {
                    if (!CanOpponentAttackTerritory(mmOpponentTerritory.OwnerPlayerID, unhandledMove.From))
                    {
                        if (unhandledMove.Armies.AttackPower * BotState.Settings.OffensiveKillRate > mmOpponentTerritory.Armies.DefensePower + BotState.GetGuessedOpponentIncome(mmOpponentTerritory.OwnerPlayerID, BotState.VisibleMap) + 3)
                        {
                            AILog.Log("found substitute move: " + unhandledMove);
                            AILog.Log(unhandledMove.To.GetArmiesAfterDeploymentAndIncomingAttacks(BotTerritory.DeploymentType.Conservative) + " | " + mmOpponentTerritory.GetArmiesAfterDeploymentAndIncomingAttacks(BotTerritory.DeploymentType.Conservative));
                            return unhandledMove;
                        }
                    }
                }
            }
            return null;
        }

        private void FillMoveTypes(List<BotOrder> unhandledMoves, BotMap movesMap)
        {
            foreach (var atm in unhandledMoves.OfType<BotOrderAttackTransfer>())
            {
                var mmToTerritory = movesMap.Territories[atm.To.ID];
                var mmFromTerritory = movesMap.Territories[atm.From.ID];
                if (atm.Message == AttackMessage.EarlyAttack)
                    EarlyAttacks.Add(atm);
                else
                {
                    // Opponent attack moves
                    if (BotState.IsOpponent(mmToTerritory.OwnerPlayerID))
                    {
                        if (atm.Armies.AttackPower == 1)
                            DelayAttackMoves.Add(atm);
                        else if (!CanOpponentAttackTerritory(mmToTerritory.OwnerPlayerID, atm.From) && GetSlipperyOpponentTerritoryNumber(atm.To) > -1 && IsProbablyCrushingMove(atm))
                            CrushingAttackMovesToSlipperyTerritories.Add(atm);
                        else if (!CanOpponentAttackTerritory(mmToTerritory.OwnerPlayerID, atm.From) && IsAlwaysGoodAttackMove(atm))
                            SafeAttackMovesWithGoodAttack.Add(atm);
                        else if (!CanOpponentAttackTerritory(mmToTerritory.OwnerPlayerID, atm.From) && !IsAlwaysGoodAttackMove(atm))
                            SafeAttackMovesWithPossibleBadAttack.Add(atm);
                        else if (CanOpponentAttackTerritory(mmToTerritory.OwnerPlayerID, atm.From))
                            RiskyAttackMoves.Add(atm);
                    }
                    else if (mmToTerritory.OwnerPlayerID == BotState.Me.ID) // Transfer moves
                    {
                        if (mmToTerritory.GetOpponentNeighbors().Count > 0 && CanOpponentBreakTerritory(mmToTerritory))
                            SupportMovesWhereOpponentMightBreak.Add(atm);
                        else if (mmToTerritory.GetOpponentNeighbors().Count > 0 && CanOpponentGetAGoodAttack(mmToTerritory))
                            SupportMovesWhereOpponentMightGetAGoodAttack.Add(atm);
                        else if (mmToTerritory.GetOpponentNeighbors().Count > 0 && CanAnyOpponentAttackTerritory(mmToTerritory))
                            SupportMovesWhereOpponentMightAttack.Add(atm);
                        else if (mmToTerritory.GetOpponentNeighbors().Count > 0 && !CanAnyOpponentAttackTerritory(mmToTerritory))
                            NormalSupportMoves.Add(atm);
                        else if (mmToTerritory.GetOpponentNeighbors().Count == 0)
                            TransferMoves.Add(atm);
                    }
                    else if (mmToTerritory.OwnerPlayerID == TerritoryStanding.NeutralPlayerID) // Expansion moves
                    {


                        if (atm.Message == AttackMessage.Snipe)
                            SnipeMoves.Add(atm);
                        else if (Math.Round(atm.Armies.AttackPower * BotState.Settings.OffensiveKillRate) < mmToTerritory.Armies.DefensePower)
                            TransferingExpansionMoves.Add(atm);
                        else if (atm.Armies.AttackPower > 3 && !CanAnyOpponentAttackTerritory(mmFromTerritory))
                            BigExpansionMovesNonAttack.Add(atm);
                        else if (atm.Armies.AttackPower <= 3 && mmToTerritory.GetOpponentNeighbors().Count == 0 && !CanAnyOpponentAttackTerritory(mmFromTerritory))
                            NonOpponentBorderingSmallExpansionMovesNonAttack.Add(atm);
                        else if (atm.Armies.AttackPower <= 3 && mmToTerritory.GetOpponentNeighbors().Count > 0 && !CanAnyOpponentAttackTerritory(mmFromTerritory))
                            OpponentBorderingSmallExpansionMovesNonAttack.Add(atm);
                        else if (atm.Armies.AttackPower > 3 && CanAnyOpponentAttackTerritory(mmFromTerritory))
                            BigExpansionMovesWithAttack.Add(atm);
                        else if (atm.Armies.AttackPower <= 3 && mmToTerritory.GetOpponentNeighbors().Count == 0 && CanAnyOpponentAttackTerritory(mmFromTerritory))
                            NonOpponentBorderingSmallExpansionMovesWithAttack.Add(atm);
                        else if (atm.Armies.AttackPower <= 3 && mmToTerritory.GetOpponentNeighbors().Count > 0 && CanAnyOpponentAttackTerritory(mmFromTerritory))
                            OpponentBorderingSmallExpansionMovesWithAttack.Add(atm);
                    }
                }
            }
        }

        private bool IsAlwaysGoodAttackMove(BotOrderAttackTransfer atm)
        {
            var opponentIncome = BotState.GetGuessedOpponentIncome(atm.To.OwnerPlayerID, BotState.VisibleMap);
            var opponentArmies = atm.To.Armies.DefensePower + opponentIncome;
            // Heuristic since the opponent might have more income than expected
            opponentArmies += 3;
            return Math.Round(atm.Armies.AttackPower * BotState.Settings.OffensiveKillRate) >= Math.Round(opponentArmies * BotState.Settings.DefensiveKillRate);
        }

        /// <remarks>
        /// Calculates the highest defense territory value of a bordering territory that the opponent might break from his slippery territory. If there is no such territory then returns -1.
        /// </remarks>
        /// <param name="opponentTerritory"></param>
        /// <returns></returns>
        private int GetSlipperyOpponentTerritoryNumber(BotTerritory slipperyOpponentTerritory)
        {
            List<BotTerritory> territoriesOpponentMightBreak = new List<BotTerritory>();
            var opponentIncome = BotState.GetGuessedOpponentIncome(slipperyOpponentTerritory.OwnerPlayerID, BotState.VisibleMap);
            var opponentAttackingArmies = opponentIncome + slipperyOpponentTerritory.Armies.AttackPower - BotState.MustStandGuardOneOrZero;
            var neededArmiesForDefense = (int)Math.Round(opponentAttackingArmies * BotState.Settings.OffensiveKillRate);
            foreach (var ownedNeighbor in slipperyOpponentTerritory.GetOwnedNeighbors())
            {
                if (ownedNeighbor.GetArmiesAfterDeploymentAndIncomingMoves().DefensePower < neededArmiesForDefense)
                    territoriesOpponentMightBreak.Add(ownedNeighbor);
            }
            var sortedOwnedNeighbors = BotState.TerritoryValueCalculator.SortDefenseValue(territoriesOpponentMightBreak);
            if (sortedOwnedNeighbors.Count > 0)
                return sortedOwnedNeighbors[0].DefenceTerritoryValue;
            else
                return -1;
        }

        private bool IsProbablyCrushingMove(BotOrderAttackTransfer attack)
        {
            var guessedOpponentArmies = attack.To.GetArmiesAfterDeployment(BotTerritory.DeploymentType.Normal).DefensePower;
            var maximumOpponentArmies = attack.To.Armies.DefensePower + BotState
                .GetGuessedOpponentIncome(attack.To.OwnerPlayerID, BotState.VisibleMap);
            var adjustedOpponentArmies = Math.Max(guessedOpponentArmies, maximumOpponentArmies - 2);
            var isCrushingMove = Math.Round(attack.Armies.AttackPower * BotState.Settings.OffensiveKillRate) >= adjustedOpponentArmies;
            return isCrushingMove;
        }

        private bool CanOpponentBreakTerritory(BotTerritory ourTerritory)
        {
            var oppNeighbors = ourTerritory.GetOpponentNeighbors();

            if (oppNeighbors.Count == 0)
                return false;

            foreach (var group in oppNeighbors.GroupBy(o => o.OwnerPlayerID))
            {
                var opponentIncome = BotState.GetGuessedOpponentIncome(group.Key, BotState.VisibleMap);
                var ourArmies = ourTerritory.GetArmiesAfterDeploymentAndIncomingMoves();
                var opponentAttackingArmies = opponentIncome;
                foreach (var opponentNeighbor in group)
                    opponentAttackingArmies += opponentNeighbor.Armies.AttackPower - BotState.MustStandGuardOneOrZero;

                if (Math.Round(opponentAttackingArmies * BotState.Settings.OffensiveKillRate) >= ourArmies.DefensePower)
                    return true;
            }

            return false;
        }

        private bool CanOpponentGetAGoodAttack(BotTerritory ourTerritory)
        {
            var oppNeighbors = ourTerritory.GetOpponentNeighbors();

            if (oppNeighbors.Count == 0)
                return false;

            foreach (var group in oppNeighbors.GroupBy(o => o.OwnerPlayerID))
            {
                var opponentIncome = BotState.GetGuessedOpponentIncome(group.Key, BotState.VisibleMap);
                var ourArmies = ourTerritory.GetArmiesAfterDeploymentAndIncomingMoves();
                var opponentAttackingArmies = opponentIncome;
                foreach (var opponentNeighbor in group)
                    opponentAttackingArmies += opponentNeighbor.Armies.AttackPower - BotState.MustStandGuardOneOrZero;

                if (Math.Round(opponentAttackingArmies * BotState.Settings.OffensiveKillRate) >= Math.Round(ourArmies.DefensePower * BotState.Settings.DefensiveKillRate))
                    return true;
            }
            return false;
        }

        private bool CanAnyOpponentAttackTerritory(BotTerritory ourTerritory)
        {
            var oppNeighbors = ourTerritory.GetOpponentNeighbors();
            if (oppNeighbors.Count == 0)
                return false;

            foreach (var group in oppNeighbors.GroupBy(o => o.OwnerPlayerID))
            {
                var opponentIncome = BotState.GetGuessedOpponentIncome(group.Key, BotState.VisibleMap);
                var ourArmies = ourTerritory.Armies;
                var opponentAttackingArmies = opponentIncome;
                foreach (var opponentNeighbor in group)
                    opponentAttackingArmies += opponentNeighbor.Armies.AttackPower - BotState.MustStandGuardOneOrZero;

                if (Math.Round(opponentAttackingArmies * BotState.Settings.OffensiveKillRate) >= Math.Round(ourArmies.DefensePower * BotState.Settings.DefensiveKillRate))
                    return true;
            }

            return false;
        }

        private bool CanOpponentAttackTerritory(PlayerIDType opponentID, BotTerritory ourTerritory)
        {
            if (ourTerritory.GetOpponentNeighbors().Count == 0)
                return false;
            var opponentIncome = BotState.GetGuessedOpponentIncome(opponentID, BotState.VisibleMap);
            var ourArmies = ourTerritory.Armies;
            var opponentAttackingArmies = opponentIncome;
            foreach (var opponentNeighbor in ourTerritory.GetOpponentNeighbors())
                opponentAttackingArmies += opponentNeighbor.Armies.AttackPower - BotState.MustStandGuardOneOrZero;

            return Math.Round(opponentAttackingArmies * BotState.Settings.OffensiveKillRate) >= Math.Round(ourArmies.DefensePower * BotState.Settings.DefensiveKillRate);
        }

        /// <summary>
        /// Sorts the expansion moves according to the distance of the toTerritory to the direct opponent border (without
        /// blocking neutrals).
        /// </summary>
        /// <remarks>
        /// Sorts the expansion moves according to the distance of the toTerritory to the direct opponent border (without
        /// blocking neutrals).
        /// </remarks>
        /// <param name="unsortedMoves">the unsorted moves</param>
        /// <param name="reverse">if true then the move with the biggest to territory distance is returned first, else returned last
        /// </param>
        /// <returns>sorted moves</returns>
        private List<BotOrderAttackTransfer> SortExpansionMovesOpponentDistance(List
            <BotOrderAttackTransfer> unsortedMoves, bool reverse)
        {
            var outvar = new List<BotOrderAttackTransfer>();
            var temp = new List<BotOrderAttackTransfer>();
            temp.AddRange(unsortedMoves);
            while (temp.Any())
            {
                var extremestDistanceMove = temp[0];
                foreach (BotOrderAttackTransfer atm in temp)
                {
                    var reverseCondition = atm.To.DirectDistanceToOpponentBorder > extremestDistanceMove.To.DirectDistanceToOpponentBorder;
                    var nonReverseCondition = atm.To.DirectDistanceToOpponentBorder < extremestDistanceMove.To.DirectDistanceToOpponentBorder;
                    if ((reverseCondition && reverse) || (nonReverseCondition && !reverse))
                        extremestDistanceMove = atm;
                }
                temp.Remove(extremestDistanceMove);
                outvar.Add(extremestDistanceMove);
            }
            return outvar;
        }
    }
}
