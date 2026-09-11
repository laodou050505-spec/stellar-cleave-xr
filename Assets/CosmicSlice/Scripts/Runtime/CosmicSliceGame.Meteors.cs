using UnityEngine;

namespace CosmicSlice
{
    public sealed partial class CosmicSliceGame
    {
        // PICO supplies head pose, not torso pose. Capture the entry/recenter heading once;
        // walking moves the emission volumes, while looking sideways never moves the rain.
        private Vector3 _meteorBodyForward = Vector3.forward;
        private void CaptureMeteorBodyHeading()
        {
            var forward = _camera != null ? _camera.transform.forward : Vector3.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude > 0.01f) _meteorBodyForward = forward.normalized;
        }

        private void BuildLateralMeteorShowers()
        {
            _leftMeteorShowerRoot = new GameObject("Left Meteor Rain - Body Side").transform;
            _rightMeteorShowerRoot = new GameObject("Right Meteor Rain - Body Side").transform;
            CaptureMeteorBodyHeading();
            UpdateMeteorShowerAnchors();
            var headShader = meteorHeadShader != null ? meteorHeadShader : Shader.Find("CosmicSlice/MeteorHead");
            var trailShader = meteorTrailShader != null ? meteorTrailShader : Shader.Find("CosmicSlice/MeteorTrail");
            var head = new Material(headShader) { name = "Meteor White Core and Chromatic Halo" };
            var trail = new Material(trailShader) { name = "Meteor Tapered Plasma Ribbon" };
            _leftMeteorShower = CreateLateralMeteorShower(_leftMeteorShowerRoot, -1f, CosmicPalette.Cyan, head, trail);
            _rightMeteorShower = CreateLateralMeteorShower(_rightMeteorShowerRoot, 1f, CosmicPalette.Amber, head, trail);
        }

