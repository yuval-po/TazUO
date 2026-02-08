namespace ClassicUO.Ipc;

public interface ICoreToUiMessage : IIpcConnector {}

public record ShowSettingsMessage : ICoreToUiMessage;
