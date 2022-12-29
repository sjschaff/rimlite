using BB;
using System;
using System.Collections;
using UnityEngine;


using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;
using Vec3 = UnityEngine.Vector3;
using Vec3I = UnityEngine.Vector3Int;

namespace BB {
  public delegate float KernelFn(in Vec3 dist);
  public delegate Vec3 KernelGradientFn(in Vec3 dist);

  public struct Kernel {
    public readonly KernelFn W;
    public readonly KernelGradientFn Gradient;
    public readonly KernelFn Laplacian;

    public Kernel(KernelFn kernel, KernelGradientFn grad, KernelFn laplacian) {
      this.W = kernel;
      this.Gradient = grad;
      this.Laplacian = laplacian;
    }

    public static Kernel GetPoly6(float smoothing_len) {
      /*
        poly6(r,h) = 315/64*π*(h^9) *  { (h^2 - r^2)^3     0 ≤ r ≤ h
                                       { 0

        ∇Poly6(r,h) = -r * (945/32*π*h^9) * (h^2 - r^2)^2

        ∇∇Poly6(r,h) = (945/8*π*h^9) * (h^2 - r^2) * (r^2 - (3/4)*(h^2 - r^2))
      */

      float H = smoothing_len;
      float H2 = smoothing_len * smoothing_len;
      float C_KERN = (float)(315 / (64 * Math.PI * Math.Pow(H, 9)));
      float C_GRAD = (float)(945 / (32 * Math.PI * Math.Pow(H, 9)));
      float C_LAPL = (float)(945 /  (8 * Math.PI * Math.Pow(H, 9)));

      KernelFn kernel = (in Vec3 dist) => {
        float R2 = dist.sqrMagnitude;
        if (R2 >= H2)
          return 0f;

        float X = H2 - R2;
        return C_KERN * (X * X * X);
      };

      KernelGradientFn gradient = (in Vec3 dist) => {
        float R2 = dist.sqrMagnitude;
        if (R2 >= H2)
          return new Vec3(0, 0, 0);

        float X = H2 - R2;
        float M = C_GRAD * X * X;
        return dist * M;
      };

      KernelFn laplacian = (in Vec3 dist) => {
        float R2 = dist.sqrMagnitude;
        if (R2 >= H2)
          return 0;

        float X = H2 - R2;
        return C_LAPL * X * (R2 - (.75f * X));
      };

      return new Kernel(kernel, gradient, laplacian);
    }

    public static Kernel GetSpikey(float smoothing_len) {
      float H = smoothing_len;
      float H2 = H * H;
      float C_KERN = (float)(15 / (Math.PI * Math.Pow(H, 6)));
      float C_GRAD = (float)(45 / (Math.PI * Math.Pow(H, 6)));

      KernelFn kernel = (in Vec3 dist) => { // listed
        float R2 = dist.sqrMagnitude;
        if (R2 >= H2)
          return 0;

        float R = MathF.Sqrt(R2);
        float X = H - R;
        return C_KERN * X * X * X;
      };

      KernelGradientFn gradient = (in Vec3 dist) => {
        float R2 = dist.sqrMagnitude;
        if (R2 >= H2 || R2 < float.Epsilon)
          return new Vec3(0, 0, 0);

        float R = MathF.Sqrt(R2);
        float X = H - R;
        float M = C_GRAD * X * X / R;
        return dist * M;
      };

      // TODO:
      KernelFn laplacian = null;

      return new Kernel(kernel, gradient, laplacian);
    }

    public static Kernel GetViscosity(float smoothing_len) {
      float H = smoothing_len;
      float H2 = H * H;

      float C_KERN = (float)(15 / (2 * Math.PI * Math.Pow(H, 3)));
      float C_KERN_C3 = -1f / (2 * MathF.Pow(H, 3));
      float C_KERN_C2 = 1f / (H * H);
      float C_KERN_C1 = H / 2f;

      float C_GRAD = C_KERN;
      float C_GRAD_C3 = 3 * C_KERN_C3;
      float C_GRAD_C2 = 2 * C_KERN_C2;
      float C_GRAD_C1 = -H / 2f;

      float C_LAPL = (float)(45 / (Math.PI * Math.Pow(H, 6)));

      KernelFn kernel = (in Vec3 dist) => {
        float R2 = dist.sqrMagnitude;
        if (R2 >= H2)
          return 0;

        float R = MathF.Sqrt(R2);
        float R3 = R * R2;

        float X = R3 * C_KERN_C3 + R2 * C_KERN_C2 + C_KERN_C1 / R - 1;
        return C_KERN * X;
      };

      KernelGradientFn gradient = (in Vec3 dist) => {
        float R2 = dist.sqrMagnitude;
        if (R2 >= H2)
          return new Vec3(0, 0, 0);

        float R = MathF.Sqrt(R2);
        float R3 = R * R2;
        float X = C_GRAD_C3 * R + C_GRAD_C2 + C_GRAD_C1 / R3;
        float M = C_GRAD * X;
        return dist * M;
      };

      // TODO:
      KernelFn laplacian = (in Vec3 dist) => {
        float R2 = dist.sqrMagnitude;
        if (R2 >= H2)
          return 0;

        float R = MathF.Sqrt(R2);
        float X = H - R;
        return C_LAPL * X;
      };

      return new Kernel(kernel, gradient, laplacian);
    }
  }
}
