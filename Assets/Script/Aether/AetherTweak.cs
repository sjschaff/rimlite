using BB;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


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

    public class AetherTool : UITool {
      private readonly AetherSPH_2 aether;

      private Dictionary<PointerEventData.InputButton, bool> button_states = new Dictionary<PointerEventData.InputButton, bool>();
      private Vec2 mouse_pos;

      public AetherTool(GameController ctrl, AetherSPH_2 aether) : base(ctrl) {
        this.aether = aether;
      }


      public override void RawMouseDown(PointerEventData.InputButton button, Vec2 pos) {
        button_states[button] = true;
        mouse_pos = pos;
      }

      public override void RawMouseUp(PointerEventData.InputButton button, Vec2 pos) {
        button_states[button] = false;
        mouse_pos = pos;
      }

      public override void RawMouseMove(Vec2 pos)
      {
        mouse_pos = pos;
      }

      public void Update() {
        aether.inject = button_states.GetOrDefault(PointerEventData.InputButton.Left, false);
        aether.remove = button_states.GetOrDefault(PointerEventData.InputButton.Right, false);
        aether.mouse_pos = mouse_pos;
      }
    }

    private AetherTool tool;

    public void Awake() {
    }

    public void Start() {
      aether = new AetherSPH_2();
    }


    public void Update() {
      var ctrl = GameController.ctrl;
      if (tool == null)
        tool = new AetherTool(ctrl, aether);
      ctrl.ReplaceTool(tool);

      UpdateParams();
      if (do_restart) {
        aether.Restart();
        do_restart = false;
      }

      tool.Update();
      aether.Update(Time.deltaTime);
    }
  }
}
