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
    public class ScenarioDataConstructor
    {
        /// <summary>
        /// Takes a empty scenario data of the given type and sets it to the default values.
        /// </summary>
        public static ScenarioDataTypes.ContractSystem ConstructData(ScenarioDataTypes.ContractSystem inputType)
        {
            inputType.header = new List<string>();

            inputType.weights = new List<string>();

            inputType.contracts = new List<ScenarioDataTypes.Contract>();

            inputType.finishedContracts = new List<ScenarioDataTypes.Contract>();

            return inputType;
        }

        /// <summary>
        /// Takes a empty scenario data of the given type and sets it to the default values.
        /// </summary>
        public static ScenarioDataTypes.Funding ConstructData(ScenarioDataTypes.Funding inputType)
        {
            inputType.header = new List<string>();

            inputType.fundsLine = string.Empty;

            return inputType;
        }

        /// <summary>
        /// Takes a empty scenario data of the given type and sets it to the default values.
        /// </summary>
        public static ScenarioDataTypes.PartUpgradeManager ConstructData(ScenarioDataTypes.PartUpgradeManager inputType)
        {
            inputType.header = new List<string>();

            inputType.upgrades = new ScenarioDataTypes.Upgrades();

            inputType.upgrades.unlocks = new List<string>();

            inputType.upgrades.enableds = new List<string>();

            return inputType;
        }

        /// <summary>
        /// Takes a empty scenario data of the given type and sets it to the default values.
        /// </summary>
        public static ScenarioDataTypes.ProgressTracking ConstructData(ScenarioDataTypes.ProgressTracking inputType)
        {
            inputType.header = new List<string>();

            inputType.basicProgress = new ScenarioDataTypes.BasicProgress();

            inputType.basicProgress.altitudeRecord = new List<string>();

            inputType.basicProgress.depthRecord = new List<string>();

            inputType.basicProgress.distanceRecord = new List<string>();

            inputType.basicProgress.firstCrewToSurvive = new List<string>();

            inputType.basicProgress.firstLaunch = new List<string>();

            inputType.basicProgress.KSCLanding = new List<string>();

            inputType.basicProgress.launchpadLanding = new List<string>();

            inputType.basicProgress.reachSpace = new List<string>();

            inputType.basicProgress.runwayLanding = new List<string>();

            inputType.basicProgress.speedRecord = new List<string>();

            inputType.basicProgress.towerBuzz = new List<string>();

            inputType.celestialProgress = new List<ScenarioDataTypes.CelestialProgress>();

            /*                            *\
             * -------------------------- *
             * Alert!!!!!!!!!!!!!!!!!!!!!!*
             * Spoilers!!!!!!!!!!!!!!!!!!!*
             * Ahead!!!!!!!!!!!!!!!!!!!!!!*
             * -------------------------- *
            \*                            */
            inputType.secrets = new ScenarioDataTypes.Secrets();

            inputType.secrets.POIBopDeadKraken = new List<string>();

            inputType.secrets.POIBopRandolith = new List<string>();

            inputType.secrets.POIDresRandolith = new List<string>();

            inputType.secrets.POIDunaFace = new List<string>();

            inputType.secrets.POIDunaMSL = new List<string>();

            inputType.secrets.POIDunaPyramid = new List<string>();

            inputType.secrets.POIDunaRandolith = new List<string>();

            inputType.secrets.POIEelooRandolith = new List<string>();

            inputType.secrets.POIEveRandolith = new List<string>();

            inputType.secrets.POIGillyRandolith = new List<string>();

            inputType.secrets.POIIkeRandolith = new List<string>();

            inputType.secrets.POIKerbinIslandAirfield = new List<string>();

            inputType.secrets.POIKerbinKSC2 = new List<string>();

            inputType.secrets.POIKerbinMonolith00 = new List<string>();

            inputType.secrets.POIKerbinMonolith01 = new List<string>();

            inputType.secrets.POIKerbinMonolith02 = new List<string>();

            inputType.secrets.POIKerbinPyramids = new List<string>();

            inputType.secrets.POIKerbinRandolith = new List<string>();

            inputType.secrets.POIKerbinUFO = new List<string>();

            inputType.secrets.POILaytheRandolith = new List<string>();

            inputType.secrets.POIMinmusMonolith00 = new List<string>();

            inputType.secrets.POIMinmusRandolith = new List<string>();

            inputType.secrets.POIMohoRandolith = new List<string>();

            inputType.secrets.POIMunArmstrongMemorial = new List<string>();

            inputType.secrets.POIMunMonolith00 = new List<string>();

            inputType.secrets.POIMunMonolith01 = new List<string>();

            inputType.secrets.POIMunMonolith02 = new List<string>();

            inputType.secrets.POIMunRandolith = new List<string>();

            inputType.secrets.POIMunRockArch00 = new List<string>();

            inputType.secrets.POIMunRockArch01 = new List<string>();

            inputType.secrets.POIMunRockArch02 = new List<string>();

            inputType.secrets.POIMunUFO = new List<string>();

            inputType.secrets.POIPolRandolith = new List<string>();

            inputType.secrets.POITyloCave = new List<string>();

            inputType.secrets.POITyloRandolith = new List<string>();

            inputType.secrets.POIVallIcehenge = new List<string>();

            inputType.secrets.POIVallRandolith = new List<string>();
            /*                            *\
             * -------------------------- *
             * End!!!!!!!!!!!!!!!!!!!!!!!!*
             * Of!!!!!!!!!!!!!!!!!!!!!!!!!*
             * Spoilers!!!!!!!!!!!!!!!!!!!*
             * -------------------------- *
            \*                            */
            
            return inputType;
        }

        /// <summary>
        /// Takes a empty scenario data of the given type and sets it to the default values.
        /// </summary>
        public static ScenarioDataTypes.Reputation ConstructData(ScenarioDataTypes.Reputation inputType)
        {
            inputType.header = new List<string>();

            inputType.repLine = string.Empty;

            return inputType;
        }

        /// <summary>
        /// Takes a empty scenario data of the given type and sets it to the default values.
        /// </summary>
        public static ScenarioDataTypes.ResearchAndDevelopment ConstructData(ScenarioDataTypes.ResearchAndDevelopment inputType)
        {
            inputType.header = new List<string>();

            inputType.scienceList = new List<List<string>>();

            inputType.sciLine = string.Empty;

            inputType.techList = new List<ScenarioDataTypes.Tech>();

            return inputType;
        }

        /// <summary>
        /// Takes a empty scenario data of the given type and sets it to the default values.
        /// </summary>
        public static ScenarioDataTypes.ResourceScenario ConstructData(ScenarioDataTypes.ResourceScenario inputType)
        {
            inputType.header = new List<string>();

            inputType.resourceSettings = new ScenarioDataTypes.ResourceSettings();

            inputType.resourceSettings.resourceLines = new List<string>();

            inputType.resourceSettings.scanDataList = new List<ScenarioDataTypes.PlanetScanData>();

            return inputType;
        }

        /// <summary>
        /// Takes a empty scenario data of the given type and sets it to the default values.
        /// </summary>
        public static ScenarioDataTypes.ScenarioCustomWaypoints ConstructData(ScenarioDataTypes.ScenarioCustomWaypoints inputType)
        {
            inputType.header = new List<string>();

            inputType.waypoints = new List<ScenarioDataTypes.Waypoint>();

            return inputType;
        }

        /// <summary>
        /// Takes a empty scenario data of the given type and sets it to the default values.
        /// </summary>
        public static ScenarioDataTypes.ScenarioDestructibles ConstructData(ScenarioDataTypes.ScenarioDestructibles inputType)
        {
            inputType.header = new List<string>();

            inputType.destructibles = new List<ScenarioDataTypes.Destructibles>();

            return inputType;
        }

        /// <summary>
        /// Takes a empty scenario data of the given type and sets it to the default values.
        /// </summary>
        public static ScenarioDataTypes.ScenarioUpgradeableFacilities ConstructData(ScenarioDataTypes.ScenarioUpgradeableFacilities inputType)
        {
            inputType.header = new List<string>();

            inputType.buildings = new ScenarioDataTypes.UpgradeableBuildings();

            inputType.buildings.administration = string.Empty;

            inputType.buildings.astronautComplex = string.Empty;

            inputType.buildings.flagPole = string.Empty;

            inputType.buildings.launchPad = string.Empty;

            inputType.buildings.missionControl = string.Empty;

            inputType.buildings.researchAndDevelopment = string.Empty;

            inputType.buildings.runway = string.Empty;

            inputType.buildings.spaceplaneHangar = string.Empty;

            inputType.buildings.trackingStation = string.Empty;

            inputType.buildings.vehicleAssemblyBuilding = string.Empty;

            return inputType;
        }

        /// <summary>
        /// Takes a empty scenario data of the given type and sets it to the default values.
        /// </summary>
        public static ScenarioDataTypes.StrategySystem ConstructData(ScenarioDataTypes.StrategySystem inputType)
        {
            inputType.header = new List<string>();

            inputType.strategies = new List<ScenarioDataTypes.Strategy>();

            return inputType;
        }

        /// <summary>
        /// Takes a empty scenario sub data of the given type and sets it to the default values.
        /// </summary>
        public static ScenarioDataTypes.CelestialProgress ConstructSubData(ScenarioDataTypes.CelestialProgress inputType)
        {
            inputType.baseConstruction = new List<string>();

            inputType.celestialBody = string.Empty;

            inputType.crewTransfer = new List<string>();

            inputType.docking = new List<string>();

            inputType.escape = new List<string>();

            inputType.flagPlant = new List<string>();

            inputType.flight = new List<string>();

            inputType.flyBy = new List<string>();

            inputType.landing = new List<string>();

            inputType.orbit = new List<string>();

            inputType.reached = string.Empty;

            inputType.rendezvous = new List<string>();

            inputType.returnFromFlyby = new List<string>();

            inputType.returnFromOrbit = new List<string>();

            inputType.returnFromSurface = new List<string>();

            inputType.science = new List<string>();

            inputType.spacewalk = new List<string>();

            inputType.splashdown = new List<string>();

            inputType.stationConstruction = new List<string>();

            inputType.suborbit = new List<string>();

            inputType.surfaceEVA = new List<string>();

            return inputType;
        }

        /// <summary>
        /// Takes a empty scenario sub data of the given type and sets it to the default values.
        /// </summary>
        public static ScenarioDataTypes.Contract ConstructSubData(ScenarioDataTypes.Contract inputType)
        {
            inputType.contractDataLines = new List<string>();

            inputType.guid = string.Empty;

            inputType.parameters = new List<ScenarioDataTypes.Param>();

            inputType.usedNodeNumbers = new List<int>();

            return inputType;
        }

        /// <summary>
        /// Takes a empty scenario sub data of the given type and sets it to the default values.
        /// </summary>
        public static ScenarioDataTypes.Param ConstructSubData(ScenarioDataTypes.Param inputType)
        {
            inputType.paramLines = new List<string>();

            inputType.nodeNumber = 0;

            inputType.subParameters = new List<ScenarioDataTypes.SubParam>();

            return inputType;
        }

        /// <summary>
        /// Takes a empty scenario sub data of the given type and sets it to the default values.
        /// </summary>
        public static ScenarioDataTypes.SubParam ConstructSubData(ScenarioDataTypes.SubParam inputType)
        {
            inputType.subParamLines = new List<string>();

            inputType.nodeNumber = 0;

            inputType.parentNodeNumber = 0;

            return inputType;
        }

        /// <summary>
        /// Takes a empty scenario sub data of the given type and sets it to the default values.
        /// </summary>
        public static ScenarioDataTypes.Destructibles ConstructSubData(ScenarioDataTypes.Destructibles inputType)
        {
            inputType.id = string.Empty;

            inputType.infoLine = string.Empty;

            return inputType;
        }

        /// <summary>
        /// Takes a empty scenario sub data of the given type and sets it to the default values.
        /// </summary>
        public static ScenarioDataTypes.PlanetScanData ConstructSubData(ScenarioDataTypes.PlanetScanData inputType)
        {
            inputType.scanDataLine = string.Empty;

            return inputType;
        }

        /// <summary>
        /// Takes a empty scenario sub data of the given type and sets it to the default values.
        /// </summary>
        public static ScenarioDataTypes.Strategy ConstructSubData(ScenarioDataTypes.Strategy inputType)
        {
            inputType.strategy = new List<string>();

            return inputType;
        }

        /// <summary>
        /// Takes a empty scenario sub data of the given type and sets it to the default values.
        /// </summary>
        public static ScenarioDataTypes.Tech ConstructSubData(ScenarioDataTypes.Tech inputType)
        {
            inputType.costLine = string.Empty;

            inputType.idLine = string.Empty;

            inputType.stateLine = string.Empty;

            inputType.parts = new List<string>();

            return inputType;
        }

        /// <summary>
        /// Takes a empty scenario sub data of the given type and sets it to the default values.
        /// </summary>
        public static ScenarioDataTypes.Waypoint ConstructSubData(ScenarioDataTypes.Waypoint inputType)
        {
            inputType.waypointLines = new List<string>();

            return inputType;
        }
    }
}
