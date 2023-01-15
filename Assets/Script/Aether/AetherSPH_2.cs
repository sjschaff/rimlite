using BB;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;
using Vec3 = UnityEngine.Vector3;
using Vec3I = UnityEngine.Vector3Int;

namespace BB
{
  public class AetherSPH_2 : AetherTestBase {
    public static void BreakOnNan(Vec3 v) {
        if (float.IsNaN(v.x) || float.IsNaN(v.x) || float.IsNaN(v.z)) {
          var x = 5 + 7;
        }
    }

    private struct Particle {
      public SpriteObj obj;
      public float mass;

      public Vec3 pos;
      public Vec3 vel;

      // #debug
      public Vec3 fPressure;
      public Vec3 fViscosity;


      // temp
      public float density;
      public float pressure;
      public Vec3 force;

      public void Render() {
        BreakOnNan(pos);
        if (obj.xf != null) // #debug
          obj.Render(pos);
      }
    }

    // BOUNDS
    public float MIN_X = 0;//4;
    public float MAX_X = 20;//16;
    public float MIN_Y = 0;


    // INITIAL STATE
    public float INIT_MIN_X = 1f;
    public float INIT_MAX_X = 6f;//12f;
    public float INIT_MIN_Y = 1f;//7.5f;
    public float INIT_JITTER = 1/16f;
    public int INIT_PARTICLES = 2000;


    // SIM PROPERTIES
    private const float PT_SIZE = 0.1f;
    private const float PT_SMOOTHING = PT_SIZE * 4;
    private const float PT_MASS = 0.08f;//2.5f;

    private readonly Kernel K_POLY6 = Kernel.GetPoly6(PT_SMOOTHING);
    private readonly Kernel K_SPIKEY = Kernel.GetSpikey(PT_SMOOTHING);
    private readonly Kernel K_VISCOSITY = Kernel.GetViscosity(PT_SMOOTHING);
    private readonly float REST_DENSITY = PT_MASS * Kernel.GetPoly6(PT_SMOOTHING).W(Vec3.zero);//;300; // TODO: compute somehow?

    private static Gradient _Gradient() {
      var g = new Gradient();
      g.SetKeys(
        new GradientColorKey[] {
          new GradientColorKey(new Color(.05f, .05f, 1f), 0f),
          new GradientColorKey(Color.blue, 0.0333333f),
          new GradientColorKey(Color.magenta, 1f),
        },
        new GradientAlphaKey[] {}
      );

      return g;
    }

    private readonly Gradient gradient = _Gradient();
    private const float COLR_SCALE = 6f;

    // private readonly float COLR_SCALING_DENSITY = 1.1f * PT_MASS * Kernel.GetPoly6(PT_SMOOTHING).W(Vec3.zero);
    // private readonly float COLR_SCALING_DENSITY2 = 4f * PT_MASS * Kernel.GetPoly6(PT_SMOOTHING).W(Vec3.zero);


    public float GAS_CONST = 200f;//.0001f * 8.3145f * (273.15f + 30f); // const for equation of state * temp
    public float VISCOSITY = 3f;//200f; // TODO:
    public float BOUNDS_DAMPING = -.9f;
    public float BOUNDS_FRICTION = .9f;
    public float VEL_DAMPING = 1;//.99f;
    private readonly Vec3 GRAVITY = 9.8f * new Vec3(0, -1, 0);


    private Vec3 GetExternalForces(in Particle p) {
      var grav = GRAVITY * p.density;

      return grav;
    }

    private /*readonly*/ Particle[] particles;
    private readonly SpatialHash<int> hash = new SpatialHash<int>(PT_SMOOTHING);

    private readonly Sprite sprite;
    private Line bounds;

