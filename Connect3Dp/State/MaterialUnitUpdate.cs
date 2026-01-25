using System;
using System.Collections.Generic;

namespace Connect3Dp.State
{
    /// <summary>
    /// Represents a partial update to a material unit.
    /// Only properties that are explicitly set will be applied to the unit.
    /// </summary>
    public class MaterialUnitUpdate(string ID)
    {
        internal string ID { get; } = ID;
        internal int? Capacity { get; private set; }
        internal string? Model { get; private set; }
        internal bool ModelSet { get; private set; }
        internal HeatingConstraints? HeatingConstraints { get; private set; }
        internal bool HeatingConstraintsSet { get; private set; }
        internal MaterialUnitFeatures? Features { get; private set; }
        internal double? HumidityPercent { get; private set; }
        internal bool HumidityPercentSet { get; private set; }
        internal double? TemperatureC { get; private set; }
        internal bool TemperatureCSet { get; private set; }
        internal PartialHeatingSettings? ActiveHeatingSettings { get; private set; }
        internal bool ActiveHeatingSettingsSet { get; private set; }

        internal Dictionary<int, Material?>? SlotsToSet { get; private set; }
        internal HashSet<int>? SlotsToClear { get; private set; }
        internal bool ClearAllSlots { get; private set; }

        internal HashSet<HeatingSchedule>? SchedulesToAdd { get; private set; }
        internal HashSet<HeatingSchedule>? SchedulesToRemove { get; private set; }
        internal bool ClearHeatingSchedule { get; private set; }

        public MaterialUnitUpdate SetCapacity(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentException("Capacity must be greater than 0", nameof(capacity));

            Capacity = capacity;
            return this;
        }

        public MaterialUnitUpdate SetHeatingConstraints(HeatingConstraints? constraints)
        {
            HeatingConstraints = constraints;
            HeatingConstraintsSet = true;
            return this;
        }

        public MaterialUnitUpdate SetModel(string? model)
        {
            Model = model;
            ModelSet = true;
            return this;
        }

        public MaterialUnitUpdate SetFeatures(MaterialUnitFeatures features)
        {
            Features = features;
            return this;
        }

        public MaterialUnitUpdate SetHumidityPercent(double? humidity)
        {
            if (humidity.HasValue && (humidity.Value < 0 || humidity.Value > 100))
                throw new ArgumentException("Humidity must be between 0 and 100", nameof(humidity));

            HumidityPercent = humidity;
            HumidityPercentSet = true;
            return this;
        }

        public MaterialUnitUpdate SetTemperatureC(double? temperature)
        {
            TemperatureC = temperature;
            TemperatureCSet = true;
            return this;
        }

        public MaterialUnitUpdate SetActiveHeatingSettings(PartialHeatingSettings? settings)
        {
            ActiveHeatingSettings = settings;
            ActiveHeatingSettingsSet = true;
            return this;
        }

        public MaterialUnitUpdate SetSlot(int slotNumber, Material material)
        {
            if (slotNumber < 0)
                throw new ArgumentException("Slot number cannot be negative", nameof(slotNumber));

            SlotsToSet ??= [];
            SlotsToSet[slotNumber] = material;
            return this;
        }

        public MaterialUnitUpdate ClearSlot(int slotNumber)
        {
            if (slotNumber < 0)
                throw new ArgumentException("Slot number cannot be negative", nameof(slotNumber));

            SlotsToClear ??= new HashSet<int>();
            SlotsToClear.Add(slotNumber);
            return this;
        }

        public MaterialUnitUpdate AddHeatingSchedule(HeatingSchedule schedule)
        {
            if (schedule == null)
                throw new ArgumentNullException(nameof(schedule));

            SchedulesToAdd ??= new HashSet<HeatingSchedule>();
            SchedulesToAdd.Add(schedule);
            return this;
        }

        public MaterialUnitUpdate RemoveHeatingSchedule(HeatingSchedule schedule)
        {
            if (schedule == null)
                throw new ArgumentNullException(nameof(schedule));

            SchedulesToRemove ??= new HashSet<HeatingSchedule>();
            SchedulesToRemove.Add(schedule);
            return this;
        }

        public MaterialUnitUpdate ClearAllHeatingSchedules()
        {
            ClearHeatingSchedule = true;
            return this;
        }
    }

    public partial class MaterialUnit
    {
        /// <summary>
        /// Applies a partial update to this material unit.
        /// Only properties that were explicitly set in the update will be modified.
        /// </summary>
        internal void ApplyUpdate(MaterialUnitUpdate update)
        {
            if (update.Capacity.HasValue)
                Capacity = update.Capacity.Value;

            if (update.ModelSet)
                Model = update.Model;

            if (update.Features.HasValue)
                Features = update.Features.Value;

            if (update.HumidityPercentSet)
                HumidityPercent = update.HumidityPercent;

            if (update.TemperatureCSet)
                TemperatureC = update.TemperatureC;

            if (update.ActiveHeatingSettingsSet)
                HeatingJob = update.ActiveHeatingSettings;

            if (update.HeatingConstraintsSet)
                HeatingConstraints = update.HeatingConstraints;

            if (update.SlotsToSet != null)
            {
                foreach (var (slotNumber, material) in update.SlotsToSet)
                {
                    if (slotNumber >= Capacity) throw new ArgumentException($"Slot number {slotNumber} exceeds capacity {Capacity}");

                    if (material.HasValue)
                    {
                        Loaded[slotNumber] = material.Value;
                    }
                    else
                    {
                        Loaded.Remove(slotNumber);
                    }

                }
            }

            if (update.SlotsToClear != null)
            {
                foreach (var slotNumber in update.SlotsToClear)
                {
                    Loaded.Remove(slotNumber);
                }
            }

            // Heating schedules
            if (update.ClearHeatingSchedule)
            {
                HeatingSchedule.Clear();
            }

            if (update.SchedulesToAdd != null)
            {
                foreach (var schedule in update.SchedulesToAdd)
                {
                    HeatingSchedule.Add(schedule);
                }
            }

            if (update.SchedulesToRemove != null)
            {
                foreach (var schedule in update.SchedulesToRemove)
                {
                    HeatingSchedule.Remove(schedule);
                }
            }
        }
    }
}