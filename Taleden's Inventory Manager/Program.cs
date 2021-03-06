﻿using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        #region mdk preserve
        // how often the script should update
        //     UpdateFrequency.None      - No automatic updating (manual only)
        //     UpdateFrequency.Once      - next tick (is unset after run)
        //     UpdateFrequency.Update1   - update every tick
        //     UpdateFrequency.Update10  - update every 10 ticks
        //     UpdateFrequency.Update100 - update every 100 ticks
        const UpdateFrequency UPDATE_FREQUENCY = UpdateFrequency.Update100;

        // The maximum run time of the script per call.
        // Measured in milliseconds.
        const double MAX_RUN_TIME = 35;

        // The maximum percent load that this script will allow
        // regardless of how long it has been executing.
        const double MAX_LOAD = 0.8;

        /*
        ***********************
        ADVANCED CONFIGURATION

        The settings below may be changed if you like, but read the notes and remember
        that any changes will be reverted when you update the script from the workshop.
        */

        // Each "Type/" section can have multiple "/Subtype"s, which are formatted like
        // "/Subtype,MinQta,PctQta,Label,Blueprint". Label and Blueprint specified only
        // if different from Subtype, but Ingot and Ore have no Blueprint. Quota values
        // are based on material requirements for various blueprints (some built in to
        // the game, some from the community workshop).
        const string DEFAULT_ITEMS = @"
AmmoMagazine/
/Missile200mm
/NATO_25x184mm,,,,NATO_25x184mmMagazine
/NATO_5p56x45mm,,,,NATO_5p56x45mmMagazine

ConsumableItem/
/Powerkit
/Medkit
/CosmicCoffee
/ClangCola

Datapad/
/Datapad

Package/
/Package

PhysicalObect/
/SpaceCredit

Component/
/BulletproofGlass,50,2%
/Computer,30,5%,,ComputerComponent
/Construction,150,20%,,ConstructionComponent
/Detector,10,0.1%,,DetectorComponent
/Display,10,0.5%
/Explosives,5,0.1%,,ExplosivesComponent
/Girder,10,0.5%,,GirderComponent
/GravityGenerator,1,0.1%,GravityGen,GravityGeneratorComponent
/InteriorPlate,100,10%
/LargeTube,10,2%
/Medical,15,0.1%,,MedicalComponent
/MetalGrid,20,2%
/Motor,20,4%,,MotorComponent
/PowerCell,20,1%
/RadioCommunication,10,0.5%,RadioComm,RadioCommunicationComponent
/Reactor,25,2%,,ReactorComponent
/SmallTube,50,3%
/SolarCell,20,0.1%
/SteelPlate,150,40%
/Superconductor,10,1%
/Thrust,15,5%,,ThrustComponent
/Canvas,5,0.01%

GasContainerObject/
/HydrogenBottle

Ingot/
/Cobalt,50,3.5%
/Gold,5,0.2%
/Iron,200,88%
/Magnesium,5,0.1%
/Nickel,30,1.5%
/Platinum,5,0.1%
/Silicon,50,2%
/Silver,20,1%
/Stone,50,2.5%
/Uranium,1,0.1%

Ore/
/Cobalt
/Gold
/Ice
/Iron
/Magnesium
/Nickel
/Platinum
/Scrap
/Silicon
/Silver
/Stone
/Uranium

OxygenContainerObject/
/OxygenBottle

