using UndertaleModLib;
using UndertaleModLib.Models;

namespace GmmlHooker;

public static class UndertaleInstructionExtensions {
    public static bool Match(this UndertaleInstruction left, UndertaleInstruction right) {
        return left.Kind == right.Kind && UndertaleInstruction.GetInstructionType(left.Kind) switch {
            UndertaleInstruction.InstructionType.SingleTypeInstruction => left.MatchType(right),
            UndertaleInstruction.InstructionType.DoubleTypeInstruction => left.MatchType(right),
            UndertaleInstruction.InstructionType.ComparisonInstruction => left.MatchType(right),
            UndertaleInstruction.InstructionType.GotoInstruction => left.MatchGoto(right),
            UndertaleInstruction.InstructionType.PopInstruction => left.MatchPop(right),
            UndertaleInstruction.InstructionType.PushInstruction => left.MatchPush(right),
            UndertaleInstruction.InstructionType.CallInstruction => left.MatchCall(right),
            UndertaleInstruction.InstructionType.BreakInstruction => left.MatchBreak(right),
            _ => false
        };
    }

    private static bool MatchType(this UndertaleInstruction left, UndertaleInstruction right) =>
        left.Type1 == right.Type1 && left.Type2 == right.Type2 && left.ComparisonKind == right.ComparisonKind &&
        left.Extra == right.Extra;

    private static bool MatchGoto(this UndertaleInstruction left, UndertaleInstruction right) =>
        left.JumpOffsetPopenvExitMagic == right.JumpOffsetPopenvExitMagic &&
        (left.JumpOffsetPopenvExitMagic || left.JumpOffset == right.JumpOffset);

    private static bool MatchPop(this UndertaleInstruction left, UndertaleInstruction right) =>
        left.Type1 == right.Type1 &&
        (left.Type1 == UndertaleInstruction.DataType.Int16 && left.SwapExtra == right.SwapExtra ||
            left.Type1 != UndertaleInstruction.DataType.Int16 && left.TypeInst == right.TypeInst &&
            left.Destination.Type == right.Destination.Type && left.Destination.Target == right.Destination.Target);

    private static bool MatchPush(this UndertaleInstruction left, UndertaleInstruction right) =>
        left.Type1 == right.Type1 && left.Type1 switch {
        UndertaleInstruction.DataType.Int32 when
            left.Value is UndertaleInstruction.Reference<UndertaleFunction> leftRef &&
            right.Value is UndertaleInstruction.Reference<UndertaleFunction> rightRef => leftRef.Type ==
            rightRef.Type && leftRef.Target == rightRef.Target,
        UndertaleInstruction.DataType.Variable when
            left.Value is UndertaleInstruction.Reference<UndertaleVariable> leftRef &&
            right.Value is UndertaleInstruction.Reference<UndertaleVariable> rightRef => leftRef.Type ==
            rightRef.Type && leftRef.Target == rightRef.Target,
        UndertaleInstruction.DataType.Variable when
            left.Value is UndertaleResourceById<UndertaleString, UndertaleChunkSTRG> leftRef &&
            right.Value is UndertaleResourceById<UndertaleString, UndertaleChunkSTRG> rightRef =>
            leftRef.Resource == rightRef.Resource,
        _ => left.Value.Equals(right.Value)
    };

    private static bool MatchCall(this UndertaleInstruction left, UndertaleInstruction right) =>
        left.Type1 == right.Type1 && left.ArgumentsCount == right.ArgumentsCount &&
        left.Function.Type == right.Function.Type && left.Function.Target == right.Function.Target;

    private static bool MatchBreak(this UndertaleInstruction left, UndertaleInstruction right) =>
        left.Type1 == right.Type1 && left.Value.Equals(right.Value);
}
