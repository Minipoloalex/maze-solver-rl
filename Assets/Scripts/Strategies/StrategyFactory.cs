// FILE: Assets/Scripts/Strategies/StrategyFactory.cs

using UnityEngine;

public static class StrategyFactory
{
    public static IStrategy Create(StrategicPlatformAgent.StrategyType type)
    {
        switch (type)
        {
            case StrategicPlatformAgent.StrategyType.BalanceToTarget:
                return new BalanceToTargetStrategy();

            case StrategicPlatformAgent.StrategyType.MazeSolver:
                return new MazeSolverStrategy();

            default:
                Debug.LogError($"Unknown strategy type: {type}");
                return null;
        }
    }
}