/*
 * This code was auto-converted from a java project, originally written by Dan Zou
 */

using System;
using System.Collections.Generic;
using System.Linq;
using WarLight.AI.Cowzow.Comparators;
using WarLight.AI.Cowzow.Fulkerson2;
using WarLight.AI.Cowzow.Map;
using WarLight.AI.Cowzow.Move;

namespace WarLight.AI.Cowzow.Bot
{
    public class CowzowBot : IWarLightAI
    {
        public GameSettings Settings;
        public MapDetails Map;
        public GameStanding DistributionStanding;
        public GameStanding LatestStanding;
        public GameStanding PreviousTurnStanding;
        public PlayerIncome MyIncome;
        public GameOrder[] PreviousTurn;
        public GamePlayer Me;
        public Dictionary<PlayerIDType, GamePlayer> Players;
        public int NumberOfTurns;
        public BotMap BotMap;
        public HashSet<TerritoryIDType> MyDeployments = new HashSet<TerritoryIDType>();
        public Dictionary<TerritoryIDType, int> CaptureCosts = new Dictionary<TerritoryIDType, int>();
        private EdgePriorityComparator Eval;
        public readonly Dictionary<TerritoryIDType, int> OpponentVision = new Dictionary<TerritoryIDType, int>();
        public BonusAnalyzer Analyzer;

        public void Init(PlayerIDType myPlayerID, Dictionary<PlayerIDType, GamePlayer> players, MapDetails map, GameStanding distributionStanding, GameSettings gameSettings, int numberOfTurns, Dictionary<PlayerIDType, PlayerIncome> incomes, GameOrder[] prevTurn, GameStanding latestTurnStanding, GameStanding previousTurnStanding, Dictionary<PlayerIDType, TeammateOrders> teammatesOrders, List<CardInstance> cards, int cardsMustPlay)
        {
            this.Me = players[myPlayerID];
            this.Players = players;
            this.NumberOfTurns = numberOfTurns;
            this.Settings = gameSettings;
            this.Map = map;
            this.DistributionStanding = distributionStanding;
            this.LatestStanding = latestTurnStanding;
            this.PreviousTurnStanding = previousTurnStanding;
            this.MyIncome = incomes[myPlayerID];
            this.PreviousTurn = prevTurn;

            //teammatesOrders
            //cards
            //cardsMustPlay

            this.BotMap = new BotMap(this, Map, LatestStanding ?? DistributionStanding);
        }



        public bool IsTeammate(PlayerIDType playerID)
        {
            return playerID != Me.ID && Me.Team != PlayerInvite.NoTeam && Players.ContainsKey(playerID) && Players[playerID].Team == Me.Team;
        }
        public bool IsTeammateOrUs(PlayerIDType playerID)
        {
            return Me.ID == playerID || IsTeammate(playerID);
        }
        public bool IsOpponent(PlayerIDType playerID)
        {
            return Players.ContainsKey(playerID) && !IsTeammateOrUs(playerID);
        }

        public IEnumerable<GamePlayer> Opponents
        {
            get { return Players.Values.Where(o => IsOpponent(o.ID)); }
        }

        public IEnumerable<GameOrder> OpponentOrders
        {
            get
            {
                if (this.PreviousTurn == null)
                    return new GameOrder[0];
                else
                    return this.PreviousTurn.Where(o => IsOpponent(o.PlayerID));
            }
        }
        public int MustStandGuardOneOrZero
        {
            get { return Settings.OneArmyMustStandGuard ? 1 : 0; }
        }

        public List<TerritoryIDType> GetPicks()
        {
            var territoryList = DistributionStanding.Territories.Values.Where(o => o.OwnerPlayerID == TerritoryStanding.AvailableForDistribution).Select(o => BotMap.Territories[o.ID]).ToList();
            territoryList.Sort(new StartingTerritoryComparator(this));

            AILog.Log("Picking " + territoryList.Count + " territories: ");
            foreach (var terr in territoryList)
                AILog.Log(" - " + terr);

            return territoryList.Select(o => o.ID).ToList();
        }

