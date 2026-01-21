using Connect3Dp.Plugins.OME;
using Connect3Dp.Utilities;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Connect3Dp.Connectors
{
    public abstract class MachineConnector
    {
        private static readonly Logger Logger = new Logger(nameof(MachineConnector));

        public readonly MachineState State;

        /// <summary>
        /// Tracks the changes of <see cref="State"/>, since the last <see cref="PollChanges"/>.
        /// </summary>
        private readonly TrackedMachineState PolledState;

        public event EventHandler<MachineState> OnChange;

        protected MachineConnector(string nickname, string id, string company, string model)
        {
            State = new MachineState(nickname, id, company, model);
            PolledState = new TrackedMachineState(State);
        }

        #region Connect()

        public Task Connect(CancellationToken cancellationToken = default)
        {
            return Do(async () =>
            {
                var connectOp = new RunnableOperation(
                    async (_) => await Connect_Internal(),
                    (_) => Task.FromResult(this.State.IsConnected ? CompletionStatus.Complete() : CompletionStatus.NotComplete()),
                    timeout: TimeSpan.FromSeconds(15));

                await connectOp.RunAsync();

                _ = this.PollChanges();

            }, Constants.MachineMessages.FailedToConnect);
        }

        protected abstract Task Connect_Internal(CancellationToken cancellationToken = default);

        #endregion

        #region Light Fixtures

        protected async Task SetLightFixture(string name, bool isOn)
        {
            State.EnsureHasFeature(MachineFeature.Lighting);

            var changeOp = new RunnableOperation(
                async (_) => await SetLightFixture_Internal(name, isOn),
                (_) => Task.FromResult((State.LightFixtures.ContainsKey(name) && State.LightFixtures.GetValueOrDefault(name) == isOn) ? CompletionStatus.Complete() : CompletionStatus.NotComplete()),
                timeout: TimeSpan.FromSeconds(5));

            var result = await changeOp.RunAsync();

            _ = this.PollChanges();
        }

        protected virtual Task SetLightFixture_Internal(string name, bool isOn)
        {
            throw new NotImplementedException($"{nameof(SetLightFixture_Internal)} has not been implemented on the Connector");
        }

        #endregion

        #region Air Duct

        public async Task ChangeAirDuct(MachineAirDuctMode mode)
        {
            State.EnsureHasFeature(MachineFeature.AirDuct);

            var changeOp = new RunnableOperation(
                async (_) => await ChangeAirDuctMode_Internal(mode),
                (_) => Task.FromResult(this.State.AirDuctMode == mode ? CompletionStatus.Complete() : CompletionStatus.NotComplete()),
                timeout: TimeSpan.FromSeconds(5));

            var result = await changeOp.RunAsync();

            if (result.Exception != null)
            {
                Logger.Category($"{nameof(MachineConnector)} {nameof(ChangeAirDuct)}").Error(result.Exception.ToString());
            }


            _ = this.PollChanges();
        }

        protected virtual Task ChangeAirDuctMode_Internal(MachineAirDuctMode mode)
        {
            throw new NotImplementedException($"{nameof(ChangeAirDuctMode_Internal)} has not been implemented on the Connector");
        }

        #endregion

        #region Streaming, Oven Media Engine

        /// <summary>
        /// Supplies a PULL URL for an Oven Media Engine Origin.
        /// </summary>
        /// <remarks>
        /// <see cref="MachineFeature.OME"/> Feature.
        /// </remarks>
        internal virtual bool OvenMediaEnginePullURL_Internal([NotNullWhen(true)] out string? passURL)
        {
            passURL = null;
            return false;
        }

        #endregion

        /// <summary>
        /// Once changes to state have been made, invoke this function to process events, message auto-resolves, and etc.
        /// </summary>
       
        protected Task PollChanges()
        {
            if (PolledState.Nickname.HasChanged)
                Logger.Trace("Nickname has changed");

            if (PolledState.UID.HasChanged)
                Logger.Trace("UID has changed");

            if (PolledState.Company.HasChanged)
                Logger.Trace("Company has changed");

            if (PolledState.Model.HasChanged)
                Logger.Trace("Model has changed");

            if (PolledState.IsConnected.HasChanged)
                Logger.Trace("IsConnected has changed");

            if (PolledState.Features.HasChanged)
                Logger.Trace("Features has changed");

            if (PolledState.Status.HasChanged)
                Logger.Trace("Status has changed");

            if (PolledState.CurrentJob.HasChanged)
                Logger.Trace("PrintJob has changed");

            if (PolledState.MaterialUnits.HasChanged)
                Logger.Trace("MaterialUnits has changed");

            if (PolledState.Messages.HasChanged)
                Logger.Trace("Messages has changed");

            if (PolledState.AirDuctMode.HasChanged)
                Logger.Trace("AirDuctMode has changed");


            var isDif = this.PolledState.HasChanged;

            if (PolledState.IsConnected.TryUse(markAsSeen: true, out bool isConnected))
            {
                if (isConnected)
                {
                    Logger.Info($"Machine {this.State.Nickname} ({this.State.UID}) Connected!");
                }
                else
                {
                    Logger.Warning($"Machine {this.State.Nickname} ({this.State.UID}) Disconnected!");
                }
            }

            if (PolledState.Features.TryUse(markAsSeen: true, out var updatedFeatures))
            {
                if (updatedFeatures.HasFlag(MachineFeature.OME))
                {
                    // Find which streaming plugin is available, currently, it's only OME.

                    if (AvailablePlugins.OME != null && !AvailablePlugins.OME.IsConnectorRegistered(this) && this.OvenMediaEnginePullURL_Internal(out _))
                    {
                        AvailablePlugins.OME.RegisterConnector(this);
                    }
                }
            }

            this.PolledState.ViewAll();

            if (isDif) this.OnChange?.Invoke(this, State);

            return Task.CompletedTask;
        }

        #region Runnable

        private MonoMachine Mono { get; } = new();
        public bool IsMutating => Mono.IsMutating;

        protected Task Do(Func<Task> mutateAction, MachineMessage errorMessage, [CallerMemberName] string callerName = "")
        {
            try
            {
                return Mono.Mutate(mutateAction, callerName);
            }
            catch (MachineException mEx)
            {
                State.Messages.Add(errorMessage);
                _ = this.PollChanges();
                throw;
            }
            catch (Exception ex)
            {
                var mEx = new MachineException(errorMessage, ex);
                State.Messages.Add(errorMessage);
                _ = this.PollChanges();
                throw mEx;
            }
        }

        protected async Task<T> Do<T>(Func<Task<T>> mutateAction, MachineMessage errorMessage, [CallerMemberName] string callerName = "")
        {
            try
            {
                return await Mono.Mutate(mutateAction, callerName);
            }
            catch (MachineException mEx)
            {
                State.Messages.Add(errorMessage);
                _ = this.PollChanges();
                throw;
            }
            catch (Exception ex)
            {
                var mEx = new MachineException(errorMessage, ex);
                State.Messages.Add(errorMessage);
                _ = this.PollChanges();
                throw mEx;
            }
        }

        protected Task DoUntil(Func<Task> mutateAction, Func<bool> predicate, TimeSpan timeout, MachineMessage errorMessage, [CallerMemberName] string callerName = "")
        {
            try
            {
                return Mono.MutateUntil(mutateAction, predicate, timeout, callerName);
            }
            catch (MachineException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new MachineException(errorMessage, ex);
            }
        }

        #endregion

        internal static class AvailablePlugins
        {
            public static readonly OMEPlugin? OME;

            static AvailablePlugins()
            {
                if (OMEPlugin.TryGetInstance(out var omeInstance))
                {
                    OME = omeInstance;
                }
            }
        }
    }
}
