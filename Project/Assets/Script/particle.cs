using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
[RequireComponent(typeof(ParticleSystem))]
public class particle : MonoBehaviour
{
    public ParticleSystem particlesSystem;

    private static readonly System.Random _rand = new System.Random();

    public Vector2[] particles_pos;
    public Vector2[] particles_vel;
    public Vector2[] predictpos;

    public Vector2 initialPos;
    public float boxWidth = 10f;
    public float boxHeight = 10f;
    private float boxBottom;
    private float boxTop;
    private float boxLeft;
    private float boxRight;

    [Header("粒子设置")]
    [Range(0, 5000)]
    public int numParticles = 100;
    [Range(0, 11)]
    public float gravity;
    [Range(0, 1f)]
    public float radius;
    public Color particalColor;
    [Range(0, 1f)]
    public float boundDamping;
    [Range(0, 1f)]
    public float spacing = 0f;
    [Range(0, 3f)]
    public float smootherRadius = 2f;

    public ParticleSystem.Particle[] particlesArray;
    public float[] particles_density;

    [Header("压强设置")]
    public float targetdensity;
    public float pressureMulitiper;

    [Header("哈希计算")]
    public Dictionary<int, List<int>> particles_dic;
    int[] dx = { 0, 0, -1, 1, 1, 1, -1, -1, 0 };
    int[] dy = { 1, -1, 0, 0, 1, -1, 1, -1, 0 };
    [Obsolete]
    void Start()
    {
        particlesSystem = GetComponent<ParticleSystem>();
        ParticleSystem.MainModule mainModule = particlesSystem.main;
        mainModule.maxParticles = 1000;
        mainModule.startLifetime = Mathf.Infinity;
        mainModule.simulationSpace = ParticleSystemSimulationSpace.World;

        boxBottom = initialPos.y - boxHeight / 2;
        boxTop = initialPos.y + boxHeight / 2;
        boxLeft = initialPos.x - boxWidth / 2;
        boxRight = initialPos.x + boxWidth / 2;

        particlesArray = new ParticleSystem.Particle[numParticles];
        particles_pos = new Vector2[numParticles];
        particles_vel = new Vector2[numParticles];
        predictpos = new Vector2[numParticles];
        particles_density = new float[numParticles];
        initParticles();
        initParticlesHash();
        updateDensity();
    }
    void initParticles()
    {
        int particalCol = (int)Mathf.Sqrt(numParticles);
        int particalRow = (numParticles - 1) / particalCol + 1;

        float step = radius * 2 + spacing;

        float cell = (spacing + 2 * radius) / 2f;
        float particalTotalWidth = (particalCol - 1) * cell;
        float particalTotalHeight = (particalRow - 1) * cell;
        float StartX = initialPos.x - particalTotalWidth / 2f;
        float StartY = initialPos.y + particalTotalHeight / 2f;
        for (int i = 0; i < numParticles; i++)
        {
            int col = i % particalRow;
            int row = i / particalRow;

            float px = StartX + col * cell;
            float py = StartY - row * cell;
            Vector2 pos = new Vector2(px, py);
            Vector2 vel = Vector2.zero;

            particles_pos[i] = pos;
            particles_vel[i] = vel;
            predictpos[i] = pos;

            ParticleSystem.Particle particleData = new ParticleSystem.Particle();
            particleData.position = pos;
            particleData.startSize = radius;
            particleData.startColor = particalColor;

            particlesArray[i] = particleData;
        }
        particlesSystem.SetParticles(particlesArray, particlesArray.Length);
        //particlesSystem.Play();
    }
    // Update is called once per frame
    void Update()
    {
        updateHashGrid();
        //updateDensity();
        updateParticles_2();
    }
    public void updateParticles()
    {
        for (int i = 0; i < numParticles; i++)
        {
            Vector2 pos = particles_pos[i];
            Vector2 vel = particles_vel[i];
            float density = particles_density[i];
            updateVelocity(ref pos, ref vel, i);
            particles_pos[i] = pos;
            particles_vel[i] = vel;
            ParticleSystem.Particle particle = particlesArray[i];
            particle.position = pos;
            particle.startColor = particalColor;
            particle.startSize = radius;
            particlesArray[i] = particle;
        }
        particlesSystem.SetParticles(particlesArray, particlesArray.Length);
    }
    public void updateVelocity(ref Vector2 pos, ref Vector2 vel, int index)
    {
        vel += Vector2.down * gravity * Time.deltaTime;
        float density = particles_density[index];
        Vector2 pressureForce = calculatePressureForceByHash(index);
        Vector2 pressureForceAcceleration = pressureForce / density;
        vel += pressureForceAcceleration * Time.deltaTime;
        pos += vel * Time.deltaTime;
        boundForBox(ref pos, ref vel);
    }
    public void updateParticles_2()
    {
        float dt =Time.deltaTime;
        Parallel.For(0, numParticles, i =>
        {
            Vector2 vel = particles_vel[i];
            vel += Vector2.down * gravity * dt;
            predictpos[i] = particles_pos[i] + vel * dt;

            particles_density[i]=calculateDensityByHash(predictpos[i]);

            Vector2 pressureForce = calculatePressureForceByHash(i);
            Vector2 pressureForceAcceleration = pressureForce /particles_density[i];
            vel += pressureForceAcceleration * dt;

            particles_vel[i] = vel;
        });

        Parallel.For(0, numParticles, i => 
        {
            Vector2 vel=particles_vel[i];
            Vector2 pos=particles_pos[i];

            pos += vel * dt;
            boundForBox(ref pos,ref vel);
            particles_vel[i] = vel;
            particles_pos[i] = pos;
        });

        for (int i = 0; i < numParticles; i++) {
            ParticleSystem.Particle particle = particlesArray[i];
            particle.position = particles_pos[i];
            particle.startColor = particalColor;
            particle.startSize = radius;
            particlesArray[i] = particle;
        }
        particlesSystem.SetParticles(particlesArray, particlesArray.Length);
    }
    public void boundForBox(ref Vector2 pos, ref Vector2 vel)
    {
        if (pos.y - radius / 2 < boxBottom)
        {
            pos.y = boxBottom + radius / 2;
            vel.y *= -1 * boundDamping;
        }
        if (pos.y + radius / 2 > boxTop)
        {
            pos.y = boxTop - radius / 2;
            vel.y *= -1 * boundDamping;
        }
        if (pos.x - radius / 2 < boxLeft)
        {
            pos.x = boxLeft + radius / 2;
            vel.x *= -1 * boundDamping;
        }
        if (pos.x + radius / 2 > boxRight)
        {
            pos.x = boxRight - radius / 2;
            vel.x *= -1 * boundDamping;
        }
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Vector2 center = initialPos;
        Vector2 size = new Vector2(boxWidth, boxHeight);
        Gizmos.DrawWireCube(center, size);
    }