        public List<GameOrder> GetOrders()
        {
            var deploys = GetPlaceArmiesMoves();
            var attacks = GetAttackTransferMoves();

            var final = deploys.Cast<BotOrder>().Concat(attacks).ToList();

            AILog.Log("Final " + final.Count + " orders: ");
            foreach (var order in final)
                AILog.Log(" - " + order);

            return BotOrder.Convert(final);
        }


        public IEnumerable<BotOrderDeploy> GetPlaceArmiesMoves()
        {
            GatherInformation();
            var placeArmiesMoves = new Dictionary<TerritoryIDType, int>();
            var armiesLeft = MyIncome.Total;

            // Find all my bordering territories.
            var borderingTerritories = new HashSet<TerritoryIDType>();
            foreach (var terr in BotMap.VisibleTerritories.Where(o => o.OwnerPlayerID == this.Me.ID))
                if (terr.GetStrongestNearestEnemy() > 0)
                    borderingTerritories.Add(terr.ID);


            // reserveTroops declaration
            var reserveTroops = new Dictionary<TerritoryIDType, int>();
            foreach (var terr in BotMap.VisibleTerritories.Where(o => o.OwnerPlayerID == this.Me.ID))
                reserveTroops[terr.ID] = terr.Armies - 1;  //TODO: OneArmyMustStandGuard

            // Get all attack paths
            var targets = BotMap.GetUnfriendlyTerritories().Select(o => o.ID).ToHashSet(true);
            var totalAttackPaths = new List<Edge>(100);
            foreach (var terr in targets)
                totalAttackPaths.AddRange(BotMap.Territories[terr].GetAttackPaths());

            // Add defensive moves
            foreach (var terr in borderingTerritories)
            {
                var selfEdge = new Edge(BotMap.Territories[terr], BotMap.Territories[terr], 0);
                if (Eval.GetScore(selfEdge) > 0)
                    totalAttackPaths.Add(selfEdge);
            }

            // v185
            totalAttackPaths.Sort(Eval);

            foreach (var attackPath in totalAttackPaths.Take(10))
                AILog.Log("Top attack path: " + attackPath + " score=" + Eval.GetScore(attackPath));

            var it = new EdgeChooser(totalAttackPaths, reserveTroops, Eval);
            while (it.HasNext() && armiesLeft > 0)
            {
                var curr = it.Next();
                if (Eval.GetScore(curr) < 0.05)
                    break;

                if (targets.Contains(curr.End.ID) || borderingTerritories.Contains(curr.End.ID))
                {
                    var armiesInReserve = reserveTroops[curr.Start.ID];
                    var armiesNeeded = CaptureCosts[curr.End.ID];
                    if (armiesNeeded > armiesInReserve + armiesLeft && curr.End.OwnerPlayerID == TerritoryStanding.NeutralPlayerID && curr.End.Armies < 4)  //TODO: < 4
                        continue;

                    if (curr.End.OwnerPlayerID == this.Me.ID)
                        armiesNeeded = Math.Max(1, CaptureCosts[curr.End.ID] - armiesInReserve); //how many we need to defend
                    else
                    {
                        if (Eval.DumpSet.ContainsKey(curr.ID) || curr.End.Bonuses.Any(Analyzer.MightBeOwned))
                            armiesNeeded = Math.Max(armiesLeft * 3 / 5, armiesNeeded * 3 / 2);
                    }
                    if (armiesInReserve >= armiesNeeded)
                        reserveTroops[curr.Start.ID] = armiesInReserve - armiesNeeded;
                    else
                    {
                        armiesNeeded -= armiesInReserve;
                        reserveTroops[curr.Start.ID] = 0;
                        var armiesPlaced = Math.Min(armiesNeeded, armiesLeft);
                        curr.Start.Armies += armiesPlaced;
                        placeArmiesMoves.AddTo(curr.Start.ID, armiesPlaced);
                        armiesLeft -= armiesPlaced;
                    }
                    targets.Remove(curr.End.ID);
                }
            }

            var best = totalAttackPaths[0].Start;
            if (armiesLeft > 0)
            {
                placeArmiesMoves.AddTo(best.ID, armiesLeft);
                best.Armies = best.Armies + armiesLeft;
            }

            MyDeployments.Clear();
            foreach (var move in placeArmiesMoves)
                MyDeployments.Add(move.Key);

            return placeArmiesMoves.Select(o => new BotOrderDeploy(Me.ID, BotMap.Territories[o.Key], o.Value));
        }