    private void DebugSim() {
      // DEBUG
      particles = new Particle[4];
      for (int i = 0; i < particles.Length; ++i)
        particles[i].mass = PT_MASS;

      particles[0].pos = new Vec3(0, 0, 0);
      particles[1].pos = new Vec3(.05f, 0, 0);

      particles[2].pos = new Vec3(0, 10, 0);
      particles[3].pos = new Vec3(.1f, 10, 0);

      CalcDensityPressure();
      CalcForces();

      void LogParticle(int i) {
        Debug.Log($"particle {i}: {particles[i].pos}\n" +
          $"  density: {particles[i].density}\n" +
          $"  pressure: {particles[i].pressure}\n" +
          $"  fPressure: {particles[i].fPressure} => {particles[i].fPressure / particles[i].density}\n" +
          $"  fViscosity: {particles[i].fViscosity} => {particles[i].fViscosity / particles[i].density}\n");

      }

      for (int i = 0; i < particles.Length; ++i)
        LogParticle(i);
    }

    public AetherSPH_2() {
      sprite = this.CreateParticleSprite(AssetSrc.CreateFlatTex(Color.magenta), 1/16f);
      Init();
    }

    private bool do_restart = false;

    public void Restart() {
      do_restart = true;
    }

    private void Init() {
      if (particles != null)
        for (int i = 0; i < particles.Length; ++i)
          particles[i].obj.Destroy();

      // DebugSim();

      if (bounds != null)
        bounds.Destroy();

      InitBoundsRender();
      particles = InitParticles(sprite);

      do_restart = false;
    }

    private void InitBoundsRender() {
      bounds = AssetSrc.singleton.CreateLine(root_xf, "bounds", RenderLayer.OverMinion.Layer(200), Color.white, 1/16f, false, false);

      Vec2[] pts = {
        new Vec2(MIN_X, MIN_Y + 100),
        new Vec2(MIN_X, MIN_Y),
        new Vec2(MAX_X, MIN_Y),
        new Vec2(MAX_X, MIN_Y + 100)};
      bounds.SetPts(pts);
    }

    private Particle[] InitParticles(Sprite sprite) {
      var rand = new System.Random(0);
      var pos = new Vec2(INIT_MIN_X, INIT_MIN_Y);

      float gap = PT_SIZE * 4;

      var particles = new Particle[INIT_PARTICLES];
      for (int i = 0; i < particles.Length; ++i) {
        var jitter = ((float)rand.NextDouble() - .5f) * PT_SIZE * INIT_JITTER;
        var colr = i % 2 == 0 ? Color.blue : Color.red;
        var p = InitParticle(sprite, pos + new Vec2(jitter, 0), colr);
        particles[i] = p;



        pos.x += gap;
        if (pos.x > INIT_MAX_X) {
          pos.x = INIT_MIN_X;
          pos.y += gap;
        }
      }

      return particles;
    }

    private Particle InitParticle(Sprite sprite, Vec2 pos, Color colr) {
      var p = new Particle();
      p.obj = CreateSpriteObj(sprite);

      // var line = AssetSrc.singleton.CreateLine(p.obj.xf, "circle", RenderLayer.OverMinion.Layer(200), colr, 1/64f, true, false);
      // line.SetCircle(new Circle(Vec2.zero, PT_SMOOTHING),16);
      // p.obj.objs.Add(line.transform.gameObject);

      var line2 = AssetSrc.singleton.CreateLine(
        root_xf, "circle", RenderLayer.OverMinion.Layer(200), Color.cyan, .2f/*1/64f*/, true, false,
        useDefaultMaterial: true);
      line2.SetCircle(new Circle(Vec2.zero, PT_SIZE),16);
      // p.obj.objs.Add(line2.transform.gameObject);
      p.obj.xf = line2.transform;

      p.mass = PT_MASS;
      p.pos = pos;
      p.Render();

      return p;
    }

    private void CalcDensityPressure() {
      for (int i = 0; i < particles.Length; ++i) {
        ref var p = ref particles[i];
        var density = 0f;

        foreach (int j in hash.GetNeighbors(p.pos, PT_SMOOTHING, false)) {
          ref var pj = ref particles[j];
          density += pj.mass * K_POLY6.W(pj.pos - p.pos);
        }

        p.density = density;
        p.pressure = GAS_CONST * (density - REST_DENSITY);
      }
    }

