using System;

namespace Oath
{
    public enum MessageKind { Info, Good, Warning, Danger }

    /// <summary>Sự kiện toàn cục dùng để báo thông điệp lên HUD.</summary>
    public static class GameEvents
    {
        public static event Action<string, MessageKind> OnMessage;

        public static void Message(string text, MessageKind kind = MessageKind.Info)
        {
            OnMessage?.Invoke(text, kind);
        }

        /// <summary>Xoá mọi đăng ký sự kiện tĩnh khi vào màn mới.</summary>
        public static void ResetAll()
        {
            OnMessage = null;
            Unit.ResetEvents();
            Building.ResetEvents();
            HealthBar.ResetContainer();
        }
    }
}
