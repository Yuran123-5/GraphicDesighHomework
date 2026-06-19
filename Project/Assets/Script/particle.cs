using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
[RequireComponent(typeof(ParticleSystem))]
public class particle : MonoBehaviour
{
    public ParticleSystem particlesSystem;

    public Vector2[] particles_pos;
    public Vector2[] particles_vel;

    public Vector2 initialPos;
    public float boxWidth = 10f;
    public float boxHeight = 10f;
    private float boxBottom;
    private float boxTop;
    private float boxLeft;
    private float boxRight;

    [Header("粒子设置")]
    [Range(0,1000)]
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
    public float smootherRadius=2f;

    public ParticleSystem.Particle[] particlesArray;
    public float[] particles_density;

    [Header("压强设置")]
    public float targetdensity;
    public float pressureMulitiper;
    [Obsolete]
    void Start()
    {
        particlesSystem = GetComponent<ParticleSystem>();
        ParticleSystem.MainModule mainModule = particlesSystem.main;
        mainModule.maxParticles = 1000;
        mainModule.startLifetime= Mathf.Infinity;
        mainModule.simulationSpace = ParticleSystemSimulationSpace.World;

        boxBottom = initialPos.y - boxHeight / 2;
        boxTop = initialPos.y + boxHeight / 2;
        boxLeft = initialPos.x - boxWidth / 2;
        boxRight = initialPos.x + boxWidth / 2;

        particlesArray = new ParticleSystem.Particle[numParticles];
        particles_pos = new Vector2[numParticles];
        particles_vel = new Vector2[numParticles];
        particles_density = new float[numParticles];
        initParticles();
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

            ParticleSystem.Particle particleData=new ParticleSystem.Particle();
            particleData.position = pos;
            particleData.startSize = radius;
            particleData.startColor = particalColor;

            particlesArray[i]=particleData;
        }
        particlesSystem.SetParticles(particlesArray, particlesArray.Length);
        //particlesSystem.Play();
    }
    // Update is called once per frame
    void Update()
    {
        updateDensity();
        updateParticles();
    }
    public void updateParticles()
    {
        for(int i = 0; i <numParticles; i++)
        {
            Vector2 pos=particles_pos[i];
            Vector2 vel=particles_vel[i];
            float density=particles_density[i];
            updateVelocity(ref pos, ref vel,i);
            particles_pos[i] = pos;
            particles_vel[i] = vel;
            ParticleSystem.Particle particle = particlesArray[i];
            particle.position = pos;
            particle.startColor=particalColor;
            particle.startSize = radius;
            particlesArray[i]=particle;
        }
        particlesSystem.SetParticles(particlesArray, particlesArray.Length);
    }
    public void updateVelocity(ref Vector2 pos,ref Vector2 vel,int index)
    {
        vel+= Vector2.down * gravity * Time.deltaTime;
        float density=particles_density[index];
        Vector2 pressureForce=calculatePressureForce(index);
        Vector2 pressureForceAcceleration = pressureForce / density;
        vel += pressureForceAcceleration * Time.deltaTime;
        pos += vel * Time.deltaTime;
        boundForBox(ref pos, ref vel);
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

    public float SmoothKerneel(float smoothRadius,float distance)
    {
        if (distance >= smoothRadius)
            return 0;
        float volume = MathF.PI * MathF.Pow(smoothRadius, 4) / 6;
        return (smoothRadius-distance)* (smoothRadius - distance) /volume;
    }
    public float smoothKerneelDerivative(float smoothRadius,float distance)
    {
        if(distance>=smoothRadius)
            return 0;
        float f=  smoothRadius -distance;
        float scale = -12 / (Mathf.PI * Mathf.Pow(smoothRadius, 4));
        return scale*  f;
    }
    public float calculateDensity(Vector2 samplePoint) 
    {
        float density = 0;
        const int mass = 1;
        for (int i = 0; i < particles_pos.Length; i++) 
        {
            float distance = (particles_pos[i]-samplePoint).magnitude;
            float influence=SmoothKerneel(smootherRadius,distance);
            density += influence*mass;
        }
        return density;
    }
    public void updateDensity()
    {
        for (int i = 0;i < particles_density.Length;i++)
        {
            float density=particles_density[i];
            density=calculateDensity(particles_pos[i]);
            particles_density[i]=density;
        }
    }
    public Vector2 calculatePressureForce(int index)
    {
        Vector2 pressureForce = Vector2.zero;
        const int mass = 1;
        Vector2 samplePoint=particles_pos[index];
        for (int i = 0; i < numParticles; i++) {
            if (index == i)
                continue;
            float dst=(particles_pos[i]-samplePoint).magnitude;

            Vector2 dir= dst==0?GetRandomDir():(particles_pos[i] - samplePoint)/dst;
            float slope = smoothKerneelDerivative(smootherRadius, dst);
            float density = particles_density[i];
            float sharedPressure = calculateSharedPress(density, particles_density[index]);
            pressureForce += sharedPressure * dir * slope * mass / density;
        }
        return pressureForce;
    }
    public float converDensityToPressure(float density)
    {
        float densityError=density-targetdensity;
        float pressure = densityError * pressureMulitiper;
        return pressure;
    }
    public Vector2 GetRandomDir()
    {
        float x = 0;
        float y = 0;
        while(x==0&&y==0)
        {
            x = UnityEngine.Random.Range(-1, 1);
            y = UnityEngine.Random.Range(-1, 1);
        }
        float dst=new Vector2(x,y).magnitude;
        return new Vector2(x,y)/dst;
    }
    public float calculateSharedPress(float densitya, float densityb)
    {
        float pressurea = converDensityToPressure(densitya);
        float pressureb = converDensityToPressure(densityb);
        return (pressurea + pressureb) / 2;
    }
}