        public List<BotOrderAttackTransfer> GetAttackTransferMoves()
        {

            var myMovableTerritories = new HashSet<TerritoryIDType>();

            foreach (var r in BotMap.VisibleTerritories.Where(o => o.OwnerPlayerID == Me.ID))
                if (r.Armies > 1) //TODO: OneArmyMustStandGuard
                    myMovableTerritories.Add(r.ID);

            var manager = new OrderManager(this, myMovableTerritories);
            var network = new TerritoryBipartiteNetwork(this, Eval);

            // For each attacker, match them to enemy territories and neutral territories better than the first one they see.
            foreach (var attackerID in myMovableTerritories)
            {
                var attacker = BotMap.Territories[attackerID];

                var attackerChoices = new List<Edge>();
                foreach (var neighbor in attacker.Neighbors)
                    attackerChoices.Add(new Edge(attacker, neighbor, 0));

                attackerChoices.Add(new Edge(attacker, attacker, 0));
                attackerChoices.Sort(Eval);
                network.AddAttacker(attacker, attacker.Armies - 1); //TODO: OneArmyMustStandGuard
                var firstEnemy = false;
                foreach (var e in attackerChoices)
                {
                    if (e.End.OwnerPlayerID == TerritoryStanding.NeutralPlayerID && Eval.GetScore(e) < 0.05 && MyIncome.Total < 2 * Analyzer.TroopEstimate) //TODO: Settings
                    {
                        // && eval.getScore(attackerChoices.get(0)) >= 0.06
                        break;
                    }
                    if (firstEnemy == false || IsOpponent(e.End.OwnerPlayerID))
                    {
                        var est = CaptureCosts[e.End.ID];
                        // Section which ignores all neutral territories if an edge of a certain type is seen
                        // Ignore all neutral territories if there is a friendly territory with higher importance
                        if (e.End.OwnerPlayerID == Me.ID)
                        {
                            firstEnemy = true;
                            continue;
                        }
                        if (e.End.OwnerPlayerID == TerritoryStanding.NeutralPlayerID && attacker.Armies < est || e.End.OwnerPlayerID == TerritoryStanding.NeutralPlayerID && NumberOfTurns == 0 && e.End.Bonuses.Any(o => !o.IsSafe()))
                            firstEnemy = true;

                        if (IsOpponent(e.End.OwnerPlayerID))
                            firstEnemy = true;

                        network.AddTarget(e.End, est);
                        network.AddEdge(attacker, e.End, est, true);
                        if (Eval.DumpSet.ContainsKey(e.ID))
                            break;
                    }
                }
            }

            // Now we gotta order the stragglers
            var destinations = new HashSet<TerritoryIDType>();
            var orders = network.GenerateStrictFlow();

            foreach (var order in orders)
            {
                manager.AddOrder(order.Start, order.End, order.Flow);
                destinations.Add(order.End.ID);
            }
            var remainder = manager.GetRemainingMoves();
            foreach (var terrID in remainder.Keys)
            {
                var terr = BotMap.Territories[terrID];

                var existingEnemyOrders = new List<Edge>();
                foreach (var e in manager.GetOrders(terrID))
                    existingEnemyOrders.Add(e); //enemy orders???
                foreach (var e_1 in terr.GetFromPaths())
                    if (destinations.Contains(e_1.End.ID) && remainder[e_1.Start.ID] > CaptureCosts[e_1.End.ID])
                        existingEnemyOrders.Add(e_1);

                // Figure out if you want to leave this commented....... v183 commented, v184 not ocmmented
                if (!MyDeployments.Contains(terrID))
                {
                    foreach (var n in terr.Neighbors)
                        if (n.OwnerPlayerID == Me.ID)
                            existingEnemyOrders.Add(new Edge(terr, n, 0));
                }

                var best = new Edge(terr, terr, 0);
                foreach (var candidate in existingEnemyOrders)
                {
                    if (candidate.Start.ID == candidate.End.ID)
                        throw new ArgumentException();
                    if (IsOpponent(best.End.OwnerPlayerID) && !IsOpponent(candidate.End.OwnerPlayerID))
                        continue;
                    if (best.Start.ID == best.End.ID && best.End.GetStrongestNearestEnemy() > 0 && candidate.End.GetStrongestNearestEnemy() > 0)
                        best = candidate;
                    if (!IsOpponent(best.End.OwnerPlayerID) && IsOpponent(candidate.End.OwnerPlayerID))
                        best = candidate;
                    if (best.Start.ID == best.End.ID && best.Start.GetStrongestNearestEnemy() == 0)
                        best = candidate;
                    if (Eval.Compare(candidate, best) <= 0)
                        best = candidate;
                }
                if (best.Start.ID == best.End.ID)
                {
                    var bestCount = 0;
                    foreach (var neighbor in best.Start.Neighbors)
                    {
                        var count = 0;
                        foreach (var tmp in neighbor.Neighbors)
                            if (!tmp.IsVisible && tmp.Bonuses.Any(Analyzer.MightBeOwned))
                                count++;
                        if (neighbor.OwnerPlayerID == TerritoryStanding.NeutralPlayerID && remainder[best.Start.ID] > ArmiesNeededToCapture(neighbor.Armies) && count > bestCount)
                        {
                            bestCount = count;
                            best = new Edge(best.Start, neighbor, 0);
                        }
                    }
                }
                if (Eval.GetScore(best) != 0)
                    manager.AddOrder(best.Start, best.End, remainder[best.Start.ID]);
                else
                {
                    var e_2 = OrderStraggler(best.Start);
                    manager.AddOrder(e_2.Start, e_2.End, remainder[e_2.Start.ID]);
                }
            }
            var formalOrders = manager.GetFormalOrders();
            formalOrders.Sort(new AttackOrderComparator(this));
            return formalOrders;
        }

