using BB;
using System;
using System.Collections;
using UnityEngine;


using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;
using Vec3 = UnityEngine.Vector3;
using Vec3I = UnityEngine.Vector3Int;

namespace BB
{
  public class AetherSPH : AetherTestBase {
    private struct Particle {
      public SpriteObj obj;

      // state
      public Vec3 loc;
      public Vec3 vel;
      public float mass;

      // temp storage
      public Vec3 acc;
      public float pd_ratio;

      public void Render() {
        obj.Render(loc);
      }
    }

    Particle[] particles;

    private const int NUM_PARTICLES = 30;
    private const float MASS = 2; // ???


    private const float SMOOTHING_DIST = .4f; // h
    private const float EQ_OF_STATE_CONSTANT = .1f; // k
    private const float POLYTOPIC_INDEX = 3.5f; //1; // n  (compressibility, 0 == totally compressible)
    private const float DAMPING = .99f;

    private readonly Vec3 GRAVITY_CENTER = new Vec3(1.5f, 3.5f, 0);
    private const float GRAVITY_STRENGTH = 0;//2.01f * .25f;zv


    private const float RESTITUTION = .2f;
    private const float MAX_X = 10f;
    private const float DOWN_GRAV = 0.8f;


    public AetherSPH(Game game) : base(game) {
      var sprite = this.CreateParticleSprite(AssetSrc.CreateFlatTex(Color.magenta), 1/16f);

      var lr_bounds = game.assets.CreateLine(game.aetherContainer, "bounds", RenderLayer.OverMinion.Layer(200), Color.white, 1/16f, false, false);

      Vec2[] pts = {
        new Vec2(0, 100),
        new Vec2(0, 0),
        new Vec2(MAX_X, 0),
        new Vec2(MAX_X, 100)};
      lr_bounds.SetPts(pts);

      particles = new Particle[NUM_PARTICLES];
      for (int i = 0; i < particles.Length; ++i) {
        var name = $"aether_particle_{i}";

        var x = (float)(i % 10);
        var y = (float)Math.Floor(i / 10f);
        var x_jitter = y * .0001f;
        var y_jitter = x * .0001f;
        var pos = new Vec2(x + x_jitter, y + y_jitter) * .5f;
        pos = pos * 2f + new Vec2(1, 2);

        // var spriteRenderer = game.assets.CreateObjectWithRenderer<SpriteRenderer>(
        //     game.aetherContainer, pos, name, RenderLayer.OverMinion.Layer(200));
        // spriteRenderer.sprite = sprite;

        var p = new Particle();
        p.obj = CreateSpriteObj(sprite);
        var l = game.assets.CreateObjectWithRenderer<LineRenderer>(p.obj.xf, Vec2.zero, "circle", RenderLayer.OverMinion.Layer(200));
        l.loop = true;
        l.widthMultiplier = 1/32f;
        l.useWorldSpace = false;
        var line = new Line(l);
        line.SetCircle(new Circle(Vec2.zero, SMOOTHING_DIST),32);

        p.mass = MASS;
        p.loc = pos;
        p.Render();
        //p.vel = new Vec2(0, .5f);

        particles[i] = p;
      }
    }


    private float Kernel(Vec3 dist) {
      var m2 = dist.sqrMagnitude;
      // TODO: prob should pre-compute
      var c = Math.Pow(1f / (SMOOTHING_DIST * Math.Sqrt(Math.PI)), 3);
      var k = c * Math.Exp(-m2 / Math.Pow(SMOOTHING_DIST, 2));
      return (float)k;
    /*
  """
  Gausssian  Smoothing kernel (3D)
  x     is a vector/matrix of x positions
  y     is a vector/matrix of y positions
  z     is a vector/matrix of z positions
  h     is the smoothing length
  w     is the evaluated smoothing function
  """

  r = np.sqrt(x**2 + y**2 + z**2)

  w = (1.0 / (h*np.sqrt(np.pi)))**3 * np.exp( -r**2 / h**2)

  return w
*/
    }


