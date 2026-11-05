using UnityEngine;
using UnityEngine.Rendering;

namespace LabGame.World
{
    /// <summary>
    /// Осадки: две процедурные системы частиц (дождь и снег), которые
    /// включаются по интенсивности погоды и летят вокруг игрока.
    /// </summary>
    public class WeatherVisuals : MonoBehaviour
    {
        [Header("Следование")]
        [SerializeField] private float heightOffset = 10f;
        [SerializeField] private float areaSize = 40f;

        [Header("Плотность")]
        [SerializeField] private float maxRainRate = 2500f;
        [SerializeField] private float maxSnowRate = 450f;

        private ParticleSystem rain;
        private ParticleSystem snow;
        private Transform follow;
        private bool initialized;

        public bool RainActive => rain != null && rain.gameObject.activeSelf;
        public bool SnowActive => snow != null && snow.gameObject.activeSelf;

        private void Awake() => Initialize();

        /// <summary>
        /// Создаёт системы частиц, если их ещё нет. Идемпотентно,
        /// поэтому работает и в редакторе, и в рантайме.
        /// </summary>
        public void Initialize()
        {
            if (initialized) return;

            initialized = true;
            BuildSystems();
        }

        /// <summary>Кого догонять осадками (обычно игрок или камера).</summary>
        public void SetFollowTarget(Transform target)
        {
            if (target != null) follow = target;
        }

        private void BuildSystems()
        {
            if (rain == null)
                rain = CreateSystem("Rain", new Color(0.62f, 0.70f, 0.82f, 0.55f), 6000, true);

            if (snow == null)
                snow = CreateSystem("Snow", new Color(1f, 1f, 1f, 0.9f), 4000, false);
        }

        private void LateUpdate()
        {
            FollowTarget();
        }

        /// <summary>Применить интенсивность осадков и ветер.</summary>
        public void Apply(float rainAmount, float snowAmount, float wind)
        {
            ApplySystem(rain, rainAmount, maxRainRate, wind, 8f, -22f);
            ApplySystem(snow, snowAmount, maxSnowRate, wind, 3f, -1.8f);
        }

        /// <summary>
        /// Скорость излучения с учётом лимита частиц.
        /// Если частица живёт L секунд, а всего их может быть N, то больше N/L
        /// в секунду излучать нельзя: иначе система упирается в лимит и
        /// «замолкает», пока не погибнут старые частицы — визуально это
        /// выглядит как снег, идущий с паузами.
        /// </summary>
        public static float SafeEmissionRate(float amount, float maxRate, float lifetime, int maxParticles)
        {
            float requested = Mathf.Max(0f, amount) * Mathf.Max(0f, maxRate);
            float limit = Mathf.Max(0, maxParticles) / Mathf.Max(0.1f, lifetime);

            return Mathf.Min(requested, limit);
        }

        private void ApplySystem(ParticleSystem system, float amount, float maxRate, float wind,
            float windSpeed, float fallSpeed)
        {
            if (system == null) return;

            bool active = amount > 0.02f;

            if (system.gameObject.activeSelf != active)
                system.gameObject.SetActive(active);

            if (!active) return;

            var main = system.main;
            float lifetime = Mathf.Max(0.1f, main.startLifetime.constant);

            var emission = system.emission;
            emission.rateOverTime = SafeEmissionRate(amount, maxRate, lifetime, main.maxParticles);

            var velocity = system.velocityOverLifetime;
            velocity.x = new ParticleSystem.MinMaxCurve(wind * windSpeed);
            velocity.z = new ParticleSystem.MinMaxCurve(wind * windSpeed * 0.35f);
            velocity.y = new ParticleSystem.MinMaxCurve(fallSpeed * (0.8f + amount * 0.4f));

            if (!system.isPlaying) system.Play();
        }

        private void FollowTarget()
        {
            if (follow == null)
            {
                var camera = Camera.main;
                if (camera != null) follow = camera.transform;
            }

            if (follow == null) return;

            transform.position = follow.position + Vector3.up * heightOffset;
        }

        // ─────────────────────────────────────────────────────────────
        //  Создание систем частиц
        // ─────────────────────────────────────────────────────────────

        private ParticleSystem CreateSystem(string name, Color color, int maxParticles, bool stretch)
        {
            // Система может уже быть в сцене — тогда используем её,
            // но настройки всё равно приводим к текущим значениям из кода.
            var existing = transform.Find(name);
            var system = existing != null ? existing.GetComponent<ParticleSystem>() : null;
            bool created = system == null;

            var go = system != null ? system.gameObject : new GameObject(name);

            if (system == null)
            {
                go.transform.SetParent(transform, false);
                system = go.AddComponent<ParticleSystem>();
            }

            var main = system.main;
            main.loop = true;
            main.playOnAwake = false;
            main.maxParticles = maxParticles;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startSpeed = 0f;
            main.startSize = stretch ? 0.06f : 0.10f;
            main.startLifetime = stretch ? 1.6f : 7f;
            main.startColor = color;
            main.gravityModifier = 0f;

            var emission = system.emission;
            emission.rateOverTime = 0f;

            var shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(areaSize, 2f, areaSize);

            var velocity = system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(0f);
            velocity.y = new ParticleSystem.MinMaxCurve(-12f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f);

            var renderer = go.GetComponent<ParticleSystemRenderer>();

            if (renderer != null)
            {
                renderer.renderMode = stretch
                    ? ParticleSystemRenderMode.Stretch
                    : ParticleSystemRenderMode.Billboard;

                renderer.velocityScale = 0.03f;
                renderer.lengthScale = 2.5f;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;

                if (created || renderer.sharedMaterial == null)
                {
                    var material = CreateMaterial();
                    if (material != null) renderer.material = material;
                }
            }

            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            go.SetActive(false);

            return system;
        }

        private static Material CreateMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");

            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");

            if (shader == null)
            {
                Debug.LogWarning("[Weather] не найден шейдер для частиц осадков");
                return null;
            }

            var material = new Material(shader) { name = "WeatherParticles" };

            // Делаем материал прозрачным, иначе капли выглядят сплошными плитками.
            // Цвет самой капли задаётся через ParticleSystem.startColor.
            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = (int)RenderQueue.Transparent;
            }

            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);

            return material;
        }
    }
}