        // A memory of all enemy territories owned by the enemy
        // What my opponent sees before any placements
        // What my opponent placed last turn
        private int ArmiesNeededToCapture(int troops)
        {
            return (int)(1.6 * troops + 0.5);  //TODO: Settings
        }

        private Dictionary<TerritoryIDType, int> ConstructCaptureCosts(CowzowBot Bot)
        {
            var captureCosts = new Dictionary<TerritoryIDType, int>();
            foreach (var r in BotMap.VisibleTerritories)
                if (r.OwnerPlayerID == TerritoryStanding.NeutralPlayerID)
                {
                    var est = ArmiesNeededToCapture(r.Armies);
                    captureCosts[r.ID] = est;
                }
                else
                {
                    if (IsOpponent(r.OwnerPlayerID))
                    {
                        if (OpponentOrders.OfType<GameOrderDeploy>().None(o => o.DeployOn == r.ID) && r.Bonuses.Any(Analyzer.MightBeOwned))
                        {
                            var maxThreat = 0;
                            foreach (var n in r.Neighbors)
                                if (n.IsVisible && n.OwnerPlayerID == Me.ID)
                                {
                                    var threat = OpponentVision[n.ID];
                                    if (threat > maxThreat)
                                        maxThreat = threat;
                                }
                            var visibleCount = r.Bonuses.SelectMany(o => o.Territories).Where(o => o.IsVisible).Select(o => o.ID).Distinct().Count();

                            var est = Math.Max(maxThreat - 1, ArmiesNeededToCapture(r.Armies + 2));
                            if (visibleCount == 1 && r.Bonuses.Any(b => b.ArmiesReward >= 3)) //TODO: Magic numbers
                            {
                                var defendEst = (int)(0.75 * Math.Min(Analyzer.TroopEstimate, MyIncome.Total)); //TODO: Settings
                                est = Math.Max(est, ArmiesNeededToCapture(defendEst));
                            }
                            // est = Math.max(est, armiesNeededToCapture(r.getArmies()));
                            // est = Math.max(est, 5);
                            captureCosts[r.ID] = est;
                        }
                        else
                        {
                            if (OpponentOrders.OfType<GameOrderDeploy>().None(o => o.DeployOn == r.ID) && !r.Bonuses.Any(Analyzer.MightBeOwned))
                            {
                                var count = 0;
                                foreach (var tmp in BotMap.VisibleTerritories)
                                    if (IsOpponent(tmp.OwnerPlayerID))
                                        count++;
                                var est = ArmiesNeededToCapture(r.Armies + Math.Min(2, Analyzer.GetEffectiveTroops() / Math.Max(count, 1)));
                                captureCosts[r.ID] = est;
                            }
                            else
                            {
                                var oppDeployed = OpponentOrders.OfType<GameOrderDeploy>().Single(o => o.DeployOn == r.ID).NumArmies;
                                var guess = r.Armies;
                                guess += (int)((oppDeployed + 0.5) / 2);

                                foreach (var move in Bot.PreviousTurn.OfType<GameOrderAttackTransfer>().Where(o => o.PlayerID == Bot.Me.ID))
                                    if (move.To == r.ID)
                                    {
                                        guess += oppDeployed / 2;
                                        break;
                                    }

                                if (r.Bonuses.Any(Analyzer.MightBeOwned))
                                    guess += 1;
                                var est = ArmiesNeededToCapture(guess);
                                captureCosts[r.ID] = est;
                            }
                        }
                    }
                    else
                    {
                        var est = (int)Math.Max(1.7 * r.Armies, r.GetStrongestNearestEnemy() + 2); //TODO: Settings
                        est = Math.Max(est, r.Armies + Analyzer.TroopEstimate / 2);
                        captureCosts[r.ID] = est;
                    }
                }
            return captureCosts;
        }

