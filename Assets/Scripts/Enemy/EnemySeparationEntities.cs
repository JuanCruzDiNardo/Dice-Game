#if UNITY_EDITOR
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

internal struct BenchmarkAgentInput : IComponentData
{
    public EnemySeparationBatch.Agent Agent;
    public int Index;
}

internal struct BenchmarkAgentResult : IComponentData
{
    public float2 Separation;
    public int Index;
}

[BurstCompile(CompileSynchronously = true)]
internal partial struct BenchmarkSeparationEntityJob : IJobEntity
{
    [ReadOnly] public NativeArray<EnemySeparationBatch.Agent> Agents;
    [ReadOnly] public NativeArray<int> Neighbours;

    private void Execute(ref BenchmarkAgentResult result, in BenchmarkAgentInput input)
    {
        result.Index = input.Index;
        result.Separation = EnemySeparationBatch.SeparationCalculation.Calculate(input.Agent, Agents, Neighbours);
    }
}

internal sealed class EnemySeparationEntities : IDisposable
{
    private readonly World world;
    private readonly EntityManager manager;
    private readonly EntityQuery query;
    private NativeArray<Entity> entities;

    public EnemySeparationEntities(int count)
    {
        world = new World("Dice Game ECS benchmark");
        try
        {
            manager = world.EntityManager;
            var archetype = manager.CreateArchetype(typeof(BenchmarkAgentInput), typeof(BenchmarkAgentResult));
            entities = new NativeArray<Entity>(count, Allocator.Persistent);
            manager.CreateEntity(archetype, entities);
            query = manager.CreateEntityQuery(ComponentType.ReadOnly<BenchmarkAgentInput>(), ComponentType.ReadWrite<BenchmarkAgentResult>());
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public void Prepare(NativeArray<EnemySeparationBatch.Agent> agents)
    {
        for (int i = 0; i < entities.Length; i++)
            manager.SetComponentData(entities[i], new BenchmarkAgentInput { Agent = agents[i], Index = i });
    }

    public void Compute(NativeArray<EnemySeparationBatch.Agent> agents, NativeArray<int> neighbours)
    {
        new BenchmarkSeparationEntityJob { Agents = agents, Neighbours = neighbours }
            .ScheduleParallel(query, default).Complete();
    }

    public void CopyResults(NativeArray<float2> results)
    {
        using var components = query.ToComponentDataArray<BenchmarkAgentResult>(Allocator.Temp);
        foreach (BenchmarkAgentResult result in components)
            results[result.Index] = result.Separation;
    }

    public void Dispose()
    {
        if (entities.IsCreated)
            entities.Dispose();
        if (world.IsCreated)
            world.Dispose();
    }
}
#endif
