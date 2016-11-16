/*   Syncrio License
 *   
 *   Copyright © 2016 Caleb Huyck
 *   
 *   This file is part of Syncrio.
 *   
 *   Syncrio is free software: you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation, either version 3 of the License, or
 *   (at your option) any later version.
 *   
 *   Syncrio is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *   GNU General Public License for more details.
 *   
 *   You should have received a copy of the GNU General Public License
 *   along with Syncrio.  If not, see <http://www.gnu.org/licenses/>.
 */


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyncrioUtil
{
    public class ScenarioDataTypes
    {
        //ContractSystem
        public struct ContractSystem
        {
            public List<string> header;
            public List<string> weights;
            public List<Contract> contracts;
            public List<Contract> finishedContracts;
        }
        public struct Contract
        {
            public string guid;
            public List<string> contractDataLines;
            public List<int> usedNodeNumbers;
            public List<Param> parameters;
        }
        public struct Param
        {
            public int nodeNumber;
            public List<string> paramLines;
            public List<SubParam> subParameters;
        }
        public struct SubParam
        {
            public int nodeNumber;
            public int parentNodeNumber;
            public List<string> subParamLines;
        }
        //Funding
        public struct Funding
        {
            public List<string> header;
            public string fundsLine;
        }
        //PartUpgradeManager
        public struct PartUpgradeManager
        {
            public List<string> header;
            public Upgrades upgrades;
        }
        public struct Upgrades
        {
            public List<string> unlocks;
            public List<string> enableds;
        }
        //ProgressTracking
        public struct ProgressTracking
        {
            public List<string> header;
            public BasicProgress basicProgress;
            public List<CelestialProgress> celestialProgress;
            public Secrets secrets;
        }
        public struct BasicProgress
        {
            public List<string> firstLaunch;
            public List<string> firstCrewToSurvive;
            public List<string> reachSpace;
            public List<string> KSCLanding;
            public List<string> launchpadLanding;
            public List<string> runwayLanding;
            public List<string> towerBuzz;

            public List<string> altitudeRecord;
            public List<string> depthRecord;
            public List<string> distanceRecord;
            public List<string> speedRecord;
        }
        public struct CelestialProgress
        {
            public string celestialBody;
            public string reached;

            public List<string> baseConstruction;
            public List<string> crewTransfer;
            public List<string> docking;
            public List<string> escape;
            public List<string> flagPlant;
            public List<string> flight;
            public List<string> flyBy;
            public List<string> landing;
            public List<string> orbit;
            public List<string> rendezvous;
            public List<string> returnFromFlyby;
            public List<string> returnFromOrbit;
            public List<string> returnFromSurface;
            public List<string> science;
            public List<string> spacewalk;
            public List<string> splashdown;
            public List<string> stationConstruction;
            public List<string> suborbit;
            public List<string> surfaceEVA;
        }
        /*                            *\
         * -------------------------- *
         * Alert!!!!!!!!!!!!!!!!!!!!!!*
         * Spoilers!!!!!!!!!!!!!!!!!!!*
         * Ahead!!!!!!!!!!!!!!!!!!!!!!*
         * -------------------------- *
        \*                            */
        public struct Secrets
        {
            public List<string> POIBopDeadKraken;
            public List<string> POIBopRandolith;
            public List<string> POIDresRandolith;
            public List<string> POIDunaFace;
            public List<string> POIDunaMSL;
            public List<string> POIDunaPyramid;
            public List<string> POIDunaRandolith;
            public List<string> POIEelooRandolith;
            public List<string> POIEveRandolith;
            public List<string> POIGillyRandolith;
            public List<string> POIIkeRandolith;
            public List<string> POIKerbinIslandAirfield;
            public List<string> POIKerbinKSC2;
            public List<string> POIKerbinMonolith00;
            public List<string> POIKerbinMonolith01;
            public List<string> POIKerbinMonolith02;
            public List<string> POIKerbinPyramids;
            public List<string> POIKerbinRandolith;
            public List<string> POIKerbinUFO;
            public List<string> POILaytheRandolith;
            public List<string> POIMinmusMonolith00;
            public List<string> POIMinmusRandolith;
            public List<string> POIMohoRandolith;
            public List<string> POIMunArmstrongMemorial;
            public List<string> POIMunMonolith00;
            public List<string> POIMunMonolith01;
            public List<string> POIMunMonolith02;
            public List<string> POIMunRandolith;
            public List<string> POIMunRockArch00;
            public List<string> POIMunRockArch01;
            public List<string> POIMunRockArch02;
            public List<string> POIMunUFO;
            public List<string> POIPolRandolith;
            public List<string> POITyloCave;
            public List<string> POITyloRandolith;
            public List<string> POIVallIcehenge;
            public List<string> POIVallRandolith;
        }
        /*                            *\
         * -------------------------- *
         * End!!!!!!!!!!!!!!!!!!!!!!!!*
         * Of!!!!!!!!!!!!!!!!!!!!!!!!!*
         * Spoilers!!!!!!!!!!!!!!!!!!!*
         * -------------------------- *
        \*                            */
        //Reputation
        public struct Reputation
        {
            public List<string> header;
            public string repLine;
        }
        //ResearchAndDevelopment
        public struct ResearchAndDevelopment
        {
            public List<string> header;
            public string sciLine;
            public List<Tech> techList;
            public List<List<string>> scienceList;
        }
        public struct Tech
        {
            public string idLine;
            public string stateLine;
            public string costLine;
            public List<string> parts;
        }
        //ResourceScenario
        public struct ResourceScenario
        {
            public List<string> header;
            public ResourceSettings resourceSettings;
        }
        public struct ResourceSettings
        {
            public List<string> resourceLines;
            public List<PlanetScanData> scanDataList;
        }
        public struct PlanetScanData
        {
            public string scanDataLine;
        }
        //ScenarioCustomWaypoints
        public struct ScenarioCustomWaypoints
        {
            public List<string> header;
            public List<Waypoint> waypoints;
        }
        public struct Waypoint
        {
            public List<string> waypointLines;
        }
        //ScenarioDestructibles
        public struct ScenarioDestructibles
        {
            public List<string> header;
            public List<Destructibles> destructibles;
        }
        public struct Destructibles
        {
            public string id;
            public string infoLine;
        }
        //ScenarioUpgradeableFacilities
        public struct ScenarioUpgradeableFacilities
        {
            public List<string> header;
            public UpgradeableBuildings buildings;
        }
        public struct UpgradeableBuildings
        {
            public string launchPad;
            public string runway;
            public string vehicleAssemblyBuilding;
            public string spaceplaneHangar;
            public string trackingStation;
            public string astronautComplex;
            public string missionControl;
            public string researchAndDevelopment;
            public string administration;
            public string flagPole;
        }
        //StrategySystem
        public struct StrategySystem
        {
            public List<string> header;
            public List<Strategy> strategies;
        }
        public struct Strategy
        {
            public List<string> strategy;
        }
    }
}