    public float SmoothKerneel(float smoothRadius, float distance)
    {
        if (distance >= smoothRadius)
            return 0;
        float volume = MathF.PI * MathF.Pow(smoothRadius, 4) / 6;
        return (smoothRadius - distance) * (smoothRadius - distance) / volume;
    }
    public float smoothKerneelDerivative(float smoothRadius, float distance)
    {
        if (distance >= smoothRadius)
            return 0;
        float f = smoothRadius - distance;
        float scale = -12 / (Mathf.PI * Mathf.Pow(smoothRadius, 4));
        return scale * f;
    }
    public float calculateDensity(Vector2 samplePoint)
    {
        float density = 0;
        const int mass = 1;
        for (int i = 0; i < particles_pos.Length; i++)
        {
            float distance = (particles_pos[i] - samplePoint).magnitude;
            float influence = SmoothKerneel(smootherRadius, distance);
            density += influence * mass;
        }
        return density;
    }
    public float calculateDensityByHash(Vector2 samplePoint)
    {
        float density = 0;
        const int mass = 1;
        HashSet<int> surroundHash = new HashSet<int>();

        Vector2 CellPos=calculateCellPos(samplePoint);
        for (int i = 0; i < 9; i++)
        {
            Vector2 dir = new Vector2(dx[i], dy[i]);
            Vector2 surroundGrid = CellPos + dir;
            int hashkey = calculateCellHashKey(surroundGrid);
            surroundHash.Add(hashkey);
        }
        List<int> surroundParticlesByHash = new List<int>();
        foreach (int hashkey in surroundHash)
        {
            if (!particles_dic.ContainsKey(hashkey))
                continue;
            List<int> particlesByHash = particles_dic[hashkey];
            for (int i = 0; i < particlesByHash.Count; i++)
            {
                surroundParticlesByHash.Add(particlesByHash[i]);
            }
        }
        for (int i = 0; i < surroundParticlesByHash.Count; i++)
        {
            float distance = (particles_pos[surroundParticlesByHash[i]] - samplePoint).magnitude;
            float influence = SmoothKerneel(smootherRadius, distance);
            density += influence * mass;
        }
        return density;
    }
    public void updateDensity()
    {
        for (int i = 0; i < particles_density.Length; i++)
        {
            float density = particles_density[i];
            density = calculateDensityByHash(particles_pos[i]);
            particles_density[i] = density;
        }
    }
    public Vector2 calculatePressureForce(int index)
    {
        Vector2 pressureForce = Vector2.zero;
        const int mass = 1;
        Vector2 samplePoint = particles_pos[index];
        for (int i = 0; i < numParticles; i++)
        {
            if (index == i)
                continue;
            float dst = (particles_pos[i] - samplePoint).magnitude;

            Vector2 dir = dst == 0 ? GetRandomDir() : (particles_pos[i] - samplePoint) / dst;
            float slope = smoothKerneelDerivative(smootherRadius, dst);
            float density = particles_density[i];
            float sharedPressure = calculateSharedPress(density, particles_density[index]);
            pressureForce += sharedPressure * dir * slope * mass / density;
        }
        return pressureForce;
    }
    public Vector2 calculatePressureForceByHash(int index)
    {
        Vector2 pressureForce = Vector2.zero;
        const int mass = 1;
        Vector2 samplePoint = predictpos[index];
        HashSet<int> surroundHash = new HashSet<int>();

        Vector2 CellPos = calculateCellPos(samplePoint);
        for (int i = 0; i < 9; i++)
        {
            Vector2 dir = new Vector2(dx[i], dy[i]);
            Vector2 surroundGrid = CellPos + dir;
            int hashkey = calculateCellHashKey(surroundGrid);
            surroundHash.Add(hashkey);
        }
        List<int> surroundParticlesByHash = new List<int>();
        foreach (int hashkey in surroundHash)
        {
            if (!particles_dic.ContainsKey(hashkey))
                continue;
            List<int> particlesByHash = particles_dic[hashkey];
            for (int i = 0; i < particlesByHash.Count; i++)
            {
                surroundParticlesByHash.Add(particlesByHash[i]);
            }
        }
        for (int i = 0; i < surroundParticlesByHash.Count; i++)
        {
            int neighborhood= surroundParticlesByHash[i];
            if (index == neighborhood)
                continue;
            float dst = (predictpos[surroundParticlesByHash[i]] - samplePoint).magnitude;
            if(dst>smootherRadius)
                continue;
            Vector2 dir = dst == 0 ? GetRandomDir() : (predictpos[neighborhood] - samplePoint) / dst;
            float slope = smoothKerneelDerivative(smootherRadius, dst);
            float density = particles_density[neighborhood];
            //float sharedPressure = calculateSharedPress(density, particles_density[index]);
            float p_i = converDensityToPressure(particles_density[index]);
            float p_j = converDensityToPressure(density);
            float sharedPressure = (p_i * density) / particles_density[index] + (p_j * particles_density[index]) / density;
            pressureForce += sharedPressure * dir * slope * mass / density;
            //pressureForce += converDensityToPressure(density) * dir * slope * mass / density;
        }
        return pressureForce;
    }
    public float converDensityToPressure(float density)
    {
        float densityError = density - targetdensity;
        //float pressure =Mathf.Max(0, densityError * pressureMulitiper);
        float pressure = density * pressureMulitiper;
        return pressure;
    }
    public Vector2 GetRandomDir()
    {
        double rad = _rand.NextDouble() * Math.PI * 2;
        float x = (float)Math.Cos(rad);
        float y = (float)Math.Sin(rad);
        return new Vector2(x, y);
    }
    public float calculateSharedPress(float densitya, float densityb)
    {
        float pressurea = converDensityToPressure(densitya);
        float pressureb = converDensityToPressure(densityb);
        return (pressurea + pressureb) / 2;
    }
    public Vector2 calculateCellPos(Vector2 pos)
    {
        int x = (int)(pos.x / smootherRadius);
        int y = (int)(pos.y / smootherRadius);
        return new Vector2(x, y);
    }
    public int calculateCellHashKey(Vector2 CellPos)
    {
        return (int)((uint)(CellPos.x * 42688201 + CellPos.y * 79193993) % 1000);
    }
    public void initParticlesHash()
    {
        particles_dic = new Dictionary<int, List<int>>();
        updateHashGrid();   
    }
    public void updateHashGrid()
    {
        particles_dic.Clear();
        for (int i = 0; i < numParticles; i++)
        {
            Vector2 pos = particles_pos[i];
            int hashKey = calculateCellHashKey(calculateCellPos(pos));
            if (!particles_dic.ContainsKey(hashKey))
            {
                particles_dic[hashKey] = new List<int>();
            }
            particles_dic[hashKey].Add(i);
        }
    }
}
