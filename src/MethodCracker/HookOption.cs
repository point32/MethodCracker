namespace MethodCracker;

[Flags]
public enum HookOption
{
    /// <summary>
    /// Replace new method with methods that is originally here or were added before.
    /// If there's already a method was marked with <see cref="HookOption.ConflictWithReplaces"/> before you add the hook,
    /// it will raise an <see cref="InvalidOperationException"/> by <see cref="HookCollection{THookType}"/>.
    /// </summary>
    Replace = SoftReplace | ConflictWithReplaces,

    SoftReplace = 0b0001,
    BeforeOrigin = 0b0010,
    AfterOrigin = 0b0100,
    ConflictWithReplaces = 0b1000
}
