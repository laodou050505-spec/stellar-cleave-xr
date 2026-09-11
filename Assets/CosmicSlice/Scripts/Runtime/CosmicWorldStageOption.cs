using UnityEngine;
namespace CosmicSlice
{
    public sealed class CosmicWorldStageOption : MonoBehaviour
    {
        public int Index { get; private set; }
        public void Configure(int index) => Index = index;
    }
}
