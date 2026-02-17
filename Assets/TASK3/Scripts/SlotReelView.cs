using System.Collections.Generic;
using AxGrid;
using AxGrid.Base;
using AxGrid.Model;
using UnityEngine;
using UnityEngine.UI;

namespace Task3.Slot
{
    public class SlotReelView : MonoBehaviourExtBind
    {
        private enum SpinMode
        {
            Idle,
            Accelerating,
            Running,
            Decelerating,
            Snapping
        }

        [Header("Items")]
        [SerializeField] private RectTransform reelRoot;
        private readonly List<RectTransform> items = new List<RectTransform>();
        private readonly List<Image> itemImages = new List<Image>();
        [SerializeField] private List<Sprite> symbols = new List<Sprite>();

        [Header("Layout")]
        [SerializeField] private float itemHeight = 220f;
        [SerializeField] private float centerLineY = 0f;

        [Header("Motion")]
        [SerializeField] private float maxSpeed = 980f;
        [SerializeField] private float accelerationTime = 0.9f;
        [SerializeField] private float decelerationTime = 0.85f;
        [SerializeField] private float decelerationEndSpeed = 80f;
        [SerializeField] private float snapTime = 0.32f;
        [SerializeField] private AnimationCurve accelerationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve decelerationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private SpinMode mode = SpinMode.Idle;
        private float currentSpeed;
        private float phaseTimer;
        private float decelerationStartSpeed;
        private float snapApplied;
        private float snapTo;

        [OnAwake]
        private void AwakeThis()
        {
            if (reelRoot == null)
                reelRoot = transform as RectTransform;

            items.Clear();
            if (reelRoot != null)
            {
                for (var i = 0; i < reelRoot.childCount; i++)
                {
                    var child = reelRoot.GetChild(i) as RectTransform;
                    if (child == null)
                        continue;

                    // Skip non-item children (e.g., utility objects) that carry SlotReelView.
                    if (child.GetComponent<SlotReelView>() != null)
                        continue;

                    if (child != null)
                        items.Add(child);
                }
            }

            itemImages.Clear();
            foreach (var item in items)
            {
                if (item == null)
                {
                    itemImages.Add(null);
                    continue;
                }

                itemImages.Add(item.GetComponent<Image>());
            }

            RandomizeAllSymbols();
            mode = SpinMode.Idle;
            currentSpeed = 0f;
        }

        [Bind(SlotContracts.EventStartSpin)]
        private void OnStartSpin()
        {
            mode = SpinMode.Accelerating;
            phaseTimer = 0f;
        }

        [Bind(SlotContracts.EventStopSpin)]
        private void OnStopSpin()
        {
            if (mode == SpinMode.Idle || mode == SpinMode.Snapping)
                return;

            decelerationStartSpeed = currentSpeed;
            phaseTimer = 0f;
            mode = SpinMode.Decelerating;
        }

        [OnUpdate]
        private void UpdateThis()
        {
            var dt = Time.deltaTime;
            UpdateMotion(dt);

            if (currentSpeed > 0f)
                MoveItemsDown(currentSpeed * dt);
        }

        private void UpdateMotion(float dt)
        {
            switch (mode)
            {
                case SpinMode.Accelerating:
                    phaseTimer += dt;
                    currentSpeed = maxSpeed * accelerationCurve.Evaluate(Normalized(phaseTimer, accelerationTime));
                    if (phaseTimer >= accelerationTime)
                    {
                        currentSpeed = maxSpeed;
                        mode = SpinMode.Running;
                    }
                    break;

                case SpinMode.Running:
                    currentSpeed = maxSpeed;
                    break;

                case SpinMode.Decelerating:
                    phaseTimer += dt;
                    var t = Normalized(phaseTimer, decelerationTime);
                    currentSpeed = Mathf.Lerp(decelerationStartSpeed, decelerationEndSpeed, decelerationCurve.Evaluate(t));
                    if (phaseTimer >= decelerationTime)
                        StartSnapToCenter();
                    break;

                case SpinMode.Snapping:
                    phaseTimer += dt;
                    var s = Mathf.SmoothStep(0f, 1f, Normalized(phaseTimer, snapTime));
                    var targetApplied = Mathf.Lerp(0f, snapTo, s);
                    var delta = targetApplied - snapApplied;
                    snapApplied = targetApplied;
                    MoveItemsDown(delta);

                    if (phaseTimer >= snapTime)
                    {
                        currentSpeed = 0f;
                        mode = SpinMode.Idle;
                        Settings.Invoke(SlotContracts.EventVisualStopped);
                    }
                    break;

                case SpinMode.Idle:
                    currentSpeed = 0f;
                    break;
            }
        }

        private void MoveItemsDown(float delta)
        {
            if (Mathf.Abs(delta) <= 0.0001f)
                return;

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item == null)
                    continue;

                var pos = item.anchoredPosition;
                pos.y -= delta;
                item.anchoredPosition = pos;
            }

            RecycleLoop();
        }

        private void RecycleLoop()
        {
            // We need the current topmost item so recycled items stack correctly above it.
            var highestY = GetTopmostY();
            var bottomRecycleY = GetRecycleBottomY();

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item == null)
                    continue;

                var pos = item.anchoredPosition;
                if (pos.y < bottomRecycleY)
                {
                    pos.y = highestY + itemHeight;
                    item.anchoredPosition = pos;
                    highestY = pos.y;
                    RandomizeSymbol(i);
                }
            }
        }

        private float GetTopmostY()
        {
            var highestY = GetRecycleTopY() - itemHeight;
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item != null && item.anchoredPosition.y > highestY)
                    highestY = item.anchoredPosition.y;
            }

            return highestY;
        }

        private float GetRecycleTopY()
        {
            if (items.Count <= 0)
                return 0f;

            var halfSpan = (items.Count - 1) * 0.5f;
            return itemHeight * halfSpan;
        }

        private float GetRecycleBottomY()
        {
            return -GetRecycleTopY();
        }

        private void StartSnapToCenter()
        {
            var nearest = GetNearestItemYToCenter();
            var deltaToCenter = nearest - centerLineY;
            snapApplied = 0f;
            snapTo = deltaToCenter;
            phaseTimer = 0f;
            mode = SpinMode.Snapping;
            currentSpeed = 0f;
        }

        private float GetNearestItemYToCenter()
        {
            var nearest = 0f;
            var best = float.MaxValue;

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item == null)
                    continue;

                var y = item.anchoredPosition.y;
                var d = Mathf.Abs(y - centerLineY);
                if (d < best)
                {
                    best = d;
                    nearest = y;
                }
            }

            return nearest;
        }

        private void RandomizeAllSymbols()
        {
            for (var i = 0; i < itemImages.Count; i++)
                RandomizeSymbol(i);
        }

        private void RandomizeSymbol(int index)
        {
            if (symbols.Count == 0 || index < 0 || index >= itemImages.Count)
                return;

            var image = itemImages[index];
            if (image == null)
                return;

            image.sprite = symbols[Random.Range(0, symbols.Count)];
        }

        private static float Normalized(float value, float max)
        {
            if (max <= 0.0001f)
                return 1f;
            return Mathf.Clamp01(value / max);
        }
    }
}