PhysicalGunObject/
/AngleGrinderItem,,,,AngleGrinder
/AngleGrinder2Item,,,,AngleGrinder2
/AngleGrinder3Item,,,,AngleGrinder3
/AngleGrinder4Item,,,,AngleGrinder4
/AutomaticRifleItem,,,AutomaticRifle,AutomaticRifle
/HandDrillItem,,,,HandDrill
/HandDrill2Item,,,,HandDrill2
/HandDrill3Item,,,,HandDrill3
/HandDrill4Item,,,,HandDrill4
/PreciseAutomaticRifleItem,,,PreciseAutomaticRifle,PreciseAutomaticRifle
/RapidFireAutomaticRifleItem,,,RapidFireAutomaticRifle,RapidFireAutomaticRifle
/UltimateAutomaticRifleItem,,,UltimateAutomaticRifle,UltimateAutomaticRifle
/WelderItem,,,,Welder
/Welder2Item,,,,Welder2
/Welder3Item,,,,Welder3
/Welder4Item,,,,Welder4
";

        // Item types which may have quantities which are not whole numbers.
        static readonly HashSet<string> FRACTIONAL_TYPES = new HashSet<string> { "INGOT", "ORE" };

        // Ore subtypes which refine into Ingots with a different subtype name, or
        // which cannot be refined at all (if set to "").
        static readonly Dictionary<string, string> ORE_PRODUCT = new Dictionary<string, string>
        {
            // vanilla products
            { "ICE", "" }, { "ORGANIC", "" }, { "SCRAP", "IRON" },

            // better stone products
            // http://steamcommunity.com/sharedfiles/filedetails/?id=406244471
            {"DENSE IRON", "IRON"}, {"ICY IRON", "IRON"}, {"HEAZLEWOODITE", "NICKEL"}, {"CATTIERITE", "COBALT"}, {"PYRITE", "GOLD"},
            {"TAENITE", "NICKEL"}, {"COHENITE", "COBALT"}, {"KAMACITE", "NICKEL"}, {"GLAUCODOT", "COBALT"}, {"ELECTRUM", "GOLD"},
            {"PORPHYRY", "GOLD"}, {"SPERRYLITE", "PLATINUM"}, {"NIGGLIITE", "PLATINUM"}, {"GALENA", "SILVER"}, {"CHLORARGYRITE", "SILVER"},
            {"COOPERITE", "PLATINUM"}, {"PETZITE", "SILVER"}, {"HAPKEITE", "SILICON"}, {"DOLOMITE", "MAGNESIUM"}, {"SINOITE", "SILICON"},
            {"OLIVINE", "MAGNESIUM"}, {"QUARTZ", "SILICON"}, {"AKIMOTOITE", "MAGNESIUM"}, {"WADSLEYITE", "MAGNESIUM"}, {"CARNOTITE", "URANIUM"},
            {"AUTUNITE", "URANIUM"}, {"URANIAURITE", "GOLD"}
        };

        // Block types/subtypes which restrict item types/subtypes from their first
        // inventory. Missing or "*" subtype indicates all subtypes of the given type.
        const string DEFAULT_RESTRICTIONS =
        MOB + "Assembler:AmmoMagazine,Component,GasContainerObject,Ore,OxygenContainerObject,PhysicalGunObject\n" +
        MOB + "InteriorTurret:AmmoMagazine/Missile200mm,AmmoMagazine/NATO_25x184mm," + NON_AMMO +
        MOB + "LargeGatlingTurret:AmmoMagazine/Missile200mm,AmmoMagazine/NATO_5p56x45mm," + NON_AMMO +
        MOB + "LargeMissileTurret:AmmoMagazine/NATO_25x184mm,AmmoMagazine/NATO_5p56x45mm," + NON_AMMO +
        MOB + "OxygenGenerator:AmmoMagazine,Component,Ingot,Ore/Cobalt,Ore/Gold,Ore/Iron,Ore/Magnesium,Ore/Nickel,Ore/Organic,Ore/Platinum,Ore/Scrap,Ore/Silicon,Ore/Silver,Ore/Stone,Ore/Uranium,PhysicalGunObject\n" +
        MOB + "OxygenTank:AmmoMagazine,Component,GasContainerObject,Ingot,Ore,PhysicalGunObject\n" +
        MOB + "OxygenTank/LargeHydrogenTank:AmmoMagazine,Component,Ingot,Ore,OxygenContainerObject,PhysicalGunObject\n" +
        MOB + "OxygenTank/SmallHydrogenTank:AmmoMagazine,Component,Ingot,Ore,OxygenContainerObject,PhysicalGunObject\n" +
        MOB + "Reactor:AmmoMagazine,Component,GasContainerObject,Ingot/Cobalt,Ingot/Gold,Ingot/Iron,Ingot/Magnesium,Ingot/Nickel,Ingot/Platinum,Ingot/Scrap,Ingot/Silicon,Ingot/Silver,Ingot/Stone,Ore,OxygenContainerObject,PhysicalGunObject\n" +
        MOB + "Refinery:AmmoMagazine,Component,GasContainerObject,Ingot,Ore/Ice,Ore/Organic,OxygenContainerObject,PhysicalGunObject\n" +
        MOB + "Refinery/Blast Furnace:AmmoMagazine,Component,GasContainerObject,Ingot,Ore/Gold,Ore/Ice,Ore/Organic,Ore/Platinum,Ore/Silver,Ore/Uranium,OxygenContainerObject,PhysicalGunObject\n" +
        MOB + "SmallGatlingGun:AmmoMagazine/Missile200mm,AmmoMagazine/NATO_5p56x45mm," + NON_AMMO +
        MOB + "SmallMissileLauncher:AmmoMagazine/NATO_25x184mm,AmmoMagazine/NATO_5p56x45mm," + NON_AMMO +
        MOB + "SmallMissileLauncherReload:AmmoMagazine/NATO_25x184mm,AmmoMagazine/NATO_5p56x45mm," + NON_AMMO +
        MOB + "Parachute:Ingot,Ore,OxygenContainerObject,PhysicalGunObject,AmmoMagazine,GasContainerObject,Component/Construction,Component/MetalGrid,Component/InteriorPlate,Component/SteelPlate,Component/Girder,Component/SmallTube,Component/LargeTube,Component/Motor,Component/Display,Component/BulletproofGlass,Component/Superconductor,Component/Computer,Component/Reactor,Component/Thrust,Component/GravityGenerator,Component/Medical,Component/RadioCommunication,Component/Detector,Component/Explosives,Component/Scrap,Component/SolarCell,Component/PowerCell"
        ;
        #endregion

        const string MOB = "MyObjectBuilder_";
        const string NON_AMMO = "Component,GasContainerObject,Ingot,Ore,OxygenContainerObject,PhysicalGunObject\n";
        #region Fields

        #region mdk macros

        const int VERSION_MAJOR = 1, VERSION_MINOR = 8, VERSION_REVISION = 0;
        const string VERSION_UPDATE = "$MDK_DATE$";
        readonly string VERSION_NICE_TEXT = string.Format("v{0}.{1}.{2} ({3})", VERSION_MAJOR, VERSION_MINOR, VERSION_REVISION, VERSION_UPDATE);

        #endregion

        #region Format Strings

        const string FORMAT_TIM_UPDATE_TEXT = "Taleden's Inventory Manager\n{0}\nLast run: #{{0}} at {{1}}";
        /// <summary>
        /// The format string for building the tag parser.
        /// {0}: tag open.
        /// {1}: tag close.
        /// {2}: tag prefix.
        /// </summary>
        const string FORMAT_TAG_REGEX_BASE_PREFIX = @"{0} *{2}(|[ ,]+[^{1}]*){1}";
        /// <summary>
        /// The format string for building the tag parser.
        /// {0}: tag open.
        /// {2}: tag close.
        /// </summary>
        const string FORMAT_TAG_REGEX_BASE_NO_PREFIX = @"{0}([^{1}]*){1}";

        #endregion

        #region Arguments

        #region Defaults

        const bool DEFAULT_ARG_REWRITE_TAGS = true;
        const bool DEFAULT_ARG_QUOTA_STABLE = true;
        const char DEFAULT_ARG_TAG_OPEN = '[';
        const char DEFAULT_ARG_TAG_CLOSE = ']';
        const string DEFAULT_ARG_TAG_PREFIX = "TIM";
        const bool DEFAULT_ARG_SCAN_COLLECTORS = false;
        const bool DEFAULT_ARG_SCAN_DRILLS = false;
        const bool DEFAULT_ARG_SCAN_GRINDERS = false;
        const bool DEFAULT_ARG_SCAN_WELDERS = false;

        #endregion

        #region Actual

        bool argRewriteTags;
        bool argQuotaStable;
        char argTagOpen;
        char argTagClose;
        string argTagPrefix;
        bool argScanCollectors;
        bool argScanDrills;
        bool argScanGrinders;
        bool argScanWelders;
        string completeArguments;

        #endregion

        #region Handling

        const string ARGUMENT_PARSE_REGEX = @"^([^=\n]*)(?:=([^=\n]*))?$";
        readonly System.Text.RegularExpressions.Regex argParseRegex = new System.Text.RegularExpressions.Regex(
            ARGUMENT_PARSE_REGEX,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Multiline |
            System.Text.RegularExpressions.RegexOptions.Compiled);

        #endregion

        #endregion

        #region Helpers

        const StringComparison OIC = StringComparison.OrdinalIgnoreCase;
        const StringSplitOptions REE = StringSplitOptions.RemoveEmptyEntries;
        static readonly char[] SPACE = { ' ', '\t', '\u00AD' }, COLON = { ':' }, NEWLINE = { '\r', '\n' }, SPACECOMMA = { ' ', '\t', '\u00AD', ',' };

        #endregion

        #region Block & Type/SubType Information

        /// <summary>
        /// The items that are not allowed in the given block.
        /// Block Type -> block SubType -> Type -> [SubType].
        /// </summary>
        static Dictionary<string, Dictionary<string, Dictionary<string, HashSet<string>>>> blockSubTypeRestrictions = new Dictionary<string, Dictionary<string, Dictionary<string, HashSet<string>>>>();
        static List<string> types = new List<string>();
        static Dictionary<string, string> typeLabel = new Dictionary<string, string>();
        static Dictionary<string, List<string>> typeSubs = new Dictionary<string, List<string>>();
        static Dictionary<string, long> typeAmount = new Dictionary<string, long>();
        static List<string> subs = new List<string>();
        static Dictionary<string, string> subLabel = new Dictionary<string, string>();
        static Dictionary<string, List<string>> subTypes = new Dictionary<string, List<string>>();
        static Dictionary<string, Dictionary<string, InventoryItemData>> typeSubData = new Dictionary<string, Dictionary<string, InventoryItemData>>();
        static Dictionary<MyDefinitionId, ItemId> blueprintItem = new Dictionary<MyDefinitionId, ItemId>();

        #endregion

        #region Script state & storage

        string panelStatsHeader = "";
        string[] statsLog = new string[12];
        /// <summary>
        /// The time we started the last cycle at.
        /// </summary>
        DateTime currentCycleStartTime;
        long totalCallCount;
        int numberTransfers;
        int numberRefineries;
        int numberAssemblers;
        int processStep;
        readonly Action[] processSteps;
        System.Text.RegularExpressions.Regex tagRegex;
        static bool foundNewItem;
        string timUpdateText;

        Dictionary<int, Dictionary<string, Dictionary<string, Dictionary<IMyInventory, long>>>> priTypeSubInvenRequest = new Dictionary<int, Dictionary<string, Dictionary<string, Dictionary<IMyInventory, long>>>>();
        Dictionary<IMyTextPanel, int> qpanelPriority = new Dictionary<IMyTextPanel, int>();
        Dictionary<IMyTextPanel, List<string>> qpanelTypes = new Dictionary<IMyTextPanel, List<string>>();
        Dictionary<IMyTextPanel, List<string>> ipanelTypes = new Dictionary<IMyTextPanel, List<string>>();
        List<IMyTextPanel> statusPanels = new List<IMyTextPanel>();
        List<IMyTextPanel> debugPanels = new List<IMyTextPanel>();
        Dictionary<IMyTerminalBlock, System.Text.RegularExpressions.Match> blockGtag = new Dictionary<IMyTerminalBlock, System.Text.RegularExpressions.Match>();
        Dictionary<IMyTerminalBlock, System.Text.RegularExpressions.Match> blockTag = new Dictionary<IMyTerminalBlock, System.Text.RegularExpressions.Match>();
        HashSet<IMyInventory> invenLocked = new HashSet<IMyInventory>();
        HashSet<IMyInventory> invenHidden = new HashSet<IMyInventory>();
        Dictionary<IMyRefinery, HashSet<string>> refineryOres = new Dictionary<IMyRefinery, HashSet<string>>();
        Dictionary<IMyAssembler, HashSet<ItemId>> assemblerItems = new Dictionary<IMyAssembler, HashSet<ItemId>>();
        Dictionary<IMyFunctionalBlock, ProducerWork> producerWork = new Dictionary<IMyFunctionalBlock, ProducerWork>();
        Dictionary<IMyFunctionalBlock, int> producerJam = new Dictionary<IMyFunctionalBlock, int>();
        Dictionary<IMyTextPanel, Pair> panelSpan = new Dictionary<IMyTextPanel, Pair>();
        Dictionary<IMyTerminalBlock, HashSet<IMyTerminalBlock>> blockErrors = new Dictionary<IMyTerminalBlock, HashSet<IMyTerminalBlock>>();

        #endregion

        #endregion

        #region Properties

        int ExecutionTime
        {
            get { return (int)((DateTime.Now - currentCycleStartTime).TotalMilliseconds + 0.5); }
        }

        double ExecutionLoad
        {
            get {
                double instructions = Runtime.CurrentInstructionCount;
                return instructions / Runtime.MaxInstructionCount;
            }
        }
        #endregion

        #region Entry Points

        public Program()
        {
            // initialise the process steps we will need to do
            processSteps = new Action[]
            {
                ProcessStepProcessArgs,           // 0:  always process arguments first to handle changes
                ProcessStepScanGrids,             // 1:  scan grids next to find out if there is another TIM in the terminal system
                ProcessStepStandbyCheck,          // 2:  detect if another TIM should run instead and if we should be backup
                ProcessStepInventoryScan,         // 3:  do the inventory scanning
                ProcessStepParseTags,             // 4:  parse the tags of the blocks we found
                ProcessStepAmountAdjustment,      // 5:  adjust item amounts based on what's available
                ProcessStepQuotaPanels,           // 6:  handle quota panels
                ProcessStepLimitedItemRequests,   // 7:  handle limited item requests
                ProcessStepManageRefineries,      // 8:  handle all refineries we need to
                ProcessStepUnlimitedItemRequests, // 9:  handle unlimited item requests
                ProcessStepManageAssemblers,      // 10: handle all assemblers we need to
                ProcessStepScanProduction,        // 11: scan all production blocks and handle them
                ProcessStepUpdateInventoryPanels, // 12: update all inventory panels
            };

            // initialize panel data
            int unused;
            ScreenFormatter.Init();
            panelStatsHeader = "Taleden's Inventory Manager\n" +
                               VERSION_NICE_TEXT + "\n\n" +
                               ScreenFormatter.Format("Run", 80, out unused, 1) +
                               ScreenFormatter.Format("F-Step", 125 + unused, out unused, 1) +
                               ScreenFormatter.Format("Time", 145 + unused, out unused, 1) +
                               ScreenFormatter.Format("Load", 105 + unused, out unused, 1) +
                               ScreenFormatter.Format("S", 65 + unused, out unused, 1) +
                               ScreenFormatter.Format("R", 65 + unused, out unused, 1) +
                               ScreenFormatter.Format("A", 65 + unused, out unused, 1) +
                               "\n\n";

            // initialize default items, quotas, labels and blueprints
            // (TIM can also learn new items it sees in inventory)
            InitItems(DEFAULT_ITEMS);

            // initialize block:item restrictions
            // (TIM can also learn new restrictions whenever item transfers fail)
            InitBlockRestrictions(DEFAULT_RESTRICTIONS);

            // Set run frequency
            Runtime.UpdateFrequency = UPDATE_FREQUENCY;

            // echo compilation statement
            Echo("Compiled TIM " + VERSION_NICE_TEXT);

            // format terminal info text
            timUpdateText = string.Format(FORMAT_TIM_UPDATE_TEXT, VERSION_NICE_TEXT);
        }

        public void Main(string argument)
        {
            // init call
            currentCycleStartTime = DateTime.Now;
            int startingStep = processStep;

            // output terminal info
            Echo(string.Format(timUpdateText, ++totalCallCount, currentCycleStartTime.ToString("h:mm:ss tt")));

            // reset status every cycle
            ClearDebugMessages();
            numberTransfers = numberRefineries = numberAssemblers = 0;

            bool didAtLeastOneStep = false;
            try
            {
                do
                {
                    Debug(string.Format("> Doing step {0}", processStep));
                    processSteps[processStep]();
                    processStep++;
                    didAtLeastOneStep = true;
                } while (processStep < processSteps.Length && DoExecutionLimitCheck());
                // if we get here it means we completed all the process steps
                processStep = 0;
            }
            catch (ArgumentException ex)
            {
                Echo(ex.Message);
                processStep = 0;
                return;
            }
            catch (IgnoreExecutionException)
            {
                processStep = 0;
                return;
            }
            catch (PutOffExecutionException)
            { }
            catch (Exception ex)
            {
                // if the process step threw an exception, make sure we print the info
                // we need to debug it
                string err = "An error occured,\n" +
                    "please give the following information to the developer:\n" +
                    string.Format("Current step on error: {0}\n{1}", processStep, ex.ToString().Replace("\r", ""));
                Debug(err);
                UpdateStatusPanels();
                Echo(err);
                throw ex;
            }

            // update script status and debug panels on every cycle step
            int goalStep = processStep == 0 ? 13 : processStep;

            int executionTime = ExecutionTime;
            double executionLoadPercent = Math.Round(100.0f * ExecutionLoad, 1);

            int stepCompleted = (processStep == 0 ? processSteps.Length : processStep);
            int steps = processSteps.Length;

            int unused;
            statsLog[totalCallCount % statsLog.Length] = ScreenFormatter.Format($"{totalCallCount}", 80, out unused, 1) +
                                                         ScreenFormatter.Format($"{stepCompleted} / {steps}", 125 + unused, out unused, 1, true) +
                                                         ScreenFormatter.Format($"{executionTime} ms", 145 + unused, out unused, 1) +
                                                         ScreenFormatter.Format($"{executionLoadPercent}%", 105 + unused, out unused, 1, true) +
                                                         ScreenFormatter.Format($"{numberTransfers}", 65 + unused, out unused, 1, true) +
                                                         ScreenFormatter.Format($"{numberRefineries}", 65 + unused, out unused, 1, true) +
                                                         ScreenFormatter.Format($"{numberAssemblers}", 65 + unused, out unused, 1, true) +
                                                         "\n";

            string stepText = StepStatusText(startingStep, processStep, goalStep, didAtLeastOneStep);
            var msg = $"Completed {stepText} in {executionTime}ms, {executionLoadPercent}% load ({Runtime.CurrentInstructionCount} instructions)";
            Echo(msg);
            Debug(msg);
            UpdateStatusPanels();
        }

        string StepStatusText(int startingStep, int processStep, int goalStep, bool didAtLeastOneStep)
        {
            if (processStep == 0 && startingStep == 0 && didAtLeastOneStep)
                return "all steps";
            else if (processStep == startingStep)
                return $"step {processStep} partially";
            else if (goalStep - startingStep == 1)
                return $"step {startingStep}";
            return $"steps {startingStep} to {goalStep - 1}";
        }

        #endregion

        #region Init

        void InitItems(string data)
        {
            string itemTypeId = "";
            foreach (string line in data.Split(NEWLINE, REE))
            {
                string[] words = (line.Trim() + ",,,,").Split(SPACECOMMA, 6);
                words[0] = words[0].Trim();
                if (words[0].EndsWith("/"))
                {
                    itemTypeId = words[0].Substring(0, words[0].Length - 1);
                }
                else if (itemTypeId != "" & words[0].StartsWith("/"))
                {
                    string itemSubTypeId = words[0].Substring(1);

                    long absoluteQuota;
                    long.TryParse(words[1], out absoluteQuota);

                    string ratioQuotaExpression = words[2].Substring(0, (words[2] + "%").IndexOf("%"));
                    float ratioQuota;
                    float.TryParse(ratioQuotaExpression, out ratioQuota);

                    string label = words[3].Trim();

                    string blueprint = itemTypeId == "Ingot" | itemTypeId == "Ore" ? null : words[4].Trim();

                    InventoryItemData.InitItem(itemTypeId, itemSubTypeId, absoluteQuota, ratioQuota, label, blueprint);
                }
            }
        }

        void InitBlockRestrictions(string data)
        {
            foreach (string line in data.Split(NEWLINE, REE))
            {
                string[] blockitems = (line + ":").Split(':');
                string[] block = (blockitems[0] + "/*").Split('/');

                string blockTypeId = block[0].Trim(SPACE);
                string blockSubTypeId = block[1].Trim(SPACE);

                foreach (string item in blockitems[1].Split(','))
                {
                    string[] typesub = item.ToUpper().Split('/');

                    string itemTypeId = typesub[0];
                    string itemSubTypeId = typesub.Length > 1 ? typesub[1] : null;

                    AddBlockRestriction(blockTypeId, blockSubTypeId, itemTypeId, itemSubTypeId, true);
                }
            }
        }

        #endregion

        #region Runtime

        #region Arguments

        void ProcessScriptArgs()
        {
            // init all args back to default
            argRewriteTags = DEFAULT_ARG_REWRITE_TAGS;
            argTagOpen = DEFAULT_ARG_TAG_OPEN;
            argTagClose = DEFAULT_ARG_TAG_CLOSE;
            argTagPrefix = DEFAULT_ARG_TAG_PREFIX;
            argScanCollectors = DEFAULT_ARG_SCAN_COLLECTORS;
            argScanDrills = DEFAULT_ARG_SCAN_DRILLS;
            argScanGrinders = DEFAULT_ARG_SCAN_GRINDERS;
            argScanWelders = DEFAULT_ARG_SCAN_WELDERS;
            argQuotaStable = DEFAULT_ARG_QUOTA_STABLE;
            ResetDebugFlags();

            foreach (System.Text.RegularExpressions.Match match in argParseRegex.Matches(Me.CustomData))
            {
                var arg = match.Groups[1].Value.ToLower();
                var value = "";
                var hasValue = match.Groups[2].Success;
                if (hasValue)
                    value = match.Groups[2].Value.Trim();
                switch (arg)
                {
                    case "rewrite":
                        if (hasValue)
                            throw new ArgumentException("Argument 'rewrite' does not have a value");
                        argRewriteTags = true;
                        Debug("Tag rewriting enabled");
                        break;
                    case "norewrite":
                        if (hasValue)
                            throw new ArgumentException("Argument 'norewrite' does not have a value");
                        argRewriteTags = false;
                        Debug("Tag rewriting disabled");
                        break;
                    case "tags":
                        if (value.Length != 2)
                            throw new ArgumentException(string.Format("Invalid 'tags=' delimiters '{0}': must be exactly two characters", value));
                        else if (char.ToUpper(value[0]) == char.ToUpper(value[1]))
                            throw new ArgumentException(string.Format("Invalid 'tags=' delimiters '{0}': characters must be different", value));
                        else
                        {
                            argTagOpen = char.ToUpper(value[0]);
                            argTagClose = char.ToUpper(value[1]);
                            Debug(string.Format("Tags are delimited by '{0}' and '{1}", argTagOpen, argTagClose));
                        }
                        break;
                    case "prefix":
                        argTagPrefix = value.ToUpper();
                        if (argTagPrefix == "")
                            Debug("Tag prefix disabled");
                        else
                            Debug(string.Format("Tag prefix is '{0}'", argTagPrefix));
                        break;
                    case "scan":
                        switch (value.ToLower())
                        {
                            case "collectors":
                                argScanCollectors = true;
                                Debug("Enabled scanning of Collectors");
                                break;
                            case "drills":
                                argScanDrills = true;
                                Debug("Enabled scanning of Drills");
                                break;
                            case "grinders":
                                argScanGrinders = true;
                                Debug("Enabled scanning of Grinders");
                                break;
                            case "welders":
                                argScanWelders = true;
                                Debug("Enabled scanning of Welders");
                                break;
                            default:
                                throw new ArgumentException(string.Format("Invalid 'scan=' block type '{0}': must be 'collectors', 'drills', 'grinders' or 'welders'", value));
                        }
                        break;
                    case "quota":
                        switch (value.ToLower())
                        {
                            case "literal":
                                argQuotaStable = false;
                                Debug("Disabled stable dynamic quotas");
                                break;
                            case "stable":
                                argQuotaStable = true;
                                Debug("Enabled stable dynamic quotas");
                                break;
                            default:
                                throw new ArgumentException(string.Format("Invalid 'quota=' mode '{0}': must be 'literal' or 'stable'", value));
                        }
                        break;
                    case "debug":
                        value = value.ToLower();
                        EnableDebugFlag(value);
                        break;
                    case "":
                    case "tim_version":
                        break;
                    default:
                        // if an argument is not recognised, abort
                        throw new ArgumentException(string.Format("Unrecognized argument: '{0}'", arg));
                }
            }

            tagRegex = new System.Text.RegularExpressions.Regex(string.Format(
                argTagPrefix != "" ? FORMAT_TAG_REGEX_BASE_PREFIX : FORMAT_TAG_REGEX_BASE_NO_PREFIX, // select regex statement
                System.Text.RegularExpressions.Regex.Escape(argTagOpen.ToString()),
                System.Text.RegularExpressions.Regex.Escape(argTagClose.ToString()),
                System.Text.RegularExpressions.Regex.Escape(argTagPrefix)), // format in args
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);
        }

        #endregion

        #region Process Steps

        // This is where all the steps we need to complete are.
        // At the end of each step, a check will be done to decide
        // whether we should continue the processing or wait till the next
        // call. However, if any step raises a PutOffExecutionException,
        // then we will wait until the next call to complete that step.

        /// <summary>
        /// Processes the block arguments.
        /// </summary>
        /// <returns>Whether the step completed.</returns>
        public void ProcessStepProcessArgs()
        {
            if (Me.CustomData != completeArguments)
            {
                Debug("Arguments changed, re-processing...");
                ProcessScriptArgs();
                completeArguments = Me.CustomData;
            }
        }

        /// <summary>
        /// Scans all the grids and initialises the connections
        /// </summary>
        /// <returns>Whether the step completed.</returns>
        public void ProcessStepScanGrids()
        {
            Debug("Scanning grid connectors...");
            ScanGrids();
        }

        public void ProcessStepStandbyCheck()
        {
            Debug("Checking for other TIMs...");
            CheckStandby();
        }

        /// <summary>
        /// Scans all inventories to build what blocks need to be processed.
        /// </summary>
        /// <returns>Whether the step completed.</returns>
        public void ProcessStepInventoryScan()
        {
            Debug("Scanning inventories...");

            // reset everything that we'll check during this step
            foreach (string itype in types)
            {
                typeAmount[itype] = 0;
                foreach (InventoryItemData data in typeSubData[itype].Values)
                {
                    data.amount = 0L;
                    data.avail = 0L;
                    data.locked = 0L;
                    data.invenTotal.Clear();
                    data.invenSlot.Clear();
                }
            }
            blockTag.Clear();
            blockGtag.Clear();
            invenLocked.Clear();
            invenHidden.Clear();

            // scan inventories
            ScanGroups();
            ScanBlocks<IMyAssembler>();
            ScanBlocks<IMyCargoContainer>();
            if (argScanCollectors)
                ScanBlocks<IMyCollector>();
            ScanBlocks<IMyGasGenerator>();
            ScanBlocks<IMyGasTank>();
            ScanBlocks<IMyOxygenFarm>(); // scan oxygen farm to allow nanite support
            ScanBlocks<IMyReactor>();
            ScanBlocks<IMyRefinery>();
            ScanBlocks<IMyShipConnector>();
            ScanBlocks<IMyShipController>();
            if (argScanDrills)
                ScanBlocks<IMyShipDrill>();
            if (argScanGrinders)
                ScanBlocks<IMyShipGrinder>();
            if (argScanWelders)
                ScanBlocks<IMyShipWelder>();
            ScanBlocks<IMyTextPanel>();
            ScanBlocks<IMyUserControllableGun>();
            ScanBlocks<IMyParachute>();

            // if we found any new item type/subtypes, re-sort the lists
            if (foundNewItem)
            {
                foundNewItem = false;
                types.Sort();
                foreach (string itype in types)
                    typeSubs[itype].Sort();
                subs.Sort();
                foreach (string isub in subs)
                    subTypes[isub].Sort();
            }
        }

        /// <summary>
        /// Parses all found block tags.
        /// </summary>
        /// <returns>Whether the step completed.</returns>
        public void ProcessStepParseTags()
        {
            Debug("Scanning tags...");

            // reset everything that we'll check during this step
            foreach (string itype in types)
            {
                foreach (InventoryItemData data in typeSubData[itype].Values)
                {
                    data.qpriority = -1;
                    data.quota = 0L;
                    data.producers.Clear();
                }
            }
            qpanelPriority.Clear();
            qpanelTypes.Clear();
            ipanelTypes.Clear();
            priTypeSubInvenRequest.Clear();
            statusPanels.Clear();
            debugPanels.Clear();
            refineryOres.Clear();
            assemblerItems.Clear();
            panelSpan.Clear();

            // parse tags
            ParseBlockTags();
        }

        /// <summary>
        /// Adjusts the tracked amounts of items in inventories.
        /// </summary>
        /// <returns>Whether the step completed.</returns>
        public void ProcessStepAmountAdjustment()
        {
            Debug("Adjusting tallies...");
            AdjustAmounts();
        }

        /// <summary>
        /// Processes quota panels.
        /// </summary>
        /// <returns>Whether the step completed.</returns>
        public void ProcessStepQuotaPanels()
        {
            Debug("Scanning quota panels...");
            ProcessQuotaPanels(argQuotaStable);
        }

        /// <summary>
        /// Processes the limited item allocations.
        /// </summary>
        /// <returns>Whether the step completed.</returns>
        public void ProcessStepLimitedItemRequests()
        {
            Debug("Processing limited item requests...");
            AllocateItems(true); // limited requests
        }

        /// <summary>
        /// Manages handled refineries.
        /// </summary>
        /// <returns>Whether the step completed.</returns>
        public void ProcessStepManageRefineries()
        {
            Debug("Managing refineries...");
            ManageRefineries();
        }

        /// <summary>
        /// Scans all production blocks.
        /// </summary>
        /// <returns>Whether the step completed.</returns>
        public void ProcessStepScanProduction()
        {
            Debug("Scanning production...");
            ScanProduction();
        }

        /// <summary>
        /// Process unlimited item requests using the remaining items.
        /// </summary>
        /// <returns>Whether the step completed.</returns>
        public void ProcessStepUnlimitedItemRequests()
        {
            Debug("Processing remaining item requests...");
            AllocateItems(false); // unlimited requests
        }

        /// <summary>
        /// Manages handled assemblers.
        /// </summary>
        /// <returns>Whether the step completed.</returns>
        public void ProcessStepManageAssemblers()
        {
            Debug("Managing assemblers...");
            ManageAssemblers();
        }

        /// <summary>
        /// Updates all inventory panels.
        /// </summary>
        /// <returns>Whether the step completed.</returns>
        public void ProcessStepUpdateInventoryPanels()
        {
            Debug("Updating inventory panels...");
            UpdateInventoryPanels();
        }

        #endregion

        #endregion

        #region Util

        /// <summary>
        /// Checks if the current call has exceeded the maximum execution limit.
        /// If it has, then it will raise a <see cref="PutOffExecutionException:T"/>.
        /// </summary>
        /// <returns>True.</returns>
        /// <remarks>This methods returns true by default to allow use in the while check.</remarks>
        bool DoExecutionLimitCheck()
        {
            if (ExecutionTime > MAX_RUN_TIME || ExecutionLoad > MAX_LOAD)
                throw new PutOffExecutionException();
            return true;
        }

        void AddBlockRestriction(string btype, string bsub, string itype, string isub, bool init = false)
        {
            Dictionary<string, Dictionary<string, HashSet<string>>> bsubItypeRestr;
            Dictionary<string, HashSet<string>> itypeRestr;
            HashSet<string> restr;

            if (!blockSubTypeRestrictions.TryGetValue(btype.ToUpper(), out bsubItypeRestr))
                blockSubTypeRestrictions[btype.ToUpper()] = bsubItypeRestr = new Dictionary<string, Dictionary<string, HashSet<string>>> { { "*", new Dictionary<string, HashSet<string>>() } };
            if (!bsubItypeRestr.TryGetValue(bsub.ToUpper(), out itypeRestr))
            {
                bsubItypeRestr[bsub.ToUpper()] = itypeRestr = new Dictionary<string, HashSet<string>>();
                if (bsub != "*" & !init)
                {
                    foreach (KeyValuePair<string, HashSet<string>> pair in bsubItypeRestr["*"])
                        itypeRestr[pair.Key] = pair.Value != null ? new HashSet<string>(pair.Value) : null;
                }
            }
            if (isub == null | isub == "*")
            {
                itypeRestr[itype] = null;
            }
            else
            {
                itypeRestr.GetOrAdd(itype, Make.HashSet<string>).Add(isub);
            }
            if (!init) Debug(btype + "/" + bsub + " does not accept " + typeLabel[itype] + "/" + subLabel[isub]);
        }

        bool BlockAcceptsTypeSub(IMyCubeBlock block, string itype, string isub)
        {
            Dictionary<string, Dictionary<string, HashSet<string>>> bsubItypeRestr;
            Dictionary<string, HashSet<string>> itypeRestr;
            HashSet<string> restr;

            if (blockSubTypeRestrictions.TryGetValue(block.BlockDefinition.TypeIdString.ToUpper(), out bsubItypeRestr))
            {
                bsubItypeRestr.TryGetValue(block.BlockDefinition.SubtypeName.ToUpper(), out itypeRestr);
                if ((itypeRestr ?? bsubItypeRestr["*"]).TryGetValue(itype, out restr))
                    return !(restr == null || restr.Contains(isub));
            }
            return true;
        }

        HashSet<string> GetBlockAcceptedSubs(IMyCubeBlock block, string itype, HashSet<string> mysubs = null)
        {
            Dictionary<string, Dictionary<string, HashSet<string>>> bsubItypeRestr;
            Dictionary<string, HashSet<string>> itypeRestr;
            HashSet<string> restr;

            mysubs = mysubs ?? new HashSet<string>(typeSubs[itype]);
            if (blockSubTypeRestrictions.TryGetValue(block.BlockDefinition.TypeIdString.ToUpper(), out bsubItypeRestr))
            {
                bsubItypeRestr.TryGetValue(block.BlockDefinition.SubtypeName.ToUpper(), out itypeRestr);
                if ((itypeRestr ?? bsubItypeRestr["*"]).TryGetValue(itype, out restr))
                    mysubs.ExceptWith(restr ?? mysubs);
            }
            return mysubs;
        }

        string GetBlockImpliedType(IMyCubeBlock block, string isub)
        {
            string rtype;
            rtype = null;
            foreach (string itype in subTypes[isub])
            {
                if (BlockAcceptsTypeSub(block, itype, isub))
                {
                    if (rtype != null)
                        return null;
                    rtype = itype;
                }
            }
            return rtype;
        }

        string GetShorthand(long amount)
        {
            long scale;
            if (amount <= 0L)
                return "0";
            if (amount < 10000L)
                return "< 0.01";
            if (amount >= 100000000000000L)
                return "" + amount / 1000000000000L + " M";
            scale = (long)Math.Pow(10.0, Math.Floor(Math.Log10(amount)) - 2.0);
            amount = (long)((double)amount / scale + 0.5) * scale;
            if (amount < 1000000000L)
                return (amount / 1e6).ToString("0.##");
            if (amount < 1000000000000L)
                return (amount / 1e9).ToString("0.##") + " K";
            return (amount / 1e12).ToString("0.##") + " M";
        }

        #endregion

        #region Scanning

        #region Inventory Scanning

        void ScanGroups()
        {
            List<IMyBlockGroup> groups = new List<IMyBlockGroup>();
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            System.Text.RegularExpressions.Match match;

            GridTerminalSystem.GetBlockGroups(groups);
            foreach (IMyBlockGroup group in groups)
            {
                if ((match = tagRegex.Match(group.Name)).Success)
                {
                    group.GetBlocks(blocks);
                    foreach (IMyTerminalBlock block in blocks)
                        blockGtag[block] = match;
                }
            }
        }

        void ScanBlocks<T>() where T : class
        {
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            System.Text.RegularExpressions.Match match;
            int i, s, n;
            IMyInventory inven;
            List<MyInventoryItem> stacks = new List<MyInventoryItem>();
            string itype, isub;
            InventoryItemData data;
            long amount, total;

            GridTerminalSystem.GetBlocksOfType<T>(blocks);
            foreach (IMyTerminalBlock block in blocks)
            {
                if (!DockedTo(block))
                    continue;
                match = tagRegex.Match(block.CustomName);
                if (match.Success)
                {
                    blockGtag.Remove(block);
                    blockTag[block] = match;
                }
                else if (blockGtag.TryGetValue(block, out match))
                {
                    blockTag[block] = match;
                }

                if ((block is IMySmallMissileLauncher & !(block is IMySmallMissileLauncherReload | block.BlockDefinition.SubtypeName == "LargeMissileLauncher")) | block is IMyLargeInteriorTurret)
                {
                    // can't sort with no conveyor port
                    invenLocked.Add(block.GetInventory(0));
                }
                else if (block is IMyFunctionalBlock && (block as IMyFunctionalBlock).Enabled & block.IsFunctional)
                {
                    if ((block is IMyRefinery | block is IMyReactor | block is IMyGasGenerator) & !blockTag.ContainsKey(block))
                    {
                        // don't touch input of enabled and untagged refineries, reactors or oxygen generators
                        invenLocked.Add(block.GetInventory(0));
                    }
                    else if (block is IMyAssembler && !(block as IMyAssembler).IsQueueEmpty)
                    {
                        // don't touch input of enabled and active assemblers
                        invenLocked.Add(block.GetInventory((block as IMyAssembler).Mode == MyAssemblerMode.Disassembly ? 1 : 0));
                    }
                }

                i = block.InventoryCount;
                while (i-- > 0)
                {
                    inven = block.GetInventory(i);
                    stacks.Clear();
                    inven.GetItems(stacks);
                    s = stacks.Count;
                    while (s-- > 0)
                    {
                        // identify the stacked item
                        itype = "" + stacks[s].Type.TypeId;
                        itype = itype.Substring(itype.LastIndexOf('_') + 1);
                        isub = stacks[s].Type.SubtypeId;

                        // new type or subtype?
                        InventoryItemData.InitItem(itype, isub, 0L, 0.0f, stacks[s].Type.SubtypeId, null);
                        itype = itype.ToUpper();
                        isub = isub.ToUpper();

                        // update amounts
                        amount = (long)((double)stacks[s].Amount * 1e6);
                        typeAmount[itype] += amount;
                        data = typeSubData[itype][isub];
                        data.amount += amount;
                        data.avail += amount;
                        data.invenTotal.TryGetValue(inven, out total);
                        data.invenTotal[inven] = total + amount;
                        data.invenSlot.TryGetValue(inven, out n);
                        data.invenSlot[inven] = Math.Max(n, s + 1);
                    }
                }
            }
        }

        #endregion

        #endregion

        #region Quota Processing

        void AdjustAmounts()
        {
            string itype, isub;
            long amount;
            InventoryItemData data;
            List<MyInventoryItem> stacks = new List<MyInventoryItem>();

            foreach (IMyInventory inven in invenHidden)
            {
                stacks.Clear();
                inven.GetItems(stacks);
                foreach (MyInventoryItem stack in stacks)
                {
                    itype = "" + stack.Type.TypeId;
                    itype = itype.Substring(itype.LastIndexOf('_') + 1).ToUpper();
                    isub = stack.Type.SubtypeId.ToUpper();

                    amount = (long)((double)stack.Amount * 1e6);
                    typeAmount[itype] -= amount;
                    typeSubData[itype][isub].amount -= amount;
                }
            }

            foreach (IMyInventory inven in invenLocked)
            {
                stacks.Clear();
                inven.GetItems(stacks);
                foreach (MyInventoryItem stack in stacks)
                {
                    itype = "" + stack.Type.TypeId;
                    itype = itype.Substring(itype.LastIndexOf('_') + 1).ToUpper();
                    isub = stack.Type.SubtypeId.ToUpper();

                    amount = (long)((double)stack.Amount * 1e6);
                    data = typeSubData[itype][isub];
                    data.avail -= amount;
                    data.locked += amount;
                }
            }
        }

        void ProcessQuotaPanels(bool quotaStable)
        {
            bool debug = ShouldDebug("quotas");
            int l, x, y, wide, size, spanx, spany, height, p, priority;
            long amount, round, total;
            float ratio;
            bool force;
            string itypeCur, itype, isub;
            string[] words, empty = new string[1] { " " };
            string[][] spanLines;
            IMyTextPanel panel2;
            IMySlimBlock slim;
            Matrix matrix;
            StringBuilder sb = new StringBuilder();
            List<string> qtypes = new List<string>(), errors = new List<string>(), scalesubs = new List<string>();
            Dictionary<string, SortedDictionary<string, string[]>> qtypeSubCols = new Dictionary<string, SortedDictionary<string, string[]>>();
            InventoryItemData data;
            ScreenFormatter sf;

            // reset ore "quotas"
            foreach (InventoryItemData d in typeSubData["ORE"].Values)
                d.minimum = d.amount == 0L ? 0L : Math.Max(d.minimum, d.amount);

            foreach (IMyTextPanel panel in qpanelPriority.Keys)
            {
                wide = panel.BlockDefinition.SubtypeName.EndsWith("Wide") ? 2 : 1;
                size = panel.BlockDefinition.SubtypeName.StartsWith("Small") ? 3 : 1;
                spanx = spany = 1;
                if (panelSpan.ContainsKey(panel))
                {
                    spanx = panelSpan[panel].A;
                    spany = panelSpan[panel].B;
                }

                // (re?)assemble (spanned?) user quota text
                spanLines = new string[spanx][];
                panel.Orientation.GetMatrix(out matrix);
                sb.Clear();
                for (y = 0; y < spany; y++)
                {
                    height = 0;
                    for (x = 0; x < spanx; x++)
                    {
                        spanLines[x] = empty;
                        slim = panel.CubeGrid.GetCubeBlock(new Vector3I(panel.Position + x * wide * size * matrix.Right + y * size * matrix.Down));
                        panel2 = slim != null ? slim.FatBlock as IMyTextPanel : null;
                        if (panel2 != null && "" + panel2.BlockDefinition == "" + panel.BlockDefinition)
                        {
                            spanLines[x] = panel2.GetText().Split('\n');
                            height = Math.Max(height, spanLines[x].Length);
                        }
                    }
                    for (l = 0; l < height; l++)
                    {
                        for (x = 0; x < spanx; x++)
                            sb.Append(l < spanLines[x].Length ? spanLines[x][l] : " ");
                        sb.Append("\n");
                    }
                }

                // parse user quotas
                priority = qpanelPriority[panel];
                itypeCur = "";
                qtypes.Clear();
                qtypeSubCols.Clear();
                errors.Clear();
                foreach (string line in sb.ToString().Split('\n'))
                {
                    words = line.ToUpper().Split(SPACE, 4, REE);
                    if (words.Length >= 1)
                    {
                        if (ParseItemValueText(null, words, itypeCur, out itype, out isub, out p, out amount, out ratio, out force) & itype == itypeCur & itype != "" & isub != "")
                        {
                            data = typeSubData[itype][isub];
                            qtypeSubCols[itype][isub] = new[] { data.label, "" + Math.Round(amount / 1e6, 2), "" + Math.Round(ratio * 100.0f, 2) + "%" };
                            if ((priority > 0 & (priority < data.qpriority | data.qpriority <= 0)) | (priority == 0 & data.qpriority < 0))
                            {
                                data.qpriority = priority;
                                data.minimum = amount;
                                data.ratio = ratio;
                            }
                            else if (priority == data.qpriority)
                            {
                                data.minimum = Math.Max(data.minimum, amount);
                                data.ratio = Math.Max(data.ratio, ratio);
                            }
                        }
                        else if (ParseItemValueText(null, words, "", out itype, out isub, out p, out amount, out ratio, out force) & itype != itypeCur & itype != "" & isub == "")
                        {
                            if (!qtypeSubCols.ContainsKey(itypeCur = itype))
                            {
                                qtypes.Add(itypeCur);
                                qtypeSubCols[itypeCur] = new SortedDictionary<string, string[]>();
                            }
                        }
                        else if (itypeCur != "")
                        {
                            qtypeSubCols[itypeCur][words[0]] = words;
                        }
                        else
                        {
                            errors.Add(line);
                        }
                    }
                }

                // redraw quotas
                sf = new ScreenFormatter(4, 2);
                sf.SetAlign(1, 1);
                sf.SetAlign(2, 1);
                if (qtypes.Count == 0 & qpanelTypes[panel].Count == 0)
                    qpanelTypes[panel].AddRange(types);
                foreach (string qtype in qpanelTypes[panel])
                {
                    if (!qtypeSubCols.ContainsKey(qtype))
                    {
                        qtypes.Add(qtype);
                        qtypeSubCols[qtype] = new SortedDictionary<string, string[]>();
                    }
                }
                foreach (string qtype in qtypes)
                {
                    if (qtype == "ORE")
                        continue;
                    if (sf.GetNumRows() > 0)
                        sf.AddBlankRow();
                    sf.Add(0, typeLabel[qtype], true);
                    sf.Add(1, "  Min", true);
                    sf.Add(2, "  Pct", true);
                    sf.Add(3, "", true);
                    sf.AddBlankRow();
                    foreach (InventoryItemData d in typeSubData[qtype].Values)
                    {
                        if (!qtypeSubCols[qtype].ContainsKey(d.subType))
                            qtypeSubCols[qtype][d.subType] = new[] { d.label, "" + Math.Round(d.minimum / 1e6, 2), "" + Math.Round(d.ratio * 100.0f, 2) + "%" };
                    }
                    foreach (string qsub in qtypeSubCols[qtype].Keys)
                    {
                        words = qtypeSubCols[qtype][qsub];
                        sf.Add(0, typeSubData[qtype].ContainsKey(qsub) ? words[0] : words[0].ToLower(), true);
                        sf.Add(1, words.Length > 1 ? words[1] : "", true);
                        sf.Add(2, words.Length > 2 ? words[2] : "", true);
                        sf.Add(3, words.Length > 3 ? words[3] : "", true);
                    }
                }
                WriteTableToPanel("TIM Quotas", sf, panel, true, errors.Count == 0 ? "" : String.Join("\n", errors).Trim().ToLower() + "\n\n");
            }

            // update effective quotas
            foreach (string qtype in types)
            {
                round = 1L;
                if (!FRACTIONAL_TYPES.Contains(qtype))
                    round = 1000000L;
                total = typeAmount[qtype];
                if (quotaStable & total > 0L)
                {
                    scalesubs.Clear();
                    foreach (InventoryItemData d in typeSubData[qtype].Values)
                    {
                        if (d.ratio > 0.0f & total >= (long)(d.minimum / d.ratio))
                            scalesubs.Add(d.subType);
                    }
                    if (scalesubs.Count > 0)
                    {
                        scalesubs.Sort((s1, s2) =>
                        {
                            InventoryItemData d1 = typeSubData[qtype][s1], d2 = typeSubData[qtype][s2];
                            long q1 = (long)(d1.amount / d1.ratio), q2 = (long)(d2.amount / d2.ratio);
                            return q1 == q2 ? d1.ratio.CompareTo(d2.ratio) : q1.CompareTo(q2);
                        });
                        isub = scalesubs[(scalesubs.Count - 1) / 2];
                        data = typeSubData[qtype][isub];
                        total = (long)(data.amount / data.ratio + 0.5f);
                        if (debug)
                        {
                            Debug("median " + typeLabel[qtype] + " is " + subLabel[isub] + ", " + total / 1e6 + " -> " + data.amount / 1e6 / data.ratio);
                            foreach (string qsub in scalesubs)
                            {
                                data = typeSubData[qtype][qsub];
                                Debug("  " + subLabel[qsub] + " @ " + data.amount / 1e6 + " / " + data.ratio + " => " + (long)(data.amount / 1e6 / data.ratio + 0.5f));
                            }
                        }
                    }
                }
                foreach (InventoryItemData d in typeSubData[qtype].Values)
                {
                    amount = Math.Max(d.quota, Math.Max(d.minimum, (long)(d.ratio * total + 0.5f)));
                    d.quota = amount / round * round;
                }
            }
        }

        #endregion

        #region Directive Parsing

        void ParseBlockTags()
        {
            StringBuilder name = new StringBuilder();
            IMyTextPanel blkPnl;
            IMyRefinery blkRfn;
            IMyAssembler blkAsm;
            System.Text.RegularExpressions.Match match;
            int i, priority, spanwide, spantall;
            string[] attrs, fields;
            string attr, itype, isub;
            long amount;
            float ratio;
            bool grouped, force, egg = false;

            // loop over all tagged blocks
            foreach (IMyTerminalBlock block in blockTag.Keys)
            {
                match = blockTag[block];
                attrs = match.Groups[1].Captures[0].Value.Split(SPACECOMMA, REE);
                name.Clear();
                if (!(grouped = blockGtag.ContainsKey(block)))
                {
                    name.Append(block.CustomName, 0, match.Index);
                    name.Append(argTagOpen);
                    if (argTagPrefix != "")
                        name.Append(argTagPrefix + " ");
                }

                // loop over all tag attributes
                if ((blkPnl = block as IMyTextPanel) != null)
                {
                    foreach (string a in attrs)
                    {
                        attr = a.ToUpper();
                        fields = attr.Split(COLON);
                        attr = fields[0];

                        if (attr.Length >= 4 & "STATUS".StartsWith(attr))
                        {
                            if (blkPnl.Enabled) statusPanels.Add(blkPnl);
                            name.Append("STATUS ");
                        }
                        else if (attr.Length >= 5 & "DEBUGGING".StartsWith(attr))
                        {
                            if (blkPnl.Enabled) debugPanels.Add(blkPnl);
                            name.Append("DEBUG ");
                        }
                        else if (attr == "SPAN")
                        {
                            if (fields.Length >= 3 && int.TryParse(fields[1], out spanwide) & int.TryParse(fields[2], out spantall) & spanwide >= 1 & spantall >= 1)
                            {
                                panelSpan[blkPnl] = new Pair(spanwide, spantall);
                                name.Append("SPAN:" + spanwide + ":" + spantall + " ");
                            }
                            else
                            {
                                name.Append((attr = String.Join(":", fields).ToLower()) + " ");
                                Debug("Invalid panel span rule: " + attr);
                            }
                        }
                        else if (attr.Length >= 3 & "QUOTAS".StartsWith(attr))
                        {
                            if (blkPnl.Enabled & !qpanelPriority.ContainsKey(blkPnl)) qpanelPriority[blkPnl] = 0;
                            if (blkPnl.Enabled & !qpanelTypes.ContainsKey(blkPnl)) qpanelTypes[blkPnl] = new List<string>();
                            name.Append("QUOTA");
                            i = 0;
                            while (++i < fields.Length)
                            {
                                if (ParseItemTypeSub(null, true, fields[i], "", out itype, out isub) & itype != "ORE" & isub == "")
                                {
                                    if (blkPnl.Enabled) qpanelTypes[blkPnl].Add(itype);
                                    name.Append(":" + typeLabel[itype]);
                                }
                                else if (fields[i].StartsWith("P") & int.TryParse(fields[i].Substring(Math.Min(1, fields[i].Length)), out priority))
                                {
                                    if (blkPnl.Enabled) qpanelPriority[blkPnl] = Math.Max(0, priority);
                                    if (priority > 0) name.Append(":P" + priority);
                                }
                                else
                                {
                                    name.Append(":" + fields[i].ToLower());
                                    Debug("Invalid quota panel rule: " + fields[i].ToLower());
                                }
                            }
                            name.Append(" ");
                        }
                        else if (attr.Length >= 3 & "INVENTORY".StartsWith(attr))
                        {
                            if (blkPnl.Enabled & !ipanelTypes.ContainsKey(blkPnl)) ipanelTypes[blkPnl] = new List<string>();
                            name.Append("INVEN");
                            i = 0;
                            while (++i < fields.Length)
                            {
                                if (ParseItemTypeSub(null, true, fields[i], "", out itype, out isub) & isub == "")
                                {
                                    if (blkPnl.Enabled) ipanelTypes[blkPnl].Add(itype);
                                    name.Append(":" + typeLabel[itype]);
                                }
                                else
                                {
                                    name.Append(":" + fields[i].ToLower());
                                    Debug("Invalid inventory panel rule: " + fields[i].ToLower());
                                }
                            }
                            name.Append(" ");
                        }
                        else
                        {
                            name.Append((attr = String.Join(":", fields).ToLower()) + " ");
                            Debug("Invalid panel attribute: " + attr);
                        }
                    }
                }
                else
                {
                    blkRfn = block as IMyRefinery;
                    blkAsm = block as IMyAssembler;
                    foreach (string a in attrs)
                    {
                        attr = a.ToUpper();
                        fields = attr.Split(COLON);
                        attr = fields[0];

                        if ((attr.Length >= 4 & "LOCKED".StartsWith(attr)) | attr == "EXEMPT")
                        { // EXEMPT for AIS compat
                            i = block.InventoryCount;
                            while (i-- > 0)
                                invenLocked.Add(block.GetInventory(i));
                            name.Append(attr + " ");
                        }
                        else if (attr == "HIDDEN")
                        {
                            i = block.InventoryCount;
                            while (i-- > 0)
                                invenHidden.Add(block.GetInventory(i));
                            name.Append("HIDDEN ");
                        }
                        else if (block is IMyShipConnector & attr == "DOCK")
                        {
                            // handled in ScanGrids(), just rewrite
                            name.Append(String.Join(":", fields) + " ");
                        }
                        else if ((blkRfn != null | blkAsm != null) & attr == "AUTO")
                        {
                            name.Append("AUTO");
                            HashSet<string> ores, autoores = blkRfn == null | fields.Length > 1 ? new HashSet<string>() : GetBlockAcceptedSubs(blkRfn, "ORE");
                            HashSet<ItemId> items, autoitems = new HashSet<ItemId>();
                            i = 0;
                            while (++i < fields.Length)
                            {
                                if (ParseItemTypeSub(null, true, fields[i], blkRfn != null ? "ORE" : "", out itype, out isub) & blkRfn != null == (itype == "ORE") & (blkRfn != null | itype != "INGOT"))
                                {
                                    if (isub == "")
                                    {
                                        if (blkRfn != null)
                                        {
                                            autoores.UnionWith(typeSubs[itype]);
                                        }
                                        else
                                        {
                                            foreach (string s in typeSubs[itype])
                                                autoitems.Add(new ItemId(itype, s));
                                        }
                                        name.Append(":" + typeLabel[itype]);
                                    }
                                    else
                                    {
                                        if (blkRfn != null)
                                        {
                                            autoores.Add(isub);
                                        }
                                        else
                                        {
                                            autoitems.Add(new ItemId(itype, isub));
                                        }
                                        name.Append(":" + (blkRfn == null & subTypes[isub].Count > 1 ? typeLabel[itype] + "/" : "") + subLabel[isub]);
                                    }
                                }
                                else
                                {
                                    name.Append(":" + fields[i].ToLower());
                                    Debug("Unrecognized or ambiguous item: " + fields[i].ToLower());
                                }
                            }
                            if (blkRfn != null)
                            {
                                if (blkRfn.Enabled)
                                    refineryOres.GetOrAdd(blkRfn, Make.HashSet<string>).UnionWith(autoores);
                            }
                            else if (blkAsm.Enabled)
                                assemblerItems.GetOrAdd(blkAsm, Make.HashSet<ItemId>).UnionWith(autoitems);
                            name.Append(" ");
                        }
                        else if (!ParseItemValueText(block, fields, "", out itype, out isub, out priority, out amount, out ratio, out force))
                        {
                            name.Append((attr = String.Join(":", fields).ToLower()) + " ");
                            Debug("Unrecognized or ambiguous item: " + attr);
                        }
                        else if (!block.HasInventory | (block is IMySmallMissileLauncher & !(block is IMySmallMissileLauncherReload | block.BlockDefinition.SubtypeName == "LargeMissileLauncher")) | block is IMyLargeInteriorTurret)
                        {
                            name.Append(String.Join(":", fields).ToLower() + " ");
                            Debug("Cannot sort items to " + block.CustomName + ": no conveyor-connected inventory");
                        }
                        else
                        {
                            if (isub == "")
                            {
                                foreach (string s in force ? (IEnumerable<string>)typeSubs[itype] : (IEnumerable<string>)GetBlockAcceptedSubs(block, itype))
                                    AddInvenRequest(block, 0, itype, s, priority, amount);
                            }
                            else
                            {
                                AddInvenRequest(block, 0, itype, isub, priority, amount);
                            }
                            if (argRewriteTags & !grouped)
                            {
                                if (force)
                                {
                                    name.Append("FORCE:" + typeLabel[itype]);
                                    if (isub != "")
                                        name.Append("/" + subLabel[isub]);
                                }
                                else if (isub == "")
                                {
                                    name.Append(typeLabel[itype]);
                                }
                                else if (subTypes[isub].Count == 1 || GetBlockImpliedType(block, isub) == itype)
                                {
                                    name.Append(subLabel[isub]);
                                }
                                else
                                {
                                    name.Append(typeLabel[itype] + "/" + subLabel[isub]);
                                }
                                if (priority > 0 & priority < int.MaxValue)
                                    name.Append(":P" + priority);
                                if (amount >= 0L)
                                    name.Append(":" + amount / 1e6);
                                name.Append(" ");
                            }
                        }
                    }
                }

                if (argRewriteTags & !grouped)
                {
                    if (name[name.Length - 1] == ' ')
                        name.Length--;
                    name.Append(argTagClose).Append(block.CustomName, match.Index + match.Length, block.CustomName.Length - match.Index - match.Length);
                    block.CustomName = name.ToString();
                }

                if (block.GetUserRelationToOwner(Me.OwnerId) != MyRelationsBetweenPlayerAndBlock.Owner & block.GetUserRelationToOwner(Me.OwnerId) != MyRelationsBetweenPlayerAndBlock.FactionShare)
                    Debug("Cannot control \"" + block.CustomName + "\" due to differing ownership");
            }
        }

        bool ParseItemTypeSub(IMyCubeBlock block, bool force, string typesub, string qtype, out string itype, out string isub)
        {
            int t, s, found;
            string[] parts;

            itype = "";
            isub = "";
            found = 0;
            parts = typesub.Trim().Split('/');
            if (parts.Length >= 2)
            {
                parts[0] = parts[0].Trim();
                parts[1] = parts[1].Trim();
                if (typeSubs.ContainsKey(parts[0]) && parts[1] == "" | typeSubData[parts[0]].ContainsKey(parts[1]))
                {
                    // exact type/subtype
                    if (force || BlockAcceptsTypeSub(block, parts[0], parts[1]))
                    {
                        found = 1;
                        itype = parts[0];
                        isub = parts[1];
                    }
                }
                else
                {
                    // type/subtype?
                    t = types.BinarySearch(parts[0]);
                    t = Math.Max(t, ~t);
                    while (found < 2 & t < types.Count && types[t].StartsWith(parts[0]))
                    {
                        s = typeSubs[types[t]].BinarySearch(parts[1]);
                        s = Math.Max(s, ~s);
                        while (found < 2 & s < typeSubs[types[t]].Count && typeSubs[types[t]][s].StartsWith(parts[1]))
                        {
                            if (force || BlockAcceptsTypeSub(block, types[t], typeSubs[types[t]][s]))
                            {
                                found++;
                                itype = types[t];
                                isub = typeSubs[types[t]][s];
                            }
                            s++;
                        }
                        // special case for gravel
                        if (found == 0 & types[t] == "INGOT" & "GRAVEL".StartsWith(parts[1]) & (force || BlockAcceptsTypeSub(block, "INGOT", "STONE")))
                        {
                            found++;
                            itype = "INGOT";
                            isub = "STONE";
                        }
                        t++;
                    }
                }
            }
            else if (typeSubs.ContainsKey(parts[0]))
            {
                // exact type
                if (force || BlockAcceptsTypeSub(block, parts[0], ""))
                {
                    found++;
                    itype = parts[0];
                    isub = "";
                }
            }
            else if (subTypes.ContainsKey(parts[0]))
            {
                // exact subtype
                if (qtype != "" && typeSubData[qtype].ContainsKey(parts[0]))
                {
                    found++;
                    itype = qtype;
                    isub = parts[0];
                }
                else
                {
                    t = subTypes[parts[0]].Count;
                    while (found < 2 & t-- > 0)
                    {
                        if (force || BlockAcceptsTypeSub(block, subTypes[parts[0]][t], parts[0]))
                        {
                            found++;
                            itype = subTypes[parts[0]][t];
                            isub = parts[0];
                        }
                    }
                }
            }
            else if (qtype != "")
            {
                // subtype of a known type
                s = typeSubs[qtype].BinarySearch(parts[0]);
                s = Math.Max(s, ~s);
                while (found < 2 & s < typeSubs[qtype].Count && typeSubs[qtype][s].StartsWith(parts[0]))
                {
                    found++;
                    itype = qtype;
                    isub = typeSubs[qtype][s];
                    s++;
                }
                // special case for gravel
                if (found == 0 & qtype == "INGOT" & "GRAVEL".StartsWith(parts[0]))
                {
                    found++;
                    itype = "INGOT";
                    isub = "STONE";
                }
            }
            else
            {
                // type?
                t = types.BinarySearch(parts[0]);
                t = Math.Max(t, ~t);
                while (found < 2 & t < types.Count && types[t].StartsWith(parts[0]))
                {
                    if (force || BlockAcceptsTypeSub(block, types[t], ""))
                    {
                        found++;
                        itype = types[t];
                        isub = "";
                    }
                    t++;
                }
                // subtype?
                s = subs.BinarySearch(parts[0]);
                s = Math.Max(s, ~s);
                while (found < 2 & s < subs.Count && subs[s].StartsWith(parts[0]))
                {
                    t = subTypes[subs[s]].Count;
                    while (found < 2 & t-- > 0)
                    {
                        if (force || BlockAcceptsTypeSub(block, subTypes[subs[s]][t], subs[s]))
                        {
                            if (found != 1 || itype != subTypes[subs[s]][t] | isub != "" | typeSubs[itype].Count != 1)
                                found++;
                            itype = subTypes[subs[s]][t];
                            isub = subs[s];
                        }
                    }
                    s++;
                }
                // special case for gravel
                if (found == 0 & "GRAVEL".StartsWith(parts[0]) & (force || BlockAcceptsTypeSub(block, "INGOT", "STONE")))
                {
                    found++;
                    itype = "INGOT";
                    isub = "STONE";
                }
            }

            // fill in implied subtype
            if (!force & block != null & found == 1 & isub == "")
            {
                HashSet<string> mysubs = GetBlockAcceptedSubs(block, itype);
                if (mysubs.Count == 1)
                    isub = mysubs.First();
            }

            return found == 1;
        }

        bool ParseItemValueText(IMyCubeBlock block, string[] fields, string qtype, out string itype, out string isub, out int priority, out long amount, out float ratio, out bool force)
        {
            int f, l;
            double val, mul;

            itype = "";
            isub = "";
            priority = 0;
            amount = -1L;
            ratio = -1.0f;
            force = block == null;

            // identify the item
            f = 0;
            if (fields[0].Trim() == "FORCE")
            {
                if (fields.Length == 1)
                    return false;
                force = true;
                f = 1;
            }
            if (!ParseItemTypeSub(block, force, fields[f], qtype, out itype, out isub))
                return false;

            // parse the remaining fields
            while (++f < fields.Length)
            {
                fields[f] = fields[f].Trim();
                l = fields[f].Length;

                if (l != 0)
                {
                    if (fields[f] == "IGNORE")
                    {
                        amount = 0L;
                    }
                    else if (fields[f] == "OVERRIDE" | fields[f] == "SPLIT")
                    {
                        // these AIS tags are TIM's default behavior anyway
                    }
                    else if (fields[f][l - 1] == '%' & double.TryParse(fields[f].Substring(0, l - 1), out val))
                    {
                        ratio = Math.Max(0.0f, (float)(val / 100.0));
                    }
                    else if (fields[f][0] == 'P' & double.TryParse(fields[f].Substring(1), out val))
                    {
                        priority = Math.Max(1, (int)(val + 0.5));
                    }
                    else
                    {
                        // check for numeric suffixes
                        mul = 1.0;
                        if (fields[f][l - 1] == 'K')
                        {
                            l--;
                            mul = 1e3;
                        }
                        else if (fields[f][l - 1] == 'M')
                        {
                            l--;
                            mul = 1e6;
                        }

                        // try parsing the field as an amount value
                        if (double.TryParse(fields[f].Substring(0, l), out val))
                            amount = Math.Max(0L, (long)(val * mul * 1e6 + 0.5));
                    }
                }
            }

            return true;
        }

        #endregion

        #region Item Transfer Functions

        void AddInvenRequest(IMyTerminalBlock block, int inv, string itype, string isub, int priority, long amount)
        {
            long a;
            Dictionary<string, Dictionary<string, Dictionary<IMyInventory, long>>> tsir;
            Dictionary<string, Dictionary<IMyInventory, long>> sir;
            Dictionary<IMyInventory, long> ir;

            // no priority -> last priority
            if (priority == 0)
                priority = int.MaxValue;

            // new priority/type/sub?
            tsir = priTypeSubInvenRequest.GetOrAdd(priority, Make.Dictionary<string, Dictionary<string, Dictionary<IMyInventory, long>>>);
            sir = tsir.GetOrAdd(itype, Make.Dictionary<string, Dictionary<IMyInventory, long>>);
            ir = sir.GetOrAdd(isub, Make.Dictionary<IMyInventory, long>);

            // update request
            IMyInventory inven = block.GetInventory(inv);
            ir.TryGetValue(inven, out a);
            ir[inven] = amount;
            typeSubData[itype][isub].quota += Math.Min(0L, -a) + Math.Max(0L, amount);

            // disable conveyor for some block types
            // (IMyInventoryOwner is supposedly obsolete but there's no other way to do this for all of these block types at once)
            if (inven.Owner != null)
            {
                if (block is IMyRefinery && (block as IMyProductionBlock).UseConveyorSystem)
                {
                    block.GetActionWithName("UseConveyor").Apply(block);
                    Debug("Disabling conveyor system for " + block.CustomName);
                }

                if (block is IMyGasGenerator && (block as IMyGasGenerator).UseConveyorSystem)
                {
                    block.GetActionWithName("UseConveyor").Apply(block);
                    Debug("Disabling conveyor system for " + block.CustomName);
                }

                if (block is IMyReactor && (block as IMyReactor).UseConveyorSystem)
                {
                    block.GetActionWithName("UseConveyor").Apply(block);
                    Debug("Disabling conveyor system for " + block.CustomName);
                }

                if (block is IMyLargeConveyorTurretBase && ((IMyLargeConveyorTurretBase)block).UseConveyorSystem)
                {
                    block.GetActionWithName("UseConveyor").Apply(block);
                    Debug("Disabling conveyor system for " + block.CustomName);
                }

                if (block is IMySmallGatlingGun && ((IMySmallGatlingGun)block).UseConveyorSystem)
                {
                    block.GetActionWithName("UseConveyor").Apply(block);
                    Debug("Disabling conveyor system for " + block.CustomName);
                }

                if (block is IMySmallMissileLauncher && ((IMySmallMissileLauncher)block).UseConveyorSystem)
                {
                    block.GetActionWithName("UseConveyor").Apply(block);
                    Debug("Disabling conveyor system for " + block.CustomName);
                }
            }
        }

        // ================ local persisted vars ================
        List<int> AllocateItems_priorities;
        int AllocateItems_prioritiesIndex;
        List<string> AllocateItems_inventoryRequestTypes;
        int AllocateItems_inventoryRequestTypesIndex;
        List<string> AllocateItems_inventoryRequestSubTypes;
        int AllocateItems_inventoryRequestSubTypesIndex;
        /// <summary>
        /// Allocates all inventory items.
        /// </summary>
        /// <param name="limited">Whether to allocate limited or unlimited items.</param>
        void AllocateItems(bool limited)
        {
            // establish priority order, adding 0 for refinery management
            if (AllocateItems_priorities == null) // if not null, then ignore and continue what we were doing
            {
                AllocateItems_priorities = new List<int>(priTypeSubInvenRequest.Keys);
                AllocateItems_priorities.Sort();
                AllocateItems_prioritiesIndex = 0;
            }
            // indexes and lists are stored in a way that enables persisting between calls
            // this enables us to continue where we left of if we need to
            for (; AllocateItems_prioritiesIndex < AllocateItems_priorities.Count; AllocateItems_prioritiesIndex++)
            {
                if (AllocateItems_inventoryRequestTypes == null) // if not null, then ignore and continue what we were doing
                {
                    AllocateItems_inventoryRequestTypes = new List<string>(priTypeSubInvenRequest
                        [AllocateItems_priorities[AllocateItems_prioritiesIndex]].Keys);
                    AllocateItems_inventoryRequestTypesIndex = 0;
                }
                for (; AllocateItems_inventoryRequestTypesIndex < AllocateItems_inventoryRequestTypes.Count; AllocateItems_inventoryRequestTypesIndex++)
                {
                    if (AllocateItems_inventoryRequestSubTypes == null) // if not null, then ignore and continue what we were doing
                    {
                        AllocateItems_inventoryRequestSubTypes = new List<string>(priTypeSubInvenRequest
                            [AllocateItems_priorities[AllocateItems_prioritiesIndex]]
                                [AllocateItems_inventoryRequestTypes[AllocateItems_inventoryRequestTypesIndex]].Keys);
                        AllocateItems_inventoryRequestSubTypesIndex = 0;
                    }
                    bool doneAtLeast1Allocation = false;
                    for (; AllocateItems_inventoryRequestSubTypesIndex < AllocateItems_inventoryRequestSubTypes.Count; AllocateItems_inventoryRequestSubTypesIndex++)
                    {
                        // we check the exectution limit to ensure that we haven't gone over.
                        // if we do, then the variables for the loops should persist
                        // and since they are not set to null, we should restart the loops exactly where we stopped.
                        // this is done now to allow the index vars to increment on each iteration first (in order to not do the same
                        // thing twice).
                        if (doneAtLeast1Allocation) // only check if at least 1 allocation was completed (stops infinite loops occuring)
                            DoExecutionLimitCheck();
                        AllocateItemBatch(limited, // limited var
                            AllocateItems_priorities[AllocateItems_prioritiesIndex], // current priority
                            AllocateItems_inventoryRequestTypes[AllocateItems_inventoryRequestTypesIndex], // current type
                            AllocateItems_inventoryRequestSubTypes[AllocateItems_inventoryRequestSubTypesIndex]); // current subtype
                        doneAtLeast1Allocation = true;
                    }
                    // clear list so we know that it was completed
                    AllocateItems_inventoryRequestSubTypes = null;
                }
                // clear list so we know that it was completed
                AllocateItems_inventoryRequestTypes = null;
            }
            // clear list so we know that it was completed
            AllocateItems_priorities = null;

            // if we just finished the unlimited requests, check for leftovers
            if (!limited)
            {
                foreach (string itype in types)
                {
                    foreach (InventoryItemData data in typeSubData[itype].Values)
                    {
                        if (data.avail > 0L)
                            Debug("No place to put " + GetShorthand(data.avail) + " " + typeLabel[itype] + "/" + subLabel[data.subType] + ", containers may be full");
                    }
                }
            }
        }

        void AllocateItemBatch(bool limited, int priority, string itype, string isub)
        {
            bool debug = ShouldDebug("sorting");
            int locked, dropped;
            long totalrequest, totalavail, request, avail, amount, moved, round;
            List<IMyInventory> invens = null;
            Dictionary<IMyInventory, long> invenRequest;

            if (debug) Debug("sorting " + typeLabel[itype] + "/" + subLabel[isub] + " lim=" + limited + " p=" + priority);

            round = 1L;
            if (!FRACTIONAL_TYPES.Contains(itype))
                round = 1000000L;
            invenRequest = new Dictionary<IMyInventory, long>();
            InventoryItemData data = typeSubData[itype][isub];

            // sum up the requests
            totalrequest = 0L;
            foreach (IMyInventory reqInven in priTypeSubInvenRequest[priority][itype][isub].Keys)
            {
                request = priTypeSubInvenRequest[priority][itype][isub][reqInven];
                if (request != 0L & limited == request >= 0L)
                {
                    if (request < 0L)
                    {
                        request = 1000000L;
                        if (reqInven.MaxVolume != VRage.MyFixedPoint.MaxValue)
                            request = (long)((double)reqInven.MaxVolume * 1e6);
                    }
                    invenRequest[reqInven] = request;
                    totalrequest += request;
                }
            }
            if (debug) Debug("total req=" + totalrequest / 1e6);
            if (totalrequest <= 0L)
                return;
            totalavail = data.avail + data.locked;
            if (debug) Debug("total avail=" + totalavail / 1e6);

            // disqualify any locked invens which already have their share
            if (totalavail > 0L)
            {
                invens = new List<IMyInventory>(data.invenTotal.Keys);
                do
                {
                    locked = 0;
                    dropped = 0;
                    foreach (IMyInventory amtInven in invens)
                    {
                        avail = data.invenTotal[amtInven];
                        if (avail > 0L & invenLocked.Contains(amtInven))
                        {
                            locked++;
                            invenRequest.TryGetValue(amtInven, out request);
                            amount = (long)((double)request / totalrequest * totalavail);
                            if (limited)
                                amount = Math.Min(amount, request);
                            amount = amount / round * round;

                            if (avail >= amount)
                            {
                                if (debug) Debug("locked " + (amtInven.Owner == null ? "???" : (amtInven.Owner as IMyTerminalBlock).CustomName) + " gets " + amount / 1e6 + ", has " + avail / 1e6);
                                dropped++;
                                totalrequest -= request;
                                invenRequest[amtInven] = 0L;
                                totalavail -= avail;
                                data.locked -= avail;
                                data.invenTotal[amtInven] = 0L;
                            }
                        }
                    }
                } while (locked > dropped & dropped > 0);
            }

            // allocate the remaining available items
            foreach (IMyInventory reqInven in invenRequest.Keys)
            {
                // calculate this inven's allotment
                request = invenRequest[reqInven];
                if (request <= 0L | totalrequest <= 0L | totalavail <= 0L)
                {
                    if (limited & request > 0L) Debug("Insufficient " + typeLabel[itype] + "/" + subLabel[isub] + " to satisfy " + (reqInven.Owner == null ? "???" : (reqInven.Owner as IMyTerminalBlock).CustomName));
                    continue;
                }
                amount = (long)((double)request / totalrequest * totalavail);
                if (limited)
                    amount = Math.Min(amount, request);
                amount = amount / round * round;
                if (debug) Debug((reqInven.Owner == null ? "???" : (reqInven.Owner as IMyTerminalBlock).CustomName) + " gets " + request / 1e6 + " / " + totalrequest / 1e6 + " of " + totalavail / 1e6 + " = " + amount / 1e6);
                totalrequest -= request;

                // check how much it already has
                if (data.invenTotal.TryGetValue(reqInven, out avail))
                {
                    avail = Math.Min(avail, amount);
                    amount -= avail;
                    totalavail -= avail;
                    if (invenLocked.Contains(reqInven))
                    {
                        data.locked -= avail;
                    }
                    else
                    {
                        data.avail -= avail;
                    }
                    data.invenTotal[reqInven] -= avail;
                }

                // get the rest from other unlocked invens
                foreach (IMyInventory amtInven in invens)
                {
                    avail = Math.Min(data.invenTotal[amtInven], amount);
                    moved = 0L;
                    if (avail > 0L & invenLocked.Contains(amtInven) == false)
                    {
                        moved = TransferItem(itype, isub, avail, amtInven, reqInven);
                        amount -= moved;
                        totalavail -= moved;
                        data.avail -= moved;
                        data.invenTotal[amtInven] -= moved;
                    }
                    // if we moved some but not all, we're probably full
                    if (amount <= 0L | (moved != 0L & moved != avail))
                        break;
                }

                if (limited & amount > 0L)
                {
                    Debug("Insufficient " + typeLabel[itype] + "/" + subLabel[isub] + " to satisfy " + (reqInven.Owner == null ? "???" : (reqInven.Owner as IMyTerminalBlock).CustomName));
                }
            }

            if (debug) Debug("" + totalavail / 1e6 + " left over");
        }


        long TransferItem(string itype, string isub, long amount, IMyInventory fromInven, IMyInventory toInven)
        {
            bool debug = ShouldDebug("sorting");
            List<MyInventoryItem> stacks = new List<MyInventoryItem>();
            int s;
            VRage.MyFixedPoint remaining, moved;
            uint id;
            //	double volume;
            string stype, ssub;

            remaining = (VRage.MyFixedPoint)(amount / 1e6);
            fromInven.GetItems(stacks);
            s = Math.Min(typeSubData[itype][isub].invenSlot[fromInven], stacks.Count);
            while (remaining > 0 & s-- > 0)
            {
                stype = "" + stacks[s].Type.TypeId;
                stype = stype.Substring(stype.LastIndexOf('_') + 1).ToUpper();
                ssub = stacks[s].Type.SubtypeId.ToUpper();
                if (stype == itype & ssub == isub)
                {
                    moved = stacks[s].Amount;
                    id = stacks[s].ItemId;
                    //			volume = (double)fromInven.CurrentVolume;
                    if (fromInven == toInven)
                    {
                        remaining -= moved;
                        if (remaining < 0)
                            remaining = 0;
                    }
                    else if (fromInven.TransferItemTo(toInven, s, null, true, remaining))
                    {
                        stacks.Clear();
                        fromInven.GetItems(stacks);
                        if (s < stacks.Count && stacks[s].ItemId == id)
                            moved -= stacks[s].Amount;
                        if (moved <= 0)
                        {
                            if ((double)toInven.CurrentVolume < (double)toInven.MaxVolume / 2 & toInven.Owner != null)
                            {
                                VRage.ObjectBuilders.SerializableDefinitionId bdef = (toInven.Owner as IMyCubeBlock).BlockDefinition;
                                AddBlockRestriction(bdef.TypeIdString, bdef.SubtypeName, itype, isub);
                            }
                            s = 0;
                        }
                        else
                        {
                            numberTransfers++;
                            if (debug) Debug(
                                "Transferred " + GetShorthand((long)((double)moved * 1e6)) + " " + typeLabel[itype] + "/" + subLabel[isub] +
                                " from " + (fromInven.Owner == null ? "???" : (fromInven.Owner as IMyTerminalBlock).CustomName) + " to " + (toInven.Owner == null ? "???" : (toInven.Owner as IMyTerminalBlock).CustomName)
                            );
                            //					volume -= (double)fromInven.CurrentVolume;
                            //					typeSubData[itype][isub].volume = (1000.0 * volume / (double)moved);
                        }
                        remaining -= moved;
                    }
                    else if (!fromInven.IsConnectedTo(toInven) & fromInven.Owner != null & toInven.Owner != null)
                    {
                        if (!blockErrors.ContainsKey(fromInven.Owner as IMyTerminalBlock))
                            blockErrors[fromInven.Owner as IMyTerminalBlock] = new HashSet<IMyTerminalBlock>();
                        blockErrors[fromInven.Owner as IMyTerminalBlock].Add(toInven.Owner as IMyTerminalBlock);
                        s = 0;
                    }
                }
            }

            return amount - (long)((double)remaining * 1e6 + 0.5);
        }

        #endregion

        #region Production Management

        void ScanProduction()
        {
            List<IMyTerminalBlock> blocks1 = new List<IMyTerminalBlock>(), blocks2 = new List<IMyTerminalBlock>();
            List<MyInventoryItem> stacks = new List<MyInventoryItem>();
            string itype, isub, isubIng;
            List<MyProductionItem> queue = new List<MyProductionItem>();
            ItemId item;

            producerWork.Clear();

            GridTerminalSystem.GetBlocksOfType<IMyGasGenerator>(blocks1, DockedTo);
            GridTerminalSystem.GetBlocksOfType<IMyRefinery>(blocks2, DockedTo);
            foreach (IMyFunctionalBlock blk in blocks1.Concat(blocks2))
            {
                stacks.Clear();
                blk.GetInventory(0).GetItems(stacks);
                if (stacks.Count > 0 & blk.Enabled)
                {
                    itype = "" + stacks[0].Type.TypeId;
                    itype = itype.Substring(itype.LastIndexOf('_') + 1).ToUpper();
                    isub = stacks[0].Type.SubtypeId.ToUpper();
                    if (typeSubs.ContainsKey(itype) & subTypes.ContainsKey(isub))
                        typeSubData[itype][isub].producers.Add(blk);
                    if (itype == "ORE" & (ORE_PRODUCT.TryGetValue(isub, out isubIng) ? isubIng : isubIng = isub) != "" & typeSubData["INGOT"].ContainsKey(isubIng))
                        typeSubData["INGOT"][isubIng].producers.Add(blk);
                    producerWork[blk] = new ProducerWork(new ItemId(itype, isub), (double)stacks[0].Amount);
                }
            }

            GridTerminalSystem.GetBlocksOfType<IMyAssembler>(blocks1, DockedTo);
            foreach (IMyAssembler blk in blocks1)
            {
                if (blk.Enabled & !blk.IsQueueEmpty & blk.Mode == MyAssemblerMode.Assembly)
                {
                    blk.GetQueue(queue);
                    if (blueprintItem.TryGetValue(queue[0].BlueprintId, out item))
                    {
                        if (typeSubs.ContainsKey(item.type) & subTypes.ContainsKey(item.subType))
                            typeSubData[item.type][item.subType].producers.Add(blk);
                        producerWork[blk] = new ProducerWork(item, (double)queue[0].Amount - blk.CurrentProgress);
                    }
                }
            }
        }

        void ManageRefineries()
        {
            if (!typeSubs.ContainsKey("ORE") | !typeSubs.ContainsKey("INGOT"))
                return;

            bool debug = ShouldDebug("refineries");
            string itype, itype2, isub, isub2, isubIngot;
            InventoryItemData data;
            int level, priority;
            List<string> ores = new List<string>();
            Dictionary<string, int> oreLevel = new Dictionary<string, int>();
            List<MyInventoryItem> stacks = new List<MyInventoryItem>();
            double speed, oldspeed;
            ProducerWork work;
            bool ready;
            List<IMyRefinery> refineries = new List<IMyRefinery>();

            if (debug) Debug("Refinery management:");

            // scan inventory levels
            foreach (string isubOre in typeSubs["ORE"])
            {
                if (!ORE_PRODUCT.TryGetValue(isubOre, out isubIngot))
                    isubIngot = isubOre;
                if (isubIngot != "" & typeSubData["ORE"][isubOre].avail > 0L & typeSubData["INGOT"].TryGetValue(isubIngot, out data))
                {
                    if (data.quota > 0L)
                    {
                        level = (int)(100L * data.amount / data.quota);
                        ores.Add(isubOre);
                        oreLevel[isubOre] = level;
                        if (debug) Debug("  " + subLabel[isubIngot] + " @ " + data.amount / 1e6 + "/" + data.quota / 1e6 + "," + (isubOre == isubIngot ? "" : " Ore/" + subLabel[isubOre]) + " L=" + level + "%");
                    }
                }
            }

            // identify refineries that are ready for a new assignment
            foreach (IMyRefinery rfn in refineryOres.Keys)
            {
                itype = isub = isub2 = "";
                stacks.Clear();
                rfn.GetInventory(0).GetItems(stacks);
                if (stacks.Count > 0)
                {
                    itype = "" + stacks[0].Type.TypeId;
                    itype = itype.Substring(itype.LastIndexOf('_') + 1).ToUpper();
                    isub = stacks[0].Type.SubtypeId.ToUpper();
                    if (itype == "ORE" & oreLevel.ContainsKey(isub))
                        oreLevel[isub] += Math.Max(1, oreLevel[isub] / refineryOres.Count);
                    if (stacks.Count > 1)
                    {
                        itype2 = "" + stacks[1].Type.TypeId;
                        itype2 = itype2.Substring(itype2.LastIndexOf('_') + 1).ToUpper();
                        isub2 = stacks[1].Type.SubtypeId.ToUpper();
                        if (itype2 == "ORE" & oreLevel.ContainsKey(isub2))
                            oreLevel[isub2] += Math.Max(1, oreLevel[isub2] / refineryOres.Count);
                        AddInvenRequest(rfn, 0, itype2, isub2, -2, (long)((double)stacks[1].Amount * 1e6 + 0.5));
                    }
                }
                if (producerWork.TryGetValue(rfn, out work))
                {
                    data = typeSubData[work.item.type][work.item.subType];
                    oldspeed = data.prdSpeed.TryGetValue("" + rfn.BlockDefinition, out oldspeed) ? oldspeed : 1.0;
                    speed = work.item.subType == isub ? Math.Max(work.quantity - (double)stacks[0].Amount, 0.0) : Math.Max(work.quantity, oldspeed);
                    speed = Math.Min(Math.Max((speed + oldspeed) / 2.0, 0.2), 10000.0);
                    data.prdSpeed["" + rfn.BlockDefinition] = speed;
                    if (debug & (int)(oldspeed + 0.5) != (int)(speed + 0.5)) Debug("  Update " + rfn.BlockDefinition.SubtypeName + ":" + subLabel[work.item.subType] + " refine speed: " + (int)(oldspeed + 0.5) + " -> " + (int)(speed + 0.5) + "kg/cycle");
                }
                if (refineryOres[rfn].Count > 0) refineryOres[rfn].IntersectWith(oreLevel.Keys); else refineryOres[rfn].UnionWith(oreLevel.Keys);
                ready = refineryOres[rfn].Count > 0;
                if (stacks.Count > 0)
                {
                    speed = itype == "ORE" ? typeSubData["ORE"][isub].prdSpeed.TryGetValue("" + rfn.BlockDefinition, out speed) ? speed : 1.0 : 1e6;
                    AddInvenRequest(rfn, 0, itype, isub, -1, (long)Math.Min((double)stacks[0].Amount * 1e6 + 0.5, 10 * speed * 1e6 + 0.5));
                    ready = ready & itype == "ORE" & (double)stacks[0].Amount < 2.5 * speed & stacks.Count == 1;
                }
                if (ready)
                    refineries.Add(rfn);
                if (debug) Debug(
                    "  " + rfn.CustomName + (stacks.Count < 1 ? " idle" : " refining " + (int)stacks[0].Amount + "kg " + (isub == "" ? "unknown" : subLabel[isub] + (!oreLevel.ContainsKey(isub) ? "" : " (L=" + oreLevel[isub] + "%)")) + (stacks.Count < 2 ? "" : ", then " + (int)stacks[1].Amount + "kg " + (isub2 == "" ? "unknown" : subLabel[isub2] + (!oreLevel.ContainsKey(isub2) ? "" : " (L=" + oreLevel[isub2] + "%)")))) + "; " + (oreLevel.Count == 0 ? "nothing to do" : ready ? "ready" : refineryOres[rfn].Count == 0 ? "restricted" : "busy")
                );
            }

            // skip refinery:ore assignment if there are no ores or ready refineries
            if (ores.Count > 0 & refineries.Count > 0)
            {
                ores.Sort((o1, o2) =>
                {
                    string i1, i2;
                    if (!ORE_PRODUCT.TryGetValue(o1, out i1)) i1 = o1;
                    if (!ORE_PRODUCT.TryGetValue(o2, out i2)) i2 = o2;
                    return -1 * typeSubData["INGOT"][i1].quota.CompareTo(typeSubData["INGOT"][i2].quota);
                });
                refineries.Sort((r1, r2) => refineryOres[r1].Count.CompareTo(refineryOres[r2].Count));
                foreach (IMyRefinery rfn in refineries)
                {
                    isub = "";
                    level = int.MaxValue;
                    foreach (string isubOre in ores)
                    {
                        if ((isub == "" | oreLevel[isubOre] < level) & refineryOres[rfn].Contains(isubOre))
                        {
                            isub = isubOre;
                            level = oreLevel[isub];
                        }
                    }
                    if (isub != "")
                    {
                        numberRefineries++;
                        rfn.UseConveyorSystem = false;
                        priority = rfn.GetInventory(0).IsItemAt(0) ? -4 : -3;
                        speed = typeSubData["ORE"][isub].prdSpeed.TryGetValue("" + rfn.BlockDefinition, out speed) ? speed : 1.0;
                        AddInvenRequest(rfn, 0, "ORE", isub, priority, (long)(10 * speed * 1e6 + 0.5));
                        oreLevel[isub] += Math.Min(Math.Max((int)(oreLevel[isub] * 0.41), 1), 100 / refineryOres.Count);
                        if (debug) Debug("  " + rfn.CustomName + " assigned " + (int)(10 * speed + 0.5) + "kg " + subLabel[isub] + " (L=" + oreLevel[isub] + "%)");
                    }
                    else if (debug) Debug("  " + rfn.CustomName + " unassigned, nothing to do");
                }
            }

            for (priority = -1; priority >= -4; priority--)
            {
                if (priTypeSubInvenRequest.ContainsKey(priority))
                {
                    foreach (string isubOre in priTypeSubInvenRequest[priority]["ORE"].Keys)
                        AllocateItemBatch(true, priority, "ORE", isubOre);
                }
            }
        }

        void ManageAssemblers()
        {
            if (!typeSubs.ContainsKey("INGOT"))
                return;

            bool debug = ShouldDebug("assemblers");
            long ttlCmp;
            int level, amount;
            InventoryItemData data, data2;
            ItemId item, item2;
            List<ItemId> items;
            Dictionary<ItemId, int> itemLevel = new Dictionary<ItemId, int>(), itemPar = new Dictionary<ItemId, int>();
            List<MyProductionItem> queue = new List<MyProductionItem>();
            double speed, oldspeed;
            ProducerWork work;
            bool ready, jam;
            List<IMyAssembler> assemblers = new List<IMyAssembler>();

            if (debug) Debug("Assembler management:");

            // scan inventory levels
            typeAmount.TryGetValue("COMPONENT", out ttlCmp);
            amount = 90 + (int)(10 * typeSubData["INGOT"].Values.Min(d => d.subType != "URANIUM" & (d.minimum > 0L | d.ratio > 0.0f) ? d.amount / Math.Max(d.minimum, 17.5 * d.ratio * ttlCmp) : 2.0));
            if (debug) Debug("  Component par L=" + amount + "%");
            foreach (string itype in types)
            {
                if (itype != "ORE" & itype != "INGOT")
                {
                    foreach (string isub in typeSubs[itype])
                    {
                        data = typeSubData[itype][isub];
                        data.hold = Math.Max(0, data.hold - 1);
                        item = new ItemId(itype, isub);
                        itemPar[item] = itype == "COMPONENT" & data.ratio > 0.0f ? amount : 100;
                        level = (int)(100L * data.amount / Math.Max(1L, data.quota));
                        if (data.quota > 0L & level < itemPar[item] & data.blueprint != default(MyDefinitionId))
                        {
                            if (data.hold == 0) itemLevel[item] = level;
                            if (debug) Debug("  " + typeLabel[itype] + "/" + subLabel[isub] + (data.hold > 0 ? "" : " @ " + data.amount / 1e6 + "/" + data.quota / 1e6 + ", L=" + level + "%") + (data.hold > 0 | data.jam > 0 ? "; HOLD " + data.hold + "/" + 10 * data.jam : ""));
                        }
                    }
                }
            }

            // identify assemblers that are ready for a new assignment
            foreach (IMyAssembler asm in assemblerItems.Keys)
            {
                ready = jam = false;
                data = data2 = null;
                item = item2 = new ItemId("", "");
                if (!asm.IsQueueEmpty)
                {
                    asm.GetQueue(queue);
                    data = blueprintItem.TryGetValue(queue[0].BlueprintId, out item) ? typeSubData[item.type][item.subType] : null;
                    if (data != null & itemLevel.ContainsKey(item))
                        itemLevel[item] += Math.Max(1, (int)(1e8 * (double)queue[0].Amount / data.quota + 0.5));
                    if (queue.Count > 1 && blueprintItem.TryGetValue(queue[1].BlueprintId, out item2) & itemLevel.ContainsKey(item2))
                        itemLevel[item2] += Math.Max(1, (int)(1e8 * (double)queue[1].Amount / typeSubData[item2.type][item2.subType].quota + 0.5));
                }
                if (producerWork.TryGetValue(asm, out work))
                {
                    data2 = typeSubData[work.item.type][work.item.subType];
                    oldspeed = data2.prdSpeed.TryGetValue("" + asm.BlockDefinition, out oldspeed) ? oldspeed : 1.0;
                    if (work.item.type != item.type | work.item.subType != item.subType)
                    {
                        speed = Math.Max(oldspeed, (asm.IsQueueEmpty ? 2 : 1) * work.quantity);
                        producerJam.Remove(asm);
                    }
                    else if (asm.IsProducing)
                    {
                        speed = work.quantity - (double)queue[0].Amount + asm.CurrentProgress;
                        producerJam.Remove(asm);
                    }
                    else
                    {
                        speed = Math.Max(oldspeed, work.quantity - (double)queue[0].Amount + asm.CurrentProgress);
                        if ((producerJam[asm] = (producerJam.TryGetValue(asm, out level) ? level : 0) + 1) >= 3)
                        {
                            Debug("  " + asm.CustomName + " is jammed by " + subLabel[item.subType]);
                            producerJam.Remove(asm);
                            asm.ClearQueue();
                            data2.hold = 10 * (data2.jam < 1 | data2.hold < 1 ? data2.jam = Math.Min(10, data2.jam + 1) : data2.jam);
                            jam = true;
                        }
                    }
                    speed = Math.Min(Math.Max((speed + oldspeed) / 2.0, Math.Max(0.2, 0.5 * oldspeed)), Math.Min(1000.0, 2.0 * oldspeed));
                    data2.prdSpeed["" + asm.BlockDefinition] = speed;
                    if (debug & (int)(oldspeed + 0.5) != (int)(speed + 0.5)) Debug("  Update " + asm.BlockDefinition.SubtypeName + ":" + typeLabel[work.item.type] + "/" + subLabel[work.item.subType] + " assemble speed: " + (int)(oldspeed * 100) / 100.0 + " -> " + (int)(speed * 100) / 100.0 + "/cycle");
                }
                if (assemblerItems[asm].Count == 0) assemblerItems[asm].UnionWith(itemLevel.Keys); else assemblerItems[asm].IntersectWith(itemLevel.Keys);
                speed = data != null && data.prdSpeed.TryGetValue("" + asm.BlockDefinition, out speed) ? speed : 1.0;
                if (!jam & (asm.IsQueueEmpty || (double)queue[0].Amount - asm.CurrentProgress < 2.5 * speed & queue.Count == 1 & asm.Mode == MyAssemblerMode.Assembly))
                {
                    if (data2 != null) data2.jam = Math.Max(0, data2.jam - (data2.hold < 1 ? 1 : 0));
                    if (ready = assemblerItems[asm].Count > 0) assemblers.Add(asm);
                }
                if (debug) Debug(
                    "  " + asm.CustomName + (asm.IsQueueEmpty ? " idle" : (asm.Mode == MyAssemblerMode.Assembly ? " making " : " breaking ") + queue[0].Amount + "x " + (item.type == "" ? "unknown" : subLabel[item.subType] + (!itemLevel.ContainsKey(item) ? "" : " (L=" + itemLevel[item] + "%)")) + (queue.Count <= 1 ? "" : ", then " + queue[1].Amount + "x " + (item2.type == "" ? "unknown" : subLabel[item2.subType] + (!itemLevel.ContainsKey(item2) ? "" : " (L=" + itemLevel[item2] + "%)")))) + "; " + (itemLevel.Count == 0 ? "nothing to do" : ready ? "ready" : assemblerItems[asm].Count == 0 ? "restricted" : "busy")
                );
            }

            // skip assembler:item assignments if there are no needed items or ready assemblers
            if (itemLevel.Count > 0 & assemblers.Count > 0)
            {
                items = new List<ItemId>(itemLevel.Keys);
                items.Sort((i1, i2) => -1 * typeSubData[i1.type][i1.subType].quota.CompareTo(typeSubData[i2.type][i2.subType].quota));
                assemblers.Sort((a1, a2) => assemblerItems[a1].Count.CompareTo(assemblerItems[a2].Count));
                foreach (IMyAssembler asm in assemblers)
                {
                    item = new ItemId("", "");
                    level = int.MaxValue;
                    foreach (ItemId i in items)
                    {
                        if (itemLevel[i] < Math.Min(level, itemPar[i]) & assemblerItems[asm].Contains(i) & typeSubData[i.type][i.subType].hold < 1)
                        {
                            item = i;
                            level = itemLevel[i];
                        }
                    }
                    if (item.type != "")
                    {
                        numberAssemblers++;
                        asm.UseConveyorSystem = true;
                        asm.CooperativeMode = false;
                        asm.Repeating = false;
                        asm.Mode = MyAssemblerMode.Assembly;
                        data = typeSubData[item.type][item.subType];
                        speed = data.prdSpeed.TryGetValue("" + asm.BlockDefinition, out speed) ? speed : 1.0;
                        amount = Math.Max((int)(10 * speed), 10);
                        asm.AddQueueItem(data.blueprint, (double)amount);
                        itemLevel[item] += (int)Math.Ceiling(1e8 * amount / data.quota);
                        if (debug) Debug("  " + asm.CustomName + " assigned " + amount + "x " + subLabel[item.subType] + " (L=" + itemLevel[item] + "%)");
                    }
                    else if (debug) Debug("  " + asm.CustomName + " unassigned, nothing to do");
                }
            }
        }

        #endregion

        #region Panel Handling

        void UpdateInventoryPanels()
        {
            string text, header2, header5;
            Dictionary<string, List<IMyTextPanel>> itypesPanels = new Dictionary<string, List<IMyTextPanel>>();
            ScreenFormatter sf;
            long maxamt, maxqta;

            foreach (IMyTextPanel panel in ipanelTypes.Keys)
            {
                text = String.Join("/", ipanelTypes[panel]);
                if (itypesPanels.ContainsKey(text)) itypesPanels[text].Add(panel); else itypesPanels[text] = new List<IMyTextPanel>() { panel };
            }
            foreach (List<IMyTextPanel> panels in itypesPanels.Values)
            {
                sf = new ScreenFormatter(6);
                sf.SetBar(0);
                sf.SetFill(0);
                sf.SetAlign(2, 1);
                sf.SetAlign(3, 1);
                sf.SetAlign(4, 1);
                sf.SetAlign(5, 1);
                maxamt = maxqta = 0L;
                foreach (string itype in ipanelTypes[panels[0]].Count > 0 ? ipanelTypes[panels[0]] : types)
                {
                    header2 = " Asm ";
                    header5 = "Quota";
                    if (itype == "INGOT")
                    {
                        header2 = " Ref ";
                    }
                    else if (itype == "ORE")
                    {
                        header2 = " Ref ";
                        header5 = "Max";
                    }
                    if (sf.GetNumRows() > 0)
                        sf.AddBlankRow();
                    sf.Add(0, "");
                    sf.Add(1, typeLabel[itype], true);
                    sf.Add(2, header2, true);
                    sf.Add(3, "Qty", true);
                    sf.Add(4, " / ", true);
                    sf.Add(5, header5, true);
                    sf.AddBlankRow();
                    foreach (InventoryItemData data in typeSubData[itype].Values)
                    {
                        sf.Add(0, data.amount == 0L ? "0.0" : "" + (double)data.amount / data.quota);
                        sf.Add(1, data.label, true);
                        text = data.producers.Count > 0 ? data.producers.Count + " " + (data.producers.All(blk => !(blk is IMyProductionBlock) || (blk as IMyProductionBlock).IsProducing) ? " " : "!") : data.hold > 0 ? "-  " : "";
                        sf.Add(2, text, true);
                        sf.Add(3, data.amount > 0L | data.quota > 0L ? GetShorthand(data.amount) : "");
                        sf.Add(4, data.quota > 0L ? " / " : "", true);
                        sf.Add(5, data.quota > 0L ? GetShorthand(data.quota) : "");
                        maxamt = Math.Max(maxamt, data.amount);
                        maxqta = Math.Max(maxqta, data.quota);
                    }
                }
                sf.SetWidth(3, ScreenFormatter.GetWidth("8.88" + (maxamt >= 1000000000000L ? " M" : maxamt >= 1000000000L ? " K" : ""), true));
                sf.SetWidth(5, ScreenFormatter.GetWidth("8.88" + (maxqta >= 1000000000000L ? " M" : maxqta >= 1000000000L ? " K" : ""), true));
                foreach (IMyTextPanel panel in panels)
                    WriteTableToPanel("TIM Inventory", sf, panel);
            }
        }


        void UpdateStatusPanels()
        {
            long r;
            StringBuilder sb;

            if (statusPanels.Count > 0)
            {
                sb = new StringBuilder();
                sb.Append(panelStatsHeader);
                for (r = Math.Max(1, totalCallCount - statsLog.Length + 1); r <= totalCallCount; r++)
                    sb.Append(statsLog[r % statsLog.Length]);

                foreach (IMyTextPanel panel in statusPanels)
                {
                    panel.ContentType = ContentType.TEXT_AND_IMAGE;
                    panel.TextPadding = 0;
                    panel.WritePublicTitle("Script Status");
                    if (panelSpan.ContainsKey(panel))
                        Debug("Status panels cannot be spanned");
                    panel.WriteText(sb);
                }
            }

            if (debugPanels.Count > 0)
            {
                foreach (IMyTerminalBlock blockFrom in blockErrors.Keys)
                {
                    foreach (IMyTerminalBlock blockTo in blockErrors[blockFrom])
                        Debug("No conveyor connection from " + blockFrom.CustomName + " to " + blockTo.CustomName);
                }
                foreach (IMyTextPanel panel in debugPanels)
                {
                    panel.ContentType = ContentType.TEXT_AND_IMAGE;
                    panel.TextPadding = 0;
                    panel.WritePublicTitle("Script Debugging");
                    if (panelSpan.ContainsKey(panel))
                        Debug("Debug panels cannot be spanned");
                    panel.WriteText(String.Join("\n", DebugMessages));
                }
            }
            blockErrors.Clear();
        }


        void WriteTableToPanel(string title, ScreenFormatter sf, IMyTextPanel panel, bool allowspan = true, string before = "", string after = "")
        {
            int spanx, spany, rows, wide, size, width, height;
            int x, y, r;
            float fontsize;
            string[][] spanLines;
            string text;
            Matrix matrix;
            IMySlimBlock slim;
            IMyTextPanel spanpanel;

            // get the spanning dimensions, if any
            wide = panel.BlockDefinition.SubtypeName.EndsWith("Wide") ? 2 : 1;
            size = panel.BlockDefinition.SubtypeName.StartsWith("Small") ? 3 : 1;
            spanx = spany = 1;
            if (allowspan & panelSpan.ContainsKey(panel))
            {
                spanx = panelSpan[panel].A;
                spany = panelSpan[panel].B;
            }

            // reduce font size to fit everything
            x = sf.GetMinWidth();
            x = x / spanx + (x % spanx > 0 ? 1 : 0);
            y = sf.GetNumRows();
            y = y / spany + (y % spany > 0 ? 1 : 0);
            width = 658 * wide; // TODO monospace 26x17.5 chars
            fontsize = panel.GetValueFloat("FontSize");
            if (fontsize < 0.25f)
                fontsize = 1.0f;
            if (x > 0)
                fontsize = Math.Min(fontsize, Math.Max(0.5f, width * 100 / x / 100.0f));
            if (y > 0)
                fontsize = Math.Min(fontsize, Math.Max(0.5f, 1760 / y / 100.0f));

            // calculate how much space is available on each panel
            width = (int)(width / fontsize);
            height = (int)(17.6f / fontsize);

            // write to each panel
            if (spanx > 1 | spany > 1)
            {
                spanLines = sf.ToSpan(width, spanx);
                panel.Orientation.GetMatrix(out matrix);
                for (x = 0; x < spanx; x++)
                {
                    r = 0;
                    for (y = 0; y < spany; y++)
                    {
                        slim = panel.CubeGrid.GetCubeBlock(new Vector3I(panel.Position + x * wide * size * matrix.Right + y * size * matrix.Down));
                        if (slim != null && slim.FatBlock is IMyTextPanel && "" + slim.FatBlock.BlockDefinition == "" + panel.BlockDefinition)
                        {
                            spanpanel = slim.FatBlock as IMyTextPanel;
                            rows = Math.Max(0, spanLines[x].Length - r);
                            if (y + 1 < spany)
                                rows = Math.Min(rows, height);
                            text = "";
                            if (r < spanLines[x].Length)
                                text = String.Join("\n", spanLines[x], r, rows);
                            if (x == 0)
                                text += y == 0 ? before : y + 1 == spany ? after : "";
                            spanpanel.FontSize = fontsize;
                            spanpanel.ContentType = ContentType.TEXT_AND_IMAGE;
                            spanpanel.TextPadding = 0;
                            spanpanel.WritePublicTitle(title + " (" + (x + 1) + "," + (y + 1) + ")");
                            spanpanel.WriteText(text);
                        }
                        r += height;
                    }
                }
            }
            else
            {
                panel.FontSize = fontsize;
                panel.ContentType = ContentType.TEXT_AND_IMAGE;
                panel.TextPadding = 0;
                panel.WritePublicTitle(title);
                panel.WriteText(before + sf.ToString(width) + after);
            }
        }

#endregion
    }
}
