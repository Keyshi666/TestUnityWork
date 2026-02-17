using AxGrid;
using AxGrid.FSM;
using AxGrid.Model;

namespace Task3.Slot.States
{
    [State(Name)]
    public class SlotBootstrapState : FSMState
    {
        public const string Name = "SltBoot";

        [Enter]
        private void EnterThis()
        {
            Settings.Model.Set(SlotContracts.StartButtonEnableField, true);
            Settings.Model.Set(SlotContracts.StopButtonEnableField, false);
            Parent.Change(SlotIdleState.Name);
        }
    }

    [State(Name)]
    public class SlotIdleState : FSMState
    {
        public const string Name = "SltIdle";

        [Enter]
        private void EnterThis()
        {
            Settings.Model.Set(SlotContracts.StartButtonEnableField, true);
            Settings.Model.Set(SlotContracts.StopButtonEnableField, false);
        }

        [Bind("OnBtn")]
        private void OnButton(string buttonName)
        {
            if (buttonName == SlotContracts.StartButtonName)
                Parent.Change(SlotAcceleratingState.Name);
        }
    }

    [State(Name)]
    public class SlotAcceleratingState : FSMState
    {
        public const string Name = "SltAccel";
        private const float StopUnlockDelay = 3f;

        [Enter]
        private void EnterThis()
        {
            Settings.Model.Set(SlotContracts.StartButtonEnableField, false);
            Settings.Model.Set(SlotContracts.StopButtonEnableField, false);
            Invoke(SlotContracts.EventStartSpin);
        }

        [One(StopUnlockDelay)]
        private void UnlockStop()
        {
            Parent.Change(SlotSpinningReadyState.Name);
        }
    }

    [State(Name)]
    public class SlotSpinningReadyState : FSMState
    {
        public const string Name = "SltSpin";

        [Enter]
        private void EnterThis()
        {
            Settings.Model.Set(SlotContracts.StartButtonEnableField, false);
            Settings.Model.Set(SlotContracts.StopButtonEnableField, true);
        }

        [Bind("OnBtn")]
        private void OnButton(string buttonName)
        {
            if (buttonName == SlotContracts.StopButtonName)
                Parent.Change(SlotStoppingState.Name);
        }
    }

    [State(Name)]
    public class SlotStoppingState : FSMState
    {
        public const string Name = "SltStop";

        [Enter]
        private void EnterThis()
        {
            Settings.Model.Set(SlotContracts.StartButtonEnableField, false);
            Settings.Model.Set(SlotContracts.StopButtonEnableField, false);
            var reelsCount = Settings.Model.GetInt(SlotContracts.ReelsCountField, 1);
            Settings.Model.Set(SlotContracts.PendingStopsField, reelsCount);
            Invoke(SlotContracts.EventStopSpin);
        }

        [Bind(SlotContracts.EventVisualStopped)]
        private void OnVisualStopped()
        {
            var pendingStops = Settings.Model.GetInt(SlotContracts.PendingStopsField, 1) - 1;
            Settings.Model.Set(SlotContracts.PendingStopsField, pendingStops);
            if (pendingStops <= 0)
                Parent.Change(SlotIdleState.Name);
        }
    }
}
