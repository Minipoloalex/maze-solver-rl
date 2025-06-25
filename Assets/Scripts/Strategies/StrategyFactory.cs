using UnityEngine;

public static class StrategyFactory
{
    public static IStrategy Create(StrategicPlatformAgent.StrategyType type)
    {
        switch (type)
        {
            case StrategicPlatformAgent.StrategyType.EndToEnd:
                return new EndToEndStrategy();

            case StrategicPlatformAgent.StrategyType.Hierarchical:
                return new HierarchicalStrategy();

            default:
                Debug.LogError($"Unknown strategy type: {type}");
                return null;
        }
    }
}