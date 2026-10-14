using Contracts;
using Strategies;

namespace AutoQuickSaveSystem
{
    // The stock ContractSystem and StrategySystem only start a coroutine in OnLoad(), and read their
    // state from the save a frame later. On a scene load that frame can last several seconds. A game
    // saved in the meantime gets an empty contract list and an empty strategy list.
    internal static class ScenarioLoadState
    {
        // The name of a scenario module of the current scene that has not finished loading its
        // state, or null if there is none.
        internal static string Pending()
        {
            Game game = HighLogic.CurrentGame;
            if (game == null)
                return "Game";

            foreach (ProtoScenarioModule proto in game.scenarios)
            {
                if (proto.targetScenes == null || !proto.targetScenes.Contains(HighLogic.LoadedScene))
                    continue;

                if (proto.moduleName == nameof(ContractSystem))
                {
                    // ContractSystem.loaded is reset in OnAwake() and set at the end of the load,
                    // except when there is no CONTRACTS node to load, as in a new game.
                    if (proto.moduleRef == null)
                        return proto.moduleName;
                    if ((ContractSystem.Instance != proto.moduleRef || !ContractSystem.loaded) && proto.GetData().HasNode("CONTRACTS"))
                        return proto.moduleName;
                }
                else if (proto.moduleName == nameof(StrategySystem))
                {
                    // The load creates one strategy for each strategy config, and the list is empty
                    // before that.
                    StrategySystem strategySystem = proto.moduleRef as StrategySystem;
                    if (strategySystem == null || strategySystem.Strategies.Count < strategySystem.SystemConfig.Strategies.Count)
                        return proto.moduleName;
                }
            }

            return null;
        }
    }
}
