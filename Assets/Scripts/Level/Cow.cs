using UnityEngine;

namespace MuhDefense
{
    public enum CowState
    {
        Grazing,
        Carried,
        Returning,
    }

    /// <summary>Eine Kuh auf der Weide – das, was der Spieler beschützen muss.</summary>
    public class Cow : MonoBehaviour
    {
        const float WalkSpeed = 0.4f;
        const float ReturnSpeed = 1.6f;

        public CowState State { get; private set; } = CowState.Grazing;

        LevelManager level;
        Vector3 target;
        float waitTimer;

        public void Init(LevelManager level, Vector3 position)
        {
            this.level = level;
            transform.position = position;
            transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            waitTimer = Random.Range(0f, 3f);
            target = level.RandomPastureSpot();
            BuildModel();
        }

        void Update()
        {
            switch (State)
            {
                case CowState.Grazing:
                    if (waitTimer > 0f)
                    {
                        waitTimer -= Time.deltaTime;
                        return;
                    }
                    if (WalkTo(target, WalkSpeed))
                    {
                        waitTimer = Random.Range(1.5f, 5f);
                        target = level.RandomPastureSpot();
                    }
                    break;

                case CowState.Returning:
                    if (WalkTo(target, ReturnSpeed))
                    {
                        State = CowState.Grazing;
                        waitTimer = Random.Range(1f, 3f);
                        target = level.RandomPastureSpot();
                    }
                    break;
            }
        }

        /// <returns>true, sobald das Ziel erreicht ist.</returns>
        bool WalkTo(Vector3 destination, float speed)
        {
            Vector3 delta = destination - transform.position;
            delta.y = 0f;
            float dist = delta.magnitude;
            float step = speed * Time.deltaTime;
            if (dist <= step)
            {
                transform.position = new Vector3(destination.x, 0f, destination.z);
                return true;
            }
            transform.position += delta / dist * step;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(delta), 8f * Time.deltaTime);
            return false;
        }

        public void Grab()
        {
            State = CowState.Carried;
        }

        /// <summary>Das Alien wurde besiegt – die Kuh läuft zurück auf die Weide.</summary>
        public void Drop(Vector3 position)
        {
            State = CowState.Returning;
            transform.position = new Vector3(position.x, 0f, position.z);
            transform.rotation = Quaternion.identity;
            target = level.RandomPastureSpot();
        }

        void BuildModel()
        {
            var white = new Color(0.97f, 0.97f, 0.95f);
            var black = new Color(0.1f, 0.1f, 0.1f);
            var pink = new Color(1f, 0.7f, 0.75f);

            Visuals.Prim(PrimitiveType.Cube, transform, new Vector3(0f, 0.3f, 0f), new Vector3(0.28f, 0.24f, 0.48f), white, "Körper");
            Visuals.Prim(PrimitiveType.Cube, transform, new Vector3(0f, 0.36f, -0.06f), new Vector3(0.29f, 0.12f, 0.16f), black, "Fleck");
            Visuals.Prim(PrimitiveType.Cube, transform, new Vector3(0f, 0.42f, 0.3f), new Vector3(0.2f, 0.2f, 0.18f), white, "Kopf");
            Visuals.Prim(PrimitiveType.Cube, transform, new Vector3(0f, 0.37f, 0.4f), new Vector3(0.16f, 0.1f, 0.04f), pink, "Maul");
            Visuals.Prim(PrimitiveType.Cube, transform, new Vector3(-0.08f, 0.55f, 0.3f), new Vector3(0.03f, 0.08f, 0.03f), white, "Horn");
            Visuals.Prim(PrimitiveType.Cube, transform, new Vector3(0.08f, 0.55f, 0.3f), new Vector3(0.03f, 0.08f, 0.03f), white, "Horn");
            for (int x = -1; x <= 1; x += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Visuals.Prim(PrimitiveType.Cube, transform, new Vector3(0.09f * x, 0.1f, 0.17f * z), new Vector3(0.06f, 0.2f, 0.06f), white, "Bein");
                }
            }
        }
    }
}
