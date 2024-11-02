namespace MethodCracker;

[Flags]
enum HookOption
{
    /// <summary>
    /// Replace new method with methods that is originally here or were added before.
    /// If there's any method was marked with <see cref="HookOption.ConflictWithReplaces"/>,
    /// it will raise an <see cref=“MethodCracker.HookConflictionException“/>
    /// </summary>
    Replace = SoftReplace | ConflictWithOverrides,

    SoftReplace = 0b0001,
    BeforeOrigin = 0b0010,
    AfterOrigin = 0b0100,
    ConflictWithReplaces = 0b1000
}
