using UnityEngine;

namespace BB
{
    public class Initializer : MonoBehaviour
    {
        private GameController root;
        void Start() => root = new GameController(transform);
        void Update() => root.Update(Time.deltaTime);
    }
}