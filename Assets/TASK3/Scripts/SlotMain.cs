using AxGrid;
using AxGrid.Base;
using AxGrid.FSM;
using Task3.Slot.States;
using UnityEngine;

namespace Task3.Slot
{
    public class SlotMain : MonoBehaviourExt
    {
        [SerializeField] private SlotReelView[] reels;

        [OnStart]
        private void StartThis()
        {
            var reelsCount = ResolveReelsCount();
            Settings.Model.Set(SlotContracts.ReelsCountField, Mathf.Max(1, reelsCount));

            Settings.Fsm = new FSM();
            Settings.Fsm.Add(
                new SlotBootstrapState(),
                new SlotIdleState(),
                new SlotAcceleratingState(),
                new SlotSpinningReadyState(),
                new SlotStoppingState()
            );
            Settings.Fsm.Start(SlotBootstrapState.Name);
        }

        [OnUpdate]
        private void UpdateThis()
        {
            Settings.Fsm?.Update(Time.deltaTime);
        }

        private int ResolveReelsCount()
        {
            if (reels != null && reels.Length > 0)
                return reels.Length;

            // Fallback to a scene scan to keep things working even if the list isn't filled.
            return FindObjectsOfType<SlotReelView>().Length;
        }
    }
}
