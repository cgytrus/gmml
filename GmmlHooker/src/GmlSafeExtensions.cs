using JetBrains.Annotations;

using UndertaleModLib;
using UndertaleModLib.Models;

namespace GmmlHooker;

[PublicAPI]
public static class GmlSafeExtensions {
    public static void ReplaceGmlSafe(this UndertaleCode code, string gmlCode, UndertaleData data) {
        try { code.ReplaceGML(gmlCode, data); }
        // UndertaleModLib is trying to write profile cache but fails, we don't care
        catch(Exception ex) {
            if(ex.Message.StartsWith("Error during writing of GML code to profile", StringComparison.InvariantCulture))
                return;
            throw;
        }
    }

    public static void AppendGmlSafe(this UndertaleCode code, string gmlCode, UndertaleData data) {
        try { code.AppendGML(gmlCode, data); }
        // UndertaleModLib is trying to write profile cache but fails, we don't care
        catch(Exception ex) {
            if(ex.Message.StartsWith("Error during writing of GML code to profile", StringComparison.InvariantCulture))
                return;
            throw;
        }
    }
}