        public void GatherInformation()
        {
            if (Analyzer == null)
                Analyzer = new BonusAnalyzer(this);
            else
                Analyzer.Process();

            OpponentVision.Clear();
            var fullMap = BotMap;

            foreach (var r_1 in fullMap.VisibleTerritories.Where(o => IsOpponent(o.OwnerPlayerID)))
                foreach (var neighbor in r_1.Neighbors)
                    if (neighbor.IsVisible && neighbor.OwnerPlayerID == Me.ID)
                        OpponentVision[neighbor.ID] = neighbor.Armies;

            CaptureCosts = ConstructCaptureCosts(this);
            Eval = new EdgePriorityComparator(this, CaptureCosts, Analyzer);
        }
        

        public Edge OrderStraggler(BotTerritory start)
        {
            var discoveredMap = new Dictionary<TerritoryIDType, BotTerritory>();
            var queue = new List<BotTerritory>();
            queue.Add(start);
            discoveredMap[start.ID] = start;
            var result = new Edge(start, start, 0);
            var counter = queue.Count;
            while (queue.Count > 0)
            {
                var curr = queue[0];
                queue.RemoveAt(0);
                counter--;
                if (curr.OwnerPlayerID != Me.ID)
                {
                    var candidate = new Edge(start, curr, 0);
                    if (Eval.Compare(candidate, result) < 0)
                        result = candidate;
                }
                foreach (var n in curr.Neighbors)
                    if (n.IsVisible && !discoveredMap.ContainsKey(n.ID))
                    {
                        queue.Add(n);
                        discoveredMap[n.ID] = curr;
                    }
                if (counter == 0)
                {
                    if (result.End.ID != start.ID)
                        break;
                    counter = queue.Count;
                }
            }
            var path = new List<BotTerritory>();
            var curr_1 = result.End;
            while (curr_1.ID != start.ID)
            {
                path.Insert(0, curr_1);
                curr_1 = discoveredMap[curr_1.ID];
            }
            if (path.Count > 1)
                return new Edge(start, path[0], 0);
            return new Edge(start, start, 0);
        }
    }
}
