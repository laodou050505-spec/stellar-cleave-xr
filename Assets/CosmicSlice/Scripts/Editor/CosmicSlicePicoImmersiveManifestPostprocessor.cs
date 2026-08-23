using System.IO;
using System.Xml;
using UnityEditor.Android;

namespace CosmicSlice.Editor
{
    // Unity 6 regenerates its player Activity after the source manifest merge,
    // restoring the Shared Space/freeform flags. This hook intentionally runs
    // after the official PICO manifest hook (order 10000) and makes the final
    // Unity Activity a non-resizable immersive PICO XR entry point.
    public sealed class CosmicSlicePicoImmersiveManifestPostprocessor : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 20000;

        public void OnPostGenerateGradleAndroidProject(string gradleProjectPath)
        {
            var manifestPath = Path.Combine(gradleProjectPath, "src", "main", "AndroidManifest.xml");
            if (!File.Exists(manifestPath))
            {
                return;
            }

            var document = new XmlDocument();
            document.Load(manifestPath);
            var namespaceManager = new XmlNamespaceManager(document.NameTable);
            namespaceManager.AddNamespace("android", "http://schemas.android.com/apk/res/android");
            var activity = document.SelectSingleNode("/manifest/application/activity[@android:name='com.unity3d.player.UnityPlayerActivity']", namespaceManager) as XmlElement;
            if (activity == null)
            {
                return;
            }

            activity.SetAttribute("resizeableActivity", "http://schemas.android.com/apk/res/android", "false");
            RemoveMetaData(activity, namespaceManager, "WindowManagerPreference:FreeformWindowSize");
            RemoveMetaData(activity, namespaceManager, "WindowManagerPreference:FreeformWindowOrientation");
            var layout = activity.SelectSingleNode("layout");
            if (layout != null)
            {
                activity.RemoveChild(layout);
            }

            document.Save(manifestPath);
        }

        private static void RemoveMetaData(XmlElement activity, XmlNamespaceManager namespaceManager, string name)
        {
            var node = activity.SelectSingleNode($"meta-data[@android:name='{name}']", namespaceManager);
            if (node != null)
            {
                activity.RemoveChild(node);
            }
        }
    }
}
