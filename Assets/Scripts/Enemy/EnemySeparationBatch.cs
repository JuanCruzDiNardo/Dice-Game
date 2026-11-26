#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;

internal enum EnemyBenchmarkMethod
{
    Original,
    Sequential,
    Jobs,
    JobsBurst,
    Ecs
}

internal sealed class EnemySeparationBatch : IDisposable
{
    private const int JobBatchSize = 32;
    private static readonly ProfilerMarker PrepareMarker = new("DiceGame.Benchmark.Separation.Prepare");
    private static readonly ProfilerMarker ComputeMarker = new("DiceGame.Benchmark.Separation.Compute");
    private static readonly ProfilerMarker ApplyMarker = new("DiceGame.Benchmark.Separation.Apply");

    private readonly Enemy[] enemies;
    private readonly Transform[] transforms;
    private readonly EnemyBenchmarkMethod method;
    private readonly Dictionary<Collider, int> enemyByCollider = new();
    private readonly int stride;
    private NativeArray<Agent> agents;
    private NativeArray<int> neighbours;
    private NativeArray<float2> results;
    private readonly EnemySeparationEntities ecs;

    public EnemySeparationBatch(Enemy[] enemies, EnemyBenchmarkMethod method)
    {
        this.enemies = enemies;
        this.method = method;
        transforms = new Transform[enemies.Length];
        for (int i = 0; i < enemies.Length; i++)
        {
            Enemy enemy = enemies[i];
            transforms[i] = enemy.transform;
            stride = Mathf.Max(stride, enemy.BenchmarkNeighbourCapacity);
            foreach (Collider collider in enemy.BenchmarkColliders)
                enemyByCollider.Add(collider, i);
        }

        try
        {
            agents = new NativeArray<Agent>(enemies.Length, Allocator.Persistent);
            neighbours = new NativeArray<int>(enemies.Length * stride, Allocator.Persistent);
            results = new NativeArray<float2>(enemies.Length, Allocator.Persistent);
            if (method == EnemyBenchmarkMethod.Ecs)
                ecs = new EnemySeparationEntities(enemies.Length);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public void UpdateEnemies()
    {
        using (PrepareMarker.Auto())
        {
            // All three modes read the same positions before any enemy is moved.
            for (int i = 0; i < enemies.Length; i++)
            {
                Agent agent = default;
                Enemy enemy = enemies[i];
                if (enemy != null && enemy.CanMoveInBenchmark)
                {
                    Vector3 position = transforms[i].position;
                    Vector3 fallback = enemy.BenchmarkFallbackDirection;
                    agent.Position = new float2(position.x, position.z);
                    agent.Fallback = new float2(fallback.x, fallback.z);
                    agent.Radius = enemy.BenchmarkSeparationRadius;
                    agent.Start = i * stride;

                    int count = enemy.FindBenchmarkNeighbours(position, out Collider[] hits);
                    for (int h = 0; h < count; h++)
                    {
                        if (hits[h] != null && enemyByCollider.TryGetValue(hits[h], out int other) &&
                            other != i && enemies[other].CanMoveInBenchmark)
                        {
                            neighbours[agent.Start + agent.Count] = other;
                            agent.Count++;
                        }
                    }
                }

                agents[i] = agent;
            }
            ecs?.Prepare(agents);
        }

        using (ComputeMarker.Auto())
        {
            var calculation = new SeparationCalculation { Agents = agents, Neighbours = neighbours, Results = results };
            switch (method)
            {
                case EnemyBenchmarkMethod.Sequential:
                    for (int i = 0; i < enemies.Length; i++)
                        calculation.Execute(i);
                    break;
                case EnemyBenchmarkMethod.Jobs:
                    new SeparationJob { Calculation = calculation }.Schedule(enemies.Length, JobBatchSize).Complete();
                    break;
                case EnemyBenchmarkMethod.JobsBurst:
                    new BurstSeparationJob { Calculation = calculation }.Schedule(enemies.Length, JobBatchSize).Complete();
                    break;
                case EnemyBenchmarkMethod.Ecs:
                    ecs.Compute(agents, neighbours);
                    break;
            }
        }

        using (ApplyMarker.Auto())
        {
            ecs?.CopyResults(results);
            for (int i = 0; i < enemies.Length; i++)
            {
                Enemy enemy = enemies[i];
                if (enemy != null && enemy.CanMoveInBenchmark)
                {
                    float2 separation = results[i];
                    enemy.ApplyBenchmarkMovement(new Vector3(separation.x, 0f, separation.y));
                }
            }
        }
    }

    public void Dispose()
    {
        ecs?.Dispose();
        if (results.IsCreated)
            results.Dispose();
        if (neighbours.IsCreated)
            neighbours.Dispose();
        if (agents.IsCreated)
            agents.Dispose();
    }

    internal struct Agent
    {
        public float2 Position;
        public float2 Fallback;
        public float Radius;
        public int Start;
        public int Count;
    }

    internal struct SeparationCalculation
    {
        [ReadOnly] public NativeArray<Agent> Agents;
        [ReadOnly] public NativeArray<int> Neighbours;
        [WriteOnly] public NativeArray<float2> Results;

        public void Execute(int index)
        {
            Results[index] = Calculate(Agents[index], Agents, Neighbours);
        }

        internal static float2 Calculate(Agent agent, NativeArray<Agent> agents, NativeArray<int> neighbours)
        {
            float2 separation = default;
            int validCount = 0;
            for (int i = agent.Start; i < agent.Start + agent.Count; i++)
            {
                float2 away = agent.Position - agents[neighbours[i]].Position;
                float squaredDistance = math.lengthsq(away);
                if (squaredDistance < 0.0001f)
                {
                    separation += agent.Fallback;
                    validCount++;
                    continue;
                }

                float distance = math.sqrt(squaredDistance);
                if (distance >= agent.Radius)
                    continue;

                float weight = 1f - distance / agent.Radius;
                separation += away / distance * weight;
                validCount++;
            }

            return validCount > 0 ? separation / validCount : default;
        }
    }

    private struct SeparationJob : IJobParallelFor
    {
        public SeparationCalculation Calculation;

        public void Execute(int index) => Calculation.Execute(index);
    }

    [BurstCompile(CompileSynchronously = true)]
    private struct BurstSeparationJob : IJobParallelFor
    {
        public SeparationCalculation Calculation;

        public void Execute(int index) => Calculation.Execute(index);
    }
}
#endif