    private Vec3 DeltaKernel(Vec3 dist) {
      // TODO: this is super code+computation sharey w/ Kernel
      // should prob compute ahead of time separately.
      var m2 = dist.sqrMagnitude;
      var c = -2 / (Math.Pow(SMOOTHING_DIST, 3) * Math.Pow(Math.PI, 3f/2f));
      var f = c * Math.Exp(-m2 / Math.Pow(SMOOTHING_DIST, 2));
      return (float)f * dist;
/*
def gradW( x, y, z, h ):
  """
  Gradient of the Gausssian Smoothing kernel (3D)
  x     is a vector/matrix of x positions
  y     is a vector/matrix of y positions
  z     is a vector/matrix of z positions
  h     is the smoothing length
  wx, wy, wz     is the evaluated gradient
  """

  r = np.sqrt(x**2 + y**2 + z**2)

  n = -2 * np.exp( -r**2 / h**2) / h**5 / (np.pi)**(3/2)
  wx = n * x
  wy = n * y
  wz = n * z

  return wx, wy, wz
*/
    }

    private void CalcDensityPressureRatio(int i) {
      var density = 0f;
      ref var p = ref particles[i];
      for (int j = 0; j < particles.Length; ++j) {
        // if (j == i)
        //   continue;

        ref var pj = ref particles[j];
        var k = Kernel(p.loc - pj.loc);
        density += pj.mass * k;
      }


      var pressure = EQ_OF_STATE_CONSTANT * Math.Pow(density, 1 + (1/POLYTOPIC_INDEX));
      p.pd_ratio = (float)(density / (pressure * pressure));
    }

    private void CalcAcc(int i) {
      ref var p = ref particles[i];
      Vec3 acc = Vec3.zero;
      for (int j = 0; j < particles.Length; ++j) {
        if (j == i)
          continue;

        ref var pj = ref particles[j];
        var mag = -p.mass * (p.pd_ratio + pj.pd_ratio);
        var dk = DeltaKernel(p.loc - pj.loc);
        acc += mag * dk;
      }

      //var gravity_dir = GRAVITY_CENTER - p.loc;
      //acc += GRAVITY_STRENGTH * gravity_dir;
      acc += new Vec3(0, -DOWN_GRAV, 0);

      acc -= DAMPING * p.vel;
      p.acc = acc;

/*
h = smoothing distance
k = equation of state constant
n = polytopic index
f = 1


density(pI.pos) = sum(particles:p)(mass * kernel(pI.pos - pO.pos, h)
pressure = k * density^(1+1/n)

acceleration(pI) = - sum(particles:pJ)(mass * (pI.pdr + p).pdr) * dKernel(pI.pos - pO.pos, h) + fI
*/

    }

    private void UpdatePos(int i, float dt) {
      // kick-drift
      ref var p = ref particles[i];

      var dv = p.acc * dt * .5f;
      p.vel += dv;

      var vel = p.vel * dt;
      var loc = p.loc + vel;

      if (loc.x < 0)
        loc.x = 0;
      else if (loc.x > MAX_X)
        loc.x = MAX_X;
      if (loc.y < 0)
        loc.y = 0;

      // if (loc.y < 0) {
      //   float frdv = Math.Clamp(vel.y / p.loc.y, 0, 1);
      //   loc -= frdv * vel;
      //   p.vel.y = -p.vel.y * RESTITUTION;
      //   var adjVel = new Vec3(p.vel.x, p.vel.y * (1 - frdv), p.vel.z);
      //   loc += dt * adjVel;
      //   vel = loc - p.loc;
      // }

      // if (p.loc.x < 0) {
      //   float frdv = Math.Clamp(vel.x / p.loc.x, 0, 1);
      //   loc -= frdv * vel;
      //   p.vel.x = -p.vel.x * RESTITUTION;
      //   var adjVel = new Vec3(p.vel.x * (1 - frdv), p.vel.y, p.vel.z);
      //   loc += dt * adjVel;
      //   vel = loc - p.loc;
      // }

      // if (p.loc.x > MAX_X) {
      //   float frdv = Math.Clamp(vel.x / (p.loc.x - MAX_X), 0, 1);
      //   loc -= frdv * vel;
      //   p.vel.x = -p.vel.x * RESTITUTION;
      //   var adjVel = new Vec3(p.vel.x * (1 - frdv), p.vel.y, p.vel.z);
      //   loc += dt * adjVel;
      //   vel = loc - p.loc;
      // }

      p.loc = loc;
      p.vel += dv;
      p.Render();
    }

    public void Tick(float dt) {
      // https://philip-mocz.medium.com/create-your-own-smoothed-particle-hydrodynamics-simulation-with-python-76e1cec505f1
      for (int i = 0; i < particles.Length; ++i)
        CalcDensityPressureRatio(i);
      for (int i = 0; i < particles.Length; ++i)
        CalcAcc(i);

      for (int i = 0; i < particles.Length; ++i)
        UpdatePos(i, dt);
    }
  }
}
