using AxGrid.Base;
using AxGrid.Model;
using UnityEngine;

namespace Task3.Slot
{
    public class SlotStopFxView : MonoBehaviourExtBind
    {
        [SerializeField] private ParticleSystem stopFx;

        [OnAwake]
        private void AwakeThis()
        {
            if (stopFx == null)
                stopFx = GetComponent<ParticleSystem>();

            if (stopFx == null)
                return;

            var main = stopFx.main;
            main.playOnAwake = false;
            stopFx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        [Bind(SlotContracts.EventVisualStopped)]
        private void OnVisualStopped()
        {
            if (stopFx == null)
                return;

            stopFx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            stopFx.Play(true);
        }
    }
}
