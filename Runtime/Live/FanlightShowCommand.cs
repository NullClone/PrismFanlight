using System;

namespace PrismFanlight.Live
{
    internal readonly struct FanlightShowCommand
    {
        internal FanlightShowCommandKind Kind { get; }

        internal FanlightSetLivePatchCommand SetLivePatchCommand { get; }

        internal FanlightClearLivePatchCommand ClearLivePatchCommand { get; }

        internal FanlightTriggerCueCommand TriggerCueCommand { get; }

        internal FanlightCancelCueCommand CancelCueCommand { get; }

        internal FanlightSetSafetyPatchCommand SetSafetyPatchCommand { get; }

        internal FanlightClearSafetyPatchCommand ClearSafetyPatchCommand { get; }

        internal FanlightSelectTimeProviderCommand SelectTimeProviderCommand { get; }


        private FanlightShowCommand(
            FanlightShowCommandKind kind,
            FanlightSetLivePatchCommand setLivePatchCommand,
            FanlightClearLivePatchCommand clearLivePatchCommand,
            FanlightTriggerCueCommand triggerCueCommand,
            FanlightCancelCueCommand cancelCueCommand,
            FanlightSetSafetyPatchCommand setSafetyPatchCommand,
            FanlightClearSafetyPatchCommand clearSafetyPatchCommand,
            FanlightSelectTimeProviderCommand selectTimeProviderCommand)
        {
            Kind = kind;
            SetLivePatchCommand = setLivePatchCommand;
            ClearLivePatchCommand = clearLivePatchCommand;
            TriggerCueCommand = triggerCueCommand;
            CancelCueCommand = cancelCueCommand;
            SetSafetyPatchCommand = setSafetyPatchCommand;
            ClearSafetyPatchCommand = clearSafetyPatchCommand;
            SelectTimeProviderCommand = selectTimeProviderCommand;
        }


        internal static FanlightShowCommand From(FanlightSetLivePatchCommand command) =>
            new(FanlightShowCommandKind.SetLivePatch, command, default, default, default, default, default, default);

        internal static FanlightShowCommand From(FanlightClearLivePatchCommand command) =>
            new(FanlightShowCommandKind.ClearLivePatch, default, command, default, default, default, default, default);

        internal static FanlightShowCommand From(FanlightTriggerCueCommand command) =>
            new(FanlightShowCommandKind.TriggerCue, default, default, command, default, default, default, default);

        internal static FanlightShowCommand From(FanlightCancelCueCommand command) =>
            new(FanlightShowCommandKind.CancelCue, default, default, default, command, default, default, default);

        internal static FanlightShowCommand From(FanlightSetSafetyPatchCommand command) =>
            new(FanlightShowCommandKind.SetSafetyPatch, default, default, default, default, command, default, default);

        internal static FanlightShowCommand From(FanlightClearSafetyPatchCommand command) =>
            new(FanlightShowCommandKind.ClearSafetyPatch, default, default, default, default, default, command, default);

        internal static FanlightShowCommand From(FanlightSelectTimeProviderCommand command) =>
            new(FanlightShowCommandKind.SelectTimeProvider, default, default, default, default, default, default, command);


        internal void Validate()
        {
            switch (Kind)
            {
                case FanlightShowCommandKind.SetLivePatch:
                    _ = new FanlightSetLivePatchCommand(
                        SetLivePatchCommand.SourceId,
                        SetLivePatchCommand.Patch,
                        SetLivePatchCommand.Priority,
                        SetLivePatchCommand.TransitionSeconds);
                    break;
                case FanlightShowCommandKind.ClearLivePatch:
                    _ = new FanlightClearLivePatchCommand(
                        ClearLivePatchCommand.SourceId,
                        ClearLivePatchCommand.TransitionSeconds);
                    break;
                case FanlightShowCommandKind.TriggerCue:
                    _ = new FanlightTriggerCueCommand(TriggerCueCommand.CueId);
                    break;
                case FanlightShowCommandKind.CancelCue:
                    _ = new FanlightCancelCueCommand(CancelCueCommand.CueId);
                    break;
                case FanlightShowCommandKind.SetSafetyPatch:
                    _ = new FanlightSetSafetyPatchCommand(
                        SetSafetyPatchCommand.SourceId,
                        SetSafetyPatchCommand.Patch);
                    break;
                case FanlightShowCommandKind.ClearSafetyPatch:
                    _ = new FanlightClearSafetyPatchCommand(ClearSafetyPatchCommand.SourceId);
                    break;
                case FanlightShowCommandKind.SelectTimeProvider:
                    _ = new FanlightSelectTimeProviderCommand(SelectTimeProviderCommand.ProviderId);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(Kind));
            }
        }
    }
}
