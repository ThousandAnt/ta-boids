using UnityEngine;

namespace ThousandAnt.Boids.Common {

    public class SeparateOnIncomingAsteroid : MonoBehaviour {

#pragma warning disable 649
        [SerializeField]
        private float minDistanceToSeparate = 30f;
        [SerializeField]
        private float separationDistance = 20f;
        [SerializeField]
        private float distanceAhead = 50f;
        [SerializeField]
        private float speed = 10f;
        [SerializeField]
        private GameObject asteroidPrefab;

        [SerializeField]
        private Transform runner;

        [SerializeField]
        private KeyCode key = KeyCode.Space;
#pragma warning restore 649

        private Transform asteroid;
        private Vector3 velocity;
        private Vector3 rotationAxis;
        private float cachedSeparationDistance;
        private Runner _runner;

        void Start() {
            _runner = runner.GetComponent<Runner>();
            cachedSeparationDistance = _runner.SeparationDistance;
        }

        void Update() {
            if (Input.GetKeyUp(key) && asteroid == null) {
                var position = runner.position + runner.forward * 50f;
                var obj = Object.Instantiate(asteroidPrefab, position, Quaternion.identity);
                asteroid = obj.transform;
                rotationAxis = Random.rotation.eulerAngles;
            }

            if (asteroid) {
                var v = (runner.position - asteroid.position);
                var vFlipped = -v.normalized;
                var dot = Vector3.Dot(vFlipped, runner.forward);
                var mag = v.sqrMagnitude;

                asteroid.Rotate(rotationAxis * Time.deltaTime, Space.Self);

                if (mag <= minDistanceToSeparate * minDistanceToSeparate) {
                    _runner.SeparationDistance = separationDistance;
                }

                asteroid.position += Mathf.Sign(dot) * v.normalized * speed * Time.deltaTime;

                var halfDistance = distanceAhead / 2;

                if (dot < 0 && mag > halfDistance * halfDistance) {
                    _runner.SeparationDistance = cachedSeparationDistance;
                    Destroy(asteroid.gameObject);
                    asteroid = null;
                }
            }
        }
    }
}
