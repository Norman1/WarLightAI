using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WarLight.AI.Wunderwaffe.Bot;

namespace WarLight.AI.Wunderwaffe.Evaluation
{
    public class FogRemover
    {

        private BotMain BotState;
        public FogRemover(BotMain state)
        {

            this.BotState = state;
        }

        public void RemoveFog()
        {
            BotMap lvMap = null;
            if (BotState.NumberOfTurns == -1)
            {
                return;
            }
            else if (BotState.NumberOfTurns == 0)
            {
                lvMap = BotMap.FromStanding(BotState, BotState.DistributionStanding);
            }
            else
            {
                lvMap = BotMain.LastVisibleMap;
            }

            BotMap visibleMap = BotState.VisibleMap;
            foreach (BotTerritory vmTerritory in visibleMap.Territories.Values)
            {
                if (vmTerritory.OwnerPlayerID == TerritoryStanding.FogPlayerID)
                {
                    BotTerritory lwmTerritory = lvMap.Territories[vmTerritory.ID];
                    if (lwmTerritory.OwnerPlayerID == TerritoryStanding.NeutralPlayerID || lwmTerritory.OwnerPlayerID == TerritoryStanding.AvailableForDistribution)
                    {
                        vmTerritory.OwnerPlayerID = TerritoryStanding.NeutralPlayerID;
                    }
                    // TODO fast and wrong solution for debugging
                    else
                    {
                        vmTerritory.OwnerPlayerID = TerritoryStanding.NeutralPlayerID;
                    }
                    vmTerritory.Armies = new Armies(lwmTerritory.Armies.NumArmies);

                }
            }


        }

    }
}