        private ParticleSystem CreateLateralMeteorShower(Transform parent, float side, Color tint, Material head, Material trail)
        {
            var particles = new GameObject(side < 0f ? "Cyan Meteor Cores" : "Amber Meteor Cores").AddComponent<ParticleSystem>();
            particles.transform.SetParent(parent, false);
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.loop = true;
            main.playOnAwake = true;
            main.duration = 3f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.05f, 1.38f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.052f, 0.084f);
            main.startColor = tint;
            main.maxParticles = 24;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(0.54f, 0.28f, 1.55f);
            var emission = particles.emission;
            emission.rateOverTime = 1.15f;
            var velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = new ParticleSystem.MinMaxCurve(side < 0f ? -0.32f : 0.20f, side < 0f ? -0.20f : 0.32f);
            velocity.y = new ParticleSystem.MinMaxCurve(-2.45f, -1.95f);
            velocity.z = new ParticleSystem.MinMaxCurve(1.75f, 2.35f);
            var color = particles.colorOverLifetime;
            color.enabled = true;
            color.color = CreateParticleGradient(Color.white, tint, 0.95f);
            var size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.25f), new Keyframe(0.08f, 1f), new Keyframe(0.72f, 0.8f), new Keyframe(1f, 0f)));
            var tails = particles.trails;
            tails.enabled = true;
            tails.ratio = 1f;
            tails.lifetime = new ParticleSystem.MinMaxCurve(0.30f, 0.42f);
            tails.minVertexDistance = 0.018f;
            tails.worldSpace = true;
            tails.dieWithParticles = false;
            tails.sizeAffectsWidth = true;
            tails.sizeAffectsLifetime = false;
            tails.inheritParticleColor = true;
            tails.textureMode = ParticleSystemTrailTextureMode.Stretch;
            tails.widthOverTrail = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.72f),
                new Keyframe(0.22f, 0.46f),
                new Keyframe(1f, 0f)));
            var trailFade = new Gradient();
            trailFade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(tint, 0.32f), new GradientColorKey(tint * 0.65f, 1f) },
                new[] { new GradientAlphaKey(0.86f, 0f), new GradientAlphaKey(0.46f, 0.38f), new GradientAlphaKey(0f, 1f) });
            tails.colorOverTrail = trailFade;
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = head;
            renderer.trailMaterial = trail;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.maxParticleSize = 0.045f;
            renderer.sortingOrder = 6;
            // Fine detached embers share the same spatial volume, with a much lower light budget.
            var embers = new GameObject("Fine Detached Embers").AddComponent<ParticleSystem>();
            embers.transform.SetParent(parent, false);
            embers.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var em = embers.main;
            em.startLifetime = new ParticleSystem.MinMaxCurve(0.42f, 0.78f);
            em.startSpeed = 0f;
            em.startSize = new ParticleSystem.MinMaxCurve(0.009f, 0.022f);
            em.startColor = new Color(tint.r, tint.g, tint.b, 0.45f);
            em.maxParticles = 24;
            em.simulationSpace = ParticleSystemSimulationSpace.World;
            var es = embers.shape;
            es.shapeType = ParticleSystemShapeType.Box;
            es.scale = new Vector3(0.72f, 1.6f, 1.9f);
            es.position = new Vector3(0f, -0.8f, 0f);
            var ee = embers.emission; ee.rateOverTime = 2.4f;
            var ev = embers.velocityOverLifetime;
            ev.enabled = true; ev.space = ParticleSystemSimulationSpace.Local;
            ev.x = side * 0.34f; ev.y = -1.05f; ev.z = 0.58f;
            var ec = embers.colorOverLifetime;
            ec.enabled = true; ec.color = CreateParticleGradient(Color.white, tint, 0.5f);
            embers.GetComponent<ParticleSystemRenderer>().sharedMaterial = head;
            particles.Play(true); embers.Play(true);
            return particles;
        }

        private void UpdateMeteorShowerAnchors()
        {
            if (_leftMeteorShowerRoot == null || _rightMeteorShowerRoot == null) return;
            var eye = _camera != null ? _camera.transform.position : CosmicSliceDesign.GameplayPlayerRootPosition + Vector3.up * CosmicSliceDesign.GameplayEyeHeight;
            var ceilingY = _portalRoot != null ? _portalRoot.position.y : CosmicSliceDesign.PortalHeightAboveArena;
            var center = new Vector3(eye.x, Mathf.Max(eye.y + 0.5f, Mathf.Min(ceilingY - 0.1f, eye.y + 1.5f)), eye.z);
            var right = Vector3.Cross(Vector3.up, _meteorBodyForward);
            var orientation = Quaternion.LookRotation(_meteorBodyForward, Vector3.up);
            _leftMeteorShowerRoot.SetPositionAndRotation(center - right * 1.85f, orientation);
            _rightMeteorShowerRoot.SetPositionAndRotation(center + right * 1.85f, orientation);
        }

        private void EmitMeteorShowerBeat(long beat)
        {
            if (_leftMeteorShower == null || _rightMeteorShower == null) return;
            if (beat % 4 == 0)
            {
                EmitMeteorShower(_leftMeteorShower, _leftMeteorShowerRoot, -1f, 2, (int)(beat & 7));
                EmitMeteorShower(_rightMeteorShower, _rightMeteorShowerRoot, 1f, 2, (int)((beat + 3) & 7));
            }
            else if ((beat & 1) == 0) EmitMeteorShower(_rightMeteorShower, _rightMeteorShowerRoot, 1f, 1, (int)(beat & 7));
            else EmitMeteorShower(_leftMeteorShower, _leftMeteorShowerRoot, -1f, 1, (int)(beat & 7));
        }

        private static void EmitMeteorShower(ParticleSystem particles, Transform anchor, float side, int count, int phase)
        {
            if (particles == null || anchor == null) return;
            for (var index = 0; index < count; index++)
            {
                var sequence = index + phase * 0.37f;
                particles.Emit(new ParticleSystem.EmitParams
                {
                    position = anchor.TransformPoint(new Vector3(Mathf.Sin(sequence * 2.31f) * 0.3f, Mathf.Repeat(sequence * 0.19f, 0.2f), Mathf.Cos(sequence * 1.73f) * 0.9f)),
                    startLifetime = 1.08f + Mathf.Repeat(sequence * 0.13f, 0.28f),
                    startSize = 0.052f + Mathf.Repeat(sequence * 0.009f, 0.032f),
                    startColor = side < 0f ? CosmicPalette.Cyan : CosmicPalette.Amber
                }, 1);
            }
        }
    }
}
