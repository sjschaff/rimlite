using BB;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;
using Vec3 = UnityEngine.Vector3;
using Vec3I = UnityEngine.Vector3Int;

namespace BB {
  class AetherTweak : MonoBehaviour {

    private bool _restart = false;
    public bool restart = false;

    public float GAS_CONST = 200f;
    public float VISCOSITY = 3f;
    public float BOUNDS_DAMPING = -.9f;
    public float VEL_DAMPING = 1f;

    public float MIN_X = 0;//4;
    public float MAX_X = 20;//16;
    public float INIT_MIN_X = 7.5f;
    public float INIT_MAX_X = 12.5f;//12f;
    public float INIT_MIN_Y = 1f;//7.5f;
    public float INIT_JITTER = 1/16f;
    public int INIT_PARTICLES = 200;

    private bool do_restart = false;

    void UpdateParams() {

      CheckParam(GAS_CONST, ref aether.GAS_CONST);
      CheckParam(VISCOSITY, ref aether.VISCOSITY);
      CheckParam(BOUNDS_DAMPING, ref aether.BOUNDS_DAMPING);
      CheckParam(VEL_DAMPING, ref aether.VEL_DAMPING);

      CheckParam(MIN_X, ref aether.MIN_X, true);
      CheckParam(MAX_X, ref aether.MAX_X, true);
      CheckParam(INIT_MIN_X, ref aether.INIT_MIN_X, true);
      CheckParam(INIT_MAX_X, ref aether.INIT_MAX_X, true);
      CheckParam(INIT_MIN_Y, ref aether.INIT_MIN_Y, true);
      CheckParam(INIT_JITTER, ref aether.INIT_JITTER, true);
      CheckParam(INIT_PARTICLES, ref aether.INIT_PARTICLES, true);

      CheckParam(restart, ref _restart, true);
    }

    void CheckParam<T>(T src, ref T dst, bool restart = false) {
      T d = dst;

      if (!EqualityComparer<T>.Default.Equals(src, d)) {
        dst = src;
        if (restart)
          do_restart = true;
      }
    }


    private AetherSPH_2 aether;

    public AetherTweak() {

    }

    public void Start() {
      aether = new AetherSPH_2();
    }

    public void Update() {
      UpdateParams();
      if (do_restart) {
        aether.Restart();
        do_restart = false;
      }
      aether.Update(Time.deltaTime);
    }
  }
}
