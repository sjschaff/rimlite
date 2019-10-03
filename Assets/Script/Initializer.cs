using UnityEngine;

namespace BB
{
    public class Initializer : MonoBehaviour
    {
        private Game root;
        void Start() => root = new Game(transform);
        void Update() => root.Update(Time.deltaTime);
    }
}