    private void CalcForces() {
      for (int i = 0; i < particles.Length; ++i) {
        ref var p = ref particles[i];
        Vec3 fPressure = Vec2.zero;
        Vec3 fViscosity = Vec2.zero;

        foreach (int j in hash.GetNeighbors(p.pos, PT_SMOOTHING, false)) {
          if (j == i)
            continue;

          ref var pj = ref particles[j];
          var dist = pj.pos - p.pos;

          if (dist.sqrMagnitude > float.Epsilon)
            fPressure += -pj.mass * ((p.pressure + pj.pressure) / (2 * pj.density)) * K_SPIKEY.Gradient(dist);

          fViscosity += pj.mass * VISCOSITY * ((pj.vel - p.vel) / pj.density) * K_VISCOSITY.Laplacian(dist);
          BreakOnNan(fViscosity);
        }

        var fExternal = GetExternalForces(p);
        p.fPressure = fPressure; // #debug
        p.fViscosity = fViscosity; // #debug
        p.force = fExternal + fPressure + fViscosity;
      }
    }

    private void UpdatePos(float dt) {
      for (int i = 0; i < particles.Length; ++i) {
        ref var p = ref particles[i];
        var acc = p.force / p.density;
        var hAcc = dt * acc * .5f;

        p.vel += hAcc;
        p.pos += p.vel * dt;
        p.vel += hAcc;

        // collision
        if (p.pos.x - PT_SIZE < MIN_X) {
          p.pos.x = MIN_X + PT_SIZE;
          p.vel.x *= BOUNDS_DAMPING;
          p.vel.y *= BOUNDS_FRICTION;
        }
        if (p.pos.x + PT_SIZE > MAX_X) {
          p.pos.x = MAX_X - PT_SIZE;
          p.vel.x *= BOUNDS_DAMPING;
          p.vel.y *= BOUNDS_FRICTION;
        }
        if (p.pos.y - PT_SIZE < MIN_Y) {
          p.pos.y = MIN_Y + PT_SIZE;
          p.vel.y *= BOUNDS_DAMPING;
          p.vel.x *= BOUNDS_FRICTION;
        }

        p.vel *= VEL_DAMPING;

        var fr_grad = Mathf.Clamp((p.density - REST_DENSITY) / (COLR_SCALE - 1f), 0f, 1f);
        Color colr = gradient.Evaluate(fr_grad);

        var line = p.obj.xf.GetComponent<LineRenderer>();
        line.startColor = colr;
        line.endColor = colr;

        // This is hilariously dumb
        // p.obj.xf.GetComponent<LineRenderer>().material = AssetSrc.singleton.lineMaterials.Get(colr);
        p.Render();
      }

    }

    public bool inject = false;
    public bool remove = false;
    public Vec2 mouse_pos = Vec2.zero;

    private const int INJECT_RATE = 480;
    private const float MANIP_RADIUS = 1.5f;


    protected override void Tick(float dt) {
      if (do_restart)
        Init();


      if (remove) {
        HashSet<int> removals = new HashSet<int>(hash.GetNeighbors(mouse_pos, MANIP_RADIUS, true));
        Particle[] new_particles = new Particle[particles.Length - removals.Count];
        int j = 0;
        for (int i = 0; i < particles.Length; ++i) {
          if (!removals.Contains(i)) {
            new_particles[j] = particles[i];
            j++;
          } else {
            particles[i].obj.Destroy();
          }
        }

        particles = new_particles;
      }

      if (inject) {
        var c = particles.Length;
        var injection = Mathf.RoundToInt(dt * INJECT_RATE);
        var total = c + injection;
        var vel = GRAVITY * 2f;
        if (mouse_pos.y < 5)
          vel = Vec3.zero;
        Array.Resize(ref particles, total);
        for (int i = c; i < total; ++i) {
          var pt = UnityEngine.Random.insideUnitCircle * MANIP_RADIUS + mouse_pos;
          particles[i] = InitParticle(sprite, pt, Color.white);
          particles[i].vel = vel;
        }
      }

      hash.Clear();
      for (int i = 0; i < particles.Length; ++i)
        hash.Add(particles[i].pos, i);

      CalcDensityPressure();
      CalcForces();
      UpdatePos(dt);
    }
  }
}
