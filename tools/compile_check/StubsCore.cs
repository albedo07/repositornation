// Extra stubs for the separate Core (Dragon's Altar) pass: Unity UI, Jotunn and Valheim interfaces.
// Only types are stubbed; missing members are expected noise that also appears in the baseline.
namespace UnityEngine.UI { public class Text : UnityEngine.Component { public string text; } public class Button : UnityEngine.Component {} public class Image : UnityEngine.Component {} public class Shadow : UnityEngine.Component { public UnityEngine.Color effectColor; public UnityEngine.Vector2 effectDistance; } public class Outline : Shadow {} public class ScrollRect : UnityEngine.Component {} }
namespace Jotunn.Managers { public class GUIManager { public static GUIManager Instance; public static UnityEngine.GameObject CustomGUIFront; public static bool IsHeadless(){ return false; } public static void BlockInput(bool b){} public UnityEngine.Font AveriaSerif; public UnityEngine.Font AveriaSerifBold; } public class PrefabManager {} public class PieceManager {} }
namespace Jotunn.Configs { public class _CoreStub {} }
namespace Jotunn.Entities { public class _CoreStub {} }
public interface Hoverable {}
public interface Interactable {}